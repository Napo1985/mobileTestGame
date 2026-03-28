using UnityEngine;

/// <summary>
/// Simple trails and one-shot particle bursts (no prefabs required).
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

    public static void SpawnExplosion(Vector3 worldPosition, Color coreColor, float scale = 1f)
    {
        var go = new GameObject("Explosion");
        go.transform.position = worldPosition;
        go.SetActive(false);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.28f;
        main.startLifetime = 0.45f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f * scale, 5.5f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.2f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(
            coreColor,
            new Color(1f, 0.45f, 0.1f, 1f));
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32, 42) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f * scale;

        var pr = go.GetComponent<ParticleSystemRenderer>();
        pr.material = SpriteMat;

        go.SetActive(true);
        ps.Play();
        Object.Destroy(go, 1.2f);
    }
}
