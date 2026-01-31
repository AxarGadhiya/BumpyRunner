using System.Collections.Generic;
using UnityEngine;

public class RunTimeGame : MonoBehaviour
{
	public static List<GameObject> playerObj = new List<GameObject>();

	public static List<string> namePlayerObj = new List<string>();

	public static int round = 0;

	public static int playersQualified = 0;

	public static bool useBot = true;

	public static string nameSkin;

	public string nameSkins;

	public List<GameObject> objectPla = new List<GameObject>();

	public static List<string> nonQualifiedNames = new List<string>();

	public List<string> nonQualifiedName = new List<string>();

	public List<string> namePlayerObjS = new List<string>();

	public static List<string> mapName = new List<string>();

	public List<string> mapNames = new List<string>();

	public static bool isLocalQualified = false;

	public bool localQualified;

	private void Start()
	{
		Object.DontDestroyOnLoad(base.gameObject);
		if (SystemInfo.deviceType == DeviceType.Handheld)
		{
			Application.targetFrameRate = 60;
		}
	}

	private void Update()
	{
		nonQualifiedNames = nonQualifiedName;
		nameSkins = nameSkin;
		objectPla = playerObj;
		namePlayerObjS = namePlayerObj;
		mapNames = mapName;
		localQualified = isLocalQualified;
		Debug.Log(round);
	}
}
