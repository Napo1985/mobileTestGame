using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fills a PolygonCollider2D from a Sprite's physics outline (alpha silhouette when baked at import / runtime Create with physics).
/// </summary>
public static class SpriteCollider2DUtil
{
    public static PolygonCollider2D AddPolygonFromSprite(GameObject go, Sprite sprite, bool isTrigger = true)
    {
        var poly = go.AddComponent<PolygonCollider2D>();
        ApplySpriteToPolygonCollider(poly, sprite);
        poly.isTrigger = isTrigger;
        return poly;
    }

    public static void ApplySpriteToPolygonCollider(PolygonCollider2D poly, Sprite sprite)
    {
        if (poly == null || sprite == null)
            return;

        int n = sprite.GetPhysicsShapeCount();
        poly.pathCount = 0;

        if (n == 0)
        {
            var b = sprite.bounds;
            float hx = Mathf.Max(0.02f, b.extents.x * 0.9f);
            float hy = Mathf.Max(0.02f, b.extents.y * 0.9f);
            poly.pathCount = 1;
            poly.SetPath(0, new[]
            {
                new Vector2(-hx, -hy),
                new Vector2(hx, -hy),
                new Vector2(hx, hy),
                new Vector2(-hx, hy)
            });
            return;
        }

        poly.pathCount = n;
        var buffer = new List<Vector2>(64);
        for (int i = 0; i < n; i++)
        {
            buffer.Clear();
            sprite.GetPhysicsShape(i, buffer);
            poly.SetPath(i, buffer.ToArray());
        }
    }
}

