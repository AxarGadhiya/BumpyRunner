using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Applies all Cinemachine Player VCam Inspector settings in one go.
/// Menu: Game > Cinemachine > Apply All Player VCam Settings
/// </summary>
public static class CinemachinePlayerVcamSettingsEditor
{
	const string VcamName = "CM vcam Player";
	// Like old camera: camera BEHIND player (third-person), fixed offset. Touch only changes look direction, not position.
	static readonly Vector3 FollowOffsetBehind = new Vector3(0f, 2f, -6.5f);
	const float OrbitalRadius = 6.5f;
	const float HorizontalAxisBehindDeg = 180f;
	const float VerticalAxisRangeMin = -40f;
	const float VerticalAxisRangeMax = 40f;
	const float VerticalAxisDefaultDeg = 15f;
	const string LegacyInputMouseX = "Mouse X";
	const string LegacyInputMouseY = "Mouse Y";
	const float LegacyGainX = 1f;
	const float LegacyGainY = -1f;
	const float TouchSensitivity = 0.25f;

	[MenuItem("Game/Cinemachine/Apply All Player VCam Settings (Like Old Camera)")]
	public static void ApplyLikeOldCamera()
	{
		var vcam = FindPlayerVcam();
		if (vcam == null)
		{
			Debug.LogWarning($"CinemachinePlayerVcamSettingsEditor: No GameObject named \"{VcamName}\" found.");
			return;
		}
		Undo.RecordObject(vcam, "Cinemachine Like Old Camera");
		ApplyFollowAheadOnly(vcam);
		ApplyCinemachineObstacleExtensions(vcam);
		ApplyLookFromInputOnSetup(true);
		EditorUtility.SetDirty(vcam);
		if (vcam.scene.IsValid())
			EditorSceneManager.MarkSceneDirty(vcam.scene);
		Debug.Log($"Applied Like Old Camera: camera behind player; Deoccluder + Decollider OFF (camera stays when player behind object — use silhouette). Follow uses smooth position damping when character moves ahead.");
	}

	[MenuItem("Game/Cinemachine/Apply Fall Guys Style (Orbit, Player Center)")]
	public static void ApplyFallGuysStyle()
	{
		var vcam = FindPlayerVcam();
		if (vcam == null)
		{
			Debug.LogWarning($"CinemachinePlayerVcamSettingsEditor: No GameObject named \"{VcamName}\" found.");
			return;
		}
		Undo.RecordObject(vcam, "Cinemachine Fall Guys Style");
		ApplyFollowAheadOnly(vcam);
		ApplyLookFromInputOnSetup(false, true); // orbit, player center: useCinemachineForPosition = false, Fall Guys orbit defaults
		EditorUtility.SetDirty(vcam);
		if (vcam.scene.IsValid())
			EditorSceneManager.MarkSceneDirty(vcam.scene);
		Debug.Log($"Applied Fall Guys style: camera orbits 360° horizontal, clamped vertical; player always in center of screen.");
	}

	[MenuItem("Game/Cinemachine/Apply All Player VCam Settings (Orbital + Mouse Drag)")]
	public static void ApplyAllSettings()
	{
		var vcam = FindPlayerVcam();
		if (vcam == null)
		{
			Debug.LogWarning($"CinemachinePlayerVcamSettingsEditor: No GameObject named \"{VcamName}\" found in the scene. Create a Cinemachine Camera and name it \"{VcamName}\".");
			return;
		}

		Undo.RecordObject(vcam, "Cinemachine Player VCam Settings");

		ApplyOrbitalFollow(vcam);
		ApplyInputAxisController(vcam);
		ApplyInputProviderOnSetup();

		EditorUtility.SetDirty(vcam);
		if (vcam.scene.IsValid())
			EditorSceneManager.MarkSceneDirty(vcam.scene);
		Debug.Log($"Applied Orbital Follow + mouse/touch rotation to \"{vcam.name}\" (camera behind player, drag to rotate).");
	}

	static GameObject FindPlayerVcam()
	{
		var cameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (var cam in cameras)
		{
			if (cam.gameObject.name == VcamName)
				return cam.gameObject;
		}
		return null;
	}

