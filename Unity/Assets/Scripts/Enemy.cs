using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    int _currentHp;
    int _maxHp;
    float _speed;
    float _driftX;
    float _destroyBelowY;
    Action<int> _onKilled;

    public void Configure(int maxHp, float speed, float driftX, float destroyBelowY, Action<int> onKilled)
    {
        _maxHp = Mathf.Max(1, maxHp);
        _currentHp = _maxHp;
        _speed = speed;
        _driftX = driftX;
        _destroyBelowY = destroyBelowY;
        _onKilled = onKilled;
    }

    void Update()
    {
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

        _onKilled?.Invoke(_maxHp);
        Destroy(gameObject);
    }
}
