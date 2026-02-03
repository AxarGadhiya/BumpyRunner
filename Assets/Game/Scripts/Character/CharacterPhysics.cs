using System;
using UnityEngine;

/// <summary>
/// Rigidbody and collision-related logic only.
/// Handles grounding, floor normal, and hit detection from other players.
/// CharacterController uses this for movement and jump/dive state.
/// </summary>
public class CharacterPhysics : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Rigidbody rb;

	[Header("Ground")]
	public LayerMask whatIsGround;

	[Tooltip("Contact normal angle from up to count as floor (degrees).")]
	[SerializeField] private float floorAngle = 35f;

	[Tooltip("Vertical velocity threshold to count as landing (not still going up).")]
	[SerializeField] private float landingVelocityY = 0.5f;

	[Tooltip("Delay before clearing grounded when no floor contact.")]
	[SerializeField] private float ungroundDelay = 3f;

	/// <summary>Fired when this character is hit by another player (other's velocity >= threshold).</summary>
	public event Action OnHitByPlayer;

	/// <summary>Fired when character lands on floor (grounded and velocity.y <= threshold).</summary>
	public event Action OnLanded;

	public Rigidbody Rb => rb;
	public bool Grounded => grounded;
	public Vector3 NormalVector => normalVector;

	private bool grounded;
	private Vector3 normalVector = Vector3.up;
	private bool cancellingGrounded;

	private void Awake()
	{
		if (rb == null) rb = GetComponent<Rigidbody>();
	}

	private void OnCollisionEnter(Collision collision)
	{
		Rigidbody otherRb = collision.gameObject.GetComponent<Rigidbody>();
		if (otherRb == null) return;
		if (collision.gameObject.CompareTag("Player") && otherRb.linearVelocity.magnitude >= 7f)
			OnHitByPlayer?.Invoke();
	}

	private void OnCollisionStay(Collision other)
	{
		if ((whatIsGround.value & (1 << other.gameObject.layer)) == 0) return;

		for (int i = 0; i < other.contactCount; i++)
		{
			Vector3 normal = other.contacts[i].normal;
			if (!IsFloor(normal)) continue;

			// Always stay grounded on valid floor contact (including slopes), otherwise "Fall" will flicker
			// when running uphill because velocity.y can be > landingVelocityY.
			bool wasGrounded = grounded;
			grounded = true;
			cancellingGrounded = false;
			normalVector = normal;
			CancelInvoke(nameof(StopGrounded));

			// Only treat it as a "land" event when we're not moving upward anymore.
			if (!wasGrounded && rb.linearVelocity.y <= landingVelocityY)
				OnLanded?.Invoke();
		}

		if (!cancellingGrounded)
		{
			cancellingGrounded = true;
			// Keep previous behaviour but make it deterministic: this is effectively "ungroundDelay fixed steps".
			Invoke(nameof(StopGrounded), Time.fixedDeltaTime * ungroundDelay);
		}
	}

	private void StopGrounded()
	{
		grounded = false;
	}

	/// <summary>True if normal is within floorAngle of up.</summary>
	public bool IsFloor(Vector3 normal)
	{
		return Vector3.Angle(Vector3.up, normal) < floorAngle;
	}
}
