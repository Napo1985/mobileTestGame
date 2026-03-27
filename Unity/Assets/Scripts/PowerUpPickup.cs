using System;
using UnityEngine;

public enum PowerUpType
{
    HealthPack,
    ShotDamageMod,
    ShotSpeedMod,
    ShotRateMod,
    ShotSizeMod
}

public class PowerUpPickup : MonoBehaviour
{
    PowerUpType _type;
    float _value;
    float _fallSpeed;
    float _destroyBelowY;
    Action<PowerUpType, float> _onCollected;

    public void Configure(PowerUpType type, float value, float fallSpeed, float destroyBelowY, Action<PowerUpType, float> onCollected)
    {
        _type = type;
        _value = value;
        _fallSpeed = Mathf.Max(0.1f, fallSpeed);
        _destroyBelowY = destroyBelowY;
        _onCollected = onCollected;
    }

    void Update()
    {
        transform.position += Vector3.down * (_fallSpeed * Time.deltaTime);
        if (transform.position.y < _destroyBelowY)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerShipMarker>() == null)
            return;

        _onCollected?.Invoke(_type, _value);
        Destroy(gameObject);
    }
}
