using System;
using UnityEngine;

[Serializable]
public class TargetConnection
{
    public TargetNode node;
    [Range(0f, 1f)]
    public float cost = 0.5f;
    public ConnectionAction action = ConnectionAction.Walk;
}
