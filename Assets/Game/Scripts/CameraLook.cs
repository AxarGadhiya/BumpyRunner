using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraLook : MonoBehaviour
{
	[SerializeField]
	private float _mouseSensitivity = 3f;

	private float _rotationY;

	private float _rotationX;

	public bool isActive = true;

	//public bool introGame;

	[SerializeField]
	public Transform _target;

	public GameObject uiFinish;

	[SerializeField]
	private float _distanceFromTarget = 3f;

	//public GameObject[] players;
	private GameObject player;

	private Vector3 _currentRotation;

	private Vector3 _smoothVelocity = Vector3.zero;

	[SerializeField]
	private float _smoothTime = 0.2f;

	public Vector2 LookAxis;

	[SerializeField]
	private Vector2 _rotationXMinMax = new Vector2(-40f, 40f);

	private Touch initTouch;

	private bool canZoom;

	private float mouseX;

	private float mouseY;

	private void Start()
	{
		_distanceFromTarget = 6.5f;
		if (SystemInfo.deviceType == DeviceType.Handheld)
		{
			_mouseSensitivity /= 2f;
			return;
		}
		//Cursor.lockState = CursorLockMode.Locked;
		//Cursor.visible = false;

		GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
		gameObject.GetComponent<Movement>().disableOnAwake = false;
		_target = gameObject.transform.GetChild(0);
	}

    private void Update()
	{
		//if (SceneManager.GetActiveScene().name == "Testing Map")
		//{
			//GameObject gameObject = GameObject.FindGameObjectWithTag("Player");
			//gameObject.GetComponent<Movement>().disableOnAwake = false;
			//_target = gameObject.transform.GetChild(0);
		//}
		if (!uiFinish.activeSelf && _target == null)
		{
			SearchTargetTransfrom();
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			if (!canZoom)
			{
				canZoom = true;
			}
			else
			{
				canZoom = false;
			}
		}
		if (canZoom)
		{
			if (Input.GetAxis("Mouse ScrollWheel") > 0f)
			{
				if (_distanceFromTarget <= 7.5f)
				{
					_distanceFromTarget += 1f;
				}
			}
			else if (Input.GetAxis("Mouse ScrollWheel") < 0f && _distanceFromTarget >= 3f)
			{
				_distanceFromTarget -= 1f;
			}
		}
		Look();
	}

	private void FixedUpdate()
	{
	}

	private void Look()
	{
		if (isActive)
		{
			//if (SystemInfo.deviceType == DeviceType.Desktop)
			//{
			//	float num = Input.GetAxis("Mouse X") * _mouseSensitivity;
			//	float num2 = (0f - Input.GetAxis("Mouse Y")) * _mouseSensitivity;
			//	_rotationY += num;
			//	_rotationX += num2;
			//	_rotationX = Mathf.Clamp(_rotationX, _rotationXMinMax.x, _rotationXMinMax.y);
			//	_currentRotation = Vector3.SmoothDamp(target: new Vector3(_rotationX, _rotationY), current: _currentRotation, currentVelocity: ref _smoothVelocity, smoothTime: _smoothTime);
			//	base.transform.localEulerAngles = _currentRotation;
			//}
			//else
			//{
				_mouseSensitivity = 0.25f;
				_smoothTime = 0f;
				float num3 = LookAxis.x * _mouseSensitivity;
				float num4 = (0f - LookAxis.y) * _mouseSensitivity;
				_rotationY += num3;
				_rotationX += num4;
				_rotationX = Mathf.Clamp(_rotationX, _rotationXMinMax.x, _rotationXMinMax.y);
				_currentRotation = Vector3.SmoothDamp(target: new Vector3(_rotationX, _rotationY), current: _currentRotation, currentVelocity: ref _smoothVelocity, smoothTime: _smoothTime);
				base.transform.localEulerAngles = _currentRotation;
			//}
		}
		base.transform.position = _target.position - base.transform.forward * _distanceFromTarget;
	}

	private void SearchTargetTransfrom()
	{
        _target = player.transform.GetChild(0);

        //if (introGame)
        //{
        //	return;
        //}
        //players = GameObject.FindGameObjectsWithTag("Player");
        //if (RunTimeGame.round != 1 && !RunTimeGame.isLocalQualified)
        //{
        //	_target = players[Random.Range(0, players.Length)].transform.GetChild(0);
        //}
        //else
        //{
        //	if (!RunTimeGame.isLocalQualified && RunTimeGame.round != 1)
        //	{
        //		return;
        //	}
        //	GameObject[] array = players;
        //	foreach (GameObject gameObject in array)
        //	{
        //		if (/*gameObject.GetComponent<PhotonView>().IsMine &&*/ gameObject.gameObject.layer == LayerMask.NameToLayer("Player"))
        //		{
        //			_target = gameObject.transform.GetChild(0);
        //		}
        //	}
        //}
    }
}
