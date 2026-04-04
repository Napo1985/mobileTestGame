using UnityEngine;

/// <summary>
/// Trails, one-shot particle bursts, shockwave rings, and screen-shake helpers.
/// All methods are static — the ShockwaveAnimator MonoBehaviour drives
/// per-frame ring expansion autonomously.
/// </summary>
public static class GameplayVfx
{
    static Material _spriteMat;

    static Material SpriteMat
    {
        get
        {
            if (_spriteMat == null)
            {
                var sh = Shader.Find("Sprites/Default");
                _spriteMat = sh != null ? new Material(sh) : new Material(Shader.Find("Unlit/Color"));
            }
            return _spriteMat;
        }
    }

    // ── Trail ─────────────────────────────────────────────────────────

    public static void SetupFastTrail(GameObject go, Color color, float width, float duration)
    {
        var tr = go.AddComponent<TrailRenderer>();
        tr.material = SpriteMat;
        tr.time = duration;
        tr.startWidth = width;
        tr.endWidth = width * 0.15f;
        tr.startColor = color;
        tr.endColor = new Color(color.r, color.g, color.b, 0f);
        tr.numCapVertices = 2;
        tr.minVertexDistance = 0.02f;
    }

    // ── Explosion ─────────────────────────────────────────────────────

    /// <summary>
    /// Spawns a two-layer explosion: a tight core burst + a wider debris ring.
    /// Large explosions (scale >= 1) also spawn a shockwave ring.
    /// </summary>
    public static void SpawnExplosion(Vector3 worldPosition, Color coreColor, float scale = 1f)
    {
        SpawnCoreBurst(worldPosition, coreColor, scale);
        SpawnDebrisRing(worldPosition, scale);

        if (scale >= 0.85f)
            SpawnShockwave(worldPosition, scale * 2.8f);
    }

    static void SpawnCoreBurst(Vector3 pos, Color coreColor, float scale)
    {
        var go = new GameObject("Explosion_Core");
        go.transform.position = pos;
        go.SetActive(false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scale, 6f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f * scale, 0.22f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(
            coreColor,
            new Color(1f, 0.45f, 0.1f, 1f));
        main.gravityModifier = 0.18f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(30 * scale + 12), (short)(42 * scale + 18)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f * scale;

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.material = SpriteMat;

        go.SetActive(true);
        ps.Play();
        Object.Destroy(go, 1.4f);
    }

    static void SpawnDebrisRing(Vector3 pos, float scale)
    {
        var go = new GameObject("Explosion_Debris");
        go.transform.position = pos;
        go.SetActive(false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f * scale, 1.1f * scale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * scale, 4f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f * scale, 0.11f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.9f, 0.75f, 0.4f, 1f),
            new Color(0.55f, 0.3f, 0.15f, 0.7f));
        main.gravityModifier = 0.55f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(16 * scale + 6), (short)(28 * scale + 10)) });

        // Wide cone so debris flies outward from the ring
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f * scale;
        shape.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.material = SpriteMat;

        go.SetActive(true);
        ps.Play();
        Object.Destroy(go, 2.0f);
    }

    // ── Shockwave ring ────────────────────────────────────────────────

    /// <summary>
    /// Spawns an expanding LineRenderer circle that fades over <paramref name="duration"/> seconds.
    /// </summary>
    public static void SpawnShockwave(Vector3 worldPosition, float maxRadius, float duration = 0.42f)
    {
        var go = new GameObject("Shockwave");
        go.transform.position = worldPosition;

        var lr = go.AddComponent<LineRenderer>();
        lr.material = SpriteMat;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = 0.09f;
        lr.endWidth = 0.05f;
        lr.positionCount = ShockwaveAnimator.Segments;

        var anim = go.AddComponent<ShockwaveAnimator>();
        anim.Init(lr, maxRadius, duration);
    }

    // ── Hit / muzzle / telegraphs (lightweight for mobile) ────────────

    /// <summary>Small bright burst when a bullet connects (enemy still alive).</summary>
    public static void SpawnHitSpark(Vector3 worldPosition, float chunkyScale = 1f)
    {
        float s = Mathf.Clamp(chunkyScale, 0.55f, 1.65f);
        var go = new GameObject("HitSpark");
        go.transform.position = worldPosition;
        go.SetActive(false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.12f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * s, 3.2f * s);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f * s, 0.1f * s);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.95f, 0.55f, 1f),
            new Color(1f, 0.45f, 0.2f, 1f));
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(10 + 6 * s), (short)(14 + 8 * s)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f * s;

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.material = SpriteMat;

        go.SetActive(true);
        ps.Play();
        Object.Destroy(go, 0.45f);
    }

    /// <summary>One-frame style flash at barrel; short-lived particles only.</summary>
    public static void SpawnMuzzleFlash(Vector3 worldPosition, Color tint, float scale = 1f)
    {
        var go = new GameObject("MuzzleFlash");
        go.transform.position = worldPosition;
        go.SetActive(false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = 0.12f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f * scale, 2.4f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.12f * scale);
        main.startColor = tint;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        shape.radius = 0.02f * scale;

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.material = SpriteMat;

        go.SetActive(true);
        ps.Play();
        Object.Destroy(go, 0.35f);
    }

    /// <summary>Fading line from enemy to approximate missile path (telegraph).</summary>
    public static void SpawnMissileTelegraph(Vector3 from, Vector3 to, float duration = 0.28f)
    {
        var go = new GameObject("MissileTelegraph");
        go.transform.position = from;

        var lr = go.AddComponent<LineRenderer>();
        lr.material = SpriteMat;
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = 2;
        lr.sortingOrder = 80;
        lr.startWidth = 0.07f;
        lr.endWidth = 0.02f;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.startColor = new Color(1f, 0.35f, 0.2f, 0.55f);
        lr.endColor = new Color(1f, 0.2f, 0.1f, 0f);

        var fade = go.AddComponent<TelegraphLineFader>();
        fade.Init(duration);
    }

    /// <summary>Brief vertical pulse at top of playfield when a heavy asteroid enters.</summary>
    public static void SpawnAsteroidEntryPulse(Vector3 topWorldPosition, float halfHeight = 2.2f, float duration = 0.32f)
    {
        var go = new GameObject("AsteroidEntryPulse");
        go.transform.position = topWorldPosition;

        var lr = go.AddComponent<LineRenderer>();
        lr.material = SpriteMat;
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = 2;
        lr.sortingOrder = 75;
        lr.startWidth = 0.14f;
        lr.endWidth = 0.04f;
        Vector3 a = topWorldPosition + Vector3.down * halfHeight;
        Vector3 b = topWorldPosition + Vector3.up * (halfHeight * 0.35f);
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startColor = new Color(1f, 0.55f, 0.25f, 0.5f);
        lr.endColor = new Color(1f, 0.85f, 0.4f, 0f);

        var fade = go.AddComponent<TelegraphLineFader>();
        fade.Init(duration);
    }
}

