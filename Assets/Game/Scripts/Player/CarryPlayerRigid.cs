using UnityEngine;

public class CarryPlayerRigid : MonoBehaviour
{
	public bool inBody;

	private void Start()
	{
	}

	private void Update()
	{
		if (base.gameObject.layer == LayerMask.NameToLayer("Player") && !(base.gameObject.GetComponent<Movement>() == null) && base.gameObject.GetComponent<Movement>().beingHit)
		{
			base.transform.SetParent(null, worldPositionStays: true);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (base.gameObject.layer == LayerMask.NameToLayer("Player") /*&& base.gameObject.GetComponent<PhotonView>().IsMine*/ && collision.gameObject.tag == "Platform")
		{
			if (!inBody)
			{
				base.transform.SetParent(collision.transform, worldPositionStays: true);
			}
			else
			{
				base.transform.parent.transform.SetParent(collision.transform, worldPositionStays: true);
			}
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		if (base.gameObject.layer == LayerMask.NameToLayer("Player") && collision.gameObject.tag == "Platform")
		{
			if (!inBody)
			{
				base.transform.SetParent(null, worldPositionStays: true);
			}
			else
			{
				base.transform.parent.transform.SetParent(null, worldPositionStays: true);
			}
		}
	}
}
