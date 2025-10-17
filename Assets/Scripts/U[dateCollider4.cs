////using System.Collections.Generic;
////using UnityEngine;

////public class UpdateColliderWithSprite : MonoBehaviour
////{
////    private SpriteRenderer spriteRenderer;
////    private PolygonCollider2D polyCollider;

////    void Start()
////    {
////        spriteRenderer = GetComponent<SpriteRenderer>();
////        polyCollider = GetComponent<PolygonCollider2D>();
////    }

////    void LateUpdate()
////    {
////        // Update collider to match current sprite
////        polyCollider.pathCount = spriteRenderer.sprite.GetPhysicsShapeCount();
////        for (int i = 0; i < polyCollider.pathCount; i++)
////        {
////            List<Vector2> path = new List<Vector2>();
////            spriteRenderer.sprite.GetPhysicsShape(i, path);
////            polyCollider.SetPath(i, path.ToArray());
////        }
////    }
////}

//using System.Collections.Generic;
//using UnityEngine;

//public class UpdateColliderWithSprite : MonoBehaviour
//{
//    private SpriteRenderer spriteRenderer;
//    private PolygonCollider2D polyCollider;

//    // Reuse list and arrays to avoid GC
//    private List<Vector2> reusablePath = new List<Vector2>();
//    private List<Vector2[]> reusablePaths = new List<Vector2[]>();

//    void Start()
//    {
//        spriteRenderer = GetComponent<SpriteRenderer>();
//        polyCollider = GetComponent<PolygonCollider2D>();

//        // Initialize reusablePaths list once
//        int shapeCount = spriteRenderer.sprite.GetPhysicsShapeCount();
//        for (int i = 0; i < shapeCount; i++)
//        {
//            spriteRenderer.sprite.GetPhysicsShape(i, reusablePath);
//            reusablePaths.Add(reusablePath.ToArray());
//        }
//    }

//    void LateUpdate()
//    {
//        int shapeCount = spriteRenderer.sprite.GetPhysicsShapeCount();
//        polyCollider.pathCount = shapeCount;

//        // Adjust reusablePaths capacity if shape count changes (rare)
//        while (reusablePaths.Count < shapeCount)
//            reusablePaths.Add(new Vector2[0]);
//        while (reusablePaths.Count > shapeCount)
//            reusablePaths.RemoveAt(reusablePaths.Count - 1);

//        for (int i = 0; i < shapeCount; i++)
//        {
//            reusablePath.Clear();
//            spriteRenderer.sprite.GetPhysicsShape(i, reusablePath);

//            // Only recreate array if path size changed
//            if (reusablePaths[i].Length != reusablePath.Count)
//                reusablePaths[i] = new Vector2[reusablePath.Count];

//            // Copy list to array without allocating
//            for (int j = 0; j < reusablePath.Count; j++)
//                reusablePaths[i][j] = reusablePath[j];

//            polyCollider.SetPath(i, reusablePaths[i]);
//        }
//    }
//}
