using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    public static CheckPointManager Instance;

    // Dictionary to track the last checkpoint for each player/bot InstanceID
    private Dictionary<int, Transform> lastCheckpoints = new Dictionary<int, Transform>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterCheckpoint(int playerId, Transform checkpoint)
    {
        if (lastCheckpoints.ContainsKey(playerId))
        {
            lastCheckpoints[playerId] = checkpoint;
        }
        else
        {
            lastCheckpoints.Add(playerId, checkpoint);
        }
        // Optional: Debug log
        // Debug.Log($"Checkpoint registered for {playerId} at {checkpoint.name}");
    }

    public Transform GetLastCheckpoint(int playerId)
    {
        if (lastCheckpoints.ContainsKey(playerId))
        {
            return lastCheckpoints[playerId];
        }
        return null;
    }
}
