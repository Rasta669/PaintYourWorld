//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class BirdSpawner : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private GameObject birdPrefab;
//    [SerializeField] private Transform player;
//    [SerializeField] private Camera mainCamera;

//    [Header("Spawn Settings")]
//    [SerializeField] private int poolSize = 6;
//    [SerializeField] private float spawnBufferAhead = 3f; // how far outside camera to spawn
//    [SerializeField] private float distanceBetweenSpawns = 8f;
//    [SerializeField] private float yOffsetRange = 2.5f;
//    [SerializeField] private float spawnInterval = 2f;
//    [SerializeField] private float initialSpawnDelay = 1f;

//    [Header("Bird Settings")]
//    [SerializeField] private float minSpeed = 2f;
//    [SerializeField] private float maxSpeed = 4f;
//    [SerializeField] private float despawnDistance = 25f;

//    private List<GameObject> birdPool = new List<GameObject>();
//    private float lastSpawnX;

//    void Start()
//    {
//        if (player == null)
//        {
//            Debug.LogError("BirdSpawner: Player Transform not assigned!");
//            return;
//        }

//        if (mainCamera == null)
//            mainCamera = Camera.main;

//        // Initialize object pool
//        for (int i = 0; i < poolSize; i++)
//        {
//            GameObject bird = Instantiate(birdPrefab);
//            bird.SetActive(false);
//            birdPool.Add(bird);
//        }

//        lastSpawnX = player.position.x;
//        //SpawnBird();
//        StartCoroutine(SpawnRoutine());
//    }

//    IEnumerator SpawnRoutine()
//    {
//        //Debug.Log("SpawnRoutine initiated"); 
//        yield return new WaitForSeconds(initialSpawnDelay); // short delay before first spawn
//        //Debug.Log("SpawnRoutine started"); 
//        while (true)
//        {
//            TrySpawnBird();
//            yield return new WaitForSeconds(spawnInterval);
//        }
//    }

//    void TrySpawnBird()
//    {
//        // Only spawn if player has moved far enough to warrant another bird
//        //if (player.position.x > lastSpawnX + distanceBetweenSpawns)
//        //{
//        //    SpawnBird();
//        //    lastSpawnX = player.position.x;
//        //}

//        SpawnBird();
//    }

//    void SpawnBird()
//    {
//        GameObject bird = GetPooledBird();
//        if (bird == null) return;

//        // Get right edge of camera (world position)
//        float camRightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, 0)).x;
//        float spawnX = camRightEdge + spawnBufferAhead;

//        // Follow player height with a small random offset
//        float spawnY = player.position.y + Random.Range(-yOffsetRange, yOffsetRange);
//        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

//        // Activate and initialize
//        bird.transform.position = spawnPos;
//        bird.SetActive(true);

//        BirdMovement movement = bird.GetComponent<BirdMovement>();
//        movement.Initialize(player, Random.Range(minSpeed, maxSpeed), despawnDistance);
//    }

//    GameObject GetPooledBird()
//    {
//        foreach (var bird in birdPool)
//        {
//            if (!bird.activeInHierarchy)
//            {
//                //Debug.Log("SpawnBird called, found bird: " + (bird != null));
//                return bird;
//            }
//        }
//        return null;
//    }

//}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject birdPrefab;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject score;
    [SerializeField] private Camera mainCamera;

    [Header("Spawn Settings")]
    [SerializeField] private int poolSize = 6;
    [SerializeField] private float spawnBufferAhead = 3f;
    [SerializeField] private float distanceBetweenSpawns = 8f;
    [SerializeField] private float yOffsetRange = 2.5f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float initialSpawnDelay = 1f;

    [Header("Bird Settings")]
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float despawnDistance = 25f;

    [Header("Scoring")]
    [SerializeField] private float passBonusOffset = 1.5f;

    private List<GameObject> birdPool = new List<GameObject>();
    private Dictionary<GameObject, bool> birdPassed = new Dictionary<GameObject, bool>();
    private GameManager gameManager;
    private float lastSpawnX;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("BirdSpawner: Player Transform not assigned!");
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        gameManager = GameManager.Instance;

        // Initialize object pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bird = Instantiate(birdPrefab);
            bird.SetActive(false);
            birdPool.Add(bird);
            birdPassed[bird] = false;
        }

        lastSpawnX = player.transform.position.x;
        StartCoroutine(SpawnRoutine());
    }



    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        while (true)
        {
            TrySpawnBird();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
        if (player == null)
        {
            Debug.LogError("ObstacleSpawner: Player reference is null!");
            return;
        }

        Debug.Log($"ObstacleSpawner: Player set to {player.name}");
    }


    void Update()
    {
        CheckPassedBirds();

    }

    void TrySpawnBird()
    {
        SpawnBird();
    }

    void SpawnBird()
    {
        GameObject bird = GetPooledBird();
        if (bird == null) return;

        float camRightEdge = mainCamera.ViewportToWorldPoint(new Vector3(1, 0.5f, 0)).x;
        float spawnX = camRightEdge + spawnBufferAhead;
        float spawnY = player.transform.position.y + Random.Range(-yOffsetRange, yOffsetRange);

        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);
        bird.transform.position = spawnPos;
        bird.SetActive(true);
        birdPassed[bird] = false;

        BirdMovement movement = bird.GetComponent<BirdMovement>();
        movement.Initialize(player, Random.Range(minSpeed, maxSpeed), despawnDistance);
    }

    void CheckPassedBirds()
    {
        foreach (var bird in birdPool)
        {
            if (!bird.activeInHierarchy) continue;

            // Award XP once when player passes bird
            if (!birdPassed[bird] && score.transform.position.x > bird.transform.position.x)
            {
                birdPassed[bird] = true;
                gameManager?.AddObstacleBonus();
                Debug.Log($"✅ Player passed bird {bird.name} — bonus awarded!");
            }
            //Debug.Log($"Player X: {score.transform.position.x}, Bird X: {bird.transform.position.x}");

            // Despawn if too far behind camera
            float camLeftEdge = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.5f, 0)).x;
            if (bird.transform.position.x < camLeftEdge - despawnDistance)
            {
                bird.SetActive(false);
            }
        }
    }


    GameObject GetPooledBird()
    {
        foreach (var bird in birdPool)
        {
            if (!bird.activeInHierarchy)
                return bird;
        }
        return null;
    }
}
