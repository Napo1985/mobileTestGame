using UnityEngine;

public class Bullet : MonoBehaviour
{
    float _speed = 16f;
    float _destroyAboveY = 14f;
    int _damage = 5;

    public void Configure(float speed, float destroyAboveY, int damage)
    {
        _speed = speed;
        _destroyAboveY = destroyAboveY;
        _damage = damage;
    }

    void Update()
    {
        transform.position += Vector3.up * (_speed * Time.deltaTime);
        if (transform.position.y > _destroyAboveY)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy == null)
            return;

        enemy.ApplyDamage(_damage);
        Destroy(gameObject);
    }
}
