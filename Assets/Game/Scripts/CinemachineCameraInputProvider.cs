using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Drives Cinemachine camera rotation (e.g. Orbital Follow) from mouse or touch.
/// Sets CinemachineCore.GetInputAxis so "Mouse X" / "Mouse Y" come from touch when
/// using the look touch field, otherwise from legacy Input.
/// Add to the same GameObject as CinemachineCameraSetup (e.g. Main Camera or GameManager).
/// </summary>
public class CinemachineCameraInputProvider : MonoBehaviour
{
	[Tooltip("Optional. When assigned and the user is dragging, touch delta drives Mouse X/Y instead of legacy Input.")]
	[SerializeField] FixedTouchField touchLookField;

	[Tooltip("Multiplier for touch delta (Mouse X / Mouse Y). Match your previous camera sensitivity (e.g. 0.25).")]
	[SerializeField] float touchSensitivity = 0.25f;

	CinemachineCore.AxisInputDelegate _previousGetInputAxis;

	void OnEnable()
	{
		_previousGetInputAxis = CinemachineCore.GetInputAxis;
		CinemachineCore.GetInputAxis = GetInputAxis;
	}

	void OnDisable()
	{
		CinemachineCore.GetInputAxis = _previousGetInputAxis;
	}

	float GetInputAxis(string axisName)
	{
		if (touchLookField != null && touchLookField.Pressed)
		{
			if (axisName == "Mouse X") return touchLookField.TouchDist.x * touchSensitivity;
			if (axisName == "Mouse Y") return -touchLookField.TouchDist.y * touchSensitivity;
		}
		return Input.GetAxis(axisName);
	}
}
