using System;
using System.Collections;
using UnityEngine;

public enum EnemyType { Ship, Asteroid }

/// <summary>
/// Ship behavior is locked at spawn: either MissileFiring or SuicideRunner — never both.
/// Asteroids fall straight down and trigger AOE on death.
/// </summary>
public class Enemy : MonoBehaviour
{
    // ── Core config ────────────────────────────────────────────────────
    int _currentHp;
    int _maxHp;
    float _speed;
    float _driftX;
    float _destroyBelowY;
    float _spinDegreesPerSecond;
    Action<int, Vector3> _onKilled;
    Action _onHitPlayer;
    bool _dead;

    // ── AI config ─────────────────────────────────────────────────────
    EnemyType _enemyType;
    Transform _playerTransform;
    Action<Vector3> _spawnMissileAt;
    Action<Vector3, float> _onAoeDeath;

    // ── Ship personality (fixed at spawn) ─────────────────────────────
    enum ShipBehavior { MissileFiring, SuicideRunner }
    ShipBehavior _shipBehavior;

    // ── AI runtime ────────────────────────────────────────────────────
    enum AiState { Descend, Strafe, FireMissile, SuicideRun }
    AiState _aiState = AiState.Descend;
    float _aiTimer;
    float _strafeDir = 1f;
    float _suicideSpeed;

    // ── Animation ─────────────────────────────────────────────────────
    SpriteRenderer _sr;
    Color _originalColor;

    // ─────────────────────────────────────────────────────────────────

    public void Configure(
        int maxHp,
        float speed,
        float driftX,
        float destroyBelowY,
        Action<int, Vector3> onKilled,
        Action onHitPlayer,
        float spinDegreesPerSecond = 0f,
        EnemyType enemyType = EnemyType.Asteroid,
        Transform playerTransform = null,
        Action<Vector3> spawnMissileAt = null,
        Action<Vector3, float> onAoeDeath = null)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _speed = speed;
        _driftX = driftX;
        _destroyBelowY = destroyBelowY;
        _onKilled = onKilled;
        _onHitPlayer = onHitPlayer;
        _spinDegreesPerSecond = spinDegreesPerSecond;
        _enemyType = enemyType;
        _playerTransform = playerTransform;
        _spawnMissileAt = spawnMissileAt;
        _onAoeDeath = onAoeDeath;

        _sr = GetComponent<SpriteRenderer>();
        _originalColor = _sr != null ? _sr.color : Color.white;

        if (_enemyType == EnemyType.Asteroid && _sr != null)
        {
            // HP tier read: cooler dim rocks → brighter/violet elites (chunky = more dangerous).
            float t = Mathf.Clamp01(Mathf.InverseLerp(6f, 88f, _maxHp));
            _sr.color = Color.Lerp(new Color(0.92f, 0.94f, 1f, 1f), new Color(0.72f, 0.58f, 1f, 1f), t);
            _originalColor = _sr.color;
        }

        if (_enemyType == EnemyType.Ship)
        {
            // Lock behavior type at spawn — ships never change personality mid-life
            _shipBehavior = UnityEngine.Random.value < 0.5f
                ? ShipBehavior.MissileFiring
                : ShipBehavior.SuicideRunner;

            _aiState = AiState.Descend;
            // SuicideRunners dive sooner; missile ships descend longer before engaging
            _aiTimer = _shipBehavior == ShipBehavior.SuicideRunner
                ? UnityEngine.Random.Range(0.5f, 1.2f)
                : UnityEngine.Random.Range(1.0f, 2.0f);

            _strafeDir = UnityEngine.Random.value < 0.5f ? 1f : -1f;

            // Tint to telegraph type: orange-red for suicide, default for missile
            if (_sr != null && _shipBehavior == ShipBehavior.SuicideRunner)
            {
                _sr.color = new Color(1f, 0.55f, 0.25f, 1f);
                _originalColor = _sr.color;
            }
        }

