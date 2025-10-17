////using UnityEngine;

////public class BirdMovement : MonoBehaviour
////{
////    private Transform player;
////    private float speed;
////    private float despawnDistance;
////    private bool initialized = false;
////    private Camera mainCamera;
////    [SerializeField] private float collisionCheckRadius = 0.5f;


////    public void Initialize(Transform target, float moveSpeed, float despawnDist)
////    {
////        player = target;
////        speed = moveSpeed;
////        despawnDistance = despawnDist;
////        mainCamera = Camera.main;
////        initialized = true;
////    }

////    void Update()
////    {
////        if (!initialized || player == null) return;

////        // Move left constantly
////        transform.position += Vector3.left * speed * Time.deltaTime;

////        // Get camera left edge in world coordinates
////        float camLeftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, 0)).x;

////        // Despawn once it's far off-screen to the left
////        if (transform.position.x < camLeftEdge - despawnDistance)
////        {
////            gameObject.SetActive(false);
////            initialized = false;
////        }

////    }

////}
//using UnityEngine;

//public class BirdMovement : MonoBehaviour
//{
//    private Transform player;
//    private float speed;
//    private float despawnDistance;
//    private bool initialized = false;
//    private Camera mainCamera;
//    private bool bonusAwarded = false;
//    [SerializeField] private float passBonusOffset = 1.5f; // how far player must be past bird
//    private GameManager gameManager;

//    [Header("Collision Settings")]
//    [SerializeField] private float collisionCheckRadius = 0.4f;

//    public void Initialize(Transform target, float moveSpeed, float despawnDist)
//    {
//        player = target;
//        speed = moveSpeed;
//        despawnDistance = despawnDist;
//        gameManager = GameManager.Instance;
//        mainCamera = Camera.main;
//        initialized = true;
//        bonusAwarded = false;
//    }

//    void Update()
//    {
//        if (!initialized || player == null) return;

//        // Move bird left
//        transform.position += Vector3.left * speed * Time.deltaTime;

//        // Check collision with paint
//        CheckPaintCollision();

//        // Despawn when offscreen to the left
//        float camLeftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, 0)).x;
//        if (transform.position.x < camLeftEdge - despawnDistance)
//        {
//            gameObject.SetActive(false);
//            initialized = false;
//        }
//        // Award XP when player passes bird
//        if (!bonusAwarded && player.position.x > transform.position.x + passBonusOffset)
//        {
//            bonusAwarded = true;
//            GameManager.Instance.AddObstacleBonus();
//            Debug.Log("Player passed the bird — bonus awarded!");
//        }
//    }

//    private void CheckPaintCollision()
//    {
//        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, collisionCheckRadius);
//        foreach (var hit in hits)
//        {
//            if (hit == null) continue;
//            if (hit.CompareTag("Paint"))
//            {
//                //Debug.Log("Bird collided with Paint — destroying paint.");
//                Destroy(hit.gameObject);
//            }
//        }
//    }

//    private void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.yellow;
//        Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);
//    }
//}


using UnityEngine;

public class BirdMovement : MonoBehaviour
{
    private GameObject player;
    private float speed;
    private float despawnDistance;
    private bool initialized = false;
    private Camera mainCamera;
    private bool bonusAwarded = false;
    private GameManager gameManager;

    [Header("Collision Settings")]
    [SerializeField] private float collisionCheckRadius = 0.4f;
    [SerializeField] private float passBonusOffset = 1.5f;

    public void Initialize(GameObject target, float moveSpeed, float despawnDist)
    {
        player = target;
        speed = moveSpeed;
        despawnDistance = despawnDist;
        mainCamera = Camera.main;
        bonusAwarded = false;

        // Ensure GameManager is set here or later
        if (GameManager.Instance != null)
            gameManager = GameManager.Instance;

        initialized = true;
    }

   
    void Update()
    {
        if (!initialized || player == null) return;

        // Move bird left
        transform.position += Vector3.left * speed * Time.deltaTime;

        // Check collision with paint
        CheckPaintCollision();


        // Despawn bird when offscreen to the left
        float camLeftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, 0)).x;
        if (transform.position.x < camLeftEdge - despawnDistance)
        {
            gameObject.SetActive(false);
            initialized = false;
        }
    }

    private void CheckPaintCollision()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, collisionCheckRadius);
        foreach (var hit in hits)
        {
            if (hit != null && (hit.CompareTag("Paint") || hit.CompareTag("Blocker") ))
            {
                Destroy(hit.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);
    }
}
