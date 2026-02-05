using UnityEditor;
using UnityEngine;


public class Player : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CharacterController characterController;
	[SerializeField] private CharacterAnimator characterAnimator;
	private PlayerInput playerInput;

	[Header("Options")]
	[SerializeField] private bool requireForwardInputForAirDive = true;
	[SerializeField] private float airDiveForwardInputThreshold = 0.1f;
	[SerializeField] private float airDiveMinForwardSpeed = 1f;

	private float vertical;

   

    private MoveState moveState = MoveState.Idle;



    [Header("Fall Guys Feel")]
    [SerializeField] private float turnSpeed = 25f;
    [SerializeField] private float minMoveInput = 0.15f;   // deadzone
    [SerializeField] private float moveStartAngle = 12f;   // similar to your turnBeforeMoveAngle
    //[SerializeField] private float acceleration = 10f;     // how fast input ramps up
    //[SerializeField] private float deceleration = 14f;     // how fast input goes back to 0

    [Header("Camera References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private bool autoFindCamera = true;

    private Vector3 desiredDirection = Vector3.zero;
    [SerializeField] float inputStrength = 0.15f;

    private enum MoveState
    {
        Idle,
        Turning,
        Moving
    }

    #region Test
    public float AddInputStrengthTest(float add)
    {
        inputStrength += add;
        return inputStrength;
    }

    public string GetCurrentStat()
    {
        return moveState.ToString();
    }
    #endregion




    private void Awake()
    {
        if (autoFindCamera && playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    private void Start()
	{
		if (characterController == null) characterController = GetComponent<CharacterController>();
		if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
		if (playerInput == null) playerInput = PlayerSpawnner.Instance.playerInput;
		//if (fixedJoystick == null && GameObject.FindGameObjectWithTag("JoyStick") != null)
		//	fixedJoystick = GameObject.FindGameObjectWithTag("JoyStick").GetComponent<FixedJoystick>();

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
    private void UpdateMoveAnimation(float x, float y)
    {
        if (characterAnimator == null || characterController == null) return;

        // Always animate using player intent
        characterAnimator.SetHorizontal(x);
        characterAnimator.SetVertical(y);

        if (characterController.hasJumped)
            characterAnimator.SetJump(true);

        if (characterController.readyToJump && !characterController.grounded)
            characterAnimator.SetFall(true);
        else if (characterController.grounded)
            characterAnimator.SetFall(false);

        characterAnimator.SetIncline(!characterController.OnSlope());
    }
    // -------------------------------------------------------------
    // -------------------- UPDATE (INPUT + STATE) -----------------
    // -------------------------------------------------------------

    private void Update()
    {
        // Dive input passthrough
        if (characterController != null)
            characterController.SetDive(playerInput != null && playerInput.DivePressed);

        // Movement guards
        if (characterController == null ||
            !characterController.canMove ||
            characterController.beingHit)
            return;

        // 1️⃣ Read joystick
        float rawX = playerInput != null ? playerInput.Horizontal : 0f;
        float rawY = playerInput != null ? playerInput.Vertical : 0f;

        Vector2 input = new Vector2(rawX, rawY);
        float strength = Mathf.Clamp01(input.magnitude);

        // 2️⃣ Camera-relative input direction
        Vector3 inputDir = Vector3.zero;

        if (strength >= minMoveInput)
        {
            Transform cam = playerCamera != null ? playerCamera : transform;

            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 camRight = cam.right;

            inputDir = camForward * rawY + camRight * rawX;
            inputDir.y = 0f;
            inputDir.Normalize();
        }

        // 3️⃣ Decide STATE (NO rotation / NO movement here)
        UpdateMoveState(inputDir, strength);

        // 4️⃣ Apply STATE behavior
        ApplyMoveState(inputDir, strength);
    }

    private void UpdateMoveState(Vector3 inputDir, float strength)
    {
        // No usable input → idle
        if (strength < minMoveInput || inputDir == Vector3.zero)
        {
            moveState = MoveState.Idle;
            return;
        }

        // Angle between facing and desired direction
        float angle = Vector3.Angle(transform.forward, inputDir);

        // Too much angle → rotate first
        if (angle > moveStartAngle)
            moveState = MoveState.Turning;
        else
            moveState = MoveState.Moving;
    }

    private void ApplyMoveState(Vector3 inputDir, float strength)
    {
        switch (moveState)
        {
            case MoveState.Idle:
                // DO NOT touch rotation here (mobile fix)
                characterController.SetInput(0f, 0f);
                UpdateMoveAnimation(0f, 0f);
                break;

            case MoveState.Turning:
                // Cache direction so it doesn't get lost on mobile
                desiredDirection = inputDir;

                RotateTowards(desiredDirection);

                // Turn in place
                characterController.SetInput(0f, 0f);
                UpdateMoveAnimation(0f, strength);
                break;

            case MoveState.Moving:
                // Maintain last valid direction
                if (inputDir != Vector3.zero)
                    desiredDirection = inputDir;

                RotateTowards(desiredDirection);

                // Forward-only movement
                characterController.SetInput(0f, strength);
                UpdateMoveAnimation(0f, strength);
                break;
        }
    }

    private void RotateTowards(Vector3 dir)
    {
        if (dir == Vector3.zero) return;

        characterController.RotateTowards(dir, turnSpeed);

        //Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        //transform.rotation = Quaternion.Slerp(
        //    transform.rotation,
        //    targetRot,
        //    Time.deltaTime * turnSpeed
        //);
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

    //    private void Update()
    //    {
    //        if (characterController != null)
    //            characterController.SetDive(playerInput != null && playerInput.DivePressed);

    //        if (characterController == null || !characterController.canMove || characterController.beingHit)
    //            return;

    //        float rawX = playerInput != null ? playerInput.Horizontal : 0f;
    //        float rawY = playerInput != null ? playerInput.Vertical : 0f;

    //        Vector2 input = new Vector2(rawX, rawY);
    //        float strength = Mathf.Clamp01(input.magnitude);

    //        // --- CAMERA CHECK ---
    //        Transform cam = playerCamera;
    //        if (cam == null)
    //        {
    //            if (Camera.main != null) cam = Camera.main.transform;
    //            else cam = transform; // Fallback to self (not ideal, movement will be relative to self)

    //#if !UNITY_EDITOR
    //            if (Time.frameCount % 100 == 0 && cam == transform) 
    //                Debug.LogError("[Player] No Main Camera found! Movement relative to self.");
    //#endif
    //        }

    //        // Camera-relative world move direction
    //        Vector3 moveDir = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * new Vector3(rawX, 0f, rawY);
    //        moveDir.y = 0f;

    //        // --- ROTATION LOGIC ---
    //        if (strength >= inputStrength && moveDir.sqrMagnitude > 0.0001f)
    //        {
    //            lastMoveDir = moveDir.normalized;

    //            // Rotate towards movement direction
    //            Quaternion targetRot = Quaternion.LookRotation(lastMoveDir, Vector3.up);
    //            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

    //#if !UNITY_EDITOR
    //            if (Time.frameCount % 60 == 0)
    //                Debug.Log($"[Mobile Debug] Rotating to: {targetRot.eulerAngles.y:F1}. Camera Y: {cam.eulerAngles.y:F1}. Strength: {strength:F2}");
    //#endif
    //        }
    //        else
    //        {
    //            // Joystick released: Hold current orientation and keep upright
    //          //  transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    //        }

    //        // --- MOVEMENT LOGIC ---
    //        if (strength >= inputStrength && moveDir.sqrMagnitude > 0.0001f)
    //        {
    //            float angleDelta = Vector3.Angle(transform.forward, moveDir.normalized);
    //            if (angleDelta > moveStartAngle)
    //            {
    //                // still turning → turn in place
    //                characterController.SetInput(0f, 0f);
    //                UpdateMoveAnimation(0f, strength);
    //            }
    //            else
    //            {
    //                // Facing correct direction → move forward
    //                characterController.SetInput(0f, strength);
    //                UpdateMoveAnimation(0f, strength);
    //            }
    //        }
    //        else
    //        {
    //            // No input → stop
    //            characterController.SetInput(0f, 0f);
    //            UpdateMoveAnimation(0f, 0f);
    //        }
    //    }



}
//private float turnSmoothVelocity;
//private float horizontal;
//private Vector3 inputVector;
//private float targetAngle;
//private float angle;
//private bool getUpAnim;
//private void Update()
//{


//    if (characterController != null)
//        characterController.SetDive(playerInput != null && playerInput.DivePressed);

//    //if (characterController != null && characterController.beingHit && characterController.Rb != null &&
//    //    characterController.Rb.linearVelocity.magnitude <= 1f)
//    //	characterController.StopRagdoll();

//    if (characterController == null || !characterController.canMove || characterController.beingHit) return;

//    //if (SystemInfo.deviceType == DeviceType.Desktop)
//    //{
//    //	horizontal = Input.GetAxisRaw("Horizontal");
//    //	vertical = Input.GetAxisRaw("Vertical");
//    //}
//    //else
//    //{
//    //	horizontal = fixedJoystick != null ? fixedJoystick.Horizontal : 0f;
//    //	vertical = fixedJoystick != null ? fixedJoystick.Vertical : 0f;
//    //}

//    horizontal = playerInput.Horizontal;
//    vertical = playerInput.Vertical;

//    Transform cam = Camera.main != null ? Camera.main.transform : transform;
//    inputVector = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * new Vector3(horizontal, 0f, vertical).normalized;
//    targetAngle = Mathf.Atan2(inputVector.x, inputVector.z) * 57.29578f;
//    angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.15f);

//    if (transform.eulerAngles.x != 0f && !characterController.beingHit)
//    {
//        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Time.deltaTime * 2f);
//        if (!getUpAnim) SetAnimGetUp(true);
//    }
//    if (inputVector.magnitude >= 0.1f && transform.rotation.x <= 0.1f)
//    {
//        getUpAnim = false;
//        SetAnimGetUp(false);
//        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, angle, transform.eulerAngles.z);
//    }
//    if (inputVector.magnitude <= 0.1f)
//    {
//        getUpAnim = true;
//        SetAnimGetUp(false);
//    }

//    float x = playerInput != null ? playerInput.Horizontal : horizontal;
//    float y = playerInput != null ? playerInput.Vertical : vertical;
//    characterController.SetInput(x, y);

//    // Animation only via CharacterAnimator
//    if (characterAnimator != null)
//    {
//        if (characterController.hasJumped) characterAnimator.SetJump(true);
//        if (characterController.readyToJump && !characterController.grounded) characterAnimator.SetFall(true);
//        else if (characterController.grounded) characterAnimator.SetFall(false);
//        characterAnimator.SetIncline(!characterController.OnSlope());
//        if (characterController.Rb != null && characterController.Rb.linearVelocity.magnitude >= 2f && SystemInfo.deviceType == DeviceType.Desktop)
//            characterAnimator.SetHorizontal(Mathf.Lerp(0f, Input.GetAxis("Mouse X"), 5f * Time.deltaTime));
//        else
//            characterAnimator.SetHorizontal(SystemInfo.deviceType == DeviceType.Desktop ? x : x * 0.75f);
//        characterAnimator.SetVertical(SystemInfo.deviceType == DeviceType.Desktop ? y : y * 0.75f);
//    }
//}




//private void Update3()
//{
//    if (characterController != null)
//        characterController.SetDive(playerInput != null && playerInput.DivePressed);

//    if (characterController == null || !characterController.canMove || characterController.beingHit)
//        return;

//    // RAW input (player intent)
//    float rawX = playerInput != null ? playerInput.Horizontal : 0f;
//    float rawY = playerInput != null ? playerInput.Vertical : 0f;

//    Vector2 rawInput = new Vector2(rawX, rawY);
//    float rawMag = Mathf.Clamp01(rawInput.magnitude);

//    // Smooth input (Fall Guys has acceleration)
//    float targetX = rawX;
//    float targetY = rawY;

//    float accel = (rawMag > 0.01f) ? acceleration : deceleration;
//    smoothX = Mathf.Lerp(smoothX, targetX, Time.deltaTime * accel);
//    smoothY = Mathf.Lerp(smoothY, targetY, Time.deltaTime * accel);

//    // Camera-relative direction
//    Transform cam = Camera.main != null ? Camera.main.transform : transform;
//    Vector3 moveWorldDir = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * new Vector3(rawX, 0f, rawY);

//    float inputMagnitude = Mathf.Clamp01(moveWorldDir.magnitude);
//    Vector3 moveDirNormalized = (inputMagnitude > 0.001f) ? moveWorldDir.normalized : Vector3.zero;

//    // Compute target rotation
//    float targetYaw = transform.eulerAngles.y;
//    if (moveDirNormalized != Vector3.zero)
//        targetYaw = Mathf.Atan2(moveDirNormalized.x, moveDirNormalized.z) * Mathf.Rad2Deg;

//    // TURN FIRST
//    float sendX = smoothX;
//    float sendY = smoothY;

//    if (inputMagnitude > minMoveInput)
//    {
//        Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
//        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

//        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetYaw));

//        // If still turning, block movement but keep animation
//        if (angleDelta > moveStartAngle)
//        {
//            sendX = 0f;
//            sendY = 0f;
//        }
//    }
//    else
//    {
//        // If no input, stop sending movement input
//        sendX = 0f;
//        sendY = 0f;
//    }

//    // Send movement to physics controller
//    characterController.SetInput(sendX, sendY);

//    // Animation should always use RAW input intent
//    UpdateMoveAnimation(rawX, rawY);
//}


//private void Update2()
//{
//    if (characterController != null)
//        characterController.SetDive(playerInput != null && playerInput.DivePressed);

//    if (characterController == null || !characterController.canMove || characterController.beingHit)
//        return;

//    // 1) Read Input
//    float rawX = playerInput != null ? playerInput.Horizontal : 0f;
//    float rawY = playerInput != null ? playerInput.Vertical : 0f;

//    // 2) Camera-relative move direction
//    Transform cam = Camera.main != null ? Camera.main.transform : transform;

//    Vector3 rawInputVector = new Vector3(rawX, 0f, rawY);
//    Vector3 moveWorldDir = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * rawInputVector;

//    float inputMagnitude = Mathf.Clamp01(moveWorldDir.magnitude);
//    Vector3 moveDirNormalized = (inputMagnitude > 0.001f) ? moveWorldDir.normalized : Vector3.zero;

//    // 3) Compute target angle only if input exists
//    float targetYaw = transform.eulerAngles.y;
//    if (moveDirNormalized != Vector3.zero)
//        targetYaw = Mathf.Atan2(moveDirNormalized.x, moveDirNormalized.z) * Mathf.Rad2Deg;

//    // 4) Fall Guys turn first, then move
//    float sendX = rawX;
//    float sendY = rawY;

//    if (inputMagnitude > 0.1f)
//    {
//        // FAST turn (Fall Guys style)
//        float turnSpeed = 20f; // increase to 25-35 if you want even snappier
//        Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
//        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

//        // Block movement until facing enough
//        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetYaw));
//        if (angleDelta > turnBeforeMoveAngle)
//        {
//            sendX = 0f;
//            sendY = 0f;
//        }
//    }

//    // 5) Send movement to controller (this controls actual physics movement)
//    characterController.SetInput(sendX, sendY);

//    // 6) Animation should use RAW INPUT (so run anim plays even when turning)
//    UpdateMoveAnimation(rawX, rawY);
//}


//float horizontal;
//float targetAngle, angle;
//Vector3 inputVector;

//float turnSmoothVelocity;
//bool getUpAnim;
//float turnBeforeMoveAngle;

//private void Update()
//{

//    if (characterController != null)
//        characterController.SetDive(playerInput != null && playerInput.DivePressed);

//    //if (characterController != null && characterController.beingHit && characterController.Rb != null &&
//    //    characterController.Rb.linearVelocity.magnitude <= 1f)
//    //	characterController.StopRagdoll();

//    if (characterController == null || !characterController.canMove || characterController.beingHit) return;

//    //if (SystemInfo.deviceType == DeviceType.Desktop)
//    //{
//    //	horizontal = Input.GetAxisRaw("Horizontal");
//    //	vertical = Input.GetAxisRaw("Vertical");
//    //}
//    //else
//    //{
//    //	horizontal = fixedJoystick != null ? fixedJoystick.Horizontal : 0f;
//    //	vertical = fixedJoystick != null ? fixedJoystick.Vertical : 0f;
//    //}

//    horizontal = playerInput.Horizontal;
//    vertical = playerInput.Vertical;

//    Transform cam = Camera.main != null ? Camera.main.transform : transform;

//    inputVector = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * new Vector3(horizontal, 0f, vertical).normalized;
//    targetAngle = Mathf.Atan2(inputVector.x, inputVector.z) * 57.29578f;
//    angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.15f);

//    if (transform.eulerAngles.x != 0f && !characterController.beingHit)
//    {
//        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), Time.deltaTime * 2f);
//        if (!getUpAnim) SetAnimGetUp(true);
//    }
//    if (inputVector.magnitude >= 0.1f && transform.rotation.x <= 0.1f)
//    {
//        getUpAnim = false;
//        SetAnimGetUp(false);
//        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, angle, transform.eulerAngles.z);
//    }
//    if (inputVector.magnitude <= 0.1f)
//    {
//        getUpAnim = true;
//        SetAnimGetUp(false);
//    }

//    float x = playerInput != null ? playerInput.Horizontal : horizontal;
//    float y = playerInput != null ? playerInput.Vertical : vertical;

//    // Fall Guys-style: rotate first towards camera-relative direction, then move when mostly facing that direction.
//    if (characterController != null && inputVector.magnitude >= 0.1f)
//    {
//        // How far off from desired facing (camera forward + input) are we?
//        float currentY = transform.eulerAngles.y;
//        float angleDelta = Mathf.Abs(Mathf.DeltaAngle(currentY, targetAngle));

//        // If still turning a lot, suppress movement this frame so we turn in place.
//        if (angleDelta > turnBeforeMoveAngle)
//        {
//            x = 0f;
//            y = 0f;
//        }
//    }

//    characterController.SetInput(x, y);

//    // Animation only via CharacterAnimator
//    if (characterAnimator != null)
//    {
//        if (characterController.hasJumped) characterAnimator.SetJump(true);
//        if (characterController.readyToJump && !characterController.grounded) characterAnimator.SetFall(true);
//        else if (characterController.grounded) characterAnimator.SetFall(false);
//        characterAnimator.SetIncline(!characterController.OnSlope());
//        if (characterController.Rb != null && characterController.Rb.linearVelocity.magnitude >= 2f && SystemInfo.deviceType == DeviceType.Desktop)
//            characterAnimator.SetHorizontal(Mathf.Lerp(0f, Input.GetAxis("Mouse X"), 5f * Time.deltaTime));
//        else
//            characterAnimator.SetHorizontal(SystemInfo.deviceType == DeviceType.Desktop ? x : x * 0.75f);
//        characterAnimator.SetVertical(SystemInfo.deviceType == DeviceType.Desktop ? y : y * 0.75f);
//    }
//}


