using System;
using UnityEngine;

public class Pendulum2 : MonoBehaviour
{
	private Quaternion start;

	private Quaternion end;

	[Range(0f, 360f)]
	public float angle = 90f;

	[Range(0f, 5f)]
	public float speed = 2f;

	[Range(0f, 10f)]
	public float startTime;

	private void Start()
	{
		start = PendulumRotation(angle);
		end = PendulumRotation(0f - angle);
	}

	private void Update()
	{
		Go();
	}

	private void Go()
	{
		startTime += 0.01f;
		base.transform.rotation = Quaternion.Lerp(start, end, (Mathf.Sin(startTime * speed + (float)Math.PI / 2f) + 1f) / 2f);
	}

	private void ResetTimer()
	{
		startTime = 0f;
	}

	private Quaternion PendulumRotation(float angle)
	{
		Quaternion rotation = base.transform.rotation;
		float num = rotation.eulerAngles.z + angle;
		if (num > 180f)
		{
			num -= 360f;
		}
		else if (num < -180f)
		{
			num += 360f;
		}
		rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, num);
		return rotation;
	}
}
