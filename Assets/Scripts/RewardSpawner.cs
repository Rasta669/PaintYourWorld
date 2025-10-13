//using UnityEngine;
//using System.Collections;

//public class RewardSpawner : MonoBehaviour
//{
//    [SerializeField] private GameObject rewardPrefab;        // The reward prefab to spawn
//    [SerializeField] private GameObject player;              // Reference to the player
//    [SerializeField] private Camera mainCamera;              // Reference to the main camera
//    [SerializeField] private float initialXInterval = 100f;  // Initial X distance between rewards
//    [SerializeField] private float minXInterval = 50f;       // Minimum X interval at max difficulty
//    [SerializeField] private float difficultyIncreaseRate = 0.01f; // How fast difficulty increases
//    [SerializeField] private float rewardValue = 50f;        // XP value per reward collected

//    private float currentXInterval;
//    private float lastSpawnX = 0f;
//    private float difficultyFactor = 0f;
//    private GameManager gameManager; // Reference to GameManager for XP tracking

//    void Start()
//    {
//        if (player == null || mainCamera == null)
//        {
//            Debug.LogError("Player or Main Camera reference not set in RewardSpawner!");
//            return;
//        }

//        gameManager = FindObjectOfType<GameManager>();
//        if (gameManager == null)
//        {
//            Debug.LogError("GameManager not found in scene!");
//        }

//        // Verify player setup
//        if (!player.CompareTag("Player"))
//        {
//            Debug.LogWarning("Player GameObject does not have the 'Player' tag! Please set it in the Inspector.");
//        }
//        if (player.GetComponent<Collider2D>() == null)
//        {
//            Debug.LogError("Player GameObject is missing a 2D Collider!");
//        }
//        if (player.GetComponent<Rigidbody2D>() == null)
//        {
//            Debug.LogWarning("Player GameObject has no Rigidbody2D. Adding one with kinematic settings.");
//            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
//            rb.isKinematic = true; // Kinematic for trigger-only collisions
//        }

//        currentXInterval = initialXInterval;
//        StartCoroutine(SpawnRewardsEndlessly());
//    }

//    void Update()
//    {
//        // Increase difficulty over time
//        difficultyFactor = Mathf.Clamp01(difficultyFactor + difficultyIncreaseRate * Time.deltaTime);
//        currentXInterval = Mathf.Lerp(initialXInterval, minXInterval, difficultyFactor);
//    }

//    IEnumerator SpawnRewardsEndlessly()
//    {
//        while (true)
//        {
//            yield return new WaitUntil(() => ShouldSpawnReward());
//            SpawnReward();
//        }
//    }

//    bool ShouldSpawnReward()
//    {
//        return player.transform.position.x >= lastSpawnX + currentXInterval;
//    }

//    void SpawnReward()
//    {
//        // Calculate spawn position
//        float spawnX = player.transform.position.x + currentXInterval * 0.5f; // Spawn halfway to next interval
//        float cameraHeight = mainCamera.orthographicSize * 2f;
//        float cameraWidth = cameraHeight * mainCamera.aspect;
//        float cameraY = mainCamera.transform.position.y;
//        float minY = cameraY - cameraHeight / 2f + 1f; // Leave some margin
//        float maxY = cameraY + cameraHeight / 2f - 1f;
//        float spawnY = Random.Range(minY, maxY);

//        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

//        // Spawn the reward
//        GameObject reward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity, transform);
//        Reward rewardComponent = reward.AddComponent<Reward>();
//        rewardComponent.Initialize(rewardValue, gameManager);

//        // Ensure reward has a trigger collider
//        Collider2D rewardCollider = reward.GetComponent<Collider2D>();
//        if (rewardCollider == null)
//        {
//            Debug.LogWarning("Reward prefab has no Collider2D! Adding a CircleCollider2D.");
//            rewardCollider = reward.AddComponent<CircleCollider2D>();
//        }
//        if (!rewardCollider.isTrigger)
//        {
//            Debug.LogWarning("Reward prefab's Collider2D is not set to Trigger! Setting it now.");
//            rewardCollider.isTrigger = true;
//        }

//        lastSpawnX = spawnX;
//    }
//}

//public class Reward : MonoBehaviour
//{
//    private float xpValue;
//    private GameManager gameManager;

//    public void Initialize(float value, GameManager gm)
//    {
//        xpValue = value;
//        gameManager = gm;
//        Debug.Log($"Reward initialized with XP value: {xpValue}");
//    }

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        Debug.Log($"Trigger entered by: {other.gameObject.name}, Tag: {other.gameObject.tag}");
//        if (other.gameObject.CompareTag("Player"))
//        {
//            Debug.Log("Reward collected by player!");
//            if (gameManager != null)
//            {
//                gameManager.AddXP(xpValue);
//                Debug.Log($"Added {xpValue} XP to GameManager. Total XP: {gameManager.GetTotalXP()}");
//            }
//            else
//            {
//                Debug.LogError("GameManager reference is null in Reward!");
//            }
//            Destroy(gameObject); // Remove reward after collection
//        }
//    }
//}

