using UnityEngine;

public class TargetPointsAI : MonoBehaviour
{
	public Transform singleTarget;

	public Transform[] multipleTarget;

	public bool singleTargetOnly = true;

	public bool isThisFinal;

	private void OnTriggerEnter(Collider other)
	{
		if (!isThisFinal && other.gameObject.layer == LayerMask.NameToLayer("Bot"))
		{
			if (singleTargetOnly)
			{
				other.gameObject.GetComponent<AIController>().target = singleTarget;
				return;
			}
			int num = Random.Range(0, multipleTarget.Length);
			other.gameObject.GetComponent<AIController>().target = multipleTarget[num];
		}
	}
}
