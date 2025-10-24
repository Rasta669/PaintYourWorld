
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class ObstacleSpawner : MonoBehaviour
//{
//    [SerializeField] private List<GameObject> obstaclePrefabs;
//    [SerializeField] private GameObject player;
//    [SerializeField] private float initialSpawnRate = 2f;
//    [SerializeField] private float minSpawnRate = 0.5f;
//    [SerializeField] private float initialMinXDistance = 5f;
//    [SerializeField] private float initialMaxXDistance = 10f;
//    [SerializeField] private float minXDistanceLimit = 2f;
//    [SerializeField] private float initialMoveSpeed = 2f;
//    [SerializeField] private float maxMoveSpeed = 5f;
//    [SerializeField] private float ySpawnRangeBelow = 3f;
//    [SerializeField] private float ySpawnRangeAbove = 5f;
//    [SerializeField] private int poolSize = 20; // Number of obstacles to pool

//    private float playerLastX = 0f;
//    private const float playerProgressThreshold = 2f;

//    private float currentSpawnRate;
//    private float currentMinXDistance;
//    private float currentMaxXDistance;
//    private float currentMoveSpeed;
//    private float lastSpawnX = 0f;
//    private float difficultyFactor = 0f;
//    private const float difficultyIncreaseRate = 0.007f;
//    private Queue<GameObject> obstaclePool; // Object pool for obstacles
//    private List<GameObject> activeObstacles; // Tracks active obstacles

//    void Awake()
//    {
//        // Initialize object pool
//        obstaclePool = new Queue<GameObject>();
//        activeObstacles = new List<GameObject>();
//        InitializeObjectPool();
//    }

//    void Start()
//    {
//        currentSpawnRate = initialSpawnRate;
//        currentMinXDistance = initialMinXDistance;
//        currentMaxXDistance = initialMaxXDistance;
//        currentMoveSpeed = initialMoveSpeed;

//        if (player == null)
//        {
//            Debug.LogError("Player reference not set in ObstacleSpawner!");
//        }

//        StartCoroutine(SpawnObstaclesEndlessly());
//    }

//    private void InitializeObjectPool()
//    {
//        for (int i = 0; i < poolSize; i++)
//        {
//            GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
//            GameObject obstacle = Instantiate(obstaclePrefab, transform);
//            obstacle.SetActive(false);
//            obstaclePool.Enqueue(obstacle);
//        }
//        Debug.Log($"Obstacle pool initialized with {poolSize} obstacles.");
//    }

//    private GameObject GetObstacleFromPool(Vector3 position, Quaternion rotation)
//    {
//        if (obstaclePool.Count > 0)
//        {
//            GameObject obstacle = obstaclePool.Dequeue();
//            obstacle.transform.position = position;
//            obstacle.transform.rotation = rotation;
//            obstacle.SetActive(true);
//            return obstacle;
//        }
//        else
//        {
//            GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
//            GameObject obstacle = Instantiate(obstaclePrefab, position, rotation, transform);
//            Debug.LogWarning("Obstacle pool exhausted, instantiating new obstacle.");
//            return obstacle;
//        }
//    }

//    public void ReturnObstacleToPool(GameObject obstacle)
//    {
//        obstacle.SetActive(false);
//        activeObstacles.Remove(obstacle);
//        obstaclePool.Enqueue(obstacle);
//    }

//    void Update()
//    {
//        difficultyFactor = Mathf.Clamp01(difficultyFactor + difficultyIncreaseRate * Time.deltaTime);
//        UpdateDifficulty();
//    }

//    void UpdateDifficulty()
//    {
//        currentSpawnRate = Mathf.Lerp(initialSpawnRate, minSpawnRate, difficultyFactor);
//        currentMinXDistance = Mathf.Lerp(initialMinXDistance, minXDistanceLimit, difficultyFactor);
//        currentMaxXDistance = Mathf.Lerp(initialMaxXDistance, minXDistanceLimit + 2f, difficultyFactor);
//        currentMoveSpeed = Mathf.Lerp(initialMoveSpeed, maxMoveSpeed, difficultyFactor);
//    }

