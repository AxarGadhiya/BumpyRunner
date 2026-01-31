using UnityEngine;

public class StepClimb : MonoBehaviour
{
	private Rigidbody rigidBody;

	[SerializeField]
	private GameObject stepRayUpper;

	[SerializeField]
	private GameObject stepRayLower;

	[SerializeField]
	private float stepHeight = 0.3f;

	[SerializeField]
	private float stepSmooth = 2f;

	private void Awake()
	{
		rigidBody = GetComponent<Rigidbody>();
		stepRayUpper.transform.position = new Vector3(stepRayUpper.transform.position.x, stepHeight, stepRayUpper.transform.position.z);
	}

	private void FixedUpdate()
	{
		stepClimb();
	}

	private void stepClimb()
	{
		if (Physics.Raycast(stepRayLower.transform.position, base.transform.TransformDirection(Vector3.forward), out var _, 0.1f) && !Physics.Raycast(stepRayUpper.transform.position, base.transform.TransformDirection(Vector3.forward), out var _, 0.2f))
		{
			rigidBody.position -= new Vector3(0f, (0f - stepSmooth) * Time.deltaTime, 0f);
		}
		if (Physics.Raycast(stepRayLower.transform.position, base.transform.TransformDirection(1.5f, 0f, 1f), out var _, 0.1f) && !Physics.Raycast(stepRayUpper.transform.position, base.transform.TransformDirection(1.5f, 0f, 1f), out var _, 0.2f))
		{
			rigidBody.position -= new Vector3(0f, (0f - stepSmooth) * Time.deltaTime, 0f);
		}
		if (Physics.Raycast(stepRayLower.transform.position, base.transform.TransformDirection(-1.5f, 0f, 1f), out var _, 0.1f) && !Physics.Raycast(stepRayUpper.transform.position, base.transform.TransformDirection(-1.5f, 0f, 1f), out var _, 0.2f))
		{
			rigidBody.position -= new Vector3(0f, (0f - stepSmooth) * Time.deltaTime, 0f);
		}
	}
}