        // #region agent log
        AgentDebugLog.Write(
            "H3",
            "Enemy.cs:Configure",
            "configured",
            "{\"maxHp\":" + _maxHp +
            ",\"speed\":" + speed.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"destroyBelowY\":" + destroyBelowY.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"spin\":" + spinDegreesPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"enemyType\":\"" + enemyType + "\"" +
            (enemyType == EnemyType.Ship ? ",\"behavior\":\"" + _shipBehavior + "\"" : "") +
            "}");
        // #endregion
    }

    void Update()
    {
        if (_spinDegreesPerSecond != 0f)
            transform.Rotate(0f, 0f, _spinDegreesPerSecond * Time.deltaTime);

        if (_enemyType == EnemyType.Asteroid)
            UpdateAsteroid();
        else
            UpdateShipAI();

        if (transform.position.y < _destroyBelowY)
            Destroy(gameObject);
    }

    // ── Asteroid: dumb fall ──────────────────────────────────────────

    void UpdateAsteroid()
    {
        transform.position += new Vector3(_driftX, -_speed, 0f) * Time.deltaTime;
    }

    // ── Ship AI state machine ────────────────────────────────────────

    void UpdateShipAI()
    {
        _aiTimer -= Time.deltaTime;

        switch (_aiState)
        {
            case AiState.Descend:
                transform.position += new Vector3(_driftX, -_speed, 0f) * Time.deltaTime;
                if (_aiTimer <= 0f)
                {
                    // Behavior is fixed: route directly to the ship's one and only role
                    if (_shipBehavior == ShipBehavior.SuicideRunner)
                        TransitionToSuicideRun();
                    else
                        TransitionToStrafe();
                }
                break;

            case AiState.Strafe:
                // MissileFiring only
                ExecuteStrafe();
                if (_aiTimer <= 0f)
                    BeginFireSequence();
                break;

            case AiState.FireMissile:
                // MissileFiring only — slow descent while reloading (no upward motion).
                transform.position += new Vector3(0f, -_speed * 0.22f, 0f) * Time.deltaTime;
                if (_aiTimer <= 0f)
                {
                    _spawnMissileAt?.Invoke(transform.position);
                    TransitionToStrafe();
                }
                break;

            case AiState.SuicideRun:
                // SuicideRunner only
                ExecuteSuicideRun();
                break;
        }
    }

    void TransitionToStrafe()
    {
        _aiState = AiState.Strafe;
        _aiTimer = UnityEngine.Random.Range(1.3f, 2.6f);
        if (_playerTransform != null)
            _strafeDir = _playerTransform.position.x < transform.position.x ? -1f : 1f;
    }

    void BeginFireSequence()
    {
        _aiState = AiState.FireMissile;
        _aiTimer = 0.6f;
        _spawnMissileAt?.Invoke(transform.position); // first missile
    }

    void ExecuteStrafe()
    {
        float downSpeed = _speed * 0.38f;
        float strafeSpeed = _speed * 0.9f;

        if (_playerTransform != null)
        {
            float dx = _playerTransform.position.x - transform.position.x;
            strafeSpeed *= Mathf.Clamp01(Mathf.Abs(dx) * 0.6f + 0.3f);
            _strafeDir = Mathf.Sign(dx);
        }

        transform.position += new Vector3(_strafeDir * strafeSpeed, -downSpeed, 0f) * Time.deltaTime;
    }

    void TransitionToSuicideRun()
    {
        _aiState = AiState.SuicideRun;
        _suicideSpeed = _speed * 2.0f;
        _aiTimer = 999f;
    }

    void ExecuteSuicideRun()
    {
        Vector3 dir = Vector3.down;
        if (_playerTransform != null)
        {
            Vector3 raw = _playerTransform.position - transform.position;
            raw.y = Mathf.Min(0f, raw.y);
            if (raw.sqrMagnitude < 0.0004f)
                dir = Vector3.down;
            else
                dir = raw.normalized;
        }

        transform.position += dir * _suicideSpeed * Time.deltaTime;
        _suicideSpeed = Mathf.Min(_suicideSpeed + 5f * Time.deltaTime, _speed * 4.5f);

        // Lean sprite to face dive direction
        float angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.Euler(0f, 0f, angle),
            Time.deltaTime * 12f);
    }

    // ── Damage & death ───────────────────────────────────────────────

    public void ApplyDamage(int damage)
    {
        if (_dead)
            return;

        int before = _currentHp;
        _currentHp -= Mathf.Max(0, damage);

        // #region agent log
        AgentDebugLog.Write(
            "H4",
            "Enemy.cs:ApplyDamage",
            "damage",
            "{\"damage\":" + damage + ",\"hpBefore\":" + before + ",\"hpAfter\":" + _currentHp + "}");
        // #endregion

        if (_currentHp > 0)
        {
            float chunky = Mathf.Clamp01(Mathf.InverseLerp(12f, 95f, _maxHp));
            GameplayVfx.SpawnHitSpark(transform.position, 0.85f + chunky * 0.75f);
            GameplayAudioHub.Instance?.PlayHitEnemy();
            StartCoroutine(HitFlash(chunky));
            return;
        }

        _dead = true;

        if (_enemyType == EnemyType.Asteroid && _onAoeDeath != null)
        {
            float aoeRadius = Mathf.Lerp(1.5f, 4.5f,
                Mathf.InverseLerp(1f, 150f, _maxHp));
            _onAoeDeath.Invoke(transform.position, aoeRadius);
        }

        _onKilled?.Invoke(_maxHp, transform.position);
        Destroy(gameObject);
    }

    IEnumerator HitFlash(float chunkyScale)
    {
        if (_sr == null) yield break;
        _sr.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.35f, 1f), chunkyScale * 0.35f);
        float dur = 0.06f + chunkyScale * 0.09f;
        yield return new WaitForSeconds(dur);
        if (_sr != null)
            _sr.color = _originalColor;
    }

    // ── Player collision ─────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        // #region agent log
        bool isPlayer = other.GetComponent<PlayerShipMarker>() != null;
        AgentDebugLog.Write(
            "H2",
            "Enemy.cs:OnTriggerEnter2D",
            "trigger_enter",
            "{\"other\":" + AgentDebugLog.J(other != null ? other.name : "null") +
            ",\"isPlayer\":" + (isPlayer ? "true" : "false") + "}");
        // #endregion

        if (other.GetComponent<PlayerShipMarker>() == null)
            return;

        _onHitPlayer?.Invoke();
        Destroy(gameObject);
    }
}