//    IEnumerator SpawnObstaclesEndlessly()
//    {
//        while (true)
//        {
//            SpawnObstacle();
//            yield return new WaitForSeconds(currentSpawnRate);
//        }
//    }

//    void SpawnObstacle()
//    {
//        if (player == null) return;

//        if (Mathf.Abs(player.transform.position.x - playerLastX) < playerProgressThreshold)
//            return;

//        float maxSpawnAhead = 15f;
//        float spawnAheadDistance = Random.Range(currentMinXDistance, currentMaxXDistance);
//        spawnAheadDistance = Mathf.Min(spawnAheadDistance, maxSpawnAhead);

//        float spawnX = player.transform.position.x + spawnAheadDistance;

//        if (spawnX < lastSpawnX + currentMinXDistance)
//            spawnX = lastSpawnX + currentMinXDistance;

//        float playerY = player.transform.position.y;
//        float spawnY = playerY + Random.Range(-ySpawnRangeBelow, ySpawnRangeAbove);
//        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

//        GameObject obstacle = GetObstacleFromPool(spawnPosition, Quaternion.identity);
//        obstacle.tag = "obstacle";

//        ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
//        if (movement == null)
//        {
//            movement = obstacle.AddComponent<ObstacleMovement>();
//        }
//        movement.Initialize(GetRandomMovementType(), currentMoveSpeed, player, difficultyFactor);

//        activeObstacles.Add(obstacle);
//        lastSpawnX = spawnX;
//        playerLastX = player.transform.position.x;
//    }

//    public void SetPlayer(GameObject newPlayer)
//    {
//        player = newPlayer;
//        if (player == null)
//        {
//            Debug.LogError("ObstacleSpawner: Player reference is null!");
//            return;
//        }

//        Debug.Log($"ObstacleSpawner: Player set to {player.name}");
//    }

//    MovementType GetRandomMovementType()
//    {
//        List<MovementType> basicTypes = new List<MovementType>
//        {
//            MovementType.ZigZag,
//            MovementType.Circular,
//            MovementType.UpDown,
//            MovementType.LeftRight,
//            MovementType.Wavy
//        };

//        if (difficultyFactor >= 0.5f)
//        {
//            basicTypes.Add(MovementType.RandomMix);
//        }

//        return basicTypes[Random.Range(0, basicTypes.Count)];
//    }
//}

//public class ObstacleMovement : MonoBehaviour
//{
//    private MovementType movementType;
//    private float speed;
//    private float timeElapsed = 0f;
//    private Vector3 startPosition;
//    private GameObject player;
//    private GameManager gameManager;
//    private bool bonusAwarded = false;

//    private float amplitude = 2f;
//    private float frequency = 1f;
//    private float radius = 2f;
//    private float difficulty;
//    private Vector3 movementDirection;

//    [SerializeField] private float collisionCheckRadius = 0.5f;
//    [SerializeField, Range(0.05f, 1f)] private float blockerTimeScale = 0.35f;
//    [SerializeField, Range(0.2f, 1f)] private float obstacleTimeScale = 0.7f;
//    [SerializeField] private float obstacleSeparationStrength = 0.18f;
//    [SerializeField, Range(0f, 0.3f)] private float stuckChance = 0.15f;
//    [SerializeField] private float stuckTime = 2f;
//    [SerializeField] private float recoverySpeed = 1.2f;

//    private bool isStuck = false;
//    private float stuckTimer = 0f;
//    private float currentTimeScale = 1f;
//    private ObstacleSpawner spawner;

