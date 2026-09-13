using UnityEngine;

namespace VistaWorld.Player
{
    /// <summary>
    /// Xử lý hiệu ứng hình ảnh và Animation cho nhân vật.
    /// Animation:
    /// - IsMoving  : Idle / Run
    /// - IsJumping : Jump lần 1
    /// - IsFlip    : Jump lần 2 và lần 3
    /// - IsSliding  : Running Slide
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform chứa mô hình nhân vật")]
        [SerializeField] private Transform modelRoot;

        [SerializeField] private PlayerController playerController;

        public Transform ModelRoot
        {
            get => modelRoot;
            set => modelRoot = value;
        }

        [Header("Squash & Stretch Settings")]
        [Tooltip("Độ dãn khi Jump lần 1")]
        [SerializeField] private Vector3 jumpStretch = new Vector3(0.8f, 1.3f, 0.8f);

        [Tooltip("Độ dãn khi Flip lần 2/3")]
        [SerializeField] private Vector3 doubleJumpStretch = new Vector3(0.7f, 1.45f, 0.7f);

        [Tooltip("Độ nhún khi tiếp đất")]
        [SerializeField] private Vector3 landSquash = new Vector3(1.3f, 0.7f, 1.3f);

        [Tooltip("Tốc độ đàn hồi")]
        [SerializeField] private float elasticitySpeed = 10.0f;

        [Header("Tilt & Lean Settings")]
        [Tooltip("Góc nghiêng tối đa khi chạy")]
        [SerializeField] private float maxTiltAngle = 12.0f;

        [SerializeField] private float tiltSmoothSpeed = 8.0f;

        [Header("Animation States")]
        [SerializeField] private Animator animator;

        [Tooltip("Tên state Jump trong Animator")]
        [SerializeField] private string jumpStateName = "Jumping";

        [Tooltip("Tên state Flip trong Animator")]
        [SerializeField] private string flipStateName = "Running Forward Flip";

        [Tooltip("Tên state Chạy trong Animator")]
        [SerializeField] private string runStateName = "Running";

        [Tooltip("Tên state Đứng yên trong Animator")]
        [SerializeField] private string idleStateName = "Offensive Idle";

        [Tooltip("Tên state Slide trong Animator")]
        [SerializeField] private string slideStateName = "Running Slide";

        [Header("Model Alignment")]
        [Tooltip("Góc bù hướng mặt của model")]
        [SerializeField] private float modelFacingOffset = 0f;

        public float ModelFacingOffset
        {
            get => modelFacingOffset;
            set => modelFacingOffset = value;
        }

