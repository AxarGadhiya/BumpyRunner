using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Assigns CinemachineCamera Follow/LookAt at runtime when the player spawns.
/// Attach to a persistent GameObject (e.g. empty "CameraRig" or Main Camera).
/// Leave Virtual Camera unassigned to auto-find by name "CM vcam Player".
/// Obstacle avoidance: add CinemachineDeoccluder (or CinemachineDecollider) to the Virtual Camera.
/// </summary>
public class CinemachineCameraSetup : MonoBehaviour
{
	[Tooltip("Assign in Inspector, or leave empty to find by name below.")]
	[SerializeField] private CinemachineCamera virtualCamera;

	/// <summary>Virtual camera (for other scripts that need Follow/LookAt).</summary>
	public CinemachineCamera VirtualCamera => virtualCamera;

	[Tooltip("If true, will try to find Player by tag when Follow is null (e.g. after spawn).")]
	[SerializeField] private bool autoFindPlayerWhenNull = true;
	[SerializeField] private float autoFindInterval = 0.5f;

	private float _nextFindTime;

	private void Start()
	{
		if (virtualCamera == null)
			TryFindVirtualCamera();
	}

	private void TryFindVirtualCamera()
	{
		var cameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (var cam in cameras)
		{
			if (cam.gameObject.name == "CM vcam Player")
			{
				virtualCamera = cam;
				break;
			}
		}
	}

	private void Update()
	{
		if (virtualCamera == null) return;
		if (!autoFindPlayerWhenNull || virtualCamera.Follow != null) return;
		if (Time.time < _nextFindTime) return;
		_nextFindTime = Time.time + autoFindInterval;
		TryFindAndAssignPlayer();
	}



	/// <summary>Call this when the player is spawned to set the camera target. Prefer this over auto-find.</summary>
	public void SetPlayerTarget(Transform playerBodyTransform)
	{
		if (virtualCamera == null) return;
		virtualCamera.Follow = playerBodyTransform;
		virtualCamera.LookAt = playerBodyTransform;
		SetOrbitBehindPlayer();
	}

	/// <summary>Set Follow and optionally LookAt manually.</summary>
	public void SetTarget(Transform follow, Transform lookAt = null)
	{
		if (virtualCamera == null) return;
		virtualCamera.Follow = follow;
		virtualCamera.LookAt = lookAt != null ? lookAt : follow;
		SetOrbitBehindPlayer();
	}

	/// <summary>If using Orbital Follow, set orbit so camera is behind the player (180° = behind, not in front).</summary>
	private void SetOrbitBehindPlayer()
	{
		var orbital = virtualCamera != null ? virtualCamera.GetComponent<CinemachineOrbitalFollow>() : null;
		if (orbital == null) return;
		const float behindDeg = 180f; // 180° = behind player so you see player's back
		orbital.HorizontalAxis.Value = behindDeg;
		orbital.HorizontalAxis.Center = behindDeg;
	}

	private void TryFindAndAssignPlayer()
	{
		var player = GameObject.FindGameObjectWithTag("Player");
		if (player == null) return;
		// Same as CameraLook: use child 0 as camera target (body/head)
		Transform target = player.transform.childCount > 0 ? player.transform.GetChild(0) : player.transform;
		SetPlayerTarget(target);
	}
}