//    public void Initialize(MovementType type, float moveSpeed, GameObject playerRef, float difficultyFactor)
//    {
//        movementType = type;
//        speed = moveSpeed;
//        startPosition = transform.position;
//        player = playerRef;
//        gameManager = GameManager.Instance;
//        difficulty = difficultyFactor;
//        movementDirection = (playerRef.transform.position - transform.position).normalized;
//        timeElapsed = 0f;
//        bonusAwarded = false;
//        isStuck = false;
//        stuckTimer = 0f;
//        currentTimeScale = 1f;

//        spawner = FindObjectOfType<ObstacleSpawner>();

//        if (GetComponent<Collider2D>() == null)
//        {
//            var c = gameObject.AddComponent<BoxCollider2D>();
//            c.isTrigger = false;
//        }
//    }

//    void Update()
//    {
//        float targetTimeScale = 1f;
//        float predictedTime = timeElapsed + Time.deltaTime;
//        Vector3 predictedPos = CalculatePositionForTime(predictedTime);

//        Collider2D[] hits = Physics2D.OverlapCircleAll(predictedPos, collisionCheckRadius);
//        bool hitBlocker = false;
//        //bool hitObstacle = false;
//        Vector2 separation = Vector2.zero;

//        foreach (var h in hits)
//        {
//            if (h == null || h.gameObject == this.gameObject) continue;
//            if (h.CompareTag("Blocker")) hitBlocker = true;
//        }

//        if (hitBlocker)
//        {
//            targetTimeScale *= blockerTimeScale * 0.6f;
//            if (!isStuck && Random.value < stuckChance)
//            {
//                isStuck = true;
//                stuckTimer = stuckTime;
//                targetTimeScale = 0f;
//            }
//        }
//        //if (hitObstacle)
//        //    targetTimeScale *= obstacleTimeScale;

//        if (isStuck)
//        {
//            stuckTimer -= Time.deltaTime;
//            targetTimeScale = 0f;
//            if (stuckTimer <= 0f)
//                isStuck = false;
//        }

//        currentTimeScale = Mathf.MoveTowards(currentTimeScale, targetTimeScale, Time.deltaTime * recoverySpeed);
//        timeElapsed += Time.deltaTime * currentTimeScale;

//        Vector3 newPosition = CalculatePositionForTime(timeElapsed);
//        if (separation != Vector2.zero)
//            newPosition += (Vector3)(separation.normalized * obstacleSeparationStrength);

//        transform.position = newPosition;

//        if (!bonusAwarded && player != null && player.transform.position.x > transform.position.x + 2f)
//        {
//            bonusAwarded = true;
//            gameManager?.AddObstacleBonus();
//        }

//        if (player != null && transform.position.x < player.transform.position.x - 20f)
//        {
//            spawner?.ReturnObstacleToPool(gameObject);
//        }
//    }

//    Vector3 CalculatePositionForTime(float t)
//    {
//        Vector3 newPosition = startPosition;

//        switch (movementType)
//        {
//            case MovementType.ZigZag:
//                newPosition.x = startPosition.x + Mathf.Sin(t * speed * frequency) * amplitude;
//                newPosition.y = startPosition.y + Mathf.Cos(t * speed * frequency) * (amplitude / 2f);
//                break;

//            case MovementType.Circular:
//                newPosition.x = startPosition.x + Mathf.Cos(t * speed) * radius;
//                newPosition.y = startPosition.y + Mathf.Sin(t * speed) * radius;
//                break;

//            case MovementType.UpDown:
//                newPosition.y = startPosition.y + Mathf.Sin(t * speed * frequency) * amplitude;
//                break;

//            case MovementType.LeftRight:
//                newPosition.x = startPosition.x + Mathf.Sin(t * speed * frequency) * amplitude;
//                break;

//            case MovementType.Wavy:
//                {
//                    Vector3 offset = movementDirection * speed * t;
//                    float waveY = Mathf.Sin(t * speed * frequency) * amplitude;
//                    newPosition = startPosition + offset + new Vector3(0f, waveY, 0f);
//                }
//                break;

