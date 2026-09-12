using System;
using UnityEngine;
using VistaWorld.UI;
using VistaWorld.Platform;

namespace VistaWorld.Player
{
    /// <summary>
    /// Điều khiển nhân vật 3D Platformer dành cho Mobile Android (phong cách Vista World).
    /// Hỗ trợ di chuyển đa hướng theo Camera, Virtual Joystick, Jump, Double Jump, Coyote Time, Jump Buffer, và Gravity mượt mà.
    /// </summary>
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
        [Tooltip("Hệ số kiểm soát hướng trên không (0 = không lái được, 1 = lái như trên đất)")]
        [Range(0f, 1f)]
        [SerializeField] private float airControl = 0.85f;

        [Header("Jump & Double Jump Settings")]
        [Tooltip("Độ cao cú nhảy cơ bản (mét)")]
        [SerializeField] private float jumpHeight = 2.8f;
        [Tooltip("Độ cao cú nhảy đúp (mét)")]
        [SerializeField] private float doubleJumpHeight = 2.5f;
        [Tooltip("Thời gian ân hạn sau khi rời mép bệ vẫn được phép nhảy (Coyote Time)")]
        [SerializeField] private float coyoteTime = 0.15f;
        [Tooltip("Thời gian nhớ lệnh bấm nhảy trước khi chạm đất (Jump Buffer)")]
        [SerializeField] private float jumpBufferTime = 0.15f;

        [Header("Gravity & Physics")]
        [Tooltip("Gia tốc trọng lực")]
        [SerializeField] private float gravity = -28.0f;
        [Tooltip("Hệ số trọng lực tăng thêm khi rơi xuống để cú nhảy dứt khoát")]
        [SerializeField] private float fallMultiplier = 1.7f;
        [Tooltip("Tốc độ rơi tối đa (Terminal Velocity)")]
        [SerializeField] private float terminalVelocity = -35.0f;
        [Tooltip("Lực ép nhẹ xuống đất khi đang đứng để bám dốc/sàn ổn định")]
        [SerializeField] private float groundedGravity = -2.0f;

