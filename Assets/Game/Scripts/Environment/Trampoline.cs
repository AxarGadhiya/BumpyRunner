using UnityEngine;

public class Trampoline : MonoBehaviour
{
	public float BouncingForce = 2f;

	private void OnCollisionEnter(Collision other)
	{
		if (/*other.gameObject.GetComponent<PhotonView>().IsMine ||*/other.gameObject.layer == LayerMask.NameToLayer("Player")|| other.gameObject.layer == LayerMask.NameToLayer("Bot"))
		{
			// Apply bounce force
			other.gameObject.GetComponent<Rigidbody>().AddForce(transform.up * BouncingForce);

			// If this is a player currently diving, stop the dive animation when hitting the trampoline
			Movement movement = other.gameObject.GetComponent<Movement>();
			if (movement != null && movement.anim != null && movement.anim.GetBool("DiveStart"))
			{
				movement.anim.SetBool("DiveStart", false);
			}
		}
	}
}
