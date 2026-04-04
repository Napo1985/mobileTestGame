using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Space shooter: drag to move, hold to fire (blocked while pointer is over UI — same as before).
/// Stages advance on a timer; difficulty scales per wave. Optional GameplayAudioHub on this object for SFX/music.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    const string PrefHighScore = "SpaceShooterHighScore";
    const string PrefBestWave = "SpaceShooterBestWave";

    [SerializeField] Camera gameplayCamera;
    [SerializeField] float baseFireInterval = 0.1f;
    [SerializeField] float baseBulletSpeed = 19f;
    [SerializeField] float shipMoveSmoothing = 18f;
    [SerializeField] int baseBulletDamage = 9;
    [SerializeField] float baseBulletScale = 1.05f;
    [SerializeField] int enemyHpMin = 2;
    [SerializeField] int enemyHpMax = 72;
    [SerializeField] int strongEnemyHpThreshold = 36;
    [Tooltip("Base seconds between enemy spawns at wave 1; shrinks slightly each wave (floored).")]
    [SerializeField] float enemySpawnInterval = 1.05f;
    [SerializeField] float weakEnemySpeedMin = 2.45f;
    [SerializeField] float weakEnemySpeedMax = 3.85f;
    [SerializeField] float strongEnemySpeedMin = 1.95f;
    [SerializeField] float strongEnemySpeedMax = 2.85f;
    [SerializeField] float weakEnemyDiagonalDrift = 1.15f;
    [SerializeField] int playerMaxHp = 100;
    [SerializeField] int enemyTouchDamage = 16;
    [SerializeField] float playWidthScale = 0.7f;
    [Tooltip("Keeps the ship slightly above the bottom of the play clamp for thumb clearance.")]
    [SerializeField] float shipBottomMarginWorld = 0.85f;
    [SerializeField] float elementScaleMultiplier = 1.35f;
    [SerializeField] float healthPackDropChanceWhenDamaged = 0.14f;
    [SerializeField] float healthPackHealPercentOfMax = 0.11f;
    [SerializeField] float shotModifierDropChance = 0.18f;
    [SerializeField] float shotModifierStep = 0.12f;
    [SerializeField] float pickupFallSpeed = 2.5f;
    [SerializeField] float pickupVisualScale = 1.35f;
    [Tooltip("HP subtracted from every enemy when Atom Bomb is pressed.")]
    [SerializeField] int atomBombDamage = 42;
    [SerializeField] string mainMenuSceneName = "Main";
    [SerializeField] float gameOverReturnDelaySeconds = 2f;

    [Header("Stages / waves")]
    [Tooltip("Seconds of combat per stage before intermission and next wave.")]
    [SerializeField] float stageDurationSeconds = 44f;
    [Tooltip("Pause spawning and show STAGE CLEAR between waves.")]
    [SerializeField] float waveIntermissionSeconds = 2.35f;
    [Tooltip("0 = no bosses. Every Nth wave (after wave 1), spawns one tank asteroid at wave start.")]
    [SerializeField] int bossEveryNWaves = 4;
    [SerializeField] float bossHpMultiplier = 2.15f;
    [Tooltip("Scout spawn weight at wave 1; decreases each wave toward more asteroids.")]
    [SerializeField] float scoutSpawnWeightStart = 0.72f;
    [SerializeField] float scoutSpawnWeightPerWave = 0.028f;

    [Header("Audio (optional — add GameplayAudioHub to this GameObject and assign clips)")]
    [SerializeField] GameplayAudioHub audioHub;

    [Header("Custom images (optional — empty = Skins menu or procedural)")]
    [SerializeField] float customSpritePixelsPerUnit = 100f;
    [Tooltip("If set, overrides StreamingAssets/Skins and procedural art. Absolute path, StreamingAssets path, or Resources key.")]
    [SerializeField] string playerShipImagePath = "";
    [SerializeField] string enemyShipImagePath = "";
    [SerializeField] string asteroidImagePath = "";
    [SerializeField] string bulletImagePath = "";
    [SerializeField] string backgroundImagePath = "";
    [SerializeField] string pickupHealthImagePath = "";
    [SerializeField] string pickupPositiveImagePath = "";
    [SerializeField] string pickupNegativeImagePath = "";

    Transform _ship;
    Camera _cam;
    Sprite _pixelSprite;
    Sprite _playerShipSprite;
    Sprite _enemyScoutSprite;
    Sprite[] _asteroidSprites;
    Sprite _pickupHealthSprite;
    Sprite _pickupPositiveSprite;
    Sprite _pickupNegativeSprite;
    float _fireTimer;
    float _enemySpawnTimer;
    int _score;
    int _playerHp;

    float _bulletDamageMultiplier = 1f;
    float _bulletSpeedMultiplier = 1f;
    float _fireIntervalMultiplier = 1f;
    float _bulletSizeMultiplier = 1f;

    Text _scoreText;
    Text _hpBarLabelText;
    Image _hpFillImage;
    Text _gameOverLabel;
    Button _atomBombButton;
    bool _gameOver;

    GameObject _backdropRoot;
    SpriteRenderer _mainBackdropSr;
    Text _waveText;
    Text _stageBannerText;
    int _waveNumber = 1;
    float _stageTimeRemaining;
    bool _waveIntermission;
    float _intermissionTimer;
    int _sessionHighScore;
    int _sessionBestWave;

    // ── Animation state ──────────────────────────────────────────────
    SpriteRenderer _engineGlowLeft;
    SpriteRenderer _engineGlowRight;
    float _shipTiltAngle;
    float _shipPrevX;

    // ── Screen shake ─────────────────────────────────────────────────
    Vector3 _cameraBasePos;
    float _shakeIntensity;
    float _shakeTimeRemaining;
    float _shakeDuration;

    // ── Missile sprite ───────────────────────────────────────────────
    Sprite _missileSprite;

    void Awake()
    {
        NormalizeGameplayConfig();
        _cam = AcquireCamera();
        _cam.orthographic = true;
        _cam.orthographicSize = 8f;
        _cam.transform.position = new Vector3(0f, 0f, -10f);
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0.02f, 0.025f, 0.07f, 1f);
        _cameraBasePos = _cam.transform.position;

        BuildSpaceBackdrop();

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;

        Application.targetFrameRate = 60;

        LoadGameplaySpritesAndPickups();

        _playerHp = playerMaxHp;
        _ship = BuildShip();
        _shipPrevX = _ship.position.x;
        BuildHud();
        _sessionHighScore = PlayerPrefs.GetInt(PrefHighScore, 0);
        _sessionBestWave = PlayerPrefs.GetInt(PrefBestWave, 1);
        _stageTimeRemaining = stageDurationSeconds;
        if (audioHub == null)
            audioHub = GetComponent<GameplayAudioHub>();
        audioHub?.StartGameplayMusicIfConfigured();
        RefreshStagePresentation();
        UpdateHud();
    }

    Camera AcquireCamera()
    {
        if (gameplayCamera != null)
            return gameplayCamera;

        var fromSelf = GetComponent<Camera>();
        if (fromSelf != null)
            return fromSelf;

        if (Camera.main != null)
            return Camera.main;

        var camGo = new GameObject("GameplayCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        return cam;
    }

    void NormalizeGameplayConfig()
    {
        baseBulletDamage = Mathf.Max(1, baseBulletDamage);
        baseBulletSpeed = Mathf.Max(0.1f, baseBulletSpeed);
        baseFireInterval = Mathf.Max(0.02f, baseFireInterval);
        baseBulletScale = Mathf.Max(0.2f, baseBulletScale);
        enemyHpMin = Mathf.Max(1, enemyHpMin);
        enemyHpMax = Mathf.Max(enemyHpMin, enemyHpMax);
        strongEnemyHpThreshold = Mathf.Clamp(strongEnemyHpThreshold, enemyHpMin, enemyHpMax);
        enemySpawnInterval = Mathf.Max(0.05f, enemySpawnInterval);
        weakEnemySpeedMin = Mathf.Max(0.1f, weakEnemySpeedMin);
        weakEnemySpeedMax = Mathf.Max(weakEnemySpeedMin, weakEnemySpeedMax);
        strongEnemySpeedMin = Mathf.Max(0.1f, strongEnemySpeedMin);
        strongEnemySpeedMax = Mathf.Max(strongEnemySpeedMin, strongEnemySpeedMax);
        weakEnemyDiagonalDrift = Mathf.Max(0f, weakEnemyDiagonalDrift);
        playerMaxHp = Mathf.Max(1, playerMaxHp);
        enemyTouchDamage = Mathf.Max(1, enemyTouchDamage);
        playWidthScale = Mathf.Clamp(playWidthScale, 0.2f, 1f);
        elementScaleMultiplier = Mathf.Max(1f, elementScaleMultiplier);
        healthPackDropChanceWhenDamaged = Mathf.Clamp01(healthPackDropChanceWhenDamaged);
        healthPackHealPercentOfMax = Mathf.Clamp(healthPackHealPercentOfMax, 0.01f, 1f);
        shotModifierDropChance = Mathf.Clamp01(shotModifierDropChance);
        shotModifierStep = Mathf.Clamp(shotModifierStep, 0.01f, 1f);
        pickupFallSpeed = Mathf.Max(0.1f, pickupFallSpeed);
        pickupVisualScale = Mathf.Max(0.5f, pickupVisualScale);
        atomBombDamage = Mathf.Max(1, atomBombDamage);
        gameOverReturnDelaySeconds = Mathf.Max(0f, gameOverReturnDelaySeconds);
        customSpritePixelsPerUnit = Mathf.Max(1f, customSpritePixelsPerUnit);
        stageDurationSeconds = Mathf.Max(8f, stageDurationSeconds);
        waveIntermissionSeconds = Mathf.Max(0f, waveIntermissionSeconds);
        bossEveryNWaves = Mathf.Max(0, bossEveryNWaves);
        bossHpMultiplier = Mathf.Max(1f, bossHpMultiplier);
        scoutSpawnWeightStart = Mathf.Clamp01(scoutSpawnWeightStart);
        scoutSpawnWeightPerWave = Mathf.Max(0f, scoutSpawnWeightPerWave);
        shipBottomMarginWorld = Mathf.Max(0f, shipBottomMarginWorld);
    }

    void LoadGameplaySpritesAndPickups()
    {
        float p = customSpritePixelsPerUnit;

        _playerShipSprite = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(playerShipImagePath, GameSkinSlot.Player), p);
        if (_playerShipSprite == null)
            _playerShipSprite = GameplaySprites.PlayerShip();

        _enemyScoutSprite = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(enemyShipImagePath, GameSkinSlot.EnemyShip), p);
        if (_enemyScoutSprite == null)
            _enemyScoutSprite = GameplaySprites.EnemyScoutShip();

        const int asteroidVariantCount = 12;
        _asteroidSprites = new Sprite[asteroidVariantCount];
        var asteroidCustom = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(asteroidImagePath, GameSkinSlot.Asteroid), p);
        if (asteroidCustom != null)
        {
            for (int i = 0; i < asteroidVariantCount; i++)
                _asteroidSprites[i] = asteroidCustom;
        }
        else
        {
            for (int i = 0; i < asteroidVariantCount; i++)
                _asteroidSprites[i] = GameplaySprites.Asteroid(i * 104729);
        }

        _pixelSprite = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(bulletImagePath, GameSkinSlot.Bullet), p);
        if (_pixelSprite == null)
            _pixelSprite = BuildPixelSprite();

        _pickupHealthSprite = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(pickupHealthImagePath, GameSkinSlot.PickupHealth), p);
        _pickupPositiveSprite = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(pickupPositiveImagePath, GameSkinSlot.PickupPositive), p);
        _pickupNegativeSprite = RuntimeSpriteLoader.LoadSpriteFlexible(
            GameSkinResolver.ResolvePathForLoader(pickupNegativeImagePath, GameSkinSlot.PickupNegative), p);

        _missileSprite = GameplaySprites.EnemyMissile();
    }

    static Sprite BuildPixelSprite()
    {
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            16f,
            0u,
            SpriteMeshType.Tight,
            Vector4.zero,
            true);
    }

    void BuildSpaceBackdrop()
    {
        var go = new GameObject("SpaceBackdrop");
        _backdropRoot = go;
        var sr = go.AddComponent<SpriteRenderer>();
        _mainBackdropSr = sr;
        string bgResolved = GameSkinResolver.ResolvePathForLoader(backgroundImagePath, GameSkinSlot.Background);
        Sprite backdrop = RuntimeSpriteLoader.LoadSpriteFlexible(bgResolved, 40f);
        bool loadedCustomBackground = backdrop != null;
        if (backdrop == null)
            backdrop = SpaceBackdrop.CreateSprite();
        sr.sprite = backdrop;
        SpaceBackdrop.SetupRendererForOrthoCamera(sr, _cam);
        // Parallax star tiles only when not using a successfully loaded custom background (keeps art clean).
        if (!loadedCustomBackground)
            SpaceBackdrop.AddScrollingStarLayers(go, _cam, Color.white);
    }

    Transform BuildShip()
    {
        var go = new GameObject("Ship");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _playerShipSprite;
        sr.color = new Color(0.55f, 0.92f, 1f, 1f);
        go.transform.localScale = Vector3.one * (1.15f * elementScaleMultiplier);
        go.transform.position = new Vector3(0f, -5.5f, 0f);
        SpriteCollider2DUtil.AddPolygonFromSprite(go, _playerShipSprite, false);
        go.AddComponent<PlayerShipMarker>();

        // Pulsing engine exhausts — local offsets place glows at thruster positions
        _engineGlowLeft  = BuildEngineGlowChild(go, new Vector3(-0.19f, -0.48f, -0.1f));
        _engineGlowRight = BuildEngineGlowChild(go, new Vector3( 0.19f, -0.48f, -0.1f));

        return go.transform;
    }

    SpriteRenderer BuildEngineGlowChild(GameObject parent, Vector3 localOffset)
    {
        var go = new GameObject("EngineGlow");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localOffset;
        go.transform.localScale = Vector3.one * 0.55f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GameplaySprites.EngineGlow();
        sr.sortingOrder = -1;
        sr.color = new Color(0.2f, 0.85f, 1f, 0.8f);
        return sr;
    }

    void Update()
    {
        UpdateScreenShake();

        if (!_gameOver && _playerHp > 0)
            TickStageTimer();

        if (_gameOver)
            return;
        if (_playerHp <= 0)
            return;

        // Right-click (or two-finger tap on mobile) triggers the Atom Bomb
        if (Input.GetMouseButtonDown(1))
            UseAtomBomb();

        UpdateShipFromPointer();
        UpdateShipAnimation();
        UpdateFiring();
        UpdateEnemySpawning();
    }

    // ── Ship animation ────────────────────────────────────────────────

    void UpdateShipAnimation()
    {
        if (_ship == null) return;

        // Banking tilt: rotate Z based on horizontal velocity
        float horizVel = (_ship.position.x - _shipPrevX) / Mathf.Max(Time.deltaTime, 0.001f);
        _shipPrevX = _ship.position.x;
        float targetTilt = Mathf.Clamp(-horizVel * 2.6f, -22f, 22f);
        _shipTiltAngle = Mathf.Lerp(_shipTiltAngle, targetTilt, Time.deltaTime * 9f);
        _ship.rotation = Quaternion.Euler(0f, 0f, _shipTiltAngle);

        // Engine glow pulse — fast sine gives a "firing" feel
        float pulse = 0.68f + 0.32f * Mathf.Sin(Time.time * 22f);
        Color glowColor = new Color(0.2f, 0.85f, 1f, pulse);
        if (_engineGlowLeft  != null) _engineGlowLeft.color  = glowColor;
        if (_engineGlowRight != null) _engineGlowRight.color = glowColor;
    }

    // ── Screen shake ──────────────────────────────────────────────────

    void TriggerScreenShake(float intensity, float duration)
    {
        // Take the stronger of competing shakes; don't interrupt a bigger one
        if (intensity >= _shakeIntensity || _shakeTimeRemaining <= 0f)
        {
            _shakeIntensity = intensity;
            _shakeTimeRemaining = duration;
            _shakeDuration = Mathf.Max(0.01f, duration);
        }
    }

    void UpdateScreenShake()
    {
        if (_shakeTimeRemaining <= 0f) return;

        _shakeTimeRemaining -= Time.deltaTime;
        if (_shakeTimeRemaining <= 0f)
        {
            _cam.transform.position = _cameraBasePos;
            _shakeIntensity = 0f;
            return;
        }

        float decay = _shakeTimeRemaining / _shakeDuration;
        float ox = Random.Range(-1f, 1f) * _shakeIntensity * decay;
        float oy = Random.Range(-1f, 1f) * _shakeIntensity * decay;
        _cam.transform.position = _cameraBasePos + new Vector3(ox, oy, 0f);
    }

    void UpdateShipFromPointer()
    {
        if (IsPointerOverBlockingUi())
            return;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                MoveShipTowardScreen(t.position);
        }
        else if (Input.GetMouseButton(0))
            MoveShipTowardScreen(Input.mousePosition);
    }

    static bool IsPointerHeldForGameplay()
    {
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            return t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
        }

        return Input.GetMouseButton(0);
    }

    static bool IsPointerOverBlockingUi()
    {
        var es = EventSystem.current;
        if (es == null)
            return false;

        if (Input.touchCount > 0)
            return es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return es.IsPointerOverGameObject();
    }

    void MoveShipTowardScreen(Vector2 screen)
    {
        var target = ScreenToWorldOnPlayPlane(screen);
        target.z = 0f;
        var clamped = ClampToFrustum(target);
        var p = _ship.position;
        p = Vector3.Lerp(p, clamped, 1f - Mathf.Exp(-shipMoveSmoothing * Time.deltaTime));
        p.z = 0f;
        _ship.position = p;
    }

    Vector3 ScreenToWorldOnPlayPlane(Vector2 screen)
    {
        // Ortho camera at z=-10, gameplay on z=0
        return _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
    }

    Vector3 ClampToFrustum(Vector3 world)
    {
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect * playWidthScale;
        const float pad = 0.6f;
        world.x = Mathf.Clamp(world.x, -halfW + pad, halfW - pad);
        float minY = -halfH + pad + shipBottomMarginWorld;
        world.y = Mathf.Clamp(world.y, minY, halfH - pad);
        return world;
    }

    void UpdateFiring()
    {
        if (!IsPointerHeldForGameplay() || IsPointerOverBlockingUi())
        {
            _fireTimer = 0f;
            return;
        }

        _fireTimer -= Time.deltaTime;
        if (_fireTimer > 0f)
            return;
        _fireTimer = CurrentFireInterval();
        SpawnBullet();
    }

    void SpawnBullet()
    {
        var go = new GameObject("Bullet");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _pixelSprite;
        sr.color = new Color(1f, 0.92f, 0.35f);
        float bulletScale = baseBulletScale * _bulletSizeMultiplier;
        go.transform.localScale = new Vector3(0.22f, 0.55f, 1f) * elementScaleMultiplier * bulletScale;

        var shipP = _ship.position;
        go.transform.position = shipP + Vector3.up * 0.75f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SpriteCollider2DUtil.AddPolygonFromSprite(go, _pixelSprite, true);

        var bullet = go.AddComponent<Bullet>();
        bullet.Configure(CurrentBulletSpeed(), _cam.orthographicSize + 6f, CurrentBulletDamage());

        // #region agent log
        AgentDebugLog.Write(
            "H1",
            "GameBootstrap.cs:SpawnBullet",
            "bullet_spawned",
            "{\"useFullKinematicContacts\":" + (rb.useFullKinematicContacts ? "true" : "false") + ",\"bodyType\":" +
            AgentDebugLog.J(rb.bodyType.ToString()) + "}");
        // #endregion

        GameplayVfx.SetupFastTrail(go, new Color(1f, 0.9f, 0.35f, 0.65f), 0.12f * bulletScale * elementScaleMultiplier, 0.12f);

        GameplayVfx.SpawnMuzzleFlash(shipP + Vector3.up * 0.62f, new Color(1f, 0.88f, 0.4f, 0.9f), CurrentBulletScale());
        GameplayAudioHub.Instance?.PlayShoot();
    }

    void UpdateEnemySpawning()
    {
        if (_waveIntermission)
            return;

        _enemySpawnTimer -= Time.deltaTime;
        if (_enemySpawnTimer > 0f)
            return;

        _enemySpawnTimer = GetSpawnIntervalForWave();
        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        int hp = RollEnemyHpForCurrentWave();
        bool isStrong = hp >= strongEnemyHpThreshold;
        float speed = isStrong
            ? Random.Range(strongEnemySpeedMin, strongEnemySpeedMax)
            : Random.Range(weakEnemySpeedMin, weakEnemySpeedMax);
        float driftX = isStrong ? 0f : Random.Range(-weakEnemyDiagonalDrift, weakEnemyDiagonalDrift);

        var go = new GameObject(isStrong ? "Asteroid" : "EnemyShip");
        var sr = go.AddComponent<SpriteRenderer>();
        float spin = 0f;
        if (isStrong)
        {
            int idx = (hp * 31 + Random.Range(0, 1024)) % _asteroidSprites.Length;
            sr.sprite = _asteroidSprites[idx];
            sr.color = Color.white;
            spin = Random.Range(42f, 100f) * (Random.value < 0.5f ? 1f : -1f);
        }
        else
        {
            sr.sprite = _enemyScoutSprite;
            sr.color = Color.white;
        }

        float spawnY = _cam.orthographicSize + 1.6f;
        float halfW = _cam.orthographicSize * _cam.aspect * playWidthScale;
        float spawnX = Random.Range(-halfW + 0.8f, halfW - 0.8f);
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        if (isStrong)
            GameplayVfx.SpawnAsteroidEntryPulse(new Vector3(spawnX, spawnY + 0.35f, 0f));

        float size = Mathf.Lerp(0.65f, 1.9f, Mathf.InverseLerp(enemyHpMin, enemyHpMax, hp));
        go.transform.localScale = new Vector3(size, size, 1f) * elementScaleMultiplier;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        SpriteCollider2DUtil.AddPolygonFromSprite(go, sr.sprite, true);

        var enemy = go.AddComponent<Enemy>();
        enemy.Configure(
            hp, speed, driftX, -_cam.orthographicSize - 2f,
            OnEnemyKilled, OnPlayerHitByEnemy, spin,
            isStrong ? EnemyType.Asteroid : EnemyType.Ship,
            isStrong ? null : _ship,
            isStrong ? null : (Action<Vector3>)SpawnEnemyMissile,
            isStrong ? (Action<Vector3, float>)HandleAoeExplosion : null);

        // #region agent log
        AgentDebugLog.Write(
            "H1",
            "GameBootstrap.cs:SpawnEnemy",
            "enemy_spawned",
            "{\"isStrong\":" + (isStrong ? "true" : "false") + ",\"useFullKinematicContacts\":" +
            (rb.useFullKinematicContacts ? "true" : "false") + ",\"bodyType\":" + AgentDebugLog.J(rb.bodyType.ToString()) + "}");
        // #endregion

        if (!isStrong)
            GameplayVfx.SetupFastTrail(go, new Color(1f, 0.4f, 0.45f, 0.45f), 0.22f * size * elementScaleMultiplier, 0.18f);
    }

    void OnEnemyKilled(int maxHp, Vector3 atPosition)
    {
        float boom = Mathf.Lerp(0.55f, 1.55f, Mathf.InverseLerp(enemyHpMin, enemyHpMax, maxHp));
        GameplayVfx.SpawnExplosion(atPosition, new Color(1f, 0.55f, 0.2f, 1f), boom);

        // Shake proportional to enemy size
        TriggerScreenShake(boom * 0.12f, boom * 0.3f);

        _score += PointsForHp(maxHp);
        GameplayAudioHub.Instance?.PlayEnemyDeath();
        TrySpawnDrop(atPosition);
        UpdateHud();
    }

    static int PointsForHp(int maxHp)
    {
        return Mathf.Max(1, maxHp / 12 + (maxHp >= 40 ? 3 : 0));
    }

    void BuildHud()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvasGo = new GameObject("GameplayCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var safeGo = new GameObject("SafeArea");
        safeGo.transform.SetParent(canvasGo.transform, false);
        var safeRt = safeGo.AddComponent<RectTransform>();
        safeRt.anchorMin = Vector2.zero;
        safeRt.anchorMax = Vector2.one;
        safeRt.offsetMin = Vector2.zero;
        safeRt.offsetMax = Vector2.zero;
        ApplyScreenSafeArea(safeRt);

        var hudRoot = new GameObject("HudRoot");
        hudRoot.transform.SetParent(safeGo.transform, false);
        var hudRt = hudRoot.AddComponent<RectTransform>();
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 1f);
        hudRt.anchoredPosition = new Vector2(24f, -24f);
        hudRt.sizeDelta = new Vector2(520f, 120f);

        Font hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var scoreGo = new GameObject("ScoreText");
        scoreGo.transform.SetParent(hudRoot.transform, false);
        var scoreRect = scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(1f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = Vector2.zero;
        scoreRect.sizeDelta = new Vector2(0f, 44f);

        _scoreText = scoreGo.AddComponent<Text>();
        _scoreText.font = hudFont;
        _scoreText.fontSize = 32;
        _scoreText.fontStyle = FontStyle.Bold;
        _scoreText.alignment = TextAnchor.UpperLeft;
        _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _scoreText.verticalOverflow = VerticalWrapMode.Overflow;
        _scoreText.color = new Color(0.98f, 0.98f, 1f, 1f);
        _scoreText.raycastTarget = false;
        var scoreOutline = scoreGo.AddComponent<Outline>();
        scoreOutline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        scoreOutline.effectDistance = new Vector2(2f, -2f);

        var shieldCaptionGo = new GameObject("ShieldCaption");
        shieldCaptionGo.transform.SetParent(hudRoot.transform, false);
        var capRect = shieldCaptionGo.AddComponent<RectTransform>();
        capRect.anchorMin = new Vector2(0f, 1f);
        capRect.anchorMax = new Vector2(1f, 1f);
        capRect.pivot = new Vector2(0f, 1f);
        capRect.anchoredPosition = new Vector2(0f, -48f);
        capRect.sizeDelta = new Vector2(0f, 22f);
        var capText = shieldCaptionGo.AddComponent<Text>();
        capText.font = hudFont;
        capText.fontSize = 18;
        capText.fontStyle = FontStyle.Bold;
        capText.alignment = TextAnchor.UpperLeft;
        capText.horizontalOverflow = HorizontalWrapMode.Overflow;
        capText.verticalOverflow = VerticalWrapMode.Overflow;
        capText.color = new Color(0.55f, 0.82f, 0.95f, 0.85f);
        capText.text = "SHIELD";
        capText.raycastTarget = false;

        var barOuterGo = new GameObject("HealthBarOuter");
        barOuterGo.transform.SetParent(hudRoot.transform, false);
        var barOuterRt = barOuterGo.AddComponent<RectTransform>();
        barOuterRt.anchorMin = new Vector2(0f, 1f);
        barOuterRt.anchorMax = new Vector2(0f, 1f);
        barOuterRt.pivot = new Vector2(0f, 1f);
        barOuterRt.anchoredPosition = new Vector2(0f, -72f);
        barOuterRt.sizeDelta = new Vector2(320f, 30f);
        var barFrame = barOuterGo.AddComponent<Image>();
        barFrame.color = new Color(0.08f, 0.1f, 0.16f, 0.94f);
        barFrame.raycastTarget = false;

        var barInnerGo = new GameObject("HealthBarInner");
        barInnerGo.transform.SetParent(barOuterGo.transform, false);
        var barInnerRt = barInnerGo.AddComponent<RectTransform>();
        barInnerRt.anchorMin = Vector2.zero;
        barInnerRt.anchorMax = Vector2.one;
        barInnerRt.offsetMin = new Vector2(4f, 4f);
        barInnerRt.offsetMax = new Vector2(-4f, -4f);
        var barBg = barInnerGo.AddComponent<Image>();
        barBg.color = new Color(0.04f, 0.05f, 0.09f, 1f);
        barBg.raycastTarget = false;

        var barFillGo = new GameObject("HealthBarFill");
        barFillGo.transform.SetParent(barInnerGo.transform, false);
        var barFillRt = barFillGo.AddComponent<RectTransform>();
        barFillRt.anchorMin = Vector2.zero;
        barFillRt.anchorMax = Vector2.one;
        barFillRt.offsetMin = Vector2.zero;
        barFillRt.offsetMax = Vector2.zero;
        _hpFillImage = barFillGo.AddComponent<Image>();
        _hpFillImage.sprite = BuildUiWhiteSprite();
        _hpFillImage.type = Image.Type.Filled;
        _hpFillImage.fillMethod = Image.FillMethod.Horizontal;
        _hpFillImage.fillOrigin = 0; // horizontal: left
        _hpFillImage.fillAmount = 1f;
        _hpFillImage.color = new Color(0.35f, 0.98f, 0.82f, 1f);
        _hpFillImage.raycastTarget = false;

        var shineGo = new GameObject("HealthBarShine");
        shineGo.transform.SetParent(barFillGo.transform, false);
        var shineRt = shineGo.AddComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0f, 0.5f);
        shineRt.anchorMax = new Vector2(0.45f, 1f);
        shineRt.pivot = new Vector2(0.5f, 0.5f);
        shineRt.offsetMin = new Vector2(4f, 0f);
        shineRt.offsetMax = new Vector2(-4f, -2f);
        var shine = shineGo.AddComponent<Image>();
        shine.sprite = _hpFillImage.sprite;
        shine.color = new Color(1f, 1f, 1f, 0.22f);
        shine.raycastTarget = false;

        var hpNumbersGo = new GameObject("HpOverlay");
        hpNumbersGo.transform.SetParent(barOuterGo.transform, false);
        var hpNumRt = hpNumbersGo.AddComponent<RectTransform>();
        hpNumRt.anchorMin = Vector2.zero;
        hpNumRt.anchorMax = Vector2.one;
        hpNumRt.offsetMin = Vector2.zero;
        hpNumRt.offsetMax = Vector2.zero;
        _hpBarLabelText = hpNumbersGo.AddComponent<Text>();
        _hpBarLabelText.font = hudFont;
        _hpBarLabelText.fontSize = 20;
        _hpBarLabelText.fontStyle = FontStyle.Bold;
        _hpBarLabelText.alignment = TextAnchor.MiddleCenter;
        _hpBarLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _hpBarLabelText.verticalOverflow = VerticalWrapMode.Overflow;
        _hpBarLabelText.color = Color.white;
        var hpOutline = hpNumbersGo.AddComponent<Outline>();
        hpOutline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        hpOutline.effectDistance = new Vector2(1.5f, -1.5f);
        _hpBarLabelText.raycastTarget = false;

        var waveGo = new GameObject("WaveText");
        waveGo.transform.SetParent(safeGo.transform, false);
        var waveRect = waveGo.AddComponent<RectTransform>();
        waveRect.anchorMin = new Vector2(1f, 1f);
        waveRect.anchorMax = new Vector2(1f, 1f);
        waveRect.pivot = new Vector2(1f, 1f);
        waveRect.anchoredPosition = new Vector2(-20f, -20f);
        waveRect.sizeDelta = new Vector2(340f, 80f);
        _waveText = waveGo.AddComponent<Text>();
        _waveText.font = hudFont;
        _waveText.fontSize = 28;
        _waveText.fontStyle = FontStyle.Bold;
        _waveText.alignment = TextAnchor.UpperRight;
        _waveText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _waveText.verticalOverflow = VerticalWrapMode.Overflow;
        _waveText.color = new Color(0.95f, 0.92f, 1f, 1f);
        _waveText.raycastTarget = false;
        var waveOutline = waveGo.AddComponent<Outline>();
        waveOutline.effectColor = new Color(0f, 0f, 0f, 0.72f);
        waveOutline.effectDistance = new Vector2(2f, -2f);

        var bannerGo = new GameObject("StageBanner");
        bannerGo.transform.SetParent(safeGo.transform, false);
        var bannerRect = bannerGo.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.1f, 0.42f);
        bannerRect.anchorMax = new Vector2(0.9f, 0.58f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;
        _stageBannerText = bannerGo.AddComponent<Text>();
        _stageBannerText.font = hudFont;
        _stageBannerText.fontSize = 44;
        _stageBannerText.fontStyle = FontStyle.Bold;
        _stageBannerText.alignment = TextAnchor.MiddleCenter;
        _stageBannerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _stageBannerText.verticalOverflow = VerticalWrapMode.Overflow;
        _stageBannerText.color = new Color(0.45f, 0.98f, 0.88f, 1f);
        _stageBannerText.text = string.Empty;
        _stageBannerText.raycastTarget = false;
        var bannerOutline = bannerGo.AddComponent<Outline>();
        bannerOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        bannerOutline.effectDistance = new Vector2(3f, -3f);
        bannerGo.SetActive(false);

        var gameOverGo = new GameObject("GameOverLabel");
        gameOverGo.transform.SetParent(safeGo.transform, false);
        var gameOverRect = gameOverGo.AddComponent<RectTransform>();
        gameOverRect.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverRect.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverRect.pivot = new Vector2(0.5f, 0.5f);
        gameOverRect.anchoredPosition = Vector2.zero;
        gameOverRect.sizeDelta = new Vector2(640f, 200f);

        _gameOverLabel = gameOverGo.AddComponent<Text>();
        _gameOverLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _gameOverLabel.alignment = TextAnchor.MiddleCenter;
        _gameOverLabel.fontSize = 48;
        _gameOverLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        _gameOverLabel.verticalOverflow = VerticalWrapMode.Overflow;
        _gameOverLabel.color = new Color(1f, 0.35f, 0.38f);
        _gameOverLabel.text = string.Empty;
        _gameOverLabel.raycastTarget = false;
        var gameOverOutline = gameOverGo.AddComponent<Outline>();
        gameOverOutline.effectColor = new Color(0f, 0f, 0f, 0.75f);
        gameOverOutline.effectDistance = new Vector2(3f, -3f);

        var atomBombGo = new GameObject("AtomBombButton");
        atomBombGo.transform.SetParent(safeGo.transform, false);
        var atomBombRect = atomBombGo.AddComponent<RectTransform>();
        atomBombRect.anchorMin = new Vector2(0f, 0f);
        atomBombRect.anchorMax = new Vector2(0f, 0f);
        atomBombRect.pivot = new Vector2(0f, 0f);
        atomBombRect.anchoredPosition = new Vector2(16f, 16f);
        atomBombRect.sizeDelta = new Vector2(240f, 70f);

        var bombImage = atomBombGo.AddComponent<Image>();
        bombImage.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);
        _atomBombButton = atomBombGo.AddComponent<Button>();
        _atomBombButton.targetGraphic = bombImage;
        _atomBombButton.onClick.AddListener(() => GameplayAudioHub.Instance?.PlayUiClick());
        _atomBombButton.onClick.AddListener(UseAtomBomb);

        var buttonTextGo = new GameObject("Text");
        buttonTextGo.transform.SetParent(atomBombGo.transform, false);
        var buttonTextRect = buttonTextGo.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        var buttonText = buttonTextGo.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.text = "Atom Bomb";
    }

    static Sprite BuildUiWhiteSprite()
    {
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    void UpdateHud()
    {
        if (_scoreText != null)
        {
            int storedBest = PlayerPrefs.GetInt(PrefHighScore, 0);
            _scoreText.text = $"SCORE  {_score}    BEST  {storedBest}";
        }

        if (_waveText != null)
        {
            if (_waveIntermission)
                _waveText.text = "BREAK";
            else
                _waveText.text = $"WAVE {_waveNumber}  ·  {Mathf.CeilToInt(_stageTimeRemaining)}s";
        }

        if (_hpBarLabelText != null)
            _hpBarLabelText.text = $"{_playerHp} / {playerMaxHp}";

        if (_hpFillImage != null)
        {
            float t = playerMaxHp > 0 ? Mathf.Clamp01((float)_playerHp / playerMaxHp) : 0f;
            _hpFillImage.fillAmount = t;
            Color full = new Color(0.32f, 0.96f, 0.78f, 1f);
            Color mid = new Color(0.98f, 0.82f, 0.28f, 1f);
            Color low = new Color(0.96f, 0.28f, 0.38f, 1f);
            _hpFillImage.color = t > 0.55f
                ? Color.Lerp(mid, full, Mathf.InverseLerp(0.55f, 1f, t))
                : Color.Lerp(low, mid, t > 0.001f ? Mathf.InverseLerp(0f, 0.55f, t) : 0f);
        }
    }

    void OnPlayerHitByEnemy()
    {
        if (_playerHp <= 0)
            return;

        ApplyDamageToPlayer(enemyTouchDamage);
        TriggerScreenShake(0.22f, 0.3f);
    }

    void ApplyDamageToPlayer(int damage)
    {
        _playerHp = Mathf.Max(0, _playerHp - Mathf.Max(0, damage));
        if (damage > 0)
            GameplayAudioHub.Instance?.PlayPlayerHurt();
        UpdateHud();
        if (_playerHp > 0)
            return;

        TriggerGameOver();
    }

    void TriggerGameOver()
    {
        if (_gameOver)
            return;
        _gameOver = true;

        GameplayAudioHub.Instance?.StopMusic();
        GameplayAudioHub.Instance?.PlayGameOver();

        int prevBest = PlayerPrefs.GetInt(PrefHighScore, 0);
        if (_score > prevBest)
            PlayerPrefs.SetInt(PrefHighScore, _score);
        int prevWave = PlayerPrefs.GetInt(PrefBestWave, 1);
        PlayerPrefs.SetInt(PrefBestWave, Mathf.Max(prevWave, _waveNumber));
        PlayerPrefs.Save();

        if (_ship != null)
            _ship.gameObject.SetActive(false);
        if (_gameOverLabel != null)
        {
            int best = PlayerPrefs.GetInt(PrefHighScore, 0);
            int bestW = PlayerPrefs.GetInt(PrefBestWave, 1);
            _gameOverLabel.text = $"GAME OVER\nSCORE {_score}\nBEST {best}   WAVE {bestW}";
        }
        if (_atomBombButton != null)
            _atomBombButton.interactable = false;

        StopAllCoroutines();
        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    IEnumerator ReturnToMainMenuAfterDelay()
    {
        if (gameOverReturnDelaySeconds > 0f)
            yield return new WaitForSeconds(gameOverReturnDelaySeconds);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void UseAtomBomb()
    {
        if (_playerHp <= 0)
            return;

        var enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Vector3 p = enemies[i].transform.position;
            GameplayVfx.SpawnExplosion(p, new Color(0.7f, 0.35f, 1f, 1f), 0.9f);
            enemies[i].ApplyDamage(atomBombDamage);
        }

        // Big camera punch for Atom Bomb
        if (enemies.Length > 0)
            TriggerScreenShake(0.45f, 0.6f);
    }

    // ── Enemy missile spawning ────────────────────────────────────────

    void SpawnEnemyMissile(Vector3 fromPosition)
    {
        if (_ship == null || _playerHp <= 0 || _gameOver)
            return;

        Vector3 aim = _ship.position;
        GameplayVfx.SpawnMissileTelegraph(fromPosition, aim, 0.26f);

        var go = new GameObject("EnemyMissile");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _missileSprite;
        sr.color = Color.white;
        float s = 0.65f * elementScaleMultiplier;
        go.transform.position = fromPosition;
        go.transform.localScale = new Vector3(s, s, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SpriteCollider2DUtil.AddPolygonFromSprite(go, sr.sprite, true);
        GameplayVfx.SetupFastTrail(go, new Color(1f, 0.38f, 0.12f, 0.85f), 0.07f * s, 0.2f);

        var missile = go.AddComponent<EnemyMissile>();
        missile.Configure(
            _ship,
            8f,
            Mathf.Max(1, enemyTouchDamage / 3),
            -_cam.orthographicSize - 3f,
            _cam.orthographicSize + 3f,
            OnMissileHitPlayer);
    }

    void OnMissileHitPlayer(int damage)
    {
        if (_playerHp <= 0) return;
        ApplyDamageToPlayer(damage);
        TriggerScreenShake(0.14f, 0.22f);
    }

    // ── Asteroid AOE blast ────────────────────────────────────────────

    void HandleAoeExplosion(Vector3 center, float radius)
    {
        int aoeDamage = Mathf.Max(1, Mathf.RoundToInt(atomBombDamage * 0.75f));

        // Chain damage to all enemies within radius
        var enemies = FindObjectsOfType<Enemy>();
        foreach (var e in enemies)
        {
            if (Vector3.Distance(e.transform.position, center) <= radius)
                e.ApplyDamage(aoeDamage);
        }

        // Player takes damage if inside inner 60 % of blast radius
        if (_ship != null && _playerHp > 0)
        {
            float playerDist = Vector3.Distance(_ship.position, center);
            float innerRadius = radius * 0.6f;
            if (playerDist <= innerRadius)
            {
                float fraction = 1f - Mathf.Clamp01(playerDist / innerRadius);
                ApplyDamageToPlayer(Mathf.RoundToInt(aoeDamage * fraction));
            }
        }

        // Dedicated shockwave ring (the normal explosion from OnEnemyKilled
        // already fires; this adds the larger AOE ring on top)
        GameplayVfx.SpawnShockwave(center, radius, 0.55f);
        TriggerScreenShake(Mathf.Clamp(0.22f * radius, 0.15f, 0.5f), 0.5f);
    }

    void TrySpawnDrop(Vector3 atPosition)
    {
        if (_playerHp < playerMaxHp && Random.value <= healthPackDropChanceWhenDamaged)
        {
            SpawnPowerUp(atPosition, PowerUpType.HealthPack, healthPackHealPercentOfMax);
            return;
        }

        if (Random.value > shotModifierDropChance)
            return;

        var type = RandomShotModifierType();
        float sign = Random.value < 0.5f ? -1f : 1f;
        SpawnPowerUp(atPosition, type, shotModifierStep * sign);
    }

    PowerUpType RandomShotModifierType()
    {
        int pick = Random.Range(0, 4);
        if (pick == 0) return PowerUpType.ShotDamageMod;
        if (pick == 1) return PowerUpType.ShotSpeedMod;
        if (pick == 2) return PowerUpType.ShotRateMod;
        return PowerUpType.ShotSizeMod;
    }

    void SpawnPowerUp(Vector3 atPosition, PowerUpType type, float value)
    {
        var go = new GameObject(type.ToString());
        var sr = go.AddComponent<SpriteRenderer>();
        Sprite pickupSprite = PickupSpriteFor(type, value);
        sr.sprite = pickupSprite;
        sr.color = pickupSprite == _pixelSprite ? ColorForPowerUp(type, value) : Color.white;
        go.transform.position = atPosition;
        float s = 1.08f * pickupVisualScale;
        go.transform.localScale = new Vector3(s, s, 1f) * elementScaleMultiplier;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        SpriteCollider2DUtil.AddPolygonFromSprite(go, pickupSprite, true);

        var pickup = go.AddComponent<PowerUpPickup>();
        pickup.Configure(type, value, pickupFallSpeed, -_cam.orthographicSize - 2f, OnPowerUpCollected);
    }

    Sprite PickupSpriteFor(PowerUpType type, float value)
    {
        if (type == PowerUpType.HealthPack && _pickupHealthSprite != null)
            return _pickupHealthSprite;
        if (type != PowerUpType.HealthPack)
        {
            if (value >= 0f && _pickupPositiveSprite != null)
                return _pickupPositiveSprite;
            if (value < 0f && _pickupNegativeSprite != null)
                return _pickupNegativeSprite;
        }

        return _pixelSprite;
    }

    static Color ColorForPowerUp(PowerUpType type, float value)
    {
        if (type == PowerUpType.HealthPack)
            return new Color(0.35f, 1f, 0.45f);
        if (value >= 0f)
            return new Color(0.45f, 0.85f, 1f);
        return new Color(0.95f, 0.2f, 0.22f, 1f);
    }

    void OnPowerUpCollected(PowerUpType type, float value)
    {
        if (_playerHp <= 0)
            return;

        if (type == PowerUpType.HealthPack)
        {
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(playerMaxHp * value));
            _playerHp = Mathf.Min(playerMaxHp, _playerHp + healAmount);
        }
        else if (type == PowerUpType.ShotDamageMod)
        {
            _bulletDamageMultiplier = Mathf.Clamp(_bulletDamageMultiplier + value, 0.5f, 3f);
        }
        else if (type == PowerUpType.ShotSpeedMod)
        {
            _bulletSpeedMultiplier = Mathf.Clamp(_bulletSpeedMultiplier + value, 0.5f, 3f);
        }
        else if (type == PowerUpType.ShotRateMod)
        {
            _fireIntervalMultiplier = Mathf.Clamp(_fireIntervalMultiplier - value, 0.4f, 2.5f);
        }
        else if (type == PowerUpType.ShotSizeMod)
        {
            _bulletSizeMultiplier = Mathf.Clamp(_bulletSizeMultiplier + value, 0.5f, 3f);
        }

        UpdateHud();
        GameplayAudioHub.Instance?.PlayPickup();
    }

    void ApplyScreenSafeArea(RectTransform safeArea)
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0)
            return;
        Rect r = Screen.safeArea;
        Vector2 min = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
        Vector2 max = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
        safeArea.anchorMin = min;
        safeArea.anchorMax = max;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
    }

    void TickStageTimer()
    {
        if (_waveIntermission)
        {
            _intermissionTimer -= Time.deltaTime;
            UpdateHud();
            if (_intermissionTimer <= 0f)
                EndWaveIntermission();
            return;
        }

        _stageTimeRemaining -= Time.deltaTime;
        if (_stageTimeRemaining <= 0f)
            BeginWaveIntermission();
        else
            UpdateHud();
    }

    void BeginWaveIntermission()
    {
        if (_waveIntermission)
            return;
        _waveIntermission = true;
        _intermissionTimer = Mathf.Max(0.05f, waveIntermissionSeconds);

        int prevBestWave = PlayerPrefs.GetInt(PrefBestWave, 1);
        PlayerPrefs.SetInt(PrefBestWave, Mathf.Max(prevBestWave, _waveNumber + 1));
        int prevHi = PlayerPrefs.GetInt(PrefHighScore, 0);
        if (_score > prevHi)
            PlayerPrefs.SetInt(PrefHighScore, _score);
        PlayerPrefs.Save();

        if (_stageBannerText != null)
        {
            _stageBannerText.gameObject.SetActive(true);
            _stageBannerText.text = $"STAGE {_waveNumber} CLEAR";
        }

        GameplayAudioHub.Instance?.PlayWaveClear();
        UpdateHud();
    }

    void EndWaveIntermission()
    {
        _waveIntermission = false;
        if (_stageBannerText != null)
        {
            _stageBannerText.gameObject.SetActive(false);
            _stageBannerText.text = string.Empty;
        }

        _waveNumber++;
        _stageTimeRemaining = stageDurationSeconds;
        RefreshStagePresentation();
        TrySpawnBossForNewWave();
        UpdateHud();
    }

    void RefreshStagePresentation()
    {
        float u = Mathf.Clamp01((_waveNumber - 1) / 10f);
        var deep = Color.Lerp(new Color(0.02f, 0.025f, 0.08f, 1f), new Color(0.045f, 0.02f, 0.11f, 1f), u);
        _cam.backgroundColor = deep;

        if (_mainBackdropSr != null)
        {
            var mul = Color.Lerp(Color.white, new Color(0.88f, 0.92f, 1.05f, 1f), u * 0.4f);
            _mainBackdropSr.color = mul;
        }
    }

    float GetSpawnIntervalForWave()
    {
        float w = Mathf.Max(0, _waveNumber - 1);
        return Mathf.Max(0.36f, enemySpawnInterval - w * 0.034f);
    }

    int RollEnemyHpForCurrentWave()
    {
        float w = Mathf.Max(0, _waveNumber - 1);
        int cap = Mathf.RoundToInt(Mathf.Min(enemyHpMax, enemyHpMin + 4 + w * 5));
        int hi = Mathf.Max(enemyHpMin, cap);

        float scoutChance = Mathf.Clamp01(scoutSpawnWeightStart - w * scoutSpawnWeightPerWave);
        bool scout = strongEnemyHpThreshold > enemyHpMin && Random.value < scoutChance;

        if (scout)
        {
            int upper = Mathf.Max(enemyHpMin, strongEnemyHpThreshold - 1);
            return Random.Range(enemyHpMin, upper + 1);
        }

        int low = Mathf.Max(enemyHpMin, strongEnemyHpThreshold);
        return Random.Range(low, hi + 1);
    }

    void TrySpawnBossForNewWave()
    {
        if (bossEveryNWaves <= 0)
            return;
        if (_waveNumber <= 1)
            return;
        if (_waveNumber % bossEveryNWaves != 0)
            return;

        SpawnBossAsteroid();
    }

    void SpawnBossAsteroid()
    {
        int hp = Mathf.RoundToInt(Mathf.Lerp(strongEnemyHpThreshold, enemyHpMax, 0.82f) * bossHpMultiplier);
        hp = Mathf.Clamp(hp, strongEnemyHpThreshold + 1, enemyHpMax * 3);

        float speed = strongEnemySpeedMin * 0.85f;
        float driftX = 0f;

        var go = new GameObject("BossAsteroid");
        var sr = go.AddComponent<SpriteRenderer>();
        int idx = (hp * 17 + _waveNumber * 31) % _asteroidSprites.Length;
        sr.sprite = _asteroidSprites[idx];
        sr.color = new Color(1f, 0.82f, 0.95f, 1f);

        float spawnY = _cam.orthographicSize + 1.9f;
        float halfW = _cam.orthographicSize * _cam.aspect * playWidthScale;
        float spawnX = Random.Range(-halfW * 0.35f, halfW * 0.35f);
        go.transform.position = new Vector3(spawnX, spawnY, 0f);
        GameplayVfx.SpawnAsteroidEntryPulse(new Vector3(spawnX, spawnY + 0.5f, 0f), 3.4f, 0.45f);

        float size = 2.15f * elementScaleMultiplier;
        go.transform.localScale = new Vector3(size, size, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        SpriteCollider2DUtil.AddPolygonFromSprite(go, sr.sprite, true);

        var enemy = go.AddComponent<Enemy>();
        enemy.Configure(
            hp, speed, driftX, -_cam.orthographicSize - 2f,
            OnEnemyKilled, OnPlayerHitByEnemy, Random.Range(28f, 52f) * (Random.value < 0.5f ? 1f : -1f),
            EnemyType.Asteroid,
            null,
            null,
            (Action<Vector3, float>)HandleAoeExplosion);
    }

    int CurrentBulletDamage()
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseBulletDamage * _bulletDamageMultiplier));
    }

    float CurrentBulletSpeed()
    {
        return baseBulletSpeed * _bulletSpeedMultiplier;
    }

    float CurrentFireInterval()
    {
        return baseFireInterval * _fireIntervalMultiplier;
    }

    float CurrentBulletScale()
    {
        return baseBulletScale * _bulletSizeMultiplier;
    }

}

public class PlayerShipMarker : MonoBehaviour
{
}

