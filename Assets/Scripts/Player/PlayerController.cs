using System;
using UnityEngine;
using VistaWorld.UI;

namespace VistaWorld.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed=8.0f;
        [SerializeField] private float rotationSpeed=720.0f;
        [SerializeField] private float acceleration=25.0f;
        [SerializeField] private float deceleration=30.0f;
        [Range(0f,1f)][SerializeField] private float airControl=0.85f;

        [Header("Jump & Multi-Jump Settings")]
        [SerializeField] private float jumpHeight=2.8f;
        [SerializeField] private float doubleJumpHeight=2.5f;
        [SerializeField] private int maxJumpCount=3;
        [SerializeField] private float coyoteTime=0.15f;
        [SerializeField] private float jumpBufferTime=0.15f;

        [Header("Gravity & Physics")]
        [SerializeField] private float gravity=-28.0f;
        [SerializeField] private float fallMultiplier=1.7f;
        [SerializeField] private float terminalVelocity=-35.0f;
        [SerializeField] private float groundedGravity=-2.0f;

        [Header("Ground Check")]
        [SerializeField] private float groundCheckRadius=0.28f;
        [SerializeField] private Vector3 groundCheckOffset=new Vector3(0,0.1f,0);
        [SerializeField] private LayerMask groundLayers=~0;

        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

        [Header("Running Slide")]
        [SerializeField] private float slideSpeed=11.0f;
        [SerializeField] private float slideDuration=0.65f;
        [SerializeField] private float slideCooldown=0.35f;
        [SerializeField] private float slideDeceleration=18.0f;
        [SerializeField] private float slideControllerHeight=1.0f;

        private CharacterController characterController;
        private Vector3 currentVelocity;
        private float verticalVelocity;
        private bool isGrounded;
        private int jumpCount;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private bool wasGroundedLastFrame;

        private bool isSliding;
        private float slideTimer;
        private float slideCooldownTimer;
        private Vector3 slideDirection=Vector3.forward;
        private float originalControllerHeight;
        private Vector3 originalControllerCenter;

        private Transform currentRotatingPlatform;
        private Vector3 lastPlatformPosition;
        private Quaternion lastPlatformRotation;
        private Vector3 platformMotion;
        private Vector3 platformVelocity;
        private bool platformTrackingInitialized;

        public event Action OnJump;
        public event Action OnDoubleJump;
        public event Action OnFlipJump;
        public event Action<float> OnLanded;
        public event Action OnSlide;
        public event Action OnSlideEnd;

        public bool IsGrounded=>isGrounded;
        public Vector3 CurrentHorizontalVelocity=>new Vector3(currentVelocity.x,0f,currentVelocity.z);
        public float VerticalVelocity=>verticalVelocity;
        public int JumpCount=>jumpCount;
        public bool IsSliding=>isSliding;

        public Transform CameraTransform
        {
            get=>cameraTransform;
            set=>cameraTransform=value;
        }

        private void Awake()
        {
            characterController=GetComponent<CharacterController>();
            originalControllerHeight=characterController.height;
            originalControllerCenter=characterController.center;
            EnsureCameraReference();
        }

        private void Start()
        {
            EnsureCameraReference();
        }

        private void Update()
        {
            CheckGroundStatus();
            ApplyRotatingPlatformMotion();
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
            if(cameraTransform!=null)return;

            if(Camera.main!=null)
            {
                cameraTransform=Camera.main.transform;
                return;
            }

            var tpc=FindObjectOfType<VistaWorld.CameraControl.ThirdPersonCamera>();

            if(tpc!=null)
                cameraTransform=tpc.transform;
        }

        private void HandleKeyboardActions()
        {
            if(Input.GetKeyDown(KeyCode.LeftControl))
                StartSlide();

            if(SlideButton.Instance!=null&&SlideButton.Instance.ConsumeSlidePress())
                StartSlide();
        }

        private void HandleJumpInput()
        {
            bool jumpPressed=false;

            if(JumpButton.Instance!=null)
                jumpPressed=JumpButton.Instance.ConsumeJumpPress();

            if(!jumpPressed&&Input.GetButtonDown("Jump"))
                jumpPressed=true;

            if(jumpPressed)
                jumpBufferCounter=jumpBufferTime;
            else
                jumpBufferCounter-=Time.deltaTime;
        }

        private void CheckGroundStatus()
        {
            Vector3 spherePosition=transform.position+groundCheckOffset;
            int maskWithoutSelf=groundLayers&~(1<<gameObject.layer);

            bool physicsGrounded=Physics.CheckSphere(
                spherePosition,
                groundCheckRadius,
                maskWithoutSelf,
                QueryTriggerInteraction.Ignore
            );

            bool ccGrounded=characterController.isGrounded&&verticalVelocity<=0.1f;
            bool sphereGrounded=physicsGrounded&&verticalVelocity<=0.1f;

            isGrounded=ccGrounded||sphereGrounded;

            if(isGrounded)
            {
                coyoteTimeCounter=coyoteTime;

                if(!wasGroundedLastFrame)
                {
                    jumpCount=0;
                    OnLanded?.Invoke(Mathf.Abs(verticalVelocity));
                }
            }
            else
            {
                coyoteTimeCounter-=Time.deltaTime;
                StopRotatingPlatformTracking();
            }

            wasGroundedLastFrame=isGrounded;
        }

        private void ApplyMovement()
        {
            Vector2 input=Vector2.zero;

            if(VirtualJoystick.Instance!=null&&VirtualJoystick.Instance.IsActive)
            {
                input=VirtualJoystick.Instance.Direction;
            }
            else
            {
                input=new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );

                if(input.sqrMagnitude>1f)
                    input.Normalize();
            }

            if(isSliding)
            {
                currentVelocity=Vector3.MoveTowards(
                    currentVelocity,
                    slideDirection*slideSpeed,
                    slideDeceleration*Time.deltaTime
                );
                return;
            }

            if(cameraTransform==null)
                EnsureCameraReference();

            Vector3 moveDirection=Vector3.zero;

            if(cameraTransform!=null)
            {
                Vector3 forward=cameraTransform.forward;
                Vector3 right=cameraTransform.right;

                forward.y=0f;
                right.y=0f;

                forward.Normalize();
                right.Normalize();

                moveDirection=forward*input.y+right*input.x;
            }
            else
            {
                moveDirection=new Vector3(input.x,0f,input.y);
            }

            Vector3 targetVelocity=moveDirection*moveSpeed;

            float currentAccel;

            if(isGrounded)
            {
                currentAccel=input.sqrMagnitude>0.01f
                    ?acceleration
                    :deceleration;
            }
            else
            {
                currentAccel=input.sqrMagnitude>0.01f
                    ?acceleration*airControl
                    :deceleration*airControl;
            }

            currentVelocity.x=Mathf.MoveTowards(
                currentVelocity.x,
                targetVelocity.x,
                currentAccel*Time.deltaTime
            );

            currentVelocity.z=Mathf.MoveTowards(
                currentVelocity.z,
                targetVelocity.z,
                currentAccel*Time.deltaTime
            );

            Vector3 horizontalMove=new Vector3(
                currentVelocity.x,
                0f,
                currentVelocity.z
            );

            if(horizontalMove.sqrMagnitude>0.04f)
            {
                Quaternion targetRotation=Quaternion.LookRotation(
                    horizontalMove.normalized,
                    Vector3.up
                );

                transform.rotation=Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed*Time.deltaTime
                );
            }
        }

        private void ApplyGravityAndJump()
        {
            if(!isSliding&&jumpBufferCounter>0f)
            {
                if(jumpCount==0)
                {
                    Vector3 inheritedVelocity=platformVelocity;

                    StopRotatingPlatformTracking();

                    currentVelocity+=new Vector3(
                        inheritedVelocity.x,
                        0f,
                        inheritedVelocity.z
                    );

                    verticalVelocity=Mathf.Sqrt(
                        2f*jumpHeight*Mathf.Abs(gravity)
                    );

                    jumpCount=1;
                    jumpBufferCounter=0f;
                    coyoteTimeCounter=0f;

                    OnJump?.Invoke();
                }
                else if(!isGrounded&&jumpCount<maxJumpCount)
                {
                    verticalVelocity=Mathf.Sqrt(
                        2f*doubleJumpHeight*Mathf.Abs(gravity)
                    );

                    jumpCount++;
                    jumpBufferCounter=0f;

                    OnDoubleJump?.Invoke();
                    OnFlipJump?.Invoke();
                }
            }

            if(isGrounded&&verticalVelocity<0f)
            {
                verticalVelocity=groundedGravity;
            }
            else
            {
                bool isHoldingJump=
                    (JumpButton.Instance!=null&&JumpButton.Instance.IsHeld)||
                    Input.GetButton("Jump");

                float effectiveGravity=gravity;

                if(verticalVelocity<0f)
                    effectiveGravity*=fallMultiplier;
                else if(!isHoldingJump)
                    effectiveGravity*=fallMultiplier*1.2f;

                verticalVelocity+=effectiveGravity*Time.deltaTime;

                verticalVelocity=Mathf.Max(
                    verticalVelocity,
                    terminalVelocity
                );
            }
        }

        private void ApplyRotatingPlatformMotion()
        {
            platformMotion=Vector3.zero;

            if(currentRotatingPlatform==null||!isGrounded)
                return;

            Vector3 currentPosition=currentRotatingPlatform.position;
            Quaternion currentRotation=currentRotatingPlatform.rotation;

            if(!platformTrackingInitialized)
            {
                lastPlatformPosition=currentPosition;
                lastPlatformRotation=currentRotation;
                platformTrackingInitialized=true;
                platformVelocity=Vector3.zero;
                return;
            }

            Quaternion rotationDelta=
                currentRotation*
                Quaternion.Inverse(lastPlatformRotation);

            Vector3 offset=
                transform.position-
                lastPlatformPosition;

            Vector3 targetPosition=
                currentPosition+
                rotationDelta*offset;

            platformMotion=
                targetPosition-
                transform.position;

            if(Time.deltaTime>0f)
                platformVelocity=platformMotion/Time.deltaTime;

            lastPlatformPosition=currentPosition;
            lastPlatformRotation=currentRotation;
        }

        private void StartRotatingPlatformTracking(Transform platform)
        {
            if(platform==null)
                return;

            if(currentRotatingPlatform!=platform)
            {
                currentRotatingPlatform=platform;
                lastPlatformPosition=platform.position;
                lastPlatformRotation=platform.rotation;
                platformVelocity=Vector3.zero;
                platformTrackingInitialized=true;
            }
        }

        private void StopRotatingPlatformTracking()
        {
            currentRotatingPlatform=null;
            platformTrackingInitialized=false;
            platformMotion=Vector3.zero;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.normal.y<=0.5f)
                return;

            AutoRotate rotatingPlatform=
                hit.collider.GetComponentInParent<AutoRotate>();

            if(rotatingPlatform!=null)
            {
                StartRotatingPlatformTracking(
                    hit.collider.transform
                );
            }
        }

        public void StartSlide()
        {
            if(!isGrounded||isSliding||slideCooldownTimer>0f)
                return;

            Vector3 horizontalVelocity=new Vector3(
                currentVelocity.x,
                0f,
                currentVelocity.z
            );

            if(horizontalVelocity.magnitude<2f)
                return;

            slideDirection=horizontalVelocity.normalized;
            isSliding=true;
            slideTimer=slideDuration;
            slideCooldownTimer=slideCooldown;
            SetSlideCollider(true);
            currentVelocity=slideDirection*slideSpeed;
            OnSlide?.Invoke();
        }
        private void UpdateSlide()
        {
            if(!isSliding)
                return;

            slideTimer-=Time.deltaTime;

            if(slideTimer<=0f)
                StopSlide();
        }

        public void StopSlide()
        {
            if(!isSliding)
                return;

            isSliding=false;
            SetSlideCollider(false);
            OnSlideEnd?.Invoke();
        }

        private void SetSlideCollider(bool sliding)
        {
            if(characterController==null)
                return;

            if(sliding)
            {
                float oldHeight=characterController.height;
                float newHeight=slideControllerHeight;
                float heightDifference=oldHeight-newHeight;

                characterController.height=newHeight;

                Vector3 center=characterController.center;
                center.y-=heightDifference*0.5f;
                characterController.center=center;
            }
            else
            {
                float oldHeight=characterController.height;
                characterController.height=originalControllerHeight;

                Vector3 center=characterController.center;
                center.y+=(originalControllerHeight-oldHeight)*0.5f;
                characterController.center=center;
            }
        }

        private void UpdateCooldowns()
        {
            if(slideCooldownTimer>0f)
                slideCooldownTimer-=Time.deltaTime;
        }

        private void ExecuteMove()
        {
            Vector3 playerMotion=new Vector3(
                currentVelocity.x,
                verticalVelocity,
                currentVelocity.z
            )*Time.deltaTime;

            Vector3 totalMotion=
                playerMotion+
                platformMotion;

            characterController.Move(totalMotion);

            platformMotion=Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color=isGrounded?Color.green:Color.red;

            Gizmos.DrawWireSphere(
                transform.position+groundCheckOffset,
                groundCheckRadius
            );
        }
    }
}