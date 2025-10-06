using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    // New field in ObstacleSpawner
    private float playerLastX = 0f;
    private const float playerProgressThreshold = 2f; // how far player must move before next spawn


    private float currentSpawnRate;
    private float currentMinXDistance;
    private float currentMaxXDistance;
    private float currentMoveSpeed;
    private float lastSpawnX = 0f;
    private float difficultyFactor = 0f;
    private const float difficultyIncreaseRate = 0.007f;

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

    //void SpawnObstacle()
    //{
    //    if (player == null) return;

    //    float spawnX = player.transform.position.x + Random.Range(currentMinXDistance, currentMaxXDistance);
    //    if (spawnX < lastSpawnX + currentMinXDistance)
    //    {
    //        spawnX = lastSpawnX + currentMinXDistance;
    //    }

    //    float playerY = player.transform.position.y;
    //    float spawnY = playerY + Random.Range(-ySpawnRangeBelow, ySpawnRangeAbove);
    //    Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

    //    GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
    //    GameObject obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);

    //    ObstacleMovement movement = obstacle.AddComponent<ObstacleMovement>();
    //    movement.Initialize(GetRandomMovementType(), currentMoveSpeed, player, difficultyFactor);

    //    lastSpawnX = spawnX;
    //}


    void SpawnObstacle()
    {
        if (player == null) return;

        // Only spawn if player has moved far enough since last spawn
        if (Mathf.Abs(player.transform.position.x - playerLastX) < playerProgressThreshold)
        {
            return;
        }

        float maxSpawnAhead = 15f; // Prevent spawning too far ahead
        float spawnAheadDistance = Random.Range(currentMinXDistance, currentMaxXDistance);
        spawnAheadDistance = Mathf.Min(spawnAheadDistance, maxSpawnAhead);

        float spawnX = player.transform.position.x + spawnAheadDistance;

        // Ensure spacing from the last obstacle
        if (spawnX < lastSpawnX + currentMinXDistance)
        {
            spawnX = lastSpawnX + currentMinXDistance;
        }

        float playerY = player.transform.position.y;
        float spawnY = playerY + Random.Range(-ySpawnRangeBelow, ySpawnRangeAbove);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];
        GameObject obstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, transform);

        ObstacleMovement movement = obstacle.AddComponent<ObstacleMovement>();
        movement.Initialize(GetRandomMovementType(), currentMoveSpeed, player, difficultyFactor);

        lastSpawnX = spawnX;
        playerLastX = player.transform.position.x;
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
    private Vector3 movementDirection;  // Direction to player at spawn

    public void Initialize(MovementType type, float moveSpeed, GameObject playerRef, float difficultyFactor)
    {
        movementType = type;
        speed = moveSpeed;
        startPosition = transform.position;
        player = playerRef;
        gameManager = FindObjectOfType<GameManager>();
        difficulty = difficultyFactor;
        movementDirection = (playerRef.transform.position - transform.position).normalized;

    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        MoveObstacle();

        if (!bonusAwarded && player != null && player.transform.position.x > transform.position.x + 2f)
        {
            bonusAwarded = true;
            if (gameManager != null)
            {
                gameManager.AddObstacleBonus();
            }
        }

        if (player != null && transform.position.x < player.transform.position.x - 20f)
        {
            Destroy(gameObject);
        }
    }


    void MoveObstacle()
    {
        Vector3 newPosition = transform.position;

        switch (movementType)
        {
            case MovementType.ZigZag:
                newPosition.x = startPosition.x + Mathf.Sin(timeElapsed * speed * frequency) * amplitude;
                newPosition.y = startPosition.y + Mathf.Cos(timeElapsed * speed * frequency) * amplitude / 2f;
                break;

            case MovementType.Circular:
                newPosition.x = startPosition.x + Mathf.Cos(timeElapsed * speed) * radius;
                newPosition.y = startPosition.y + Mathf.Sin(timeElapsed * speed) * radius;
                break;

            case MovementType.UpDown:
                newPosition.y = startPosition.y + Mathf.Sin(timeElapsed * speed * frequency) * amplitude;
                break;

            case MovementType.LeftRight:
                newPosition.x = startPosition.x + Mathf.Sin(timeElapsed * speed * frequency) * amplitude;
                break;

            case MovementType.Wavy:
                {
                    Vector3 offset = movementDirection * speed * timeElapsed;
                    float waveY = Mathf.Sin(timeElapsed * speed * frequency) * amplitude;
                    newPosition = startPosition + offset + new Vector3(0f, waveY, 0f);
                }
                break;

            case MovementType.RandomMix:
                {
                    Vector3 offset = movementDirection * speed * timeElapsed;
                    float waveY = Mathf.Sin(timeElapsed * speed * frequency) * amplitude / 2f
                                + Mathf.Sin(timeElapsed * speed) * (radius * 0.3f);
                    float waveX = Mathf.Sin(timeElapsed * speed * 0.5f) * (radius * 0.5f);
                    newPosition = startPosition + offset + new Vector3(waveX, waveY, 0f);
                }
                break;


        }

        transform.position = newPosition;
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
