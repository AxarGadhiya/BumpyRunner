using UnityEngine;

/// <summary>
/// Handles animation by calling the Animator only.
/// Other scripts (Player, CharacterController, AI) decide what state to show; this script only applies it to the Animator.
/// No physics or controller logic here.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
	[SerializeField] private Animator anim;

	[Header("Optional smoothing for float params")]
	[SerializeField] private float horizontalVerticalDampTime = 0.05f;

	private void Awake()
	{
		if (anim == null) anim = GetComponentInChildren<Animator>();
	}

	// --- Bool parameters ---
	public void SetJump(bool value)
	{
		if (anim != null) anim.SetBool("Jump", value);
	}

	public void SetFall(bool value)
	{
		if (anim != null) anim.SetBool("Fall", value);
	}

	public void SetIncline(bool value)
	{
		if (anim != null) anim.SetBool("Incline", value);
	}

	public void SetDiveStart(bool value)
	{
		if (anim != null) anim.SetBool("DiveStart", value);
	}

	public void SetGetUp(bool value)
	{
		if (anim != null) anim.SetBool("GetUp", value);
	}

	// --- Float parameters (with optional damp) ---
	public void SetHorizontal(float value)
	{
		if (anim != null) anim.SetFloat("horizontal", value, horizontalVerticalDampTime, Time.deltaTime);
	}

	public void SetVertical(float value)
	{
		if (anim != null) anim.SetFloat("vertical", value, horizontalVerticalDampTime, Time.deltaTime);
	}

	// --- Trigger ---
	public void SetTrigger(string triggerName)
	{
		if (anim != null) anim.SetTrigger(triggerName);
	}

	public Animator Anim => anim;
}