//            case MovementType.RandomMix:
//                {
//                    Vector3 offset = movementDirection * speed * t;
//                    float waveY = Mathf.Sin(t * speed * frequency) * (amplitude / 2f)
//                                + Mathf.Sin(t * speed) * (radius * 0.3f);
//                    float waveX = Mathf.Sin(t * speed * 0.5f) * (radius * 0.5f);
//                    newPosition = startPosition + offset + new Vector3(waveX, waveY, 0f);
//                }
//                break;
//        }

//        return newPosition;
//    }

//    void OnDrawGizmosSelected()
//    {
//        Gizmos.color = Color.magenta;
//        Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);
//    }
//}

//public enum MovementType
//{
//    ZigZag,
//    Circular,
//    UpDown,
//    LeftRight,
//    Wavy,
//    RandomMix
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> obstaclePrefabs;
    [SerializeField] private GameObject player;
    [SerializeField] private float initialSpawnRate = 2f;
    [SerializeField] private float minSpawnRate = 0.5f;
    [SerializeField] private float initialMinXDistance = 5f;
    [SerializeField] private float initialMaxXDistance = 10f;
    [SerializeField] private float minXDistanceLimit = 2f;
    [SerializeField] private float initialMoveSpeed = 2f;
    [SerializeField] private float maxMoveSpeed = 5f;
    [SerializeField] private float ySpawnRangeBelow = 3f;
    [SerializeField] private float ySpawnRangeAbove = 5f;
    [SerializeField] private int poolSize = 20;

    private float playerLastX = 0f;
    private const float playerProgressThreshold = 2f;

    private float currentSpawnRate;
    private float currentMinXDistance;
    private float currentMaxXDistance;
    private float currentMoveSpeed;
    private float lastSpawnX = 0f;
    private float difficultyFactor = 0f;
    private const float difficultyIncreaseRate = 0.007f;
    private Queue<GameObject> obstaclePool;
    private List<GameObject> activeObstacles;

    void Awake()
    {
        obstaclePool = new Queue<GameObject>();
        activeObstacles = new List<GameObject>();
        InitializeObjectPool();
    }

    void Start()
    {
        currentSpawnRate = initialSpawnRate;
        currentMinXDistance = initialMinXDistance;
        currentMaxXDistance = initialMaxXDistance;
        currentMoveSpeed = initialMoveSpeed;

        if (player == null)
        {
            Debug.LogError("Player reference not set in ObstacleSpawner!");
        }

        StartCoroutine(SpawnObstaclesEndlessly());
    }

    private void InitializeObjectPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
            GameObject obstacle = Instantiate(obstaclePrefab, transform);
            obstacle.SetActive(false);
            obstaclePool.Enqueue(obstacle);
        }
        Debug.Log($"Obstacle pool initialized with {poolSize} obstacles.");
    }

    private GameObject GetObstacleFromPool(Vector3 position, Quaternion rotation)
    {
        if (obstaclePool.Count > 0)
        {
            GameObject obstacle = obstaclePool.Dequeue();
            obstacle.transform.position = position;
            obstacle.transform.rotation = rotation;
            obstacle.SetActive(true);
            return obstacle;
        }
        else
        {
            GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
            GameObject obstacle = Instantiate(obstaclePrefab, position, rotation, transform);
            Debug.LogWarning("Obstacle pool exhausted, instantiating new obstacle.");
            return obstacle;
        }
    }

    public void ReturnObstacleToPool(GameObject obstacle)
    {
        obstacle.SetActive(false);
        activeObstacles.Remove(obstacle);
        obstaclePool.Enqueue(obstacle);
    }

    public List<GameObject> GetActiveObstacles()
    {
        activeObstacles.RemoveAll(item => item == null);
        return activeObstacles;
    }

    void Update()
    {
        difficultyFactor = Mathf.Clamp01(difficultyFactor + difficultyIncreaseRate * Time.deltaTime);
        UpdateDifficulty();
    }

    void UpdateDifficulty()
    {
        currentSpawnRate = Mathf.Lerp(initialSpawnRate, minSpawnRate, difficultyFactor);
        currentMinXDistance = Mathf.Lerp(initialMinXDistance, minXDistanceLimit, difficultyFactor);
        currentMaxXDistance = Mathf.Lerp(initialMaxXDistance, minXDistanceLimit + 2f, difficultyFactor);
        currentMoveSpeed = Mathf.Lerp(initialMoveSpeed, maxMoveSpeed, difficultyFactor);
    }

    IEnumerator SpawnObstaclesEndlessly()
    {
        while (true)
        {
            SpawnObstacle();
            yield return new WaitForSeconds(currentSpawnRate);
        }
    }

    void SpawnObstacle()
    {
        if (player == null) return;

        if (Mathf.Abs(player.transform.position.x - playerLastX) < playerProgressThreshold)
            return;

        float maxSpawnAhead = 15f;
        float spawnAheadDistance = Random.Range(currentMinXDistance, currentMaxXDistance);
        spawnAheadDistance = Mathf.Min(spawnAheadDistance, maxSpawnAhead);

        float spawnX = player.transform.position.x + spawnAheadDistance;

        if (spawnX < lastSpawnX + currentMinXDistance)
            spawnX = lastSpawnX + currentMinXDistance;

        float playerY = player.transform.position.y;
        float spawnY = playerY + Random.Range(-ySpawnRangeBelow, ySpawnRangeAbove);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        GameObject obstacle = GetObstacleFromPool(spawnPosition, Quaternion.identity);
        obstacle.tag = "obstacle";

        ObstacleMovement movement = obstacle.GetComponent<ObstacleMovement>();
        if (movement == null)
        {
            movement = obstacle.AddComponent<ObstacleMovement>();
        }
        movement.Initialize(GetRandomMovementType(), currentMoveSpeed, player, difficultyFactor);

        activeObstacles.Add(obstacle);
        lastSpawnX = spawnX;
        playerLastX = player.transform.position.x;
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

    MovementType GetRandomMovementType()
    {
        List<MovementType> basicTypes = new List<MovementType>
        {
            MovementType.ZigZag,
            MovementType.Circular,
            MovementType.UpDown,
            MovementType.LeftRight,
            MovementType.Wavy
        };

        if (difficultyFactor >= 0.5f)
        {
            basicTypes.Add(MovementType.RandomMix);
        }

        return basicTypes[Random.Range(0, basicTypes.Count)];
    }
}

