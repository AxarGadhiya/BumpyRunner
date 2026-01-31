using UnityEngine;

namespace ithappy
{
    public class EditorLikeCameraController : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float _moveSpeed = 10f;

        [SerializeField] private float _fastMoveMultiplier = 2f;
        [SerializeField] private float _rotationSpeed = 2f;

        [Header("Zoom Settings")] [SerializeField]
        private float _zoomSpeed = 10f;

        [SerializeField] private float _minZoomDistance = 2f;
        [SerializeField] private float _maxZoomDistance = 50f;

        private Vector3 _lastMousePosition;
        private Transform _cameraTransform;
        private Transform _pivot;

        private void Awake()
        {
            _pivot = new GameObject("Camera Pivot").transform;
            _pivot.position = transform.position;
            _pivot.rotation = transform.rotation;
            
            _cameraTransform = GetComponentInChildren<Camera>().transform;
            _cameraTransform.SetParent(_pivot);
            _cameraTransform.localPosition = new Vector3(0, 0, -10f);
            _cameraTransform.LookAt(_pivot.position);
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        private void HandleMovement()
        {
            float speed = _moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? _fastMoveMultiplier : 1f);
            
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            _pivot.Translate(new Vector3(horizontal, 0, vertical) * speed * Time.deltaTime, Space.Self);
            
            if (Input.GetKey(KeyCode.Q))
            {
                _pivot.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
            }

            if (Input.GetKey(KeyCode.E))
            {
                _pivot.Translate(Vector3.up * speed * Time.deltaTime, Space.World);
            }
        }

        private void HandleRotation()
        {
            if (Input.GetMouseButton(1))
            {
                if (Input.GetMouseButtonDown(1))
                {
                    _lastMousePosition = Input.mousePosition;
                }

                Vector3 delta = Input.mousePosition - _lastMousePosition;
                _lastMousePosition = Input.mousePosition;
                
                _pivot.Rotate(Vector3.up, delta.x * _rotationSpeed, Space.World);
                
                _pivot.Rotate(Vector3.right, -delta.y * _rotationSpeed, Space.Self);
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 zoomDirection = _cameraTransform.localPosition.normalized;
                float currentDistance = _cameraTransform.localPosition.magnitude;
                float newDistance = Mathf.Clamp(currentDistance - scroll * _zoomSpeed, _minZoomDistance,
                    _maxZoomDistance);

                _cameraTransform.localPosition = zoomDirection * newDistance;
            }
        }
    }
}
