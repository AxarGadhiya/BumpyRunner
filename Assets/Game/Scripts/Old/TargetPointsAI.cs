using UnityEngine;

public class TargetPointsAI : MonoBehaviour
{
	//public Transform singleTarget;

	//public Transform[] multipleTarget;

	//public bool singleTargetOnly = true;

	//public bool isThisFinal;

	//private void OnTriggerEnter(Collider other)
	//{
	//	if (!isThisFinal && other.gameObject.layer == LayerMask.NameToLayer("Bot"))
	//	{
	//		Transform nextTarget = singleTargetOnly ? singleTarget : multipleTarget[Random.Range(0, multipleTarget.Length)];
	//		var oldAI = other.gameObject.GetComponent<AIController>();
	//		if (oldAI != null)
	//			oldAI.target = nextTarget;
	//		else
	//		{
	//			var newAI = other.gameObject.GetComponent<AI>();
	//			if (newAI != null)
	//				newAI.target = nextTarget;
	//		}
	//	}
	//}
}
