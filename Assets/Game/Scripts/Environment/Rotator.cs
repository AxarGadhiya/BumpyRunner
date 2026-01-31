using UnityEngine;

public class Rotator : MonoBehaviour
{
	public float speed = 3f;

	public bool vertical;

	//[SerializeField]
	//private GameManager gameManager;

	public bool isThisMain;

	private void Update()
	{
		if (!isThisMain)
		{
			if (vertical)
			{
				base.transform.Rotate(0f, speed * Time.deltaTime / 0.01f, 0f, Space.Self);
			}
			else
			{
				base.transform.Rotate(0f, 0f, speed * Time.deltaTime / 0.01f, Space.Self);
			}
		}
		//else if (gameManager.starting)
		//{
		//	if (vertical)
		//	{
		//		base.transform.Rotate(0f, speed * Time.deltaTime / 0.01f, 0f, Space.Self);
		//	}
		//	else
		//	{
		//		base.transform.Rotate(0f, 0f, speed * Time.deltaTime / 0.01f, Space.Self);
		//	}
		//}
	}
}
