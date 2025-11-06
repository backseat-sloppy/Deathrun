using Unity.Netcode;
using UnityEngine;
using Cinemachine;

namespace DeathrunGame
{
    /// <summary>
    /// Handles third-person camera control and player orientation.
    /// Owner-only camera rotation with smooth character turning.
    /// </summary>
    public class PlayerCameraController : NetworkBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Transform cameraFollowTarget;

        [Header("Camera Settings")]
        [SerializeField] private float mouseSensitivityX = 2f;
        [SerializeField] private float mouseSensitivityY = 2f;
        [SerializeField] private float minVerticalAngle = -90f;
        [SerializeField] private float maxVerticalAngle = 90f;

        [Header("Camera Offset")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, -5f);
        [SerializeField] private Vector3 followTargetOffset = new Vector3(0f, 1.5f, 0f);
        
        [Header("Dynamic Offset Adjustment")]
        [SerializeField] private bool enableDynamicOffset = true;
        [SerializeField] private float minOffsetDistance = -2f; // Closest distance when looking straight up
        [SerializeField] private float maxOffsetDistance = -5f; // Normal distance
        [SerializeField] private float offsetTransitionStartAngle = -60f; // Angle where transition begins
        [SerializeField] private float offsetTransitionEndAngle = -110f; // Angle where transition completes

        [Header("Character Orientation")]
        [SerializeField] private Transform characterBody;
        [SerializeField] private float rotationSmoothTime = 0.12f;

        private float cameraPitch = 0f;
        private float cameraYaw = 0f;
        private float currentRotationVelocity;
        private CinemachineTransposer transposer;
        private Vector3 originalFollowTargetLocalPosition; // Store original offset

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                if (virtualCamera != null)
                {
                    virtualCamera.Priority = 0;
                    virtualCamera.enabled = false;
                }

                enabled = false;
                return;
            }

            if (virtualCamera != null)
            {
                virtualCamera.Priority = 100;
                virtualCamera.enabled = true;
                
                // Get the Transposer component and set the offset
                transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = cameraOffset;
                }
                
                Debug.Log($"🎥 Player camera activated with priority {virtualCamera.Priority}");
            }

            // Apply the follow target offset to raise the pivot point
            if (cameraFollowTarget != null)
            {
                originalFollowTargetLocalPosition = cameraFollowTarget.localPosition;
                cameraFollowTarget.localPosition = originalFollowTargetLocalPosition + followTargetOffset;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Start()
        {
            if (!IsOwner) return;

            if (cameraFollowTarget != null)
            {
                Vector3 angles = cameraFollowTarget.eulerAngles;
                cameraPitch = angles.x;
                cameraYaw = angles.y;
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;

            HandleCameraRotation();
            OrientCharacterToCamera();
        }

        private void HandleCameraRotation()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

            cameraYaw += mouseX;

            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, minVerticalAngle, maxVerticalAngle);

            if (cameraFollowTarget != null)
            {
                cameraFollowTarget.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            }
        }

        private void OrientCharacterToCamera()
        {
            if (characterBody == null) return;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Vector3 cameraForward = new Vector3(
                    cameraFollowTarget.forward.x,
                    0f,
                    cameraFollowTarget.forward.z
                ).normalized;

                Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
                Vector3 moveDirection = cameraForward * inputDirection.z + cameraFollowTarget.right * inputDirection.x;

                if (moveDirection.sqrMagnitude > 0.1f)
                {
                    float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                    float smoothAngle = Mathf.SmoothDampAngle(
                        characterBody.eulerAngles.y,
                        targetAngle,
                        ref currentRotationVelocity,
                        rotationSmoothTime
                    );

                    characterBody.rotation = Quaternion.Euler(-90f, smoothAngle, 0f);
                }
            }
        }

        public Vector3 GetCameraForward()
        {
            if (cameraFollowTarget == null) return Vector3.forward;

            return new Vector3(
                cameraFollowTarget.forward.x,
                0f,
                cameraFollowTarget.forward.z
            ).normalized;
        }

        public Vector3 GetCameraRight()
        {
            if (cameraFollowTarget == null) return Vector3.right;

            return new Vector3(
                cameraFollowTarget.right.x,
                0f,
                cameraFollowTarget.right.z
            ).normalized;
        }
    }
}