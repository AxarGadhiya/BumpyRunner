using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shared character controller for both player and AI.
/// Handles physics movement, jump, dive, slopes, grounding, and ragdoll.
/// Use with Player.cs (player) or AI.cs (enemy). Movement.cs and AIController.cs are unchanged.
/// Named CharacterController to avoid conflict with UnityEngine.CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterPhysics))]
[RequireComponent(typeof(CharacterAnimator))]
public class CharacterController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CharacterPhysics characterPhysics;
	[SerializeField] private CharacterAnimator characterAnimator;
	[SerializeField] private Rigidbody rb;
	[SerializeField] private Animator anim;
	[SerializeField] private Ragdoll ragdollScript;
	[SerializeField] private Transform movementOrientation;

	[Header("Movement")]
	[Tooltip("Movement/jump/dive run in FixedUpdate and use fixed timestep — same feel on all FPS devices.")]
	public float moveSpeed = 4500f;
	public float slopeSpeedMultiplier = 1f;
	public float maxSpeed = 20f;
	public float counterMovement = 0.175f;
	private float threshold = 0.01f;
	public float maxSlopeAngle = 35f;
	[Tooltip("0 = no help. 1 = fully cancel gravity pulling you down the slope while you are actively moving.")]
	[Range(0f, 2f)]
	public float slopeGravityCompensation = 1f;

	[Header("Jump & Dive")]
	public float jumpForce = 550f;
	public float jumpCooldown = 0.5f;
	public float diveSpeed = 200f;
	public bool useDiveMomentum = true;

    [Header("Step Offset")]
    [Tooltip("LayerMask for objects that can trigger a step up.")]
    public LayerMask stepLayerMask = ~0; // Default to everything
    [Tooltip("Max height the character can step up automatically.")]
    public float stepHeight = 0.4f;
    [Tooltip("Smooths the step-up motion. Higher = snappier.")]
    public float stepSmooth = 2f;
    [Tooltip("Distance forward to check obstacle at FEET level.")]
    public float stepCheckDistance = 0.3f; 
    [Tooltip("Distance forward to check obstacle at LOWER HEAD level.")]
    public float stepCheckDistanceHead1 = 0.4f;
    [Tooltip("Distance forward to check obstacle at UPPER HEAD level.")]
    public float stepCheckDistanceHead2 = 0.5f;

	[Header("Behaviour (set by Player or AI)")]
	[Tooltip("Use counter movement when releasing input (player only).")]
	public bool useCounterMovement = true;
	[Tooltip("Use wall avoidance sideways force (AI only).")]
	public bool useWallAvoidance = false;

	public bool canMove = true;
	public bool grounded => characterPhysics != null && characterPhysics.Grounded;
	public bool beingHit;
	public bool readyToJump = true;
	public bool canDive = true;
	public bool isJumping;
	public bool hasJumped;

	private float x;
	private float y;
	private bool dive;
	private bool jumping;
	private float momentum = 1f;
	private bool waitDelay;
	private float capsuleHeight;
	private RaycastHit slopeHit;

    [Header("Fall Guys Braking")]
    [SerializeField] private float stopDrag = 8f;      // how hard we stop
    [SerializeField] private float minStopSpeed = 0.2f; // snap to zero

  private Transform Orientation => movementOrientation != null ? movementOrientation : transform;
	private Vector3 normalVector => characterPhysics != null ? characterPhysics.NormalVector : Vector3.up;

	private void Awake()
	{
		if (characterPhysics == null) characterPhysics = GetComponent<CharacterPhysics>();
		if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
		if (rb == null) rb = GetComponent<Rigidbody>();
		if (characterPhysics != null && rb == null) rb = characterPhysics.Rb;
        
        // Optimize for smooth camera movement
        if (rb != null) rb.interpolation = RigidbodyInterpolation.Interpolate;

		if (anim == null) anim = GetComponentInChildren<Animator>();
		if (ragdollScript == null) ragdollScript = GetComponentInChildren<Ragdoll>();

        Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    }

    /// <summary>Single path: animation only via CharacterAnimator when assigned, else Animator.</summary>
    private void SetAnimJump(bool value) { if (characterAnimator != null) characterAnimator.SetJump(value); else if (anim != null) anim.SetBool("Jump", value); }
	private void SetAnimDiveStart(bool value) { if (characterAnimator != null) characterAnimator.SetDiveStart(value); else if (anim != null) anim.SetBool("DiveStart", value); }
	private void SetAnimGetUp(bool value) { if (characterAnimator != null) characterAnimator.SetGetUp(value); else if (anim != null) anim.SetBool("GetUp", value); }

	private void Start()
	{
		if (characterPhysics != null)
		{
			characterPhysics.OnHitByPlayer += GettingHit;
			characterPhysics.OnLanded += OnLandedHandler;
		}
	}

	private void OnDestroy()
	{
		if (characterPhysics != null)
		{
			characterPhysics.OnHitByPlayer -= GettingHit;
			characterPhysics.OnLanded -= OnLandedHandler;
		}
	}

	private void OnLandedHandler()
	{
		hasJumped = false;
		SetAnimJump(false);
		if (!canDive && Rb.linearVelocity.magnitude <= 14f)
		{
			SetAnimDiveStart(false);
			canDive = true;
			waitDelay = false;
		}
	}

	// Movement/jump/dive in FixedUpdate: Time.deltaTime = Time.fixedDeltaTime here, so same behaviour at any FPS.
	private void FixedUpdate()
	{
        if (beingHit || gettingUp) return;
        Move();
        ApplyBraking();
        // StepClimb(); // Add step climb check
        ProcessDive();

     //  KeepUprightOnGround();
    }

 


    /// <summary>Set movement input. Call from Player or AI each Update.</summary>
    public void SetInput(float horizontal, float vertical)
	{
		x = horizontal;
		y = vertical;
	}

	/// <summary>Request dive (e.g. when in air). Processed in FixedUpdate.</summary>
	public void SetDive(bool requestDive)
	{
		dive = requestDive;
	}

	/// <summary>Set movement orientation (e.g. camera for player). Null = use transform (AI).</summary>
	public void SetMovementOrientation(Transform orientation)
	{
		movementOrientation = orientation;
	}

	/// <summary>For AI: set wall hit flags so controller adds sideways force.</summary>
	public void SetWallHits(bool hitLeft, bool hitRight)
	{
		// Stored and used in Move() when useWallAvoidance is true
		_hitLeft = hitLeft;
		_hitRight = hitRight;
	}

	private bool _hitLeft;
	private bool _hitRight;

	private void Move()
	{
		float dt = Time.fixedDeltaTime;
		Rb.AddForce(50f * dt * Vector3.down);
		Vector2 mag = FindVelRelativeToLook();
		float num = mag.x;
		float num2 = mag.y;

		//if (useCounterMovement && !beingHit)
		//	CounterMovement(x, y, mag);

		float num3 = maxSpeed;
		if (!canMove) return;

		float num4 = 1f;
		float num5 = 1f;
		if (!grounded)
		{
			num4 = 0.5f;
			num5 = 0.5f;
		}

		bool onSlope = OnSlope();
		// IMPORTANT: use speed along the surface (ignore vertical), otherwise slopes feel slower.
		Vector3 surfaceVel = Vector3.ProjectOnPlane(Rb.linearVelocity, onSlope ? slopeHit.normal : Vector3.up);
		float surfaceSpeed = surfaceVel.magnitude;
		bool groundedAndUnderSpeed = grounded && !jumping && surfaceSpeed <= num3;

		if (x > 0f && num > num3) x = 0f;
		if (x < 0f && num < -num3) x = 0f;
		if (y > 0f && num2 > num3) y = 0f;
		if (y < 0f && num2 < -num3) y = 0f;

		// Apply movement force once (flat and slope), so slope speed matches flat speed.
		if (surfaceSpeed <= num3)
		{
			if (useWallAvoidance && !onSlope)
			{
				if (_hitLeft)
					Rb.AddForce(dt * moveSpeed * num4 * num5 * y * transform.right, ForceMode.Force);
				else if (_hitRight)
					Rb.AddForce((-y) * dt * moveSpeed * num4 * num5 * transform.right, ForceMode.Force);
				Rb.AddRelativeForce(dt * moveSpeed * num4 * num5 * y * Vector3.forward, ForceMode.Force);
			}
			else
			{
				//Vector3 inputDir = Orientation.forward * y + Orientation.right * x;
				Vector3 inputDir = transform.forward * y + transform.right * x;
		
				if (inputDir.sqrMagnitude > 0.01f && (!grounded || groundedAndUnderSpeed))
				{
               // Debug.Log("inputDir.sqrMagnitude" + inputDir.sqrMagnitude);
                float inputMagnitude = Mathf.Min(1f, inputDir.magnitude);
					Vector3 moveDir = inputDir.normalized;
					if (onSlope && grounded)
						moveDir = Vector3.ProjectOnPlane(moveDir, slopeHit.normal).normalized;
					float speed = moveSpeed * (onSlope ? slopeSpeedMultiplier : 1f);
					Rb.AddForce(dt * inputMagnitude * num4 * num5 * speed * moveDir, ForceMode.Force);

                Rb.AddForce(Vector3.up * 3f, ForceMode.Force);

                // Optional: cancel gravity pulling down the slope so uphill speed matches flat.
                if (onSlope && grounded && slopeGravityCompensation > 0f)
					{
						Vector3 gravityAlongSlope = Vector3.ProjectOnPlane(Physics.gravity, slopeHit.normal);
						Rb.AddForce(-gravityAlongSlope * slopeGravityCompensation, ForceMode.Acceleration);
					}
				}
			}
		}

      //  Debug.Log($"x:{x} y:{y} grounded:{grounded} beingHit:{beingHit} vel:{Rb.linearVelocity}");

    }

    private void ApplyBraking()
    {
        if (!canMove || beingHit) return;

        // no input → brake
        if (Mathf.Abs(y) < 0.01f)
        {
            Vector3 vel = Rb.linearVelocity;
            Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);

            if (horizontalVel.magnitude > minStopSpeed)
            {
                // Apply counter force (Fall Guys style)
                Vector3 brakeForce = -horizontalVel.normalized * stopDrag;
                Rb.AddForce(brakeForce, ForceMode.Acceleration);
            }
            else
            {
                // snap to full stop
                Rb.linearVelocity = new Vector3(0f, vel.y, 0f);
            }
        }
    }


    private void ProcessDive()
	{
		if (!canDive && grounded && (int)Rb.linearVelocity.magnitude <= 14)
		{
			SetAnimDiveStart(false);
			canDive = true;
			StartCoroutine(DiveDelay());
		}
		if (!dive || !canDive || grounded || waitDelay) return;

		SetAnimDiveStart(true);
		hasJumped = true;
		if (useDiveMomentum)
		{
			momentum = Rb.linearVelocity.magnitude < 10f ? 1f : 0.5f;
			Rb.AddForce(momentum * Rb.linearVelocity.magnitude * transform.forward / 4f * 200f, ForceMode.Force);
		}
		else
			Rb.AddForce(transform.forward * diveSpeed, ForceMode.Force);
		canDive = false;
		waitDelay = true;
		dive = false;
		readyToJump = false;
		Invoke(nameof(ResetJump), jumpCooldown);
	}

	private IEnumerator DiveDelay()
	{
		yield return new WaitForSeconds(0.5f);
		waitDelay = false;
	}

	/// <summary>Jump straight up. Call when grounded and ready.</summary>
	public void Jump()
	{
		if (!grounded || !readyToJump) return;
		SetAnimJump(true);
		isJumping = true;
		hasJumped = true;
		readyToJump = false;
		jumping = true;
		Rb.AddForce(1.5f * jumpForce * Vector2.up);
		Rb.AddForce(0.5f * jumpForce * normalVector);
		Vector3 vel = Rb.linearVelocity;
		if (Rb.linearVelocity.y < 0.5f)
			Rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
		else if (Rb.linearVelocity.y > 0f)
			Rb.linearVelocity = new Vector3(vel.x, vel.y / 2f, vel.z);
		Invoke(nameof(ResetJump), jumpCooldown);
	}

	/// <summary>Dive forward in air (e.g. second jump for player).</summary>
	public void DiveInMovementDirection()
	{
		if (!canDive || waitDelay) return;
		SetAnimDiveStart(true);
		hasJumped = true;
		if (useDiveMomentum)
		{
			momentum = Rb.linearVelocity.magnitude < 10f ? 1f : 0.5f;
			Rb.AddForce(transform.forward * momentum * Rb.linearVelocity.magnitude / 4f * 200f, ForceMode.Force);
		}
		else
			Rb.AddForce(transform.forward * diveSpeed, ForceMode.Force);
		canDive = false;
		waitDelay = true;
		readyToJump = false;
		Invoke(nameof(ResetJump), jumpCooldown);
	}

	private void ResetJump()
	{
		readyToJump = true;
		jumping = false;
		isJumping = false;
	}

	private void CounterMovement(float x, float y, Vector2 mag)
	{
		if (!grounded || jumping) return;
		float dt = Time.fixedDeltaTime;

		if ((Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f) || (mag.x < -threshold && x > 0f) || (mag.x > threshold && x < 0f))
		{
          //  Rb.AddForce(moveSpeed * Orientation.right * dt * (-mag.x) * counterMovement);
          Rb.AddForce(moveSpeed * transform.right * dt * (-mag.x) * counterMovement);
        }
			

		if ((Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f) || (mag.y < -threshold && y > 0f) || (mag.y > threshold && y < 0f))
		{
            //Rb.AddForce(moveSpeed * Orientation.forward * dt * (-mag.y) * counterMovement);
            Rb.AddForce(moveSpeed * transform.forward * dt * (-mag.y) * counterMovement);
        }
			

		if (Mathf.Sqrt(Rb.linearVelocity.x * Rb.linearVelocity.x + Rb.linearVelocity.z * Rb.linearVelocity.z) > maxSpeed)
		{
			float ny = Rb.linearVelocity.y;
			Vector3 n = Rb.linearVelocity.normalized * maxSpeed;
			Rb.linearVelocity = new Vector3(n.x, ny, n.z);
		}
	}

	public Vector2 FindVelRelativeToLook()
	{
		// Use surface velocity (ignore vertical) so speed checks work the same on slopes.
		//float current = Orientation.eulerAngles.y;
		float current = transform.eulerAngles.y;
		bool onSlope = OnSlope();
		Vector3 surfaceVel = Vector3.ProjectOnPlane(Rb.linearVelocity, onSlope ? slopeHit.normal : Vector3.up);
		Quaternion yawRot = Quaternion.Euler(0f, current, 0f);
		Vector3 local = Quaternion.Inverse(yawRot) * surfaceVel;
		return new Vector2(local.x, local.z);
	}

	/// <summary>True if standing on a slope within maxSlopeAngle.</summary>
	public bool OnSlope()
	{
		CapsuleCollider cap = GetComponentInChildren<CapsuleCollider>();
		if (cap == null) return false;
		capsuleHeight = cap.height;
		if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, capsuleHeight / 2f + 1f) &&
		    Vector3.Angle(Vector3.up, slopeHit.normal) < maxSlopeAngle)
			return true;
		return false;
	}

	private Vector3 GetSlopeMoveDirection()
	{
		return Vector3.ProjectOnPlane(Rb.linearVelocity, slopeHit.normal).normalized;
	}

	public void GettingHit()
	{
		beingHit = true;

		Debug.Log("============Getting Hit=========");

        if (Rb != null)
            Rb.constraints = RigidbodyConstraints.None;

        if (ragdollScript != null) ragdollScript.EnableRagdoll();

		StartCoroutine(GettingHitCoolDown());
	}

	private IEnumerator GettingHitCoolDown()
	{
		Debug.Log("GettingHitCoolDown");
		yield return new WaitForSeconds(2f);
		if (ragdollScript != null)
		{
			if (ragdollScript.ragdoll) ragdollScript.DisableRagdoll();
            StartCoroutine(AutoGetUpRoutine());
            beingHit = false;
		}
	}

	public void StopRagdoll()
	{
		Debug.Log("Stop Ragdoll");
		if (ragdollScript != null && ragdollScript.ragdoll)
		{
			//SetAnimGetUp(false);
			ragdollScript.DisableRagdoll();
			StartCoroutine(AutoGetUpRoutine());
			beingHit = false;
		}
	}

	/// <summary>Expose animator for Player/AI (or use CharacterAnimator).</summary>
	public Animator Anim => characterAnimator != null ? characterAnimator.Anim : anim;

	/// <summary>Rigidbody (from CharacterPhysics when assigned, else direct reference).</summary>
	public Rigidbody Rb => characterPhysics != null ? characterPhysics.Rb : rb;

	public bool Jumping => jumping;

    private bool gettingUp;

    private IEnumerator AutoGetUpRoutine()
    {
		Debug.Log("==============AutoGetUpRoutine==========");

        gettingUp = true;
        canMove = false;

        // stop momentum
        Rb.linearVelocity = Vector3.zero;
        Rb.angularVelocity = Vector3.zero;

        // wait 1 frame so ragdoll fully disables
        yield return null;

        // wait until grounded (if in air) with a fallback timeout
        float groundWaitTimer = 0f;
        while (!grounded && groundWaitTimer < 2f)
        {
            groundWaitTimer += Time.deltaTime;
            yield return null;
        }

        // smooth upright
        float duration = 0.25f;
        float t = 0f;

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        Vector3 startPos = Rb.position;
        Vector3 targetPos = startPos + Vector3.up * 0.15f;

        while (t < 1f)
        {
            t += Time.fixedDeltaTime / duration;

            Rb.MoveRotation(Quaternion.Slerp(startRot, targetRot, t));
            Rb.MovePosition(Vector3.Lerp(startPos, targetPos, t));

            yield return new WaitForFixedUpdate();
        }

        // lock tilt after getup
       // Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // reset states
        readyToJump = true;
        jumping = false;
        isJumping = false;
        canDive = true;
        waitDelay = false;

        beingHit = false;
        canMove = true;
        gettingUp = false;
    }

    public void RotateTowards(Vector3 dir, float turnSpeed)
    {
        if (dir == Vector3.zero || Rb == null) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion newRot = Quaternion.Slerp(
            Rb.rotation,
            targetRot,
            turnSpeed * Time.fixedDeltaTime
        );

        Rb.MoveRotation(newRot);
    }


    private void KeepUprightOnGround()
    {
        if (!grounded) return;

        Vector3 upDir = OnSlope() ? slopeHit.normal : Vector3.up;

        Vector3 forwardFlat = Vector3.ProjectOnPlane(transform.forward, upDir).normalized;
        if (forwardFlat == Vector3.zero) forwardFlat = transform.forward;

        Quaternion targetRot = Quaternion.LookRotation(forwardFlat, upDir);

        float uprightSpeed = 12f;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * uprightSpeed);
    }

    private void StepClimb()
    {
        // 1. Only climb if grounded and moving
        if (beingHit||!grounded || isJumping || (new Vector2(x, y).magnitude < 0.1f)) return;

        // 2. Setup rays
        // Direction we are trying to move (Flattened to ignore looking down/up)
        Vector3 moveDir = (Orientation.forward * y + Orientation.right * x).normalized;
        Vector3 inputDir = Vector3.ProjectOnPlane(moveDir, Vector3.up).normalized;
        
        if (inputDir == Vector3.zero) return;

        // Feet level ray (just above ground)
        Vector3 feetOrigin = transform.position + Vector3.up * 0.05f; 
        
        // Head/Step-top level rays (Check 2 heights for redundancy)
        // Ray 1: Base step height
        Vector3 stepOrigin1 = transform.position + Vector3.up * (stepHeight + 0.1f);
        // Ray 2: Slightly higher (Check clearance)
        Vector3 stepOrigin2 = transform.position + Vector3.up * (stepHeight + 0.5f);

        RaycastHit hitLower;
        // Check for obstacle at feet level
        if (Physics.Raycast(feetOrigin, inputDir, out hitLower, stepCheckDistance, stepLayerMask))
        {
            // Avoid stepping up walls (filter by angle if needed, or check normal)
            // If the normal is steep (wall), we might still want to step if it's short enough.
            
            // Check for space above that obstacle using 2 rays.
            // "If any step origin ray is false (clear) between the 2"
            bool hitTop1 = Physics.Raycast(stepOrigin1, inputDir, stepCheckDistanceHead1, stepLayerMask);
            bool hitTop2 = Physics.Raycast(stepOrigin2, inputDir, stepCheckDistanceHead2, stepLayerMask);

            if (!hitTop1 || !hitTop2)
            {
                // Verify we aren't just hitting a slope we can walk up anyway (mostly handled by Move/OnSlope)
                // But specifically for vertical steps:
                
                // Add upward force to "hop" over the step
                // Effectively smoothing the Y position
                // Use MovePosition to preserve interpolation
                Rb.MovePosition(Rb.position + new Vector3(0f, stepSmooth * Time.fixedDeltaTime, 0f));

                //  Debug.Log("<color=cyan> STEP UP </color>");
            }
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Calculate Origins (Match StepClimb)
        Vector3 feetOrigin = transform.position + Vector3.up * 0.05f;
        Vector3 stepOrigin1 = transform.position + Vector3.up * (stepHeight + 0.1f);
        Vector3 stepOrigin2 = transform.position + Vector3.up * (stepHeight + 0.5f);

        // 2. Determine Direction
        Vector3 dir = transform.forward; // Default to forward for Editor mode
        if (Application.isPlaying)
        {
            // Use actual Input direction if playing
            Vector3 moveDir = (Orientation.forward * y + Orientation.right * x).normalized;
            // FLATTEN direction so looking down doesn't aim rays into the floor
             Vector3 inputDir = Vector3.ProjectOnPlane(moveDir, Vector3.up).normalized;

            if (inputDir.magnitude > 0.1f) 
            {
                dir = inputDir;
            }
        }

        // 3. Feet Ray (Lower) - We WANT this to HIT (Red = Obstacle Found)
        bool feetHit = Physics.Raycast(feetOrigin, dir, stepCheckDistance, stepLayerMask);
        Gizmos.color = feetHit ? Color.red : Color.white; 
        Gizmos.DrawLine(feetOrigin, feetOrigin + dir * stepCheckDistance);
        Gizmos.DrawWireSphere(feetOrigin + dir * stepCheckDistance, 0.02f);

        // 4. Step Ray 1 (Upper) - RED if Hit, GREEN if Clear
        bool stepHit1 = Physics.Raycast(stepOrigin1, dir, stepCheckDistanceHead1, stepLayerMask);
        Gizmos.color = stepHit1 ? Color.red : Color.green;
        Gizmos.DrawLine(stepOrigin1, stepOrigin1 + dir * stepCheckDistanceHead1);
        Gizmos.DrawWireSphere(stepOrigin1 + dir * stepCheckDistanceHead1, 0.02f);

        // 5. Step Ray 2 (Upper High) - RED if Hit, GREEN if Clear
        bool stepHit2 = Physics.Raycast(stepOrigin2, dir, stepCheckDistanceHead2, stepLayerMask);
        Gizmos.color = stepHit2 ? Color.red : Color.green;
        Gizmos.DrawLine(stepOrigin2, stepOrigin2 + dir * stepCheckDistanceHead2);
        Gizmos.DrawWireSphere(stepOrigin2 + dir * stepCheckDistanceHead2, 0.02f); 

        // Visual Aid: If Condition Met (Feet Hit AND (Top1 Miss OR Top2 Miss))
        if (feetHit && (!stepHit1 || !stepHit2))
        {
             Gizmos.color = Color.cyan;
             // Draw arrow up indicating step will happen
             Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
        }
    }
}
