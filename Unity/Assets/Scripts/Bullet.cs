using UnityEngine;

public class Bullet : MonoBehaviour
{
    float _speed = 16f;
    float _destroyAboveY = 14f;

    public void Configure(float speed, float destroyAboveY)
    {
        _speed = speed;
        _destroyAboveY = destroyAboveY;
    }

    void Update()
    {
        transform.position += Vector3.up * (_speed * Time.deltaTime);
        if (transform.position.y > _destroyAboveY)
            Destroy(gameObject);
    }
}
