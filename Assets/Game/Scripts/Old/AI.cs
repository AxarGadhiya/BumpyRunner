using System.Collections;
using UnityEngine;

/// <summary>
/// AI-specific logic: follow target, jump over gaps, jump-dive over walls, wall avoidance.
/// Use with CharacterController on the same GameObject. Does not replace AIController.cs.
/// </summary>
public class AI : MonoBehaviour
{
	//[Header("References")]
	//[SerializeField] private CharacterController characterController;
	//[SerializeField] private CharacterAnimator characterAnimator;
	//[SerializeField] private Transform sensorRaycast;

	//[Header("Target")]
	//public Transform target;
	////public Transform respawnPoint;


	//private bool hitRight;
	//private bool hitLeft;
	//private RaycastHit hitInfo;

	//private void Start()
	//{
	//	if (characterController == null) characterController = GetComponent<CharacterController>();
	//	if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
	//	if (sensorRaycast == null && transform.childCount > 3) sensorRaycast = transform.GetChild(3);
	//	if (target == null && GameObject.FindGameObjectWithTag("TargetBot") != null)
	//		target = GameObject.FindGameObjectWithTag("TargetBot").transform;

	//	if (characterController != null)
	//	{
	//		characterController.useCounterMovement = false;
	//		characterController.useWallAvoidance = true;
	//		characterController.SetMovementOrientation(null);
	//	}
	//}

	//private void Update()
	//{
	//	if (gameObject.layer == LayerMask.NameToLayer("Bot") && transform.childCount > 1)
	//		transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("Bot");


	//	if (target != null)
	//		FollowTargetWithRotation(target, 0f, 0f);

	//	MyInput();
	//	CheckWall();
	//}

	//private void MyInput()
	//{
	//	if (characterController == null) return;
	//	characterController.SetInput(0f, 1f);

	//	// Single path: animation only via CharacterAnimator when assigned, else Animator
	//	if (characterAnimator != null)
	//	{
	//		if (characterController.Jumping) characterAnimator.SetJump(true);
	//		if (characterController.readyToJump && !characterController.grounded) characterAnimator.SetFall(true);
	//		else if (characterController.grounded) characterAnimator.SetFall(false);
	//		if (characterController.canMove)
	//		{
	//			characterAnimator.SetIncline(!characterController.OnSlope());
	//			if (!characterController.isJumping && characterController.grounded)
	//				characterAnimator.SetVertical(1f);
	//		}
	//	}
	//	else if (characterController.Anim != null)
	//	{
	//		if (characterController.Jumping) characterController.Anim.SetBool("Jump", true);
	//		if (characterController.readyToJump && !characterController.grounded) characterController.Anim.SetBool("Fall", true);
	//		else if (characterController.grounded) characterController.Anim.SetBool("Fall", false);
	//		if (characterController.canMove)
	//		{
	//			characterController.Anim.SetBool("Incline", !characterController.OnSlope());
	//			if (!characterController.isJumping && characterController.grounded)
	//				characterController.Anim.SetFloat("vertical", 1f, 0.05f, Time.deltaTime);
	//		}
	//	}
	//}

	//private void FollowTargetWithRotation(Transform t, float distanceToStop, float speed)
	//{
	//	if (t == null) return;
	//	if (Vector3.Distance(transform.position, t.position) <= distanceToStop) return;
	//	Vector3 forward = t.position - transform.position;
	//	forward.y = 0f;
	//	Quaternion rot = Quaternion.LookRotation(forward);
	//	transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * 5f);
	//}

	//private void CheckWall()
	//{
	//	if (characterController == null) return;

	//	if (sensorRaycast != null && !Physics.Raycast(sensorRaycast.position, -sensorRaycast.up, out hitInfo, 5f) && characterController.grounded)
	//		characterController.Jump();

	//	if (transform.childCount > 3)
	//	{
	//		Transform child = transform.GetChild(3);
	//		if (Physics.Raycast(child.position, child.forward, out hitInfo, 1f) && characterController.grounded &&
	//		    hitInfo.transform.gameObject.tag != "Player" && hitInfo.transform.gameObject.tag != "TargetBot")
	//			StartCoroutine(JumpAndDive());

	//		if (Physics.Raycast(child.position, child.right, out hitInfo, 1f))
	//			hitRight = characterController.grounded;
	//		else
	//			hitRight = false;

	//		if (Physics.Raycast(child.position, -child.right, out hitInfo, 1f))
	//			hitLeft = characterController.grounded;
	//		else
	//			hitLeft = false;
	//	}
	//	else
	//	{
	//		hitRight = false;
	//		hitLeft = false;
	//	}

	//	characterController.SetWallHits(hitLeft, hitRight);
	//}

	//private IEnumerator JumpAndDive()
	//{
	//	characterController.Jump();
	//	yield return new WaitForSeconds(0.15f);
	//	characterController.SetDive(true);
	//}
}
