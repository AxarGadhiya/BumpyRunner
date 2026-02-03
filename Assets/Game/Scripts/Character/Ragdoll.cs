using UnityEngine;

public class Ragdoll : MonoBehaviour
{
	public Collider[] colBody;

	public Collider[] mainCols;

	public Rigidbody[] rigidBody;

	public Animator anim;

	public bool ragdoll;

	public Rigidbody mainRb;

	private bool arrayComplete;

	private void Awake()
	{
	}

	private void Start()
	{
		//if (arrayComplete)
		//{
			DisableRagdoll();
		//}
	}

	private void Update()
	{
			arrayComplete = true;
		//	if (!Input.GetKeyDown(KeyCode.G))
		//	{
		//		Input.GetKeyDown(KeyCode.T);
		//	}
	}

	public void EnableRagdoll()
	{
		Debug.Log("<color=cyan>Enable Ragdoll</color>");

		if (base.transform.parent.gameObject.layer == LayerMask.NameToLayer("Player")&&!ragdoll)
		{
			ragdoll = true;
			anim.SetBool("STOPALL", value: true);
			anim.enabled = false;
			Rigidbody[] array = rigidBody;
			foreach (Rigidbody obj in array)
			{
				obj.freezeRotation = false;
				obj.constraints = RigidbodyConstraints.None;
				obj.mass = 0.5f;
			}
			Collider[] array2 = mainCols;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = false;
			}
			array2 = colBody;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].enabled = true;
			}
			mainRb.freezeRotation = false;
		}
	}

	public void DisableRagdoll()
	{
      Debug.Log("<color=cyan>Disable Ragdoll</color>");
        ragdoll = false;
		anim.enabled = true;
		anim.SetBool("STOPALL", value: false);
		Rigidbody[] array = rigidBody;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].mass = 0f;
		}
		Collider[] array2 = mainCols;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = true;
		}
		array2 = colBody;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].enabled = false;
		}
		mainRb.freezeRotation = true;
	}
}
