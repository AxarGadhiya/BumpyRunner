using UnityEngine;

/// <summary>
/// Player-specific input and camera-relative control.
/// Use with CharacterController on the same GameObject. Does not replace Movement.cs.
/// </summary>
public class Player : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CharacterController characterController;
	[SerializeField] private CharacterAnimator characterAnimator;
	[SerializeField] private PlayerInput playerInput;
	[SerializeField] private FixedJoystick fixedJoystick;

	[Header("Options")]
	public bool disableOnAwake = true;
	[SerializeField] private bool requireForwardInputForAirDive = true;
	[SerializeField] private float airDiveForwardInputThreshold = 0.1f;
	[SerializeField] private float airDiveMinForwardSpeed = 1f;

	private float turnSmoothVelocity;
	private float horizontal;
	private float vertical;
	private Vector3 inputVector;
	private float targetAngle;
	private float angle;
	private bool getUpAnim;

	private void Start()
	{
		if (characterController == null) characterController = GetComponent<CharacterController>();
		if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
		if (playerInput == null) playerInput = GetComponent<PlayerInput>();
		if (fixedJoystick == null && GameObject.FindGameObjectWithTag("JoyStick") != null)
			fixedJoystick = GameObject.FindGameObjectWithTag("JoyStick").GetComponent<FixedJoystick>();

		if (characterController != null)
		{
			characterController.useCounterMovement = true;
			characterController.useWallAvoidance = false;
			characterController.SetMovementOrientation(Camera.main != null ? Camera.main.transform : transform);
		}
		if (playerInput != null)
			playerInput.OnJumpInput += HandleJumpInput;
	}

	private void OnDestroy()
	{
		if (playerInput != null)
			playerInput.OnJumpInput -= HandleJumpInput;
	}

	private void HandleJumpInput()
	{
		if (characterController == null) return;
		if (!characterController.grounded)
		{
			if (!characterController.hasJumped) return;
			if (requireForwardInputForAirDive && !HasForwardInput()) return;
			characterController.DiveInMovementDirection();
		}
		else if (characterController.readyToJump)
			characterController.Jump();
	}

	private bool HasForwardInput()
	{
		float forwardIntent = playerInput != null ? playerInput.Vertical : vertical;
		if (forwardIntent <= airDiveForwardInputThreshold) return false;
		if (characterController == null || characterController.Rb == null) return false;
		Vector3 horizontalVel = characterController.Rb.linearVelocity;
		horizontalVel.y = 0f;
		float forwardSpeed = Vector3.Dot(horizontalVel, transform.forward);
		return forwardSpeed >= airDiveMinForwardSpeed;
	}

	private void Update()
	{
		if (disableOnAwake) return;

		if (characterController != null)
			characterController.SetDive(playerInput != null && playerInput.DivePressed);

		if (characterController != null && characterController.beingHit && characterController.Rb != null &&
		    characterController.Rb.linearVelocity.magnitude <= 1f)
			characterController.StopRagdoll();

		if (characterController == null || !characterController.canMove || characterController.beingHit) return;

		if (SystemInfo.deviceType == DeviceType.Desktop)
		{
			horizontal = Input.GetAxisRaw("Horizontal");
			vertical = Input.GetAxisRaw("Vertical");
		}
		else
		{
			horizontal = fixedJoystick != null ? fixedJoystick.Horizontal : 0f;
			vertical = fixedJoystick != null ? fixedJoystick.Vertical : 0f;
		}

		Transform cam = Camera.main != null ? Camera.main.transform : transform;
		inputVector = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * new Vector3(horizontal, 0f, vertical).normalized;
		targetAngle = Mathf.Atan2(inputVector.x, inputVector.z) * 57.29578f;
		angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.15f);

		if (transform.eulerAngles.x != 0f && !characterController.beingHit)
		{
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Time.deltaTime * 2f);
			if (!getUpAnim) SetAnimGetUp(true);
		}
		if (inputVector.magnitude >= 0.1f && transform.rotation.x <= 0.1f)
		{
			getUpAnim = false;
			SetAnimGetUp(false);
			transform.rotation = Quaternion.Euler(transform.eulerAngles.x, angle, transform.eulerAngles.z);
		}
		if (inputVector.magnitude <= 0.1f)
		{
			getUpAnim = true;
			SetAnimGetUp(false);
		}

		float x = playerInput != null ? playerInput.Horizontal : horizontal;
		float y = playerInput != null ? playerInput.Vertical : vertical;
		characterController.SetInput(x, y);

		// Animation only via CharacterAnimator
		if (characterAnimator != null)
		{
			if (characterController.hasJumped) characterAnimator.SetJump(true);
			if (characterController.readyToJump && !characterController.grounded) characterAnimator.SetFall(true);
			else if (characterController.grounded) characterAnimator.SetFall(false);
			characterAnimator.SetIncline(!characterController.OnSlope());
			if (characterController.Rb != null && characterController.Rb.linearVelocity.magnitude >= 2f && SystemInfo.deviceType == DeviceType.Desktop)
				characterAnimator.SetHorizontal(Mathf.Lerp(0f, Input.GetAxis("Mouse X"), 5f * Time.deltaTime));
			else
				characterAnimator.SetHorizontal(SystemInfo.deviceType == DeviceType.Desktop ? x : x * 0.75f);
			characterAnimator.SetVertical(SystemInfo.deviceType == DeviceType.Desktop ? y : y * 0.75f);
		}
	}

	private void LateUpdate()
	{
		if (characterController != null && characterController.hasJumped && characterAnimator != null)
			characterAnimator.SetJump(true);
	}

	private void SetAnimGetUp(bool value)
	{
		if (characterAnimator != null) characterAnimator.SetGetUp(value);
	}
}
