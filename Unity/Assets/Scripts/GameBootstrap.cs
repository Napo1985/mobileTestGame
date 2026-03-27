using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Black screen, finger or mouse drags the ship, auto-fires upward.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] Camera gameplayCamera;
    [SerializeField] float fireInterval = 0.12f;
    [SerializeField] float bulletSpeed = 16f;
    [SerializeField] float shipMoveSmoothing = 18f;
    [SerializeField] int bulletDamage = 5;
    [SerializeField] int enemyHpMin = 5;
    [SerializeField] int enemyHpMax = 150;
    [SerializeField] int strongEnemyHpThreshold = 75;
    [SerializeField] float enemySpawnInterval = 0.8f;
    [SerializeField] float weakEnemySpeedMin = 2.8f;
    [SerializeField] float weakEnemySpeedMax = 4.2f;
    [SerializeField] float strongEnemySpeedMin = 2.2f;
    [SerializeField] float strongEnemySpeedMax = 3.2f;
    [SerializeField] float weakEnemyDiagonalDrift = 1.4f;

    Transform _ship;
    Camera _cam;
    Sprite _pixelSprite;
    float _fireTimer;
    float _enemySpawnTimer;
    int _score;
    Text _scoreLabel;

    void Awake()
    {
        NormalizeGameplayConfig();
        _cam = AcquireCamera();
        _cam.orthographic = true;
        _cam.orthographicSize = 8f;
        _cam.transform.position = new Vector3(0f, 0f, -10f);
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Color.black;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;

        Application.targetFrameRate = 60;

        _pixelSprite = BuildPixelSprite();
        _ship = BuildShip();
        _scoreLabel = BuildScoreLabel();
        UpdateScoreLabel();
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
        bulletDamage = Mathf.Max(1, bulletDamage);
        enemyHpMin = Mathf.Max(1, enemyHpMin);
        enemyHpMax = Mathf.Max(enemyHpMin, enemyHpMax);
        strongEnemyHpThreshold = Mathf.Clamp(strongEnemyHpThreshold, enemyHpMin, enemyHpMax);
        enemySpawnInterval = Mathf.Max(0.05f, enemySpawnInterval);
        weakEnemySpeedMin = Mathf.Max(0.1f, weakEnemySpeedMin);
        weakEnemySpeedMax = Mathf.Max(weakEnemySpeedMin, weakEnemySpeedMax);
        strongEnemySpeedMin = Mathf.Max(0.1f, strongEnemySpeedMin);
        strongEnemySpeedMax = Mathf.Max(strongEnemySpeedMin, strongEnemySpeedMax);
        weakEnemyDiagonalDrift = Mathf.Max(0f, weakEnemyDiagonalDrift);
    }

    static Sprite BuildPixelSprite()
    {
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            16f);
    }

    Transform BuildShip()
    {
        var go = new GameObject("Ship");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _pixelSprite;
        sr.color = new Color(0.4f, 0.95f, 1f);
        go.transform.localScale = new Vector3(1f, 1.35f, 1f);
        go.transform.position = new Vector3(0f, -5.5f, 0f);
        return go.transform;
    }

    void Update()
    {
        UpdateShipFromPointer();
        UpdateFiring();
        UpdateEnemySpawning();
    }

    void UpdateShipFromPointer()
    {
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                MoveShipTowardScreen(t.position);
        }
        else if (Input.GetMouseButton(0))
            MoveShipTowardScreen(Input.mousePosition);
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
        float halfW = halfH * _cam.aspect;
        const float pad = 0.6f;
        world.x = Mathf.Clamp(world.x, -halfW + pad, halfW - pad);
        world.y = Mathf.Clamp(world.y, -halfH + pad, halfH - pad);
        return world;
    }

    void UpdateFiring()
    {
        _fireTimer -= Time.deltaTime;
        if (_fireTimer > 0f)
            return;
        _fireTimer = fireInterval;
        SpawnBullet();
    }

    void SpawnBullet()
    {
        var go = new GameObject("Bullet");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _pixelSprite;
        sr.color = new Color(1f, 0.92f, 0.35f);
        go.transform.localScale = new Vector3(0.22f, 0.55f, 1f);

        var shipP = _ship.position;
        go.transform.position = shipP + Vector3.up * 0.75f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        var bullet = go.AddComponent<Bullet>();
        bullet.Configure(bulletSpeed, _cam.orthographicSize + 6f, bulletDamage);
    }

    void UpdateEnemySpawning()
    {
        _enemySpawnTimer -= Time.deltaTime;
        if (_enemySpawnTimer > 0f)
            return;

        _enemySpawnTimer = enemySpawnInterval;
        SpawnEnemy();
    }

    void SpawnEnemy()
    {
        int hp = Random.Range(enemyHpMin, enemyHpMax + 1);
        bool isStrong = hp >= strongEnemyHpThreshold;
        float speed = isStrong
            ? Random.Range(strongEnemySpeedMin, strongEnemySpeedMax)
            : Random.Range(weakEnemySpeedMin, weakEnemySpeedMax);
        float driftX = isStrong ? 0f : Random.Range(-weakEnemyDiagonalDrift, weakEnemyDiagonalDrift);

        var go = new GameObject("Enemy");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _pixelSprite;
        sr.color = isStrong ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.6f, 0.6f);

        float spawnY = _cam.orthographicSize + 1.6f;
        float halfW = _cam.orthographicSize * _cam.aspect;
        float spawnX = Random.Range(-halfW + 0.8f, halfW - 0.8f);
        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        float size = Mathf.Lerp(0.65f, 1.9f, Mathf.InverseLerp(enemyHpMin, enemyHpMax, hp));
        go.transform.localScale = new Vector3(size, size, 1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        var enemy = go.AddComponent<Enemy>();
        enemy.Configure(hp, speed, driftX, -_cam.orthographicSize - 2f, OnEnemyKilled);
    }

    void OnEnemyKilled(int maxHp)
    {
        _score += PointsForHp(maxHp);
        UpdateScoreLabel();
    }

    static int PointsForHp(int maxHp)
    {
        return Mathf.Max(1, ((maxHp - 1) / 50) + 1);
    }

    Text BuildScoreLabel()
    {
        var canvasGo = new GameObject("GameplayCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        var labelGo = new GameObject("ScoreLabel");
        labelGo.transform.SetParent(canvasGo.transform, false);
        var rect = labelGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -20f);
        rect.sizeDelta = new Vector2(360f, 64f);

        var text = labelGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.UpperLeft;
        text.fontSize = 30;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        return text;
    }

    void UpdateScoreLabel()
    {
        if (_scoreLabel != null)
            _scoreLabel.text = $"Score: {_score}";
    }
}