        [Header("Ground Check")]
        [Tooltip("Bán kính quả cầu dò mặt đất bổ sung dưới chân")]
        [SerializeField] private float groundCheckRadius = 0.28f;
        [Tooltip("Độ lệch tâm điểm kiểm tra chạm đất so với gốc nhân vật")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
        [Tooltip("Lớp mặt đất hợp lệ")]
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Camera Reference")]
        [Tooltip("Transform của Camera chính (để tính toán di chuyển theo hướng nhìn)")]
        [SerializeField] private Transform cameraTransform;

        // Trạng thái nội bộ
        private CharacterController characterController;
        private Vector3 currentVelocity = Vector3.zero;
        private float verticalVelocity = 0f;
        private bool isGrounded = false;
        private bool canDoubleJump = false;
        private float coyoteTimeCounter = 0f;
        private float jumpBufferCounter = 0f;
        private bool wasGroundedLastFrame = false;

        // Các sự kiện dành cho Animation, Âm thanh & VFX
        public event Action OnJump;
        public event Action OnDoubleJump;
        public event Action<float> OnLanded; // truyền vào vận tốc tiếp đất

        public bool IsGrounded => isGrounded;
        public Vector3 CurrentHorizontalVelocity => new Vector3(currentVelocity.x, 0, currentVelocity.z);
        public float VerticalVelocity => verticalVelocity;
        public bool CanDoubleJump => canDoubleJump;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraTransform == null && UnityEngine.Camera.main != null)
            {
                cameraTransform = UnityEngine.Camera.main.transform;
            }
        }

        private void Update()
        {
            CheckGroundStatus();
            HandleJumpInput();
            ApplyMovement();
            ApplyGravityAndJump();
            ExecuteMove();
        }

        /// <summary>
        /// Kiểm tra trạng thái chạm đất với độ tin cậy cao kết hợp CharacterController và Physics Check
        /// </summary>
        private void CheckGroundStatus()
        {
            Vector3 spherePosition = transform.position + groundCheckOffset;
            int maskWithoutSelf = groundLayers & ~(1 << gameObject.layer);
            bool physicsGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, maskWithoutSelf, QueryTriggerInteraction.Ignore);

            isGrounded = characterController.isGrounded || (physicsGrounded && verticalVelocity <= 0.1f);

            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
                canDoubleJump = true;

                if (!wasGroundedLastFrame)
                {
                    OnLanded?.Invoke(Mathf.Abs(verticalVelocity));
                }
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            wasGroundedLastFrame = isGrounded;
        }

        /// <summary>
        /// Lắng nghe tín hiệu bấm nút nhảy từ UI JumpButton hoặc phím Space
        /// </summary>
        private void HandleJumpInput()
        {
            bool jumpPressed = false;
            if (JumpButton.Instance != null)
            {
                jumpPressed = JumpButton.Instance.ConsumeJumpPress();
            }

            if (!jumpPressed && Input.GetButtonDown("Jump"))
            {
                jumpPressed = true;
            }

            if (jumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Xử lý di chuyển theo hướng camera từ Virtual Joystick
        /// </summary>
        private void ApplyMovement()
        {
            // 1. Nhận input từ VirtualJoystick hoặc bàn phím
            Vector2 input = Vector2.zero;
            if (VirtualJoystick.Instance != null && VirtualJoystick.Instance.IsActive)
            {
                input = VirtualJoystick.Instance.Direction;
            }
            else
            {
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (input.sqrMagnitude > 1f) input.Normalize();
            }

            // 2. Chuyển đổi input theo hướng nhìn của Camera
            Vector3 moveDirection = Vector3.zero;
            if (cameraTransform != null)
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                moveDirection = (forward * input.y + right * input.x);
            }
            else
            {
                moveDirection = new Vector3(input.x, 0, input.y);
            }

            // 3. Tính toán gia tốc / hãm tốc
            Vector3 targetVelocity = moveDirection * moveSpeed;
            float currentAccel = isGrounded ? (input.sqrMagnitude > 0.01f ? acceleration : deceleration)
                                            : (input.sqrMagnitude > 0.01f ? acceleration * airControl : deceleration * airControl);

            currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, targetVelocity.x, currentAccel * Time.deltaTime);
            currentVelocity.z = Mathf.MoveTowards(currentVelocity.z, targetVelocity.z, currentAccel * Time.deltaTime);

            // 4. Xoay hướng mặt nhân vật mượt mà theo hướng di chuyển
            Vector3 horizontalMove = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            if (horizontalMove.sqrMagnitude > 0.04f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(horizontalMove.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Tính toán nhảy đơn, nhảy đúp và trọng lực platformer dứt khoát
        /// </summary>
        private void ApplyGravityAndJump()
        {
            // 1. Kích hoạt Nhảy lần 1 (qua Jump Buffer hoặc Coyote Time)
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                if (currentMovingPlatform != null)
                {
                    currentMovingPlatform.UnregisterRider(characterController);
                    currentMovingPlatform = null;
                }
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(gravity));
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                OnJump?.Invoke();
            }
            // 2. Kích hoạt Nhảy đúp (Double Jump) trên không
            else if (jumpBufferCounter > 0f && !isGrounded && canDoubleJump)
            {
                if (currentMovingPlatform != null)
                {
                    currentMovingPlatform.UnregisterRider(characterController);
                    currentMovingPlatform = null;
                }
                verticalVelocity = Mathf.Sqrt(2f * doubleJumpHeight * Mathf.Abs(gravity));
                canDoubleJump = false;
                jumpBufferCounter = 0f;
                OnDoubleJump?.Invoke();
            }

            // 3. Tính toán trọng lực
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedGravity;
            }
            else
            {
                // Kiểm tra xem người chơi có nhả nút nhảy sớm không để tạo cú nhảy thấp (Variable Jump Height)
                bool isHoldingJump = (JumpButton.Instance != null && JumpButton.Instance.IsHeld) || Input.GetButton("Jump");

                float effectiveGravity = gravity;
                if (verticalVelocity < 0f)
                {
                    // Rơi nhanh hơn để tránh cảm giác trôi lơ lửng
                    effectiveGravity *= fallMultiplier;
                }
                else if (!isHoldingJump)
                {
                    // Nhả nút nhảy sớm thì cắt bớt lực đẩy lên
                    effectiveGravity *= (fallMultiplier * 1.2f);
                }

                verticalVelocity += effectiveGravity * Time.deltaTime;
                verticalVelocity = Mathf.Max(verticalVelocity, terminalVelocity);
            }
        }

        private MovingPlatform currentMovingPlatform;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
            {
                MovingPlatform mp = hit.gameObject.GetComponent<MovingPlatform>();
                if (mp == null && hit.transform.parent != null)
                {
                    mp = hit.transform.parent.GetComponent<MovingPlatform>();
                }

                if (mp != null)
                {
                    if (currentMovingPlatform != null && currentMovingPlatform != mp)
                    {
                        currentMovingPlatform.UnregisterRider(characterController);
                    }
                    currentMovingPlatform = mp;
                    currentMovingPlatform.RegisterRider(characterController);
                }
                else if (currentMovingPlatform != null)
                {
                    currentMovingPlatform.UnregisterRider(characterController);
                    currentMovingPlatform = null;
                }
            }
        }

        /// <summary>
        /// Thực thi di chuyển thông qua CharacterController
        /// </summary>
        private void ExecuteMove()
        {
            Vector3 motion = new Vector3(currentVelocity.x, verticalVelocity, currentVelocity.z) * Time.deltaTime;
            characterController.Move(motion);
        }

        private void OnDrawGizmosSelected()
        {
            // Vẽ quả cầu dò chạm đất trong Editor
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + groundCheckOffset, groundCheckRadius);
        }
    }
}