/// <summary>Fades out and destroys a LineRenderer used for telegraphs.</summary>
public class TelegraphLineFader : MonoBehaviour
{
    LineRenderer _lr;
    float _duration;
    float _elapsed;
    Color _color0;
    Color _color1;

    public void Init(float duration)
    {
        _lr = GetComponent<LineRenderer>();
        _duration = Mathf.Max(0.05f, duration);
        _elapsed = 0f;
        if (_lr != null)
        {
            _color0 = _lr.startColor;
            _color1 = _lr.endColor;
        }
    }

    void Update()
    {
        if (_lr == null)
        {
            Destroy(gameObject);
            return;
        }

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float a = 1f - t;
        var c0 = _color0;
        var c1 = _color1;
        c0.a = _color0.a * a;
        c1.a = _color1.a * a;
        _lr.startColor = c0;
        _lr.endColor = c1;

        if (t >= 1f)
            Destroy(gameObject);
    }
}

/// <summary>
/// Drives the per-frame expansion and fade of a shockwave LineRenderer circle.
/// Kept in this file to avoid a standalone one-method file.
/// </summary>
public class ShockwaveAnimator : MonoBehaviour
{
    public const int Segments = 40;

    LineRenderer _lr;
    float _maxRadius;
    float _duration;
    float _elapsed;

    public void Init(LineRenderer lr, float maxRadius, float duration)
    {
        _lr = lr;
        _maxRadius = maxRadius;
        _duration = duration;
        _elapsed = 0f;
        UpdateRing(0f, 1f);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);
        float alpha = 1f - t;
        UpdateRing(_maxRadius * t, alpha);

        if (t >= 1f)
            Destroy(gameObject);
    }

    void UpdateRing(float radius, float alpha)
    {
        _lr.startColor = new Color(1f, 0.72f, 0.3f, alpha * 0.92f);
        _lr.endColor   = new Color(1f, 0.5f,  0.1f, 0f);

        for (int i = 0; i < Segments; i++)
        {
            float angle = i / (float)Segments * Mathf.PI * 2f;
            _lr.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f));
        }
    }
}