public class ObstacleMovement : MonoBehaviour
{
    private MovementType movementType;
    private float speed;
    private float timeElapsed = 0f;
    private Vector3 startPosition;
    private GameObject player;
    private GameManager gameManager;
    private bool bonusAwarded = false;

    private float amplitude = 2f;
    private float frequency = 1f;
    private float radius = 2f;
    private float difficulty;
    private Vector3 movementDirection;

    [SerializeField] private float collisionCheckRadius = 0.5f;
    [SerializeField, Range(0.05f, 1f)] private float blockerTimeScale = 0.35f;
    [SerializeField, Range(0.2f, 1f)] private float obstacleTimeScale = 0.7f;
    [SerializeField] private float obstacleSeparationStrength = 0.18f;
    [SerializeField, Range(0f, 0.3f)] private float stuckChance = 0.15f;
    [SerializeField] private float stuckTime = 2f;
    [SerializeField] private float recoverySpeed = 1.2f;

    private bool isStuck = false;
    private float stuckTimer = 0f;
    private float currentTimeScale = 1f;
    private ObstacleSpawner spawner;

    public void Initialize(MovementType type, float moveSpeed, GameObject playerRef, float difficultyFactor)
    {
        movementType = type;
        speed = moveSpeed;
        startPosition = transform.position;
        player = playerRef;
        gameManager = GameManager.Instance;
        difficulty = difficultyFactor;
        movementDirection = (playerRef.transform.position - transform.position).normalized;
        timeElapsed = 0f;
        bonusAwarded = false;
        isStuck = false;
        stuckTimer = 0f;
        currentTimeScale = 1f;

        spawner = FindObjectOfType<ObstacleSpawner>();

        if (GetComponent<Collider2D>() == null)
        {
            var c = gameObject.AddComponent<BoxCollider2D>();
            c.isTrigger = false;
        }
    }

