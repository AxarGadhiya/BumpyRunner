using System;
using UnityEngine;

public enum ConnectionAction { Walk, Jump, WaitJump }



public class TargetNode : MonoBehaviour
{

    [Header("Target Settings")]
    public bool canBeStartTarget = true;

    public TargetConnection[] nextTargets;

    public float reachRadius = 0.6f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        if (nextTargets == null) return;
        Gizmos.color = Color.cyan;
        foreach (var connection in nextTargets)
        {
            if (connection != null && connection.node != null)
                Gizmos.DrawLine(transform.position, connection.node.transform.position);
        }
    }
}

