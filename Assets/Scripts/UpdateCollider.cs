using System.Collections.Generic;
using UnityEngine;

public class UpdateColliderWithSprite : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private PolygonCollider2D polyCollider;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        polyCollider = GetComponent<PolygonCollider2D>();
    }

    void LateUpdate()
    {
        // Update collider to match current sprite
        polyCollider.pathCount = spriteRenderer.sprite.GetPhysicsShapeCount();
        for (int i = 0; i < polyCollider.pathCount; i++)
        {
            List<Vector2> path = new List<Vector2>();
            spriteRenderer.sprite.GetPhysicsShape(i, path);
            polyCollider.SetPath(i, path.ToArray());
        }
    }
}