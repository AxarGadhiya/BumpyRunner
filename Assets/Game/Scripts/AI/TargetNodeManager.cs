using System.Collections.Generic;
using UnityEngine;

public class TargetNodeManager : MonoBehaviour
{
    public static TargetNodeManager Instance { get; private set; }

   [SerializeField] private List<TargetNode> allNodes = new List<TargetNode>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

    }



    // -----------------------------
    // NEAREST TARGET
    // -----------------------------
    public TargetNode GetNearestTarget(Vector3 position, bool onlyStartTargets)
    {
        TargetNode best = null;
        float minDist = float.MaxValue;

        foreach (var node in allNodes)
        {
            if (onlyStartTargets && !node.canBeStartTarget)
                continue;

            float d = Vector3.SqrMagnitude(node.transform.position - position);
            if (d < minDist)
            {
                minDist = d;
                best = node;
            }
        }

        return best;
    }

    // -----------------------------
    // NEXT TARGET SELECTION (Now returns Connection to include Action)
    // -----------------------------
    public TargetConnection GetNextConnection(TargetNode current, float intelligence)
    {
        if (current == null || current.nextTargets == null || current.nextTargets.Length == 0)
            return null;

        // Higher intelligence → prefers lower cost
        TargetConnection chosen = null;
        float bestScore = float.MinValue;

        foreach (var connection in current.nextTargets)
        {
            if (connection == null || connection.node == null) continue;

            // intelligence 0 → random
            // intelligence 1 → lowest cost
            float randomness = Random.Range(0f, 1f) * (1f - intelligence);
            float score = (1f - connection.cost) * intelligence + randomness;

            if (score > bestScore)
            {
                bestScore = score;
                chosen = connection;
            }
        }

        return chosen;
    }
}

