using System;
using UnityEngine;

/// <summary>
/// Straight-line missile fired by enemy ships.
/// Direction is locked at Configure time toward the player's position at that moment —
/// no homing after launch, so the player can dodge by moving.
/// Requires Rigidbody2D (Kinematic) and trigger PolygonCollider2D added by the spawner.
/// </summary>
public class EnemyMissile : MonoBehaviour
{
    float _speed;
    int _damage;
    float _destroyBelowY;
    float _destroyAboveY;
    Action<int> _onHitPlayer;
    Vector3 _velocity;

    public void Configure(
        Transform target,
        float speed,
        int damage,
        float destroyBelowY,
        float destroyAboveY,
        Action<int> onHitPlayer)
    {
        _speed = speed;
        _damage = damage;
        _destroyBelowY = destroyBelowY;
        _destroyAboveY = destroyAboveY;
        _onHitPlayer = onHitPlayer;

        // Snapshot direction to player at the moment of firing — never updated again.
        _velocity = target != null
            ? (target.position - transform.position).normalized * _speed
            : Vector3.down * _speed;

        AlignToVelocity();
    }

    void Update()
    {
        transform.position += _velocity * Time.deltaTime;

        float y = transform.position.y;
        if (y < _destroyBelowY || y > _destroyAboveY)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerShipMarker>() == null)
            return;

        _onHitPlayer?.Invoke(_damage);
        Destroy(gameObject);
    }

    /// <summary>Rotates transform so local +Y aligns with the travel direction.</summary>
    void AlignToVelocity()
    {
        if (_velocity.sqrMagnitude < 0.0001f)
            return;
        float angle = Mathf.Atan2(-_velocity.x, _velocity.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
