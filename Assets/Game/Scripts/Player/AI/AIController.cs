using System;
using System.Collections;
using UnityEngine;

public class AIController : MonoBehaviour
{
	public Transform target;

	public Transform respawnPoint;

    [SerializeField] Rigidbody rb;

	[SerializeField] Animator anim;

	public Transform sensorRaycast;

	public bool canMove = true;

	public bool beingHit;

	public Ragdoll ragdollScript;

	//public HugController hugScript;

	public bool disableOnAwake = true;

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

	public bool canDive = true;

	public bool useDiveMomentum;

	public float diveSpeed = 200f;

	private float x;

	private float y;

	private bool sprinting;

	private bool crouching;

	private bool dive;

	public bool jumping;

	private Vector3 normalVector = Vector3.up;

	private Vector3 wallNormalVector;

	//private PhotonView view;

	private bool hitRight;

	private bool hitLeft;

	private float momentum = 1f;

	private bool waitDelay;

	private float desiredX;

	private bool cancellingGrounded;

	private float capsuleHeight;

	private bool getUp;

	private void Awake()
	{
		//view = GetComponent<PhotonView>();
		//rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		if (GameObject.FindGameObjectWithTag("TargetBot") != null)
		{
			target = GameObject.FindGameObjectWithTag("TargetBot").transform;
		}
		//sensorRaycast = base.transform.GetChild(5);
		//anim = GetComponentInChildren<Animator>();
		ragdollScript = GetComponentInChildren<Ragdoll>();
		//hugScript = GetComponent<HugController>();
		playerScale = base.transform.localScale;
	}

	private void FixedUpdate()
	{
		if (!disableOnAwake && !beingHit)
		{
			Move();
			Dive();
		}
	}

	private void Update()
	{
		if (base.gameObject.layer == LayerMask.NameToLayer("Bot"))
		{
			base.gameObject.transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("Bot");
		}
		if (!disableOnAwake)
		{
			FollowTargetWithRotation(target, 0f, 0f);
			MyInput();
			CheckWall();
		}
	}

	private void CheckWall()
	{
		if (!Physics.Raycast(sensorRaycast.position, -sensorRaycast.up, out var hitInfo, 5f) && grounded)
		{
			Jump();
			anim.SetBool("Jump", value: true);
		}
		if (Physics.Raycast(base.transform.GetChild(3).position, base.transform.GetChild(3).forward, out hitInfo, 1f) && grounded && hitInfo.transform.gameObject.tag != "Player" && hitInfo.transform.gameObject.tag != "TargetBot")
		{
			StartCoroutine(JumpAndDive());
		}
		if (Physics.Raycast(base.transform.GetChild(3).position, base.transform.GetChild(3).right, out hitInfo, 1f))
		{
			if (grounded)
			{
				hitRight = true;
			}
		}
		else
		{
			hitRight = false;
		}
		if (Physics.Raycast(base.transform.GetChild(3).position, -base.transform.GetChild(3).right, out hitInfo, 1f))
		{
			if (grounded)
			{
				hitLeft = true;
			}
		}
		else
		{
			hitLeft = false;
		}
	}

	private void MyInput()
	{
		y = 1f;
		if (jumping)
		{
			anim.SetBool("Jump", value: true);
		}
		if (readyToJump && !grounded)
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
			float value = 1f;
			if (!isJumping && grounded)
			{
				anim.SetFloat("vertical", value, 0.05f, Time.deltaTime * 1f);
			}
		}
	}

	private void FollowTargetWithRotation(Transform target, float distanceToStop, float speed)
	{
		if (Vector3.Distance(base.transform.position, target.position) > distanceToStop)
		{
			Vector3 forward = target.position - base.transform.position;
			forward.y = 0f;
			Quaternion b = Quaternion.LookRotation(forward);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, b, Time.deltaTime * 5f);
		}
	}

	private void Move()
	{
		rb.AddForce(Vector3.down * Time.deltaTime * 50f);
		Vector2 vector = FindVelRelativeToLook();
		float num = vector.x;
		float num2 = vector.y;
		_ = beingHit;
		float num3 = maxSpeed;
		if (!canMove)
		{
			return;
		}
		if (!OnSlope() && grounded && !jumping && rb.linearVelocity.magnitude <= num3)
		{
			rb.AddForce(GetSlopeMoveDirection() * Mathf.Abs(y) * moveSpeed * Time.deltaTime, ForceMode.Acceleration);
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
		if (!grounded)
		{
			num4 = 1f;
			num5 = 1f;
		}
		if (rb.linearVelocity.magnitude <= num3)
		{
			if (hitLeft)
			{
				rb.AddForce(base.transform.right * y * moveSpeed * Time.deltaTime * num4 * num5, ForceMode.Force);
			}
			else if (hitRight)
			{
				rb.AddForce(base.transform.right * (0f - y) * moveSpeed * Time.deltaTime * num4 * num5, ForceMode.Force);
			}
		}
		if (rb.linearVelocity.magnitude <= num3)
		{
			rb.AddRelativeForce(Vector3.forward * y * moveSpeed * Time.deltaTime * num4 * num5, ForceMode.Acceleration);
		}
	}

	private void Dive()
	{
		if (!canDive && grounded && (int)rb.linearVelocity.magnitude <= 14)
		{
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

	private IEnumerator DiveDelay()
	{
		yield return new WaitForSeconds(1f);
		waitDelay = false;
	}

	private IEnumerator JumpAndDive()
	{
		anim.SetBool("Jump", value: true);
		Jump();
		yield return new WaitForSeconds(0.15f);
		dive = true;
	}

	private void Jump()
	{
		if (grounded && readyToJump)
		{
			isJumping = true;
			readyToJump = false;
			rb.AddForce(Vector2.up * jumpForce * 1.5f);
			rb.AddForce(normalVector * jumpForce * 0.5f);
			Invoke(nameof(ResetJump), jumpCooldown);
		}
	}

	private void ResetJump()
	{
		readyToJump = true;
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (!(collision.gameObject.GetComponent<Rigidbody>() == null) && collision.gameObject.tag == "Player")
		{
			_ = collision.gameObject.GetComponent<Rigidbody>().linearVelocity.magnitude;
			_ = 7f;
		}
	}

	public Vector2 FindVelRelativeToLook()
	{
		float current = Camera.main.transform.eulerAngles.y;
		float num = Mathf.Atan2(rb.linearVelocity.x, rb.linearVelocity.z) * 57.29578f;
		float num2 = Mathf.DeltaAngle(current, num);
		float num3 = 90f - num2;
		float magnitude = rb.linearVelocity.magnitude;
		return new Vector2(y: magnitude * Mathf.Cos(num2 * ((float)Math.PI / 180f)), x: magnitude * Mathf.Cos(num3 * ((float)Math.PI / 180f)));
	}

	private bool IsFloor(Vector3 v)
	{
		return Vector3.Angle(Vector3.up, v) < 35f;
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
				grounded = true;
				anim.SetBool("Jump", value: false);
				cancellingGrounded = false;
				normalVector = normal;
				CancelInvoke(nameof(StopGrounded));
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

    //---------------[PunRPC]-----------------
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
		anim.SetBool("GetUp", value: false);
		ragdollScript.DisableRagdoll();
	}

    //---------------[PunRPC]--------------------
    private void StopRagdollPun()
	{
		ragdollScript.DisableRagdoll();
		getUp = false;
		beingHit = false;
	}

	private IEnumerator GettingHitCoolDown()
	{
		yield return new WaitForSeconds(2f);
	}

	private void StopGrounded()
	{
		anim.SetBool("Jump", value: false);
		grounded = false;
	}
}
