using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Fall Guys–style orbit camera: player always in center of screen; camera orbits 360° horizontally and clamped vertically.
/// Camera rotates ONLY from: (1) FixedTouchField drag on mobile, (2) Mouse movement on PC.
/// A/D or movement joystick do NOT rotate the camera — only the player changes direction.
/// Main Camera must NOT be a child of the Player so it does not rotate when the player turns.
/// </summary>
[DefaultExecutionOrder(200)]
public class CinemachineLookFromInput : MonoBehaviour
{
	[Tooltip("Look-only touch area (mobile). Assign the drag-to-look area — NOT the movement joystick.")]
	[SerializeField] FixedTouchField touchLookField;

	[Tooltip("Look sensitivity. Touch: ~0.25, Mouse: ~10.")]
	[SerializeField] float sensitivity = 0.25f;

	[Tooltip("Horizontal (yaw): full 360° orbit. X = min, Y = max degrees. Use -180 to 180 for full orbit (Fall Guys style).")]
	[SerializeField] Vector2 yawRange = new Vector2(-180f, 180f);

	[Tooltip("Vertical (pitch) limits: X = min (look down), Y = max (look up). Clamped so camera does not flip (e.g. -40 to 40).")]
	[SerializeField] Vector2 pitchRange = new Vector2(-40f, 40f);

	[Tooltip("Orbit offset from target: (0, height, -distance). Camera orbits around player at this offset; player stays centered.")]
	[SerializeField] Vector3 offsetFromTarget = new Vector3(0f, 2f, -6.5f);

	[Header("Obstacle (use Cinemachine Deoccluder/Decollider)")]
	[Tooltip("When true, camera POSITION is from Cinemachine (vcam Follow + Deoccluder/Decollider). Only rotation from touch/mouse. Enable this and add Cinemachine Deoccluder (and optionally Decollider) to the vcam for collider-based obstacle avoidance. For future silhouette shader (player visible through walls), you can reduce Deoccluder strength or disable it so the camera doesn't pull forward.")]
	[SerializeField] bool useCinemachineForPosition = false;

	[Tooltip("FOV override. 0 = don't override.")]
	[SerializeField] float fieldOfViewOverride = 60f;

	float _yaw;
	float _pitch;
	bool _initialLookSynced;

	void Start()
	{
		// If Main Camera is under the Player, it would rotate when player turns (A/D). Unparent so only we control it.
		Camera cam = Camera.main;
		Transform target = GetFollowTarget();
		if (cam != null && target != null && cam.transform.IsChildOf(target))
		{
			cam.transform.SetParent(null);
		}
	}

	void LateUpdate()
	{
		Camera cam = Camera.main;
		if (cam == null) return;

		Transform target = GetFollowTarget();
		if (target != null && !_initialLookSynced)
			_initialLookSynced = SyncInitialLookAtTarget(cam, target);

		// ONLY these two sources — never Horizontal (A/D) or movement joystick
		float dx = 0f, dy = 0f;
		if (touchLookField != null && touchLookField.Pressed)
		{
			// Mobile: only when user drags the LOOK touch area (left/right = yaw, up/down = pitch)
			dx = touchLookField.TouchDist.x * sensitivity;
			dy = -touchLookField.TouchDist.y * sensitivity;
		}
		else if (SystemInfo.deviceType == DeviceType.Desktop)
		{
			// PC: only mouse movement (never Horizontal/Vertical = A/D or arrows)
			dx = Input.GetAxis("Mouse X") * sensitivity * 10f;
			dy = -Input.GetAxis("Mouse Y") * sensitivity * 10f;
		}

		_yaw += dx;
		_pitch += dy;
		_yaw = Mathf.Clamp(_yaw, yawRange.x, yawRange.y);
		_pitch = Mathf.Clamp(_pitch, pitchRange.x, pitchRange.y);

		if (target != null && !useCinemachineForPosition)
		{
			// Orbit: camera moves around player on a sphere; player stays in center of screen (Fall Guys style).
			Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
			Vector3 orbitOffset = orbitRot * offsetFromTarget;
			cam.transform.position = target.position + orbitOffset;
			// Look at player so player is always centered.
			cam.transform.rotation = Quaternion.LookRotation(target.position - cam.transform.position, Vector3.up);
		}
		else
		{
			Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
			cam.transform.rotation = rot;
			// When useCinemachineForPosition is true, position is set by Cinemachine (Follow + Deoccluder/Decollider on vcam)
		}

		if (fieldOfViewOverride > 0f)
			cam.fieldOfView = fieldOfViewOverride;
	}

	Transform GetFollowTarget()
	{
		var setup = GetComponent<CinemachineCameraSetup>();
		if (setup == null) return null;
		var vcam = setup.VirtualCamera;
		return vcam != null ? vcam.Follow : null;
	}

	bool SyncInitialLookAtTarget(Camera cam, Transform target)
	{
		// Compute orbit angles from current camera position so orbit matches existing placement.
		Vector3 offset = cam.transform.position - target.position;
		float distSq = offset.sqrMagnitude;
		if (distSq < 0.01f) return false;
		Vector3 d = offset.normalized;
		_yaw = Mathf.Atan2(d.x, -d.z) * Mathf.Rad2Deg;
		_pitch = Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg;
		_yaw = Mathf.Clamp(_yaw, yawRange.x, yawRange.y);
		_pitch = Mathf.Clamp(_pitch, pitchRange.x, pitchRange.y);
		return true;
	}
}
