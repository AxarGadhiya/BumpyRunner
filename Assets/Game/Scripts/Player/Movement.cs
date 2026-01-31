using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Movement : MonoBehaviour
{
	public Transform orientation;

	public Transform playerBody;

	//private PhotonView view;

	private Rigidbody rb;

	public Animator anim;

	public TextMeshProUGUI uiDebug;

	public bool canMove = true;

	public bool beingHit;

	public Ragdoll ragdollScript;

	//public HugController hugScript;

	public bool disableOnAwake = true;

	public bool getUp;

	public float moveSpeed = 4500f;

	public float maxSpeed = 20f;

	public bool grounded;

	public LayerMask whatIsGround;

	private float turnSmoothVelocity;

	public FixedJoystick fixedJoystick;

	public float counterMovement = 0.175f;

	private float threshold = 0.01f;

	public float maxSlopeAngle = 35f;

	private RaycastHit slopeHit;

	private Vector3 crouchScale = new Vector3(1f, 0.5f, 1f);

	private Vector3 playerScale;

	public float slideForce = 400f;

	public float slideCounterMovement = 0.2f;

	private bool readyToJump = true;

	private float jumpCooldown = 0.5f;

	public float jumpForce = 550f;

	public bool isJumping;
	
	private bool hasJumped; // Track if player has jumped (stays true until grounded)

	public bool canDive = true;

	public bool useDiveMomentum;

	public float diveSpeed = 200f;

	[SerializeField] private bool requireForwardInputForAirDive = true;

	[SerializeField] private float airDiveForwardInputThreshold = 0.1f;

	// Additional safety to avoid "standing double-jump => dive" due to input drift.
	// Checks actual movement in the facing (transform.forward) direction.
	[SerializeField] private float airDiveMinForwardSpeed = 1.0f;

	private float x;

	private float y;

	private bool sprinting;

	private bool crouching;

	private bool dive;

	public bool jumping;

	private float angle;

	private float inputMouse;

	private Vector3 normalVector = Vector3.up;

	private Vector3 wallNormalVector;

	private bool gettingUp;

	private float horizontal;

	private float vertical;

	private Vector3 inputVector;

	private float targetAngle;

	private bool getUpAnim;

	private float momentum = 1f;

	private bool waitDelay;

	private float desiredX;

	private bool cancellingGrounded;

	private float capsuleHeight;

	[SerializeField]private PlayerInput playerInput;


	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		//view = GetComponent<PhotonView>();
		//if (view.IsMine)
		//{
		//	_ = disableOnAwake;
		//}
	}

	private void Start()
	{
		playerScale = base.transform.localScale;
		//if (!view.IsMine)
		//{
		//	UnityEngine.Object.Destroy(base.gameObject.GetComponent<CarryPlayerRigid>());
		//}
		fixedJoystick = GameObject.FindGameObjectWithTag("JoyStick").GetComponent<FixedJoystick>();
        orientation = Camera.main.transform;
        
        // Subscribe to jump input action
        if (playerInput != null)
        {
            playerInput.OnJumpInput += HandleJumpInput;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from jump input action
        if (playerInput != null)
        {
            playerInput.OnJumpInput -= HandleJumpInput;
        }
    }
    
    // Handle jump input from action (called when jump button is pressed - keyboard or mobile)
    private void HandleJumpInput()
    {
        // IMPORTANT: If player is in air and jump input is received, it should dive
        if (!grounded)
        {
            // Mobile-style behavior: 2nd jump press (while airborne) becomes a dive,
            // but only if player actually jumped already and is moving forward.
			if (!hasJumped)
			{
				return;
			}

			if (!requireForwardInputForAirDive || HasForwardInput())
			{
				DiveInMovementDirection();
			}
        }
        else if (readyToJump)
        {
            // Player grounded: jump straight up
            JumpStraightUp();
        }
    }

	private bool HasForwardInput()
	{
		// Require BOTH:
		// 1) Intent to move forward (filters joystick drift)
		// 2) Actual forward movement in facing direction (transform.forward)
		float forwardIntent = (playerInput != null) ? playerInput.Vertical : y;
		if (forwardIntent <= airDiveForwardInputThreshold)
		{
			return false;
		}

		Vector3 horizontalVel = rb.linearVelocity;
		horizontalVel.y = 0f;
		float forwardSpeed = Vector3.Dot(horizontalVel, base.transform.forward);
		return forwardSpeed >= airDiveMinForwardSpeed;
	}

	private void FixedUpdate()
	{
		if (!disableOnAwake && !beingHit)
		{
          //  Debug.Log("Move_-1");
            Move();
		}
	}

	private void Update()
	{
		if (base.gameObject.layer == LayerMask.NameToLayer("Player"))
		{
			AIController component = GetComponent<AIController>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
			base.gameObject.transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("Player");
		}
		if (disableOnAwake /*|| !view.IsMine*/)
		{
			return;
		}
		MyInput();
		Dive();
		//orientation = Camera.main.transform;
		//if (Input.GetKeyDown(KeyCode.Alpha1))
		//{
		//	anim.SetTrigger("Emote");
		//}
		// Only disable ragdoll if it's actually active and player has settled
		if (ragdollScript != null && ragdollScript.ragdoll && beingHit && rb.linearVelocity.magnitude <= 1f)
		{
			//if (PhotonNetwork.IsConnected)
			//{
			//	view.RPC("StopRagdollPun", RpcTarget.AllBuffered, null);
			//}
			//else
			//{
				StopRagdollPun();
			//}
		}
		if (/*hugScript.isHuging ||*/ !canMove || beingHit)
		{
			return;
		}
		if (SystemInfo.deviceType == DeviceType.Desktop)
		{
			horizontal = Input.GetAxisRaw("Horizontal");
			vertical = Input.GetAxisRaw("Vertical");
		}
		else
		{
			horizontal = fixedJoystick.Horizontal;
			vertical = fixedJoystick.Vertical;
		}
		inputVector = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f) * new Vector3(horizontal, 0f, vertical).normalized;
		targetAngle = Mathf.Atan2(inputVector.x, inputVector.z) * 57.29578f;
		angle = Mathf.SmoothDampAngle(base.transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, 0.15f);
		if (base.transform.eulerAngles.x != Vector3.zero.x && !beingHit)
		{
			getUp = true;
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(0f, base.transform.eulerAngles.y, 0f), Time.deltaTime * 2f);
			if (!getUpAnim)
			{
				anim.SetBool("GetUp", value: true);
			}
		}
		if (inputVector.magnitude >= 0.1f && base.transform.rotation.x <= 0.1f)
		{
			getUpAnim = false;
			anim.SetBool("GetUp", value: false);
			base.transform.rotation = Quaternion.Euler(base.transform.eulerAngles.x, angle, base.transform.eulerAngles.z);
		}
		if (inputVector.magnitude <= 0.1f)
		{
			getUpAnim = true;
			anim.SetBool("GetUp", value: false);
		}
	}

	private void RagdollCheck()
	{
		if (rb.linearVelocity.magnitude <= 1.5f && beingHit)
		{
			StopRagdoll();
			Quaternion b = Quaternion.Euler(0f, 0f, 0f);
			base.transform.rotation = Quaternion.SlerpUnclamped(base.transform.rotation, b, Time.deltaTime * 15f);
			if (!getUpAnim)
			{
				anim.SetBool("GetUp", value: true);
				getUpAnim = true;
			}
			gettingUp = true;
		}
		if (rb.linearVelocity.magnitude <= 1.5f && base.transform.eulerAngles != Vector3.zero && beingHit)
		{
			StopRagdoll();
			Quaternion b2 = Quaternion.Euler(0f, 0f, 0f);
			base.transform.rotation = Quaternion.SlerpUnclamped(base.transform.rotation, b2, Time.deltaTime * 15f);
		}
		if (base.transform.eulerAngles == Vector3.zero && beingHit)
		{
			StopRagdoll();
			gettingUp = false;
			getUp = false;
			beingHit = false;
			anim.SetBool("GetUp", value: false);
		}
		if (!beingHit)
		{
			anim.SetBool("GetUp", value: false);
		}
	}

	private void MyInput()
	{
        x = playerInput.Horizontal;
        y = playerInput.Vertical;
       // jumping = playerInput.JumpHeld; // Use JumpHeld for animator and movement logic
        dive = playerInput.DivePressed;

        //if (SystemInfo.deviceType == DeviceType.Desktop)
        //{
        //	//Debug.Log("===Desktop Input===");
        //	//x = Input.GetAxisRaw("Horizontal");
        //	//y = Input.GetAxisRaw("Vertical");
        //	x = playerInput.Horizontal;
        //	y = playerInput.Vertical;
        //	jumping = playerInput.JumpHeld;
        //	dive = playerInput.DivePressed;
        //}
        //else
        //{
        //	x = fixedJoystick.Horizontal * 2f;
        //	y = fixedJoystick.Vertical * 2f;
        //}
        // SIMPLE LOGIC: Keep jump animator bool true as long as hasJumped is true
        // Only reset when player actually lands (handled in OnCollisionStay)
        if (hasJumped)
		{
			anim.SetBool("Jump", value: true);
		}
		
		if (readyToJump && !GroundedFloor())
		{
			anim.SetBool("Fall", value: true);
		}
		else if (grounded)
		{
			anim.SetBool("Fall", value: false);
		}
		if (canMove)
		{
			if (OnSlope())
			{
				anim.SetBool("Incline", value: false);
			}
			else
			{
				anim.SetBool("Incline", value: true);
			}
			if (rb.linearVelocity.magnitude >= 2f && SystemInfo.deviceType == DeviceType.Desktop)
			{
				inputMouse = Mathf.Lerp(inputMouse, Input.GetAxis("Mouse X"), 5f * Time.deltaTime);
				anim.SetFloat("horizontal", inputMouse, 0.05f, Time.deltaTime * 1f);
			}
			if (SystemInfo.deviceType == DeviceType.Desktop)
			{
				//float axis = Input.GetAxis("Horizontal");
				//float axisRaw = Input.GetAxisRaw("Vertical");
				anim.SetFloat("vertical", y, 0.05f, Time.deltaTime * 1f);
				anim.SetFloat("horizontal", x, 0.05f, Time.deltaTime * 1f);
			}
			else
			{
				//float value = fixedJoystick.Horizontal * 1.5f;
				//float value2 = fixedJoystick.Vertical * 1.5f;
				anim.SetFloat("vertical", y*0.75f, 0.05f, Time.deltaTime * 1f);
				anim.SetFloat("horizontal", x*0.75f, 0.05f, Time.deltaTime * 1f);
			}
		}
	}

	private void Move()
	{
       // Debug.Log("Move_0");
  //      if (!view.IsMine)
		//{
		//	return;
		//}


		rb.AddForce(Vector3.down * Time.deltaTime * 50f);
		Vector2 mag = FindVelRelativeToLook();
		float num = mag.x;
		float num2 = mag.y;
		if (!beingHit)
		{
			CounterMovement(x, y, mag);
		}
		// Jump input is now handled via OnJumpInput action (subscribed in Start)
		float num3 = maxSpeed;
		if (canMove)
		{
			if (!OnSlope() && grounded && !jumping && rb.linearVelocity.magnitude <= num3)
			{
				rb.AddForce(GetSlopeMoveDirection() * Mathf.Abs(x) * moveSpeed * Time.deltaTime, ForceMode.Force);
				rb.AddForce(GetSlopeMoveDirection() * Mathf.Abs(y) * moveSpeed * Time.deltaTime, ForceMode.Force);
			}
			if (x > 0f && num > num3)
			{
				x = 0f;
			}
			if (x < 0f && num < 0f - num3)
			{
				x = 0f;
			}
			if (y > 0f && num2 > num3)
			{
				y = 0f;
			}
			if (y < 0f && num2 < 0f - num3)
			{
				y = 0f;
			}
			float num4 = 1f;
			float num5 = 1f;
			if (!grounded /*|| hugScript.isHuging*/)
			{
				num4 = 0.5f;
				num5 = 0.5f;
			}
			if (y > 0f && !Input.GetButtonDown("Horizontal"))
			{
				//Debug.Log("!Input.GetButtonDown(\"Horizontal\")--0");
				rb.AddForce(base.transform.forward * y * moveSpeed * Time.deltaTime * num4 * num5, ForceMode.Force);
			}
			else if (y > 0f && Input.GetButton("Horizontal"))
			{
				//Debug.Log("Input.GetButton(\"Horizontal\")--1");
				rb.AddForce(base.transform.forward * y * moveSpeed * Time.deltaTime * num4 * num5, ForceMode.Force);
			}
			else if (y < 0f)
			{
				rb.AddForce(orientation.forward * y * moveSpeed * Time.deltaTime * num4 * num5, ForceMode.Force);
			}
			rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * num4, ForceMode.Force);
		}
	}

	private void Dive()
	{
		if (!canDive && grounded && (int)rb.linearVelocity.magnitude <= 14)
		{
			Debug.Log("<color=cyan>Dive</color>");
			anim.SetBool("DiveStart", value: false);
			//hugScript.canHug = true;
			canDive = true;
			StartCoroutine(DiveDelay());
		}
		if (!dive || !canDive || grounded || waitDelay)
		{
			return;
		}
		//hugScript.canHug = false;
		anim.SetBool("DiveStart", value: true);
		if (useDiveMomentum)
		{
			if (rb.linearVelocity.magnitude < 10f)
			{
				momentum = 1f;
			}
			else
			{
				momentum = 0.5f;
			}
			rb.AddForce(base.transform.forward * momentum * rb.linearVelocity.magnitude / 4f * 200f, ForceMode.Force);
		}
		else
		{
			rb.AddForce(base.transform.forward * diveSpeed, ForceMode.Force);
		}
		canDive = false;
		waitDelay = true;
	}

	public void DiveMobile()
	{
		if (!canDive || grounded || waitDelay)
		{
			return;
		}
		//hugScript.canHug = false;
		anim.SetBool("DiveStart", value: true);
		if (useDiveMomentum)
		{
			if (rb.linearVelocity.magnitude < 10f)
			{
				momentum = 1f;
			}
			else
			{
				momentum = 0.5f;
			}
			rb.AddForce(base.transform.forward * momentum * rb.linearVelocity.magnitude / 4f * 200f, ForceMode.Force);
		}
		else
		{
			rb.AddForce(base.transform.forward * diveSpeed, ForceMode.Force);
		}
		canDive = false;
		waitDelay = true;
	}

	private IEnumerator DiveDelay()
	{
		yield return new WaitForSeconds(0.5f);
		waitDelay = false;
	}

	public void Jump()
	{
		// Legacy method - kept for compatibility, but now calls JumpStraightUp
		JumpStraightUp();
	}

	private void JumpStraightUp()
	{
		// Jump straight up - works anytime if not moving, or when grounded if moving
		anim.SetBool("Jump", value: true);
        Debug.Log("Jump TRUE");
        isJumping = true;
		hasJumped = true; // Mark that player has jumped
		readyToJump = false;
		
		// Apply upward force
		rb.AddForce(Vector2.up * jumpForce * 1.5f);
		rb.AddForce(normalVector * jumpForce * 0.5f);
		
		//view.RPC("BoolJumpTrue", RpcTarget.AllBuffered, null);
		BoolJumpTrue();
		
		Vector3 velocity = rb.linearVelocity;
		if (rb.linearVelocity.y < 0.5f)
		{
			rb.linearVelocity = new Vector3(velocity.x, 0f, velocity.z);
		}
		else if (rb.linearVelocity.y > 0f)
		{
			rb.linearVelocity = new Vector3(velocity.x, velocity.y / 2f, velocity.z);
		}
		Invoke(nameof(ResetJump), jumpCooldown);
	}

	private void DiveInMovementDirection()
	{
		// Dive forward (player facing direction) on 2nd jump press while airborne
		if (!canDive || waitDelay)
		{
			return;
		}
		
		Vector3 moveDirection = base.transform.forward;
		
		//hugScript.canHug = false;
		anim.SetBool("DiveStart", value: true);
		hasJumped = true; // Mark that player has jumped (diving counts as jump state)
		
		// Apply dive force in movement direction
		if (useDiveMomentum)
		{
			if (rb.linearVelocity.magnitude < 10f)
			{
				momentum = 1f;
			}
			else
			{
				momentum = 0.5f;
			}
			rb.AddForce(moveDirection * momentum * rb.linearVelocity.magnitude / 4f * 200f, ForceMode.Force);
		}
		else
		{
			rb.AddForce(moveDirection * diveSpeed, ForceMode.Force);
		}
		
		canDive = false;
		waitDelay = true;
		readyToJump = false;
		Invoke(nameof(ResetJump), jumpCooldown);
	}

	private void ResetJump()
	{
		//view.RPC("BoolJumpFalse", RpcTarget.AllBuffered, null);
		BoolJumpFalse();
        readyToJump = true;
	}

	private bool GroundedFloor()
	{
		if (Physics.Raycast(base.transform.position, Vector3.down, out var _, capsuleHeight / 2f + 1f))
		{
			return true;
		}
		return false;
	}

    //---------------[PunRPC]------------------
    private void BoolJumpTrue()
	{
		isJumping = true;
	}

    //---------------[PunRPC]-------------
    private void BoolJumpFalse()
	{
		isJumping = false;
	}

	private void CounterMovement(float x, float y, Vector2 mag)
	{
		if (grounded && !jumping)
		{
			if ((Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f) || (mag.x < 0f - threshold && x > 0f) || (mag.x > threshold && x < 0f))
			{
				rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * (0f - mag.x) * counterMovement);
			}
			if ((Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f) || (mag.y < 0f - threshold && y > 0f) || (mag.y > threshold && y < 0f))
			{
				rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * (0f - mag.y) * counterMovement);
			}
			if (Mathf.Sqrt(Mathf.Pow(rb.linearVelocity.x, 2f) + Mathf.Pow(rb.linearVelocity.z, 2f)) > maxSpeed)
			{
				float num = rb.linearVelocity.y;
				Vector3 vector = rb.linearVelocity.normalized * maxSpeed;
				rb.linearVelocity = new Vector3(vector.x, num, vector.z);
			}
		}
	}

	public Vector2 FindVelRelativeToLook()
	{
		float current = Camera.main.transform.eulerAngles.y;
		float target = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * 57.29578f;
		float num = Mathf.DeltaAngle(current, target);
		float num2 = 90f - num;
		float magnitude = rb.linearVelocity.magnitude;
		return new Vector2(y: magnitude * Mathf.Cos(num * ((float)Math.PI / 180f)), x: magnitude * Mathf.Cos(num2 * ((float)Math.PI / 180f)));
	}

	private bool IsFloor(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) < 35f;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!(collision.gameObject.GetComponent<Rigidbody>() == null) && collision.gameObject.tag == "Player" && collision.gameObject.GetComponent<Rigidbody>().linearVelocity.magnitude >= 7f)
		{
			GettingHit();
		}
	}

	private void OnCollisionStay(Collision other)
	{
		int layer = other.gameObject.layer;
		if ((int)whatIsGround != ((int)whatIsGround | (1 << layer)))
		{
			return;
		}
		for (int i = 0; i < other.contactCount; i++)
		{
			Vector3 normal = other.contacts[i].normal;
			if (IsFloor(normal))
			{
				if (rb.linearVelocity.magnitude >= 4f && !grounded && !canDive && other.gameObject.layer != LayerMask.NameToLayer("Trampoline"))
				{
					_ = jumping;
				}
				
				// Only set grounded and reset jump state if player is actually landing (not moving upward)
				// This prevents jump animator bool from being reset immediately after jumping
				if (rb.linearVelocity.y <= 0.5f) // Player is falling or stationary (actually landing)
				{
					grounded = true;
					if (hasJumped)
					{
						hasJumped = false; // Reset jump state when actually landed
						anim.SetBool("Jump", value: false);
						Debug.Log("Jump FALSE - Actually Landed");
					}
					cancellingGrounded = false;
					normalVector = normal;
					CancelInvoke(nameof(StopGrounded));
				}
				else
				{
					// Player is still moving upward, don't set grounded yet
					// This keeps hasJumped true and animator bool true
				}
            }
		}
		float num = 3f;
		if (!cancellingGrounded)
		{
			cancellingGrounded = true;
			Invoke(nameof(StopGrounded), Time.deltaTime * num);
		}
	}

	private bool OnSlope()
	{
		capsuleHeight = base.gameObject.GetComponentInChildren<CapsuleCollider>().height;
		if (Physics.Raycast(base.transform.position, Vector3.down, out slopeHit, capsuleHeight / 2f + 1f) && Vector3.Angle(Vector3.up, slopeHit.normal) < maxSlopeAngle)
		{
			return true;
		}
		return false;
	}

	private Vector3 GetSlopeMoveDirection()
	{
		return Vector3.ProjectOnPlane(rb.linearVelocity, slopeHit.normal).normalized;
	}

	public void GettingHit()
	{
		//if (PhotonNetwork.IsConnected)
		//{
		//	view.RPC("GettingHitPun", RpcTarget.AllBuffered, null);
		//}
		//else
		//{
			GettingHitPun();
		//}
	}

    //---------------[PunRPC]----------------
    private void GettingHitPun()
	{
		beingHit = true;
		getUp = true;
		ragdollScript.EnableRagdoll();
		//hugScript.StopHug();
		StartCoroutine(GettingHitCoolDown());
	}

	public void StopRagdoll()
	{
		// Only disable ragdoll if it's actually active
		if (ragdollScript != null && ragdollScript.ragdoll)
		{
			anim.SetBool("GetUp", value: false);
			ragdollScript.DisableRagdoll();
		}
	}

    //---------------[PunRPC]----------------------
    private void StopRagdollPun()
	{
		// Only disable ragdoll if it's actually active
		if (ragdollScript != null && ragdollScript.ragdoll)
		{
			ragdollScript.DisableRagdoll();
			getUp = false;
			beingHit = false;
		}
	}

	private IEnumerator GettingHitCoolDown()
	{
		yield return new WaitForSeconds(2f);
	}

	private void StopGrounded()
	{
		// Don't set animator bool to false here - it should stay true until player lands
		// Only reset grounded state
		grounded = false;
		// hasJumped stays true until player actually lands (reset in OnCollisionStay)
	}
	
	private void LateUpdate()
	{
		// Final safeguard: Keep jump animator bool true as long as hasJumped is true
		// This ensures it stays true until OnCollisionStay resets hasJumped when landing
		if (hasJumped)
		{
			anim.SetBool("Jump", value: true);
		}
	}
}
