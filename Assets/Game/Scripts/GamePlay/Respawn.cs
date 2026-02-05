using System.Linq;
using UnityEngine;

public class Respawn : MonoBehaviour
{

	private void OnTriggerEnter(Collider other)
	{
        // Check if it's a Player
        if (other.TryGetComponent<Player>(out Player player))
        {
             PlayerSpawnner.Instance.RespawnCharacter(other.gameObject, PlayerSpawnner.Instance.playerPrefab);
             return;
        }

        // Check if it's an AI
        if (other.TryGetComponent<BotController>(out BotController ai))
        {
             PlayerSpawnner.Instance.RespawnCharacter(other.gameObject, PlayerSpawnner.Instance.botPrefab);
             return;
        }
	}
}
