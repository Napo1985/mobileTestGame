using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    int _currentHp;
    int _maxHp;
    float _speed;
    float _driftX;
    float _destroyBelowY;
    float _spinDegreesPerSecond;
    Action<int, Vector3> _onKilled;
    Action _onHitPlayer;

    public void Configure(
        int maxHp,
        float speed,
        float driftX,
        float destroyBelowY,
        Action<int, Vector3> onKilled,
        Action onHitPlayer,
        float spinDegreesPerSecond = 0f)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _speed = speed;
        _driftX = driftX;
        _destroyBelowY = destroyBelowY;
        _onKilled = onKilled;
        _onHitPlayer = onHitPlayer;
        _spinDegreesPerSecond = spinDegreesPerSecond;
    }

    void Update()
    {
        if (_spinDegreesPerSecond != 0f)
            transform.Rotate(0f, 0f, _spinDegreesPerSecond * Time.deltaTime);

        var move = new Vector3(_driftX, -_speed, 0f) * Time.deltaTime;
        transform.position += move;
        if (transform.position.y < _destroyBelowY)
            Destroy(gameObject);
    }

    public void ApplyDamage(int damage)
    {
        _currentHp -= Mathf.Max(0, damage);
        if (_currentHp > 0)
            return;

        _onKilled?.Invoke(_maxHp, transform.position);
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerShipMarker>() == null)
            return;

        _onHitPlayer?.Invoke();
        Destroy(gameObject);
    }
}
