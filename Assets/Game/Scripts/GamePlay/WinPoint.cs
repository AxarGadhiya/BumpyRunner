using UnityEngine;

public class WinPoint : MonoBehaviour
{
    private bool disable;

    private void OnTriggerEnter(Collider other)
    {
        if (disable) return;

        // Check for Player component
        if (other.TryGetComponent<Player>(out Player player))
        {
            GameManager.Instance.GameWin();
            disable = true;
            return;
        }

        // Check for AI component
        if (other.TryGetComponent<AI>(out AI ai))
        {
            GameManager.Instance.GameLoose();
            disable = true;
            return;
        }
    }
}
