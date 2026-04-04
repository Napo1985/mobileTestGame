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

    // ── Vertical bounce (MissileFiring ships only) ────────────────────
    float _bounceMinY;   // lower boundary — reverse here and go up
    float _bounceMaxY;   // upper boundary — reverse here and go down
    float _verticalDir = -1f;

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
            _verticalDir = -1f;

            if (_shipBehavior == ShipBehavior.MissileFiring)
            {
                // Bounce between spawn row and a minimum visible Y so the ship
                // never leaves the screen — it keeps strafing back and forth
                // and up/down until the player destroys it.
                _bounceMaxY = transform.position.y - 0.5f; // just below spawn row
                _bounceMinY = destroyBelowY + 5.5f;        // bottom of visible play area
            }

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
                // MissileFiring only — slow hover while reloading second salvo;
                // still respect vertical bounce so the ship doesn't drift off screen.
                if (transform.position.y <= _bounceMinY) _verticalDir =  1f;
                if (transform.position.y >= _bounceMaxY) _verticalDir = -1f;
                transform.position += new Vector3(0f, _verticalDir * _speed * 0.25f, 0f) * Time.deltaTime;
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
        float vertSpeed = _speed * 0.45f;
        float strafeSpeed = _speed * 0.9f;

        // Bounce vertically — flip direction at both boundaries so the ship
        // keeps oscillating inside the visible screen indefinitely.
        if (transform.position.y <= _bounceMinY) _verticalDir =  1f; // hit bottom → go up
        if (transform.position.y >= _bounceMaxY) _verticalDir = -1f; // hit top  → go down

        if (_playerTransform != null)
        {
            float dx = _playerTransform.position.x - transform.position.x;
            strafeSpeed *= Mathf.Clamp01(Mathf.Abs(dx) * 0.6f + 0.3f);
            _strafeDir = Mathf.Sign(dx);
        }

        transform.position += new Vector3(_strafeDir * strafeSpeed, _verticalDir * vertSpeed, 0f) * Time.deltaTime;
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
            dir = (_playerTransform.position - transform.position).normalized;

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
            StartCoroutine(HitFlash());
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

    IEnumerator HitFlash()
    {
        if (_sr == null) yield break;
        _sr.color = Color.white;
        yield return new WaitForSeconds(0.07f);
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
