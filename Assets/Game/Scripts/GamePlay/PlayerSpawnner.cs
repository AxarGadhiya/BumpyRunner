using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnner : MonoBehaviour
{
    public static PlayerSpawnner Instance;

   public PlayerInput playerInput;
    
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject botPrefab;

    [Header("Settings")]
    public Transform[] spawnPoints;
    public int botCount = 5;
    public int maxRespawns = 3;

    // Simple tracker for the local player's used lives
    private int currentRespawns = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnCharacters()
    {
        currentRespawns = 0; // Reset on spawn (if called on restart)

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned in PlayerSpawnner!");
            return;
        }

        // Spawn Player at index 0
        SpawnCharacter(playerPrefab, spawnPoints[0].position, spawnPoints[0].rotation, true);

        // Spawn Bots at remaining points
        int pointsUsed = 1;
        for (int i = 0; i < botCount; i++)
        {
            if (pointsUsed >= spawnPoints.Length) break;
            SpawnCharacter(botPrefab, spawnPoints[pointsUsed].position, spawnPoints[pointsUsed].rotation, false);
            pointsUsed++;
        }
    }

    private void SpawnCharacter(GameObject prefab, Vector3 position, Quaternion rotation, bool isPlayer)
    {
        if (prefab == null) return;
        Instantiate(prefab, position, rotation);
    }

    public void RespawnCharacter(GameObject oldInstance, GameObject originalPrefab)
    {
        bool isPlayer = oldInstance.GetComponent<Player>() != null;

        // --- Respawn Limit Logic for Player ---
        if (isPlayer)
        {
            if (currentRespawns >= maxRespawns)
            {
                Debug.Log($"GAME OVER! Player has run out of lives ({currentRespawns}/{maxRespawns}).");
                Destroy(oldInstance);
                
                // Trigger Game Over logic
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GameLoose();
                }
                else
                {
                    Debug.LogWarning("GameManager.Instance is null, cannot call GameLoose().");
                }
                return; // Stop respawn
            }

            currentRespawns++;
            Debug.Log($"Respawning Player. Lives used: {currentRespawns}/{maxRespawns}");
        }
        // ---------------------------------------

        // Get Checkpoint
        int oldId = oldInstance.GetInstanceID();
        Transform lastCheckpoint = CheckPointManager.Instance.GetLastCheckpoint(oldId);

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (lastCheckpoint != null)
        {
            spawnPos = lastCheckpoint.position;
            spawnRot = lastCheckpoint.rotation;
        }
        else
        {
            // Fallback to initial spawn point
             if (spawnPoints != null && spawnPoints.Length > 0)
            {
                spawnPos = spawnPoints[0].position;
                spawnRot = spawnPoints[0].rotation;
            }
        }

        // Destroy and Respawn
        Destroy(oldInstance);

        GameObject newInstance = Instantiate(originalPrefab, spawnPos, spawnRot);
        
        // Transfer Checkpoint info to new instance
        if (lastCheckpoint != null)
        {
            CheckPointManager.Instance.RegisterCheckpoint(newInstance.GetInstanceID(), lastCheckpoint);
        }
    }
}
