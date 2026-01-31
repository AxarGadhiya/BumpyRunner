using NUnit.Framework.Constraints;
using UnityEngine;

public class Bounce : MonoBehaviour
{
    [SerializeField] float force = 10f;

    [SerializeField] float stunTime = 0.5f;

	private Vector3 hitDir;

    [SerializeField] bool activeRagdoll = true;

	[SerializeField] string debugName;

	private void OnCollisionEnter(Collision collision)
	{
		Debug.Log("Collision ENter name=>" + collision.gameObject.name + " With " + debugName);

		if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			if (activeRagdoll)
			{
				if (string.IsNullOrEmpty(debugName))
				{
					debugName = gameObject.name;
				}

                Debug.Log($"<color=yellow>Enter Collision game object </color>{collision.gameObject.name} with {debugName}");

                collision.gameObject.GetComponentInChildren<Animator>().SetBool("STOPALL", value: true);
				collision.gameObject.GetComponent<Movement>().GettingHit();
			}
			Vector3 vector = hitDir;
			Debug.Log("HITTIBNG FORCE" + vector.ToString());
		}
	}

	private void OnCollisionExit(Collision collision)
	{
        Debug.Log("Collision Exit name=>" + collision.gameObject.name+" With "+debugName);
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			if (activeRagdoll)
			{
                if (string.IsNullOrEmpty(debugName))
                {
                    debugName = gameObject.name;
                }

                Debug.Log($"<color=yellow> Enter Collision game object </color>{collision.gameObject.name} with {debugName}");

				collision.gameObject.GetComponentInChildren<Animator>().SetBool("STOPALL", value: false);
			}
		}
	}
}