	// Smooth position damping when character moves ahead (no jerk); silhouette = camera stays when player behind object.
	const float FollowPositionDamping = 0.3f;

	static void ApplyFollowAheadOnly(GameObject vcamGo)
	{
		var orbital = vcamGo.GetComponent<CinemachineOrbitalFollow>();
		if (orbital != null)
			Undo.DestroyObjectImmediate(orbital);
		var inputController = vcamGo.GetComponent<CinemachineInputAxisController>();
		if (inputController != null)
			Undo.DestroyObjectImmediate(inputController);

		var follow = vcamGo.GetComponent<CinemachineFollow>();
		if (follow == null)
			follow = Undo.AddComponent<CinemachineFollow>(vcamGo);
		var so = new SerializedObject(follow);
		so.FindProperty("FollowOffset").vector3Value = FollowOffsetBehind;
		// Smooth follow when character moves ahead (smaller = more responsive, larger = smoother)
		var posDamp = so.FindProperty("PositionDamping") ?? so.FindProperty("m_PositionDamping");
		if (posDamp != null)
			posDamp.vector3Value = new Vector3(FollowPositionDamping, FollowPositionDamping, FollowPositionDamping);
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	static void ApplyCinemachineObstacleExtensions(GameObject vcamGo)
	{
		var deoccluder = vcamGo.GetComponent<CinemachineDeoccluder>();
		if (deoccluder == null)
			deoccluder = Undo.AddComponent<CinemachineDeoccluder>(vcamGo);
		var so = new SerializedObject(deoccluder);
		int playerLayer = LayerMask.NameToLayer("Player");
		so.FindProperty("CollideAgainst").intValue = playerLayer >= 0 ? ~(1 << playerLayer) : 1; // everything except Player
		so.FindProperty("IgnoreTag").stringValue = "Player";
		so.FindProperty("MinimumDistanceFromTarget").floatValue = 0.5f;
		var avoid = so.FindProperty("AvoidObstacles");
		if (avoid != null)
		{
			// OFF: camera does not move when player hides behind object — use silhouette to see player.
			avoid.FindPropertyRelative("Enabled").boolValue = false;
			avoid.FindPropertyRelative("CameraRadius").floatValue = 0.2f;
		}
		so.ApplyModifiedPropertiesWithoutUndo();

		var decollider = vcamGo.GetComponent<CinemachineDecollider>();
		if (decollider == null)
			decollider = Undo.AddComponent<CinemachineDecollider>(vcamGo);
		so = new SerializedObject(decollider);
		so.FindProperty("CameraRadius").floatValue = 0.2f;
		var decollProp = so.FindProperty("Decollision");
		if (decollProp != null)
		{
			// OFF: do not push camera toward player when colliding with geometry — silhouette shows player through walls.
			decollProp.FindPropertyRelative("Enabled").boolValue = false;
			decollProp.FindPropertyRelative("ObstacleLayers").intValue = playerLayer >= 0 ? ~(1 << playerLayer) : -1;
		}
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	static void ApplyLookFromInputOnSetup(bool useCinemachineForPosition, bool fallGuysOrbit = false)
	{
		var setup = Object.FindFirstObjectByType<CinemachineCameraSetup>(FindObjectsInactive.Include);
		if (setup == null) return;
		var provider = setup.GetComponent<CinemachineCameraInputProvider>();
		if (provider != null)
			Undo.DestroyObjectImmediate(provider);

		var look = setup.GetComponent<CinemachineLookFromInput>();
		if (look == null)
			look = Undo.AddComponent<CinemachineLookFromInput>(setup.gameObject);
		var so = new SerializedObject(look);
		so.FindProperty("sensitivity").floatValue = TouchSensitivity;
		so.FindProperty("useCinemachineForPosition").boolValue = useCinemachineForPosition;
		if (fallGuysOrbit)
		{
			so.FindProperty("yawRange").vector2Value = new Vector2(-180f, 180f);
			so.FindProperty("pitchRange").vector2Value = new Vector2(-40f, 40f);
			so.FindProperty("offsetFromTarget").vector3Value = new Vector3(0f, 2f, -6.5f);
		}
		var touchProp = so.FindProperty("touchLookField");
		if (touchProp.objectReferenceValue == null)
		{
			var touchField = Object.FindFirstObjectByType<FixedTouchField>(FindObjectsInactive.Include);
			if (touchField != null)
				touchProp.objectReferenceValue = touchField;
		}
		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(setup.gameObject);
	}

	static void ApplyOrbitalFollow(GameObject vcamGo)
	{
		var follow = vcamGo.GetComponent<CinemachineFollow>();
		if (follow != null)
			Undo.DestroyObjectImmediate(follow);

		var orbital = vcamGo.GetComponent<CinemachineOrbitalFollow>();
		if (orbital == null)
			orbital = Undo.AddComponent<CinemachineOrbitalFollow>(vcamGo);

		var so = new SerializedObject(orbital);
		so.FindProperty("Radius").floatValue = OrbitalRadius;
		var horizontalAxis = so.FindProperty("HorizontalAxis");
		horizontalAxis.FindPropertyRelative("Value").floatValue = HorizontalAxisBehindDeg;
		horizontalAxis.FindPropertyRelative("Center").floatValue = HorizontalAxisBehindDeg;
		var verticalAxis = so.FindProperty("VerticalAxis");
		verticalAxis.FindPropertyRelative("Range").vector2Value = new Vector2(VerticalAxisRangeMin, VerticalAxisRangeMax);
		verticalAxis.FindPropertyRelative("Value").floatValue = VerticalAxisDefaultDeg;
		verticalAxis.FindPropertyRelative("Center").floatValue = VerticalAxisDefaultDeg;
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	static void ApplyInputAxisController(GameObject vcamGo)
	{
		var inputController = vcamGo.GetComponent<CinemachineInputAxisController>();
		if (inputController == null)
			inputController = Undo.AddComponent<CinemachineInputAxisController>(vcamGo);

		inputController.SynchronizeControllers();

		var so = new SerializedObject(inputController);
		var controllers = so.FindProperty("m_ControllerManager").FindPropertyRelative("Controllers");
		if (!controllers.isArray) return;
		for (int i = 0; i < controllers.arraySize; i++)
		{
			var controller = controllers.GetArrayElementAtIndex(i);
			var nameProp = controller.FindPropertyRelative("Name");
			var name = nameProp.stringValue;
			var input = controller.FindPropertyRelative("Input");
			if (input == null) continue;

			var legacyInput = input.FindPropertyRelative("LegacyInput");
			var legacyGain = input.FindPropertyRelative("LegacyGain");
			var cancelDeltaTime = input.FindPropertyRelative("CancelDeltaTime");
			if (legacyInput == null) continue;

			if (name.Contains("Look Orbit X") || name.Contains("Look X"))
			{
				legacyInput.stringValue = LegacyInputMouseX;
				if (legacyGain != null) legacyGain.floatValue = LegacyGainX;
				if (cancelDeltaTime != null) cancelDeltaTime.boolValue = true;
			}
			else if (name.Contains("Look Orbit Y") || name.Contains("Look Y"))
			{
				legacyInput.stringValue = LegacyInputMouseY;
				if (legacyGain != null) legacyGain.floatValue = LegacyGainY;
				if (cancelDeltaTime != null) cancelDeltaTime.boolValue = true;
			}
		}
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	static void ApplyInputProviderOnSetup()
	{
		var setup = Object.FindFirstObjectByType<CinemachineCameraSetup>(FindObjectsInactive.Include);
		if (setup == null) return;

		var provider = setup.GetComponent<CinemachineCameraInputProvider>();
		if (provider == null)
			provider = Undo.AddComponent<CinemachineCameraInputProvider>(setup.gameObject);

		var so = new SerializedObject(provider);
		so.FindProperty("touchSensitivity").floatValue = TouchSensitivity;
		var touchFieldProp = so.FindProperty("touchLookField");
		if (touchFieldProp.objectReferenceValue == null)
		{
			var touchField = Object.FindFirstObjectByType<FixedTouchField>(FindObjectsInactive.Include);
			if (touchField != null)
				touchFieldProp.objectReferenceValue = touchField;
		}
		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(setup.gameObject);
	}
}
