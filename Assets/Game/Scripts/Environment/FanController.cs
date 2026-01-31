using UnityEngine;

public class FanController : MonoBehaviour
{
	public float speed = 200f;

	public bool right;

	public Vector3 normalPos;

	public Vector3 hitWallPos;

	public bool useBot;

	public LayerMask layerMask;

	private void Start()
	{
	}

	private void Update()
	{
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.tag == "Player")
		{
			if (useBot && other.gameObject.layer == LayerMask.NameToLayer("Bot"))
			{
				return;
			}
			if (right)
			{
				other.gameObject.GetComponent<Rigidbody>().AddForce(base.transform.right * speed, ForceMode.Force);
			}
			else
			{
				other.gameObject.GetComponent<Rigidbody>().AddForce(-base.transform.right * speed, ForceMode.Force);
			}
		}
		if ((layerMask.value & (1 << other.transform.gameObject.layer)) > 0)
		{
			base.transform.localPosition = new Vector3(hitWallPos.x, hitWallPos.y, hitWallPos.z);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((layerMask.value & (1 << other.transform.gameObject.layer)) > 0)
		{
			base.transform.localPosition = new Vector3(normalPos.x, normalPos.y, normalPos.z);
		}
	}
}
