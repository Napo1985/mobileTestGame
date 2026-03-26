using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Black screen, finger or mouse drags the ship, auto-fires upward.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] float fireInterval = 0.12f;
    [SerializeField] float bulletSpeed = 16f;
    [SerializeField] float shipMoveSmoothing = 18f;

    Transform _ship;
    Camera _cam;
    Sprite _pixelSprite;
    float _fireTimer;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = 8f;
        transform.position = new Vector3(0f, 0f, -10f);
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = Color.black;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;

        Application.targetFrameRate = 60;

        _pixelSprite = BuildPixelSprite();
        _ship = BuildShip();
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

        var bullet = go.AddComponent<Bullet>();
        bullet.Configure(bulletSpeed, _cam.orthographicSize + 6f);
    }
}