        private Vector3 targetScale = Vector3.one;
        private Vector3 currentScale = Vector3.one;
        private float currentTilt = 0f;

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (modelRoot == null)
            {
                Animator childAnim = GetComponentInChildren<Animator>();

                if (childAnim != null && childAnim.transform != transform)
                {
                    modelRoot = childAnim.transform;
                }
                else
                {
                    Transform childModel = transform.Find("CapsuleModel");

                    if (childModel != null)
                        modelRoot = childModel;
                    else if (transform.childCount > 0)
                        modelRoot = transform.GetChild(0);
                }
            }

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void Start()
        {
            if (modelRoot != null && modelRoot != transform)
            {
                if (Mathf.Approximately(modelFacingOffset, 0f) &&
                    Mathf.Abs(modelRoot.localEulerAngles.y) > 0.01f)
                {
                    modelFacingOffset = modelRoot.localEulerAngles.y;
                }

                modelRoot.localRotation =
                    Quaternion.Euler(0f, modelFacingOffset, 0f);
            }

            if (animator != null)
            {
                var rac = animator.runtimeAnimatorController;

                Debug.Log(
                    $"[PlayerVisuals] Animator target: {animator.gameObject.name}, " +
                    $"Controller: {(rac != null ? rac.name : "NULL")}"
                );

                if (rac != null)
                {
                    bool hasJump =
                        animator.HasState(
                            0,
                            Animator.StringToHash(jumpStateName)
                        ) ||
                        animator.HasState(
                            0,
                            Animator.StringToHash(
                                "Base Layer." + jumpStateName
                            )
                        );

                    bool hasFlip =
                        animator.HasState(
                            0,
                            Animator.StringToHash(flipStateName)
                        ) ||
                        animator.HasState(
                            0,
                            Animator.StringToHash(
                                "Base Layer." + flipStateName
                            )
                        );

                    bool hasSlide =
                        animator.HasState(
                            0,
                            Animator.StringToHash(slideStateName)
                        ) ||
                        animator.HasState(
                            0,
                            Animator.StringToHash(
                                "Base Layer." + slideStateName
                            )
                        );

                    Debug.Log(
                        $"[PlayerVisuals] States: " +
                        $"Jump={hasJump}, " +
                        $"Flip={hasFlip}, " +
                        $"Slide={hasSlide}"
                    );
                }
            }
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnJump += HandleJump;
                playerController.OnFlipJump += HandleFlipJump;
                playerController.OnLanded += HandleLanded;
                playerController.OnSlide += HandleSlide;
                playerController.OnSlideEnd += HandleSlideEnd;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnJump -= HandleJump;
                playerController.OnFlipJump -= HandleFlipJump;
                playerController.OnLanded -= HandleLanded;
                playerController.OnSlide -= HandleSlide;
                playerController.OnSlideEnd -= HandleSlideEnd;
            }
        }

        private void Update()
        {
            UpdateSquashAndStretch();
            UpdateTilt();
            UpdateAnimation();
        }

        private void PlayAnimationState(
            string stateName,
            bool forceRestart = false,
            float crossFadeDuration = 0.08f)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
                return;

            int shortHash =
                Animator.StringToHash(stateName);

            int fullHash =
                Animator.StringToHash(
                    "Base Layer." + stateName
                );

            int targetHash = 0;

            if (animator.HasState(0, shortHash))
                targetHash = shortHash;
            else if (animator.HasState(0, fullHash))
                targetHash = fullHash;

            if (targetHash != 0)
            {
                if (forceRestart)
                {
                    animator.Play(
                        targetHash,
                        0,
                        0f
                    );
                }
                else
                {
                    animator.CrossFadeInFixedTime(
                        targetHash,
                        crossFadeDuration,
                        0,
                        0f
                    );
                }
            }
            else
            {
                try
                {
                    if (forceRestart)
                    {
                        animator.Play(
                            stateName,
                            0,
                            0f
                        );
                    }
                    else
                    {
                        animator.CrossFadeInFixedTime(
                            stateName,
                            crossFadeDuration,
                            0,
                            0f
                        );
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        $"[PlayerVisuals] Không thể chuyển sang state " +
                        $"'{stateName}': {ex.Message}"
                    );
                }
            }
        }

        private void HandleJump()
        {
            currentScale = jumpStretch;

            if (animator == null)
                return;

            animator.SetBool("IsJumping", true);
            animator.SetBool("IsFlip", false);
            animator.SetBool("IsSliding", false);

            PlayAnimationState(
                jumpStateName,
                true,
                0.05f
            );
        }

        private void HandleFlipJump()
        {
            currentScale = doubleJumpStretch;

            if (animator == null)
                return;

            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFlip", true);
            animator.SetBool("IsSliding", false);

            PlayAnimationState(
                flipStateName,
                true,
                0.05f
            );
        }

        private void HandleSlide()
        {
            if (animator == null)
                return;

            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFlip", false);
            animator.SetBool("IsSliding", true);

            PlayAnimationState(
                slideStateName,
                true,
                0.05f
            );
        }

        private void HandleSlideEnd()
        {
            if (animator == null)
                return;

            animator.SetBool("IsSliding", false);

            bool isMoving =
                playerController != null &&
                playerController.CurrentHorizontalVelocity.magnitude > 0.1f;

            string targetState =
                isMoving
                    ? runStateName
                    : idleStateName;

            PlayAnimationState(
                targetState,
                false,
                0.12f
            );
        }

        private void HandleLanded(float fallVelocity)
        {
            float intensity =
                Mathf.Clamp01(fallVelocity / 20f);

            float squashY =
                Mathf.Lerp(0.85f, 0.6f, intensity);

            float squashXZ =
                Mathf.Lerp(1.15f, 1.4f, intensity);

            currentScale =
                new Vector3(
                    squashXZ,
                    squashY,
                    squashXZ
                );

            if (animator == null)
                return;

            animator.SetBool("IsJumping", false);
            animator.SetBool("IsFlip", false);
            animator.SetBool("IsSliding", false);

            bool isMoving =
                playerController != null &&
                playerController.CurrentHorizontalVelocity.magnitude > 0.1f;

            string targetState =
                isMoving
                    ? runStateName
                    : idleStateName;

            PlayAnimationState(
                targetState,
                false,
                0.12f
            );
        }

        private void UpdateSquashAndStretch()
        {
            if (modelRoot == null ||
                modelRoot == transform)
                return;

            currentScale = Vector3.Lerp(
                currentScale,
                targetScale,
                Time.deltaTime * elasticitySpeed
            );

            modelRoot.localScale = currentScale;
        }

        private void UpdateTilt()
        {
            if (modelRoot == null ||
                modelRoot == transform ||
                playerController == null)
                return;

            float moveMagnitude =
                playerController.CurrentHorizontalVelocity.magnitude;

            float targetTilt =
                Mathf.Clamp01(moveMagnitude / 8f) *
                maxTiltAngle;

            currentTilt = Mathf.Lerp(
                currentTilt,
                targetTilt,
                Time.deltaTime * tiltSmoothSpeed
            );

            modelRoot.localRotation =
                Quaternion.Euler(
                    currentTilt,
                    modelFacingOffset,
                    0f
                );
        }

        private void UpdateAnimation()
        {
            if (animator == null ||
                playerController == null)
                return;

            bool isMoving =
                playerController.CurrentHorizontalVelocity.magnitude > 0.1f;

            animator.SetBool(
                "IsMoving",
                isMoving
            );

            if (playerController.IsSliding)
            {
                animator.SetBool(
                    "IsSliding",
                    true
                );

                return;
            }

            animator.SetBool(
                "IsSliding",
                false
            );

            if (playerController.IsGrounded)
            {
                animator.SetBool(
                    "IsJumping",
                    false
                );

                animator.SetBool(
                    "IsFlip",
                    false
                );

                return;
            }

            if (animator.GetBool("IsFlip"))
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(0);

                bool isFlipState =
                    stateInfo.IsName(flipStateName) ||
                    stateInfo.IsName(
                        "Base Layer." + flipStateName
                    );

                if (isFlipState &&
                    stateInfo.normalizedTime >= 0.90f)
                {
                    animator.SetBool(
                        "IsFlip",
                        false
                    );

                    animator.SetBool(
                        "IsJumping",
                        true
                    );

                    PlayAnimationState(
                        jumpStateName,
                        false,
                        0.15f
                    );
                }
            }
        }
    }
}