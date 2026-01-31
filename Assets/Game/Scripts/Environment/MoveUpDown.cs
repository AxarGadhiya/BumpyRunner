using UnityEngine;

public class MoveUpDown : MonoBehaviour
{
	public float speed = 2f;

	public float height = 0.05f;

	public float startY = 10.25f;

	public bool horizontal;

	public bool isX = true;

	private void Update()
	{
		StartMove();
	}

	//[PunRPC]
	private void StartMove()
	{
		Vector3 position = base.transform.position;
		if (horizontal)
		{
			float num = startY + height * Mathf.Sin(Time.time * speed);
			if (isX)
			{
				base.transform.position = new Vector3(num, position.y, position.z);
			}
			else
			{
				base.transform.position = new Vector3(position.x, position.y, num);
			}
		}
		else
		{
			float y = startY + height * Mathf.Sin(Time.time * speed);
			base.transform.position = new Vector3(position.x, y, position.z);
		}
	}
}