    void Update()
    {
        float targetTimeScale = 1f;
        float predictedTime = timeElapsed + Time.deltaTime;
        Vector3 predictedPos = CalculatePositionForTime(predictedTime);

        Collider2D[] hits = Physics2D.OverlapCircleAll(predictedPos, collisionCheckRadius);
        bool hitBlocker = false;
        Vector2 separation = Vector2.zero;

        foreach (var h in hits)
        {
            if (h == null || h.gameObject == this.gameObject) continue;
            if (h.CompareTag("Blocker")) hitBlocker = true;
        }

        if (hitBlocker)
        {
            targetTimeScale *= blockerTimeScale * 0.6f;
            if (!isStuck && Random.value < stuckChance)
            {
                isStuck = true;
                stuckTimer = stuckTime;
                targetTimeScale = 0f;
            }
        }

        if (isStuck)
        {
            stuckTimer -= Time.deltaTime;
            targetTimeScale = 0f;
            if (stuckTimer <= 0f)
                isStuck = false;
        }

        currentTimeScale = Mathf.MoveTowards(currentTimeScale, targetTimeScale, Time.deltaTime * recoverySpeed);
        timeElapsed += Time.deltaTime * currentTimeScale;

        Vector3 newPosition = CalculatePositionForTime(timeElapsed);
        if (separation != Vector2.zero)
            newPosition += (Vector3)(separation.normalized * obstacleSeparationStrength);

        transform.position = newPosition;

        if (!bonusAwarded && player != null && player.transform.position.x > transform.position.x + 2f)
        {
            bonusAwarded = true;
            gameManager?.AddObstacleBonus();
        }

        if (player != null && transform.position.x < player.transform.position.x - 20f)
        {
            spawner?.ReturnObstacleToPool(gameObject);
        }
    }

    Vector3 CalculatePositionForTime(float t)
    {
        Vector3 newPosition = startPosition;

        switch (movementType)
        {
            case MovementType.ZigZag:
                newPosition.x = startPosition.x + Mathf.Sin(t * speed * frequency) * amplitude;
                newPosition.y = startPosition.y + Mathf.Cos(t * speed * frequency) * (amplitude / 2f);
                break;

            case MovementType.Circular:
                newPosition.x = startPosition.x + Mathf.Cos(t * speed) * radius;
                newPosition.y = startPosition.y + Mathf.Sin(t * speed) * radius;
                break;

            case MovementType.UpDown:
                newPosition.y = startPosition.y + Mathf.Sin(t * speed * frequency) * amplitude;
                break;

            case MovementType.LeftRight:
                newPosition.x = startPosition.x + Mathf.Sin(t * speed * frequency) * amplitude;
                break;

            case MovementType.Wavy:
                {
                    Vector3 offset = movementDirection * speed * t;
                    float waveY = Mathf.Sin(t * speed * frequency) * amplitude;
                    newPosition = startPosition + offset + new Vector3(0f, waveY, 0f);
                }
                break;

            case MovementType.RandomMix:
                {
                    Vector3 offset = movementDirection * speed * t;
                    float waveY = Mathf.Sin(t * speed * frequency) * (amplitude / 2f)
                                + Mathf.Sin(t * speed) * (radius * 0.3f);
                    float waveX = Mathf.Sin(t * speed * 0.5f) * (radius * 0.5f);
                    newPosition = startPosition + offset + new Vector3(waveX, waveY, 0f);
                }
                break;
        }

        return newPosition;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);
    }
}

public enum MovementType
{
    ZigZag,
    Circular,
    UpDown,
    LeftRight,
    Wavy,
    RandomMix
}