using UnityEngine;
using System.Collections;

public class RewardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rewardPrefab;        // The reward prefab to spawn
    [SerializeField] private GameObject player;              // Reference to the player
    [SerializeField] private Camera mainCamera;              // Reference to the main camera
    [SerializeField] private float initialXInterval = 100f;  // Initial X distance between rewards
    [SerializeField] private float minXInterval = 50f;       // Minimum X interval at max difficulty
    [SerializeField] private float difficultyIncreaseRate = 0.01f; // How fast difficulty increases
    [SerializeField] private int rewardValue = 50;        // XP value per reward collected

    private float currentXInterval;
    private float lastSpawnX = 0f;
    private float difficultyFactor = 0f;
    private GameManager gameManager;

    void Start()
    {
        if (player == null || mainCamera == null)
        {
            Debug.LogError("Player or Main Camera reference not set in RewardSpawner!");
            return;
        }

        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in scene!");
            return;
        }

        // Player setup validation
        ValidatePlayerSetup();

        currentXInterval = initialXInterval;
        StartCoroutine(SpawnRewardsEndlessly());
    }

    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
        ValidatePlayerSetup();
    }

    void ValidatePlayerSetup()
    {
        //Debug.Log($"Validating player setup: {player.name}");
        if (!player.CompareTag("Player"))
        {
            //Debug.LogWarning("Player does not have 'Player' tag! Setting it now.");
            player.tag = "Player";
        }

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
        {
            //Debug.LogWarning("Player missing Collider2D! Adding BoxCollider2D.");
            playerCollider = player.AddComponent<BoxCollider2D>();
        }
        //Debug.Log($"Player Collider: {playerCollider.GetType().Name}, IsTrigger: {playerCollider.isTrigger}");

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null)
        {
            Debug.LogWarning("Player missing Rigidbody2D! Adding kinematic Rigidbody2D.");
            playerRb = player.AddComponent<Rigidbody2D>();
            playerRb.isKinematic = true;
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        //Debug.Log($"Player Rigidbody2D: IsKinematic: {playerRb.isKinematic}, BodyType: {playerRb.bodyType}");
    }

    void Update()
    {
        difficultyFactor = Mathf.Clamp01(difficultyFactor + difficultyIncreaseRate * Time.deltaTime);
        currentXInterval = Mathf.Lerp(initialXInterval, minXInterval, difficultyFactor);
    }

    IEnumerator SpawnRewardsEndlessly()
    {
        while (true)
        {
            yield return new WaitUntil(() => ShouldSpawnReward());
            SpawnReward();
        }
    }

    bool ShouldSpawnReward()
    {
        return player.transform.position.x >= lastSpawnX + currentXInterval;
    }

    void SpawnReward()
    {
        float spawnX = player.transform.position.x + currentXInterval * 0.5f;
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        float cameraY = mainCamera.transform.position.y;
        float minY = cameraY - cameraHeight / 2f + 1f;
        float maxY = cameraY + cameraHeight / 2f - 1f;
        float spawnY = Random.Range(minY, maxY);

        Vector3 spawnPosition = new Vector3(spawnX, spawnY, 0f);

        GameObject reward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity, transform);
        Reward rewardComponent = reward.AddComponent<Reward>();
        rewardComponent.Initialize(rewardValue, gameManager);

        // Reward setup validation
        Collider2D rewardCollider = reward.GetComponent<Collider2D>();
        if (rewardCollider == null)
        {
            //Debug.LogWarning($"Reward at {spawnPosition} missing Collider2D! Adding CircleCollider2D.");
            rewardCollider = reward.AddComponent<CircleCollider2D>();
        }
        if (!rewardCollider.isTrigger)
        {
            //Debug.LogWarning($"Reward at {spawnPosition} Collider2D not set to Trigger! Setting it now.");
            rewardCollider.isTrigger = true;
        }
        //Debug.Log($"Spawned reward at {spawnPosition} with Collider: {rewardCollider.GetType().Name}, IsTrigger: {rewardCollider.isTrigger}");

        lastSpawnX = spawnX;
    }
}

public class Reward : MonoBehaviour
{
    private int xpValue;
    private GameManager gameManager ;

    public void Initialize(int value, GameManager gm)
    {
        xpValue = value;
        gameManager = gm;
        //Debug.Log($"Reward initialized at {transform.position} with XP value: {xpValue}");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log($"Trigger detected at {transform.position} with: {other.gameObject.name} (Tag: {other.gameObject.tag})");
        if (other.gameObject.CompareTag("Player"))
        {
            //Debug.Log($"Player collected reward at {transform.position}!");
            if (gameManager != null)
            {
                gameManager.AddXP(xpValue);
                //Debug.Log($"Added {xpValue} XP. Total XP: {gameManager.GetTotalXP()}");
            }
            else
            {
                Debug.LogError("GameManager is null in Reward!");
            }
            Destroy(gameObject);
        }
    }


}