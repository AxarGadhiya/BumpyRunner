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
	public float maxSpeed = 20f;
	public float counterMovement = 0.175f;
	private float threshold = 0.01f;
	public float maxSlopeAngle = 35f;

	[Header("Jump & Dive")]
	public float jumpForce = 550f;
	public float jumpCooldown = 0.5f;
	public float diveSpeed = 200f;
	public bool useDiveMomentum = true;

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

	private Transform Orientation => movementOrientation != null ? movementOrientation : transform;
	private Vector3 normalVector => characterPhysics != null ? characterPhysics.NormalVector : Vector3.up;

	private void Awake()
	{
		if (characterPhysics == null) characterPhysics = GetComponent<CharacterPhysics>();
		if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
		if (rb == null) rb = GetComponent<Rigidbody>();
		if (characterPhysics != null && rb == null) rb = characterPhysics.Rb;
		if (anim == null) anim = GetComponentInChildren<Animator>();
		if (ragdollScript == null) ragdollScript = GetComponentInChildren<Ragdoll>();
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
		if (beingHit) return;
		Move();
		ProcessDive();
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
		Rb.AddForce(Vector3.down * dt * 50f);
		Vector2 mag = FindVelRelativeToLook();
		float num = mag.x;
		float num2 = mag.y;

		if (useCounterMovement && !beingHit)
			CounterMovement(x, y, mag);

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
		bool groundedAndUnderSpeed = grounded && !jumping && Rb.linearVelocity.magnitude <= num3;

		// On slope: apply force along the slope (input direction projected onto slope plane)
		if (onSlope && groundedAndUnderSpeed)
		{
			Vector3 inputDir = Orientation.forward * y + Orientation.right * x;
			if (inputDir.sqrMagnitude > 0.01f)
			{
				Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir.normalized, slopeHit.normal).normalized;
				if (slopeDir.sqrMagnitude > 0.01f)
				{
					// Same speed in all directions: cap input magnitude at 1 so diagonal isn't faster
					float inputMagnitude = Mathf.Min(1f, inputDir.magnitude);
					Rb.AddForce(slopeDir * inputMagnitude * moveSpeed * dt * num4 * num5, ForceMode.Force);
				}
			}
		}
		// Flat ground: apply horizontal force in input direction, same magnitude for forward/diagonal
		else if (!onSlope && groundedAndUnderSpeed)
		{
			Vector3 inputDir = Orientation.forward * y + Orientation.right * x;
			if (inputDir.sqrMagnitude > 0.01f)
			{
				Vector3 moveDir = inputDir.normalized;
				float inputMagnitude = Mathf.Min(1f, inputDir.magnitude);
				Rb.AddForce(moveDir * inputMagnitude * moveSpeed * dt, ForceMode.Force);
			}
		}

		if (x > 0f && num > num3) x = 0f;
		if (x < 0f && num < -num3) x = 0f;
		if (y > 0f && num2 > num3) y = 0f;
		if (y < 0f && num2 < -num3) y = 0f;

		// When not on slope (or in air), apply normal horizontal movement (same speed all directions)
		if (!onSlope)
		{
			if (useWallAvoidance)
			{
				if (Rb.linearVelocity.magnitude <= num3)
				{
					if (_hitLeft)
						Rb.AddForce(transform.right * y * moveSpeed * dt * num4 * num5, ForceMode.Force);
					else if (_hitRight)
						Rb.AddForce(transform.right * (-y) * moveSpeed * dt * num4 * num5, ForceMode.Force);
				}
				if (Rb.linearVelocity.magnitude <= num3)
					Rb.AddRelativeForce(Vector3.forward * y * moveSpeed * dt * num4 * num5, ForceMode.Force);
			}
			else
			{
				Vector3 inputDir = Orientation.forward * y + Orientation.right * x;
				if (inputDir.sqrMagnitude > 0.01f)
				{
					float inputMagnitude = Mathf.Min(1f, inputDir.magnitude);
					Vector3 moveDir = inputDir.normalized;
					Rb.AddForce(moveDir * inputMagnitude * moveSpeed * dt * num4 * num5, ForceMode.Force);
				}
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
			Rb.AddForce(transform.forward * momentum * Rb.linearVelocity.magnitude / 4f * 200f, ForceMode.Force);
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
		Rb.AddForce(Vector2.up * jumpForce * 1.5f);
		Rb.AddForce(normalVector * jumpForce * 0.5f);
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
			Rb.AddForce(moveSpeed * Orientation.right * dt * (-mag.x) * counterMovement);
		if ((Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f) || (mag.y < -threshold && y > 0f) || (mag.y > threshold && y < 0f))
			Rb.AddForce(moveSpeed * Orientation.forward * dt * (-mag.y) * counterMovement);
		if (Mathf.Sqrt(Rb.linearVelocity.x * Rb.linearVelocity.x + Rb.linearVelocity.z * Rb.linearVelocity.z) > maxSpeed)
		{
			float ny = Rb.linearVelocity.y;
			Vector3 n = Rb.linearVelocity.normalized * maxSpeed;
			Rb.linearVelocity = new Vector3(n.x, ny, n.z);
		}
	}

	public Vector2 FindVelRelativeToLook()
	{
		float current = Orientation.eulerAngles.y;
		float target = Mathf.Atan2(Rb.linearVelocity.x, Rb.linearVelocity.z) * 57.29578f;
		float num = Mathf.DeltaAngle(current, target);
		float num2 = 90f - num;
		float magnitude = Rb.linearVelocity.magnitude;
		return new Vector2(magnitude * Mathf.Cos(num2 * (Mathf.PI / 180f)), magnitude * Mathf.Cos(num * (Mathf.PI / 180f)));
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
		if (ragdollScript != null) ragdollScript.EnableRagdoll();
		StartCoroutine(GettingHitCoolDown());
	}

	private IEnumerator GettingHitCoolDown()
	{
		yield return new WaitForSeconds(2f);
		if (ragdollScript != null && ragdollScript.ragdoll)
		{
			ragdollScript.DisableRagdoll();
			beingHit = false;
		}
	}

	public void StopRagdoll()
	{
		if (ragdollScript != null && ragdollScript.ragdoll)
		{
			SetAnimGetUp(false);
			ragdollScript.DisableRagdoll();
			beingHit = false;
		}
	}

	/// <summary>Expose animator for Player/AI (or use CharacterAnimator).</summary>
	public Animator Anim => characterAnimator != null ? characterAnimator.Anim : anim;

	/// <summary>Rigidbody (from CharacterPhysics when assigned, else direct reference).</summary>
	public Rigidbody Rb => characterPhysics != null ? characterPhysics.Rb : rb;

	public bool Jumping => jumping;
}
