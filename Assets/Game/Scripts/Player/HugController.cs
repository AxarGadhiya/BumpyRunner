using UnityEngine;

public class HugController : MonoBehaviour
{
	//public PhotonView view;

	public GameObject objHit;

	public Transform lookHit;

	public bool canHug = true;

	public Animator anim;

	public bool isHuging;

	private bool cancelGrab;

	private int jumpTimes;

	private bool addComponent;

	private void Start()
	{
	}

	private void Update()
	{
		if (/*!view.IsMine ||*/ !canHug || base.gameObject.layer == LayerMask.NameToLayer("Bot"))
		{
			return;
		}
		if (Input.GetMouseButton(0) && SystemInfo.deviceType == DeviceType.Desktop)
		{
			//if (!PhotonNetwork.IsConnected)
			//{
				StartHug();
			//}
			//else
			//{
			//	view.RPC("StartHug", RpcTarget.AllBuffered, null);
			//}
		}
		else if (Input.GetMouseButtonUp(0))
		{
			//if (!PhotonNetwork.IsConnected)
			//{
				StopHug();
			//}
			//else
			//{
			//	view.RPC("StopHug", RpcTarget.AllBuffered, null);
			//}
		}
		CheckConditions();
	}

	public void StartHugMobile()
	{
		//if (!PhotonNetwork.IsConnected)
		//{
			StartHug();
		//}
		//else
		//{
		//	view.RPC("StartHug", RpcTarget.AllBuffered, null);
		//}
	}

	public void StopHugMobile()
	{
		//if (!PhotonNetwork.IsConnected)
		//{
			StopHug();
		//}
		//else
		//{
		//	view.RPC("StopHug", RpcTarget.AllBuffered, null);
		//}
	}

	private void CheckConditions()
	{
		if (base.gameObject.GetComponent<CharacterJoint>() != null && !isHuging)
		{
			//if (!PhotonNetwork.IsConnected)
			//{
				StopHug();
			//}
			//else
			//{
			//	view.RPC("StopHug", RpcTarget.AllBuffered, null);
			//}
		}
		if (objHit != null && objHit.GetComponent<Movement>().isJumping && !cancelGrab)
		{
			jumpTimes++;
		}
		if (jumpTimes > 3 && !cancelGrab)
		{
			//view.RPC("StopHug", RpcTarget.AllBuffered, null);
			StopHug();

            anim.SetTrigger("ForceStopHug");
			cancelGrab = true;
			jumpTimes = 0;
		}
	}

	//---------------[PunRPC]-------------
	private void StartHug()
	{
		if (!base.gameObject.GetComponent<Movement>().canDive || !base.gameObject.GetComponent<Movement>().grounded)
		{
			return;
		}
		cancelGrab = false;
		anim.SetBool("isHuging", value: true);
		if (!Physics.Raycast(lookHit.position, lookHit.transform.forward, out var hitInfo, 0.75f) || isHuging)
		{
			return;
		}
		objHit = hitInfo.transform.gameObject;
		if (objHit.tag == "Player")
		{
			isHuging = true;
			Quaternion rotation = Quaternion.LookRotation(objHit.transform.position - base.transform.position);
			base.transform.rotation = rotation;
			if (!addComponent)
			{
				base.gameObject.AddComponent<CharacterJoint>().connectedBody = objHit.GetComponent<Rigidbody>();
				addComponent = true;
			}
			objHit.GetComponent<Movement>().canMove = false;
		}
		else if (objHit.tag == "CanGrab")
		{
			Debug.Log("IM GRABBING A OBJECT");
			base.gameObject.AddComponent<CharacterJoint>().connectedBody = objHit.GetComponent<Rigidbody>();
		}
	}

    //---------------[PunRPC]-------------
    public void StopHug()
	{
		anim.SetBool("isHuging", value: false);
		isHuging = false;
		if (base.gameObject.GetComponent<CharacterJoint>() != null)
		{
			if (objHit.GetComponent<Movement>() != null)
			{
				objHit.GetComponent<Movement>().canMove = true;
			}
			Object.Destroy(base.gameObject.GetComponent<CharacterJoint>());
			addComponent = false;
		}
	}
}
