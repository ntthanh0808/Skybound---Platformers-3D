using System;
using UnityEngine;
using VistaWorld.UI;
using VistaWorld.Platform;

namespace VistaWorld.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Tốc độ di chuyển tối đa")]
        [SerializeField] private float moveSpeed = 8.0f;
        [Tooltip("Tốc độ xoay mặt theo hướng di chuyển (độ/giây)")]
        [SerializeField] private float rotationSpeed = 720.0f;
        [Tooltip("Gia tốc tăng tốc khi bắt đầu di chuyển")]
        [SerializeField] private float acceleration = 25.0f;
        [Tooltip("Gia tốc hãm khi nhả joystick")]
        [SerializeField] private float deceleration = 30.0f;
        [Range(0f, 1f)]
        [Tooltip("Hệ số kiểm soát hướng trên không")]
        [SerializeField] private float airControl = 0.85f;

        [Header("Jump & Multi-Jump Settings")]
        [Tooltip("Độ cao cú nhảy cơ bản")]
        [SerializeField] private float jumpHeight = 2.8f;
        [Tooltip("Độ cao các cú nhảy trên không")]
        [SerializeField] private float doubleJumpHeight = 2.5f;
        [Tooltip("Số lần nhảy tối đa trước khi chạm đất")]
        [SerializeField] private int maxJumpCount = 3;
        [Tooltip("Thời gian ân hạn sau khi rời mép bệ")]
        [SerializeField] private float coyoteTime = 0.15f;
        [Tooltip("Thời gian nhớ lệnh bấm nhảy")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Gravity & Physics")]
        [Tooltip("Gia tốc trọng lực")]
        [SerializeField] private float gravity = -28.0f;
        [Tooltip("Hệ số trọng lực khi rơi")]
        [SerializeField] private float fallMultiplier = 1.7f;
        [Tooltip("Tốc độ rơi tối đa")]
        [SerializeField] private float terminalVelocity = -35.0f;
        [Tooltip("Lực ép nhẹ xuống đất")]
        [SerializeField] private float groundedGravity = -2.0f;

        [Header("Ground Check")]
        [Tooltip("Bán kính dò mặt đất")]
        [SerializeField] private float groundCheckRadius = 0.28f;
        [Tooltip("Độ lệch điểm kiểm tra mặt đất")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
        [Tooltip("Layer mặt đất")]
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Camera Reference")]
        [Tooltip("Transform của Camera chính")]
        [SerializeField] private Transform cameraTransform;

        [Header("Running Slide")]
        [Tooltip("Tốc độ khi đang slide")]
        [SerializeField] private float slideSpeed = 11.0f;
        [Tooltip("Thời gian slide")]
        [SerializeField] private float slideDuration = 0.65f;
        [Tooltip("Thời gian chờ trước khi slide lại")]
        [SerializeField] private float slideCooldown = 0.35f;
        [Tooltip("Tốc độ giảm dần khi slide")]
        [SerializeField] private float slideDeceleration = 18.0f;
        [Tooltip("Chiều cao CharacterController khi slide")]
        [SerializeField] private float slideControllerHeight = 1.0f;

        private CharacterController characterController;
        private Vector3 currentVelocity = Vector3.zero;
        private float verticalVelocity = 0f;
        private bool isGrounded = false;
        private int jumpCount = 0;
        private float coyoteTimeCounter = 0f;
        private float jumpBufferCounter = 0f;
        private bool wasGroundedLastFrame = false;

        private bool isSliding = false;
        private float slideTimer = 0f;
        private float slideCooldownTimer = 0f;
        private Vector3 slideDirection = Vector3.forward;
        private float originalControllerHeight;
        private Vector3 originalControllerCenter;

        private MovingPlatform currentMovingPlatform;

        public event Action OnJump;
        public event Action OnDoubleJump;
        public event Action OnFlipJump;
        public event Action<float> OnLanded;
        public event Action OnSlide;
        public event Action OnSlideEnd;

        public bool IsGrounded => isGrounded;
        public Vector3 CurrentHorizontalVelocity => new Vector3(currentVelocity.x, 0, currentVelocity.z);
        public float VerticalVelocity => verticalVelocity;
        public int JumpCount => jumpCount;
        public bool IsSliding => isSliding;

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            originalControllerHeight = characterController.height;
            originalControllerCenter = characterController.center;
            EnsureCameraReference();
        }

        private void Start()
        {
            EnsureCameraReference();
        }

        private void Update()
        {
            CheckGroundStatus();
            HandleJumpInput();
            HandleKeyboardActions();
            UpdateSlide();
            UpdateCooldowns();
            ApplyMovement();
            ApplyGravityAndJump();
            ExecuteMove();
        }

        private void EnsureCameraReference()
        {
            if (cameraTransform != null)
                return;

            if (UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
                return;
            }

            var tpc = FindObjectOfType<VistaWorld.CameraControl.ThirdPersonCamera>();

            if (tpc != null)
                cameraTransform = tpc.transform;
        }

        private void HandleKeyboardActions()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
                StartSlide();
            if (SlideButton.Instance != null &&
                SlideButton.Instance.ConsumeSlidePress())
            {
                StartSlide();
            }
        }

        private void CheckGroundStatus()
        {
            Vector3 spherePosition = transform.position + groundCheckOffset;
            int maskWithoutSelf = groundLayers & ~(1 << gameObject.layer);

            bool physicsGrounded = Physics.CheckSphere(
                spherePosition,
                groundCheckRadius,
                maskWithoutSelf,
                QueryTriggerInteraction.Ignore
            );

            bool ccGrounded = characterController.isGrounded && verticalVelocity <= 0.1f;
            bool sphereGrounded = physicsGrounded && verticalVelocity <= 0.1f;

            isGrounded = ccGrounded || sphereGrounded;

            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;

                if (!wasGroundedLastFrame)
                {
                    jumpCount = 0;
                    OnLanded?.Invoke(Mathf.Abs(verticalVelocity));
                }
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            wasGroundedLastFrame = isGrounded;
        }

        private void HandleJumpInput()
        {
            bool jumpPressed = false;

            if (JumpButton.Instance != null)
                jumpPressed = JumpButton.Instance.ConsumeJumpPress();

            if (!jumpPressed && Input.GetButtonDown("Jump"))
                jumpPressed = true;

            if (jumpPressed)
                jumpBufferCounter = jumpBufferTime;
            else
                jumpBufferCounter -= Time.deltaTime;
        }

        private void ApplyMovement()
        {
            Vector2 input = Vector2.zero;

            if (VirtualJoystick.Instance != null && VirtualJoystick.Instance.IsActive)
            {
                input = VirtualJoystick.Instance.Direction;
            }
            else
            {
                input = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );

                if (input.sqrMagnitude > 1f)
                    input.Normalize();
            }

            if (isSliding)
            {
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    slideDirection * slideSpeed,
                    slideDeceleration * Time.deltaTime
                );

                return;
            }

            if (cameraTransform == null)
                EnsureCameraReference();

            Vector3 moveDirection = Vector3.zero;

            if (cameraTransform != null)
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;

                forward.y = 0f;
                right.y = 0f;

                forward.Normalize();
                right.Normalize();

                moveDirection = forward * input.y + right * input.x;
            }
            else
            {
                moveDirection = new Vector3(input.x, 0, input.y);
            }

            Vector3 targetVelocity = moveDirection * moveSpeed;

            float currentAccel;

            if (isGrounded)
            {
                currentAccel = input.sqrMagnitude > 0.01f
                    ? acceleration
                    : deceleration;
            }
            else
            {
                currentAccel = input.sqrMagnitude > 0.01f
                    ? acceleration * airControl
                    : deceleration * airControl;
            }

            currentVelocity.x = Mathf.MoveTowards(
                currentVelocity.x,
                targetVelocity.x,
                currentAccel * Time.deltaTime
            );

            currentVelocity.z = Mathf.MoveTowards(
                currentVelocity.z,
                targetVelocity.z,
                currentAccel * Time.deltaTime
            );

            Vector3 horizontalMove = new Vector3(
                currentVelocity.x,
                0,
                currentVelocity.z
            );

            if (horizontalMove.sqrMagnitude > 0.04f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(
                    horizontalMove.normalized,
                    Vector3.up
                );

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        private void ApplyGravityAndJump()
        {
            if (!isSliding && jumpBufferCounter > 0f)
            {
                if (jumpCount == 0)
                {
                    if (currentMovingPlatform != null)
                    {
                        currentMovingPlatform.UnregisterRider(characterController);
                        currentMovingPlatform = null;
                    }

                    verticalVelocity = Mathf.Sqrt(
                        2f * jumpHeight * Mathf.Abs(gravity)
                    );

                    jumpCount = 1;
                    jumpBufferCounter = 0f;
                    coyoteTimeCounter = 0f;

                    OnJump?.Invoke();
                }
                else if (!isGrounded && jumpCount < maxJumpCount)
                {
                    if (currentMovingPlatform != null)
                    {
                        currentMovingPlatform.UnregisterRider(characterController);
                        currentMovingPlatform = null;
                    }

                    verticalVelocity = Mathf.Sqrt(
                        2f * doubleJumpHeight * Mathf.Abs(gravity)
                    );

                    jumpCount++;
                    jumpBufferCounter = 0f;

                    OnDoubleJump?.Invoke();
                    OnFlipJump?.Invoke();
                }
            }

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedGravity;
            }
            else
            {
                bool isHoldingJump =
                    (JumpButton.Instance != null && JumpButton.Instance.IsHeld) ||
                    Input.GetButton("Jump");

                float effectiveGravity = gravity;

                if (verticalVelocity < 0f)
                {
                    effectiveGravity *= fallMultiplier;
                }
                else if (!isHoldingJump)
                {
                    effectiveGravity *= fallMultiplier * 1.2f;
                }

                verticalVelocity += effectiveGravity * Time.deltaTime;

                verticalVelocity = Mathf.Max(
                    verticalVelocity,
                    terminalVelocity
                );
            }
        }

        public void StartSlide()
        {
            if (!isGrounded)
                return;

            if (isSliding)
                return;

            if (slideCooldownTimer > 0f)
                return;

            Vector3 horizontalVelocity = new Vector3(
                currentVelocity.x,
                0,
                currentVelocity.z
            );

            if (horizontalVelocity.magnitude < 2.0f)
                return;

            slideDirection = horizontalVelocity.normalized;
            isSliding = true;
            slideTimer = slideDuration;
            slideCooldownTimer = slideCooldown;

            currentVelocity = slideDirection * slideSpeed;

            SetSlideCollider(true);
            OnSlide?.Invoke();
        }

        private void UpdateSlide()
        {
            if (!isSliding)
                return;

            slideTimer -= Time.deltaTime;

            if (slideTimer <= 0f)
                StopSlide();
        }

        public void StopSlide()
        {
            if (!isSliding)
                return;

            isSliding = false;

            SetSlideCollider(false);
            OnSlideEnd?.Invoke();
        }

        private void SetSlideCollider(bool sliding)
        {
            if (characterController == null)
                return;

            if (sliding)
            {
                float heightDifference =
                    originalControllerHeight - slideControllerHeight;

                characterController.height = slideControllerHeight;

                characterController.center =
                    originalControllerCenter -
                    new Vector3(
                        0,
                        heightDifference * 0.5f,
                        0
                    );
            }
            else
            {
                characterController.height = originalControllerHeight;
                characterController.center = originalControllerCenter;
            }
        }

        private void UpdateCooldowns()
        {
            if (slideCooldownTimer > 0f)
                slideCooldownTimer -= Time.deltaTime;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
            {
                MovingPlatform mp =
                    hit.gameObject.GetComponent<MovingPlatform>();

                if (mp == null && hit.transform.parent != null)
                {
                    mp =
                        hit.transform.parent.GetComponent<MovingPlatform>();
                }

                if (mp != null)
                {
                    if (currentMovingPlatform != null &&
                        currentMovingPlatform != mp)
                    {
                        currentMovingPlatform.UnregisterRider(
                            characterController
                        );
                    }

                    currentMovingPlatform = mp;

                    currentMovingPlatform.RegisterRider(
                        characterController
                    );
                }
                else if (currentMovingPlatform != null)
                {
                    currentMovingPlatform.UnregisterRider(
                        characterController
                    );

                    currentMovingPlatform = null;
                }
            }
        }

        private void ExecuteMove()
        {
            Vector3 motion = new Vector3(
                currentVelocity.x,
                verticalVelocity,
                currentVelocity.z
            ) * Time.deltaTime;

            characterController.Move(motion);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded
                ? Color.green
                : Color.red;

            Gizmos.DrawWireSphere(
                transform.position + groundCheckOffset,
                groundCheckRadius
            );
        }
    }
}