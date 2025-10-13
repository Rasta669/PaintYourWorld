
using UnityEngine;

public class FixedXObstacleSpawner2D : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject obstaclePrefab;      // Prefab to spawn
    public Transform player;               // Player (only used once)
    public float firstSpawnXDistance = 10f;// How far from player to place first obstacle
    public float spawnYHeight = 0f;        // Fixed height for all spawns
    public float spawnXSpacing = 5f;       // Distance between each obstacle
    public float spawnInterval = 2f;       // Time between spawns
    public float obstacleLifetime = 60f;   // How long each obstacle lasts in seconds

    private float nextSpawnX;              // X position where next obstacle will spawn
    private bool hasSpawnedFirst = false;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0f;
        }
    }

    void SpawnObstacle()
    {
        if (!hasSpawnedFirst)
        {
            if (player == null || obstaclePrefab == null) return;

            // First spawn based on player position
            nextSpawnX = player.position.x + firstSpawnXDistance;
            hasSpawnedFirst = true;
        }

        Vector2 spawnPosition = new Vector2(nextSpawnX, spawnYHeight);
        GameObject spawnedObstacle = Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity);

        // Destroy the obstacle after a specified lifetime
        Destroy(spawnedObstacle, obstacleLifetime);

        // Prepare next X spawn point
        nextSpawnX += spawnXSpacing;
    }


    public void SetPlayer(Transform newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogError("Cannot set player: newPlayer is null");
            return;
        }

        player = newPlayer;
        hasSpawnedFirst = false; // Reset first spawn
        nextSpawnX = 0f;

        Debug.Log($"ObstacleSpawner player set to {player.name}");
    }
}
