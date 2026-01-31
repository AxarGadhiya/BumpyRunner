using UnityEngine;

public class Trampoline : MonoBehaviour
{
	public float BouncingForce = 2f;

	private void OnCollisionEnter(Collision other)
	{
		if (/*other.gameObject.GetComponent<PhotonView>().IsMine ||*/ other.gameObject.layer == LayerMask.NameToLayer("Bot"))
		{
			other.gameObject.GetComponent<Rigidbody>().AddForce(base.transform.up * BouncingForce);
		}
	}
}
