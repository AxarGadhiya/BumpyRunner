using UnityEngine;

namespace ithappy
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerCharacterInput : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform _cameraParent;
        [SerializeField] private CharacterAnimator _characterAnimator;
        [SerializeField] private float accelerationTime = 0.3f;
        [SerializeField] private float decelerationTime = 0.5f;

        [Header("Camera Collision")] [SerializeField]
        private float cameraDistance = 3f;

        [SerializeField] private float cameraCollisionOffset = 0.2f;
        [SerializeField] private float cameraMinDistance = 0.5f;
        [SerializeField] private LayerMask cameraCollisionLayer;

        private Transform cameraTransform;
        private Rigidbody rb;
        private float rotationX = 0f;
        private bool isGrounded;
        private Vector3 originalCameraLocalPos;
        private float currentCameraDistance;
        private float currentMoveSpeed;
        private Vector3 lastMoveDirection;
        private Vector3 targetMoveDirection;
        private bool isMoving;

        public Transform CameraParent => _cameraParent;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cameraTransform = Camera.main.transform;
            originalCameraLocalPos = cameraTransform.localPosition;
            currentCameraDistance = cameraDistance;
            currentMoveSpeed = 0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleRotation();
            HandleJump();
            HandleCameraCollision();
            HandleAnimations();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            CheckGrounded();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            targetMoveDirection =
                (cameraTransform.forward * vertical + cameraTransform.right * horizontal).normalized;
            targetMoveDirection.y = 0f;
            
            bool hasInput = targetMoveDirection.magnitude > 0.1f;

            if (hasInput)
            {
                lastMoveDirection = targetMoveDirection;
                isMoving = true;
                
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, moveSpeed,
                    Time.fixedDeltaTime * (1f / accelerationTime));
            }
            else if (isMoving)
            {
                currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f,
                    Time.fixedDeltaTime * (1f / decelerationTime));

                if (currentMoveSpeed < 0.01f)
                {
                    currentMoveSpeed = 0f;
                    isMoving = false;
                }
            }

            Vector3 moveVelocity = lastMoveDirection * currentMoveSpeed;
            rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
        }

        private void HandleRotation()
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            transform.Rotate(Vector3.up * mouseX);

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            _cameraParent.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }

        private void HandleJump()
        {
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _characterAnimator.Jump();
            }
        }

        private void HandleAnimations()
        {
            if (isGrounded)
            {
                _characterAnimator.SetMoveSpeed(currentMoveSpeed);
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _characterAnimator.Hello();
            }
        }

        private void CheckGrounded()
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        }

        private void HandleCameraCollision()
        {
            RaycastHit hit;
            Vector3 cameraDesiredPosition = _cameraParent.TransformPoint(new Vector3(0, 0, -cameraDistance));

            if (Physics.Linecast(_cameraParent.position, cameraDesiredPosition, out hit, cameraCollisionLayer))
            {
                currentCameraDistance = Mathf.Clamp(
                    (hit.distance - cameraCollisionOffset),
                    cameraMinDistance,
                    cameraDistance);
            }
            else
            {
                currentCameraDistance = cameraDistance;
            }

            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                new Vector3(0, 0, -currentCameraDistance),
                Time.deltaTime * 10f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

            if (_cameraParent != null)
            {
                Gizmos.color = Color.blue;
                Vector3 cameraDesiredPosition = _cameraParent.TransformPoint(new Vector3(0, 0, -cameraDistance));
                Gizmos.DrawLine(_cameraParent.position, cameraDesiredPosition);
            }
        }
    }
}
