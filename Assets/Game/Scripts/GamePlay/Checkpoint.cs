using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check for Player component
        if (other.TryGetComponent<Player>(out Player player))
        {
            CheckPointManager.Instance.RegisterCheckpoint(other.gameObject.GetInstanceID(), transform);
            return;
        }

        // Check for AI component
        if (other.TryGetComponent<AI>(out AI ai))
        {
            CheckPointManager.Instance.RegisterCheckpoint(other.gameObject.GetInstanceID(), transform);
            return;
        }
    }
}
