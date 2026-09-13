using UnityEngine;
using VistaWorld.UI;

namespace VistaWorld.CameraControl
{
    /// <summary>
    /// Hệ thống Camera góc nhìn thứ ba (Third-Person Platformer Camera).
    /// Đặc điểm:
    /// - Góc chéo 45 độ (Elevated Diagonal Camera) ở độ cao vừa phải, nhìn xuống nhẹ.
    /// - Nhân vật luôn nằm chính giữa màn hình theo bề ngang, bề dọc tương đối.
    /// - Smooth follow theo vị trí nhân vật mà vẫn giữ nguyên góc và khoảng cách tương đối.
    /// - Hỗ trợ xoay 360 độ tự do quanh nhân vật khi người chơi thao tác camera.
    /// - Tự động tránh vật cản / chống xuyên tường (Collision Avoidance).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        [Header("Target & Framing")]
        [Tooltip("Mục tiêu theo dõi (thường là PlayerCapsule)")]
        [SerializeField] private Transform target;

        [Tooltip("Tâm ngắm nhân vật. X = 0 để nhân vật luôn nằm chính giữa màn hình theo bề ngang")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

        [Tooltip("Thời gian trễ mượt mà khi bám theo nhân vật (Smooth Follow). Giá trị nhỏ (0.03 - 0.05) giữ nhân vật ổn định ở trung tâm không bị trôi")]
        [SerializeField] private float followSmoothTime = 0.04f;

        [Header("Distance & Zoom")]
        [Tooltip("Khoảng cách mặc định từ camera tới nhân vật")]
        [SerializeField] private float defaultDistance = 6.0f;

        [Tooltip("Khoảng cách gần nhất khi bị tường cản")]
        [SerializeField] private float minDistance = 1.5f;

        [Tooltip("Khoảng cách xa nhất")]
        [SerializeField] private float maxDistance = 8.5f;

        [Tooltip("Tốc độ chuyển đổi khoảng cách camera khi né tường")]
        [SerializeField] private float distanceSmoothSpeed = 14f;

        [Header("Angle & Orbit (Elevated Diagonal)")]
        [Tooltip("Góc lệch chéo mặc định so với hướng nhân vật (45 độ)")]
        [SerializeField] private float defaultYawOffset = 45f;

        [Tooltip("Góc cúi nhìn xuống mặc định (Pitch, độ cao vừa phải nhìn xuống nhẹ)")]
        [SerializeField] private float defaultPitch = 28f;

        [Tooltip("Độ nhạy xoay ngang (Yaw) khi thao tác")]
        [SerializeField] private float sensitivityX = 0.22f;

        [Tooltip("Độ nhạy xoay dọc (Pitch) khi thao tác")]
        [SerializeField] private float sensitivityY = 0.16f;

        [Tooltip("Góc cúi thấp nhất (tránh camera chìm dưới sàn)")]
        [SerializeField] private float minPitch = 10f;

        [Tooltip("Góc ngẩng cao nhất (nhìn từ trên cao xuống)")]
        [SerializeField] private float maxPitch = 65f;

        [Tooltip("Tự động xoay camera ra sau lưng khi nhân vật di chuyển. Tắt để giữ nguyên góc chéo tương đối")]
        [SerializeField] private bool autoAlignBehindPlayer = false;
        [SerializeField] private float autoAlignSpeed = 1.5f;

        [Header("Collision & Wall Avoidance")]
        [Tooltip("Bật tính năng đẩy camera vào gần khi bị tường cản để không bị xuyên thấu")]
        [SerializeField] private bool enableCollisionAvoidance = true;

        [Tooltip("Lớp đối tượng được tính là tường/địa hình cản camera")]
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Tooltip("Bán kính quả cầu dò vật cản")]
        [SerializeField] private float collisionRadius = 0.25f;

        [Tooltip("Khoảng đệm tránh sát vách")]
        [SerializeField] private float collisionPadding = 0.2f;

        // Trạng thái nội bộ
        private float currentYaw;
        private float currentPitch;
        private float currentDistance;
        private float targetDistance;
        private Vector3 currentFollowVelocity;
        private bool isInitialized = false;

        public Transform Target
        {
            get => target;
            set
            {
                target = value;
                if (target != null && !isInitialized)
                {
                    InitializeCamera();
                }
            }
        }

        public float CurrentYaw => currentYaw;
        public float CurrentPitch => currentPitch;
        public float CurrentDistance => currentDistance;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            currentDistance = defaultDistance;
            targetDistance = defaultDistance;
        }

        private void Start()
        {
            // Tự động tìm nhân vật mang tag Player nếu chưa gán
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            if (target != null)
            {
                InitializeCamera();
            }
            else
            {
                Vector3 angles = transform.eulerAngles;
                currentYaw = angles.y != 0 ? angles.y : defaultYawOffset;
                currentPitch = angles.x != 0 ? angles.x : defaultPitch;
            }
        }

        /// <summary>
        /// Khởi tạo vị trí và góc nhìn chéo 45 độ ban đầu
        /// </summary>
        public void InitializeCamera()
        {
            if (target == null) return;

            // Đặt góc chéo 45 độ so với góc xoay của nhân vật
            currentYaw = target.eulerAngles.y + defaultYawOffset;
            currentPitch = defaultPitch;
            currentDistance = defaultDistance;
            targetDistance = defaultDistance;

            Vector3 targetFocusPos = target.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredDirection = rotation * -Vector3.forward;

            transform.position = targetFocusPos + desiredDirection * currentDistance;
            transform.rotation = rotation;
            currentFollowVelocity = Vector3.zero;
            isInitialized = true;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdateCameraPosition();
        }

        /// <summary>
        /// Xử lý thao tác xoay camera 360 độ từ TouchField (Mobile) hoặc Chuột (PC / Editor)
        /// </summary>
        private void HandleInput()
        {
            Vector2 inputDelta = Vector2.zero;

            // 1. Nhận tín hiệu từ TouchField (vùng cảm ứng nửa phải màn hình)
            if (TouchField.Instance != null)
            {
                inputDelta = TouchField.Instance.TouchDist;
            }

            // 2. Dự phòng chuột phải trên PC / Unity Editor nếu không chạm vào TouchField
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (inputDelta.sqrMagnitude < 0.001f && Input.GetMouseButton(1))
            {
                inputDelta = new Vector2(
                    Input.GetAxis("Mouse X") * (sensitivityX * 25f),
                    Input.GetAxis("Mouse Y") * (sensitivityY * 25f)
                );
            }
            #endif

            // 3. Áp dụng xoay camera
            if (inputDelta.sqrMagnitude > 0.0001f)
            {
                // Xoay ngang 360 độ tự do quanh nhân vật
                currentYaw += inputDelta.x * sensitivityX;
                NormalizeYaw();

                // Xoay dọc giới hạn góc nhìn vừa phải
                currentPitch -= inputDelta.y * sensitivityY;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
            else if (autoAlignBehindPlayer)
            {
                // Tự động xoay nhẹ theo hướng nhân vật (nếu bật)
                float targetYaw = target.eulerAngles.y + defaultYawOffset;
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * autoAlignSpeed);
                NormalizeYaw();
            }
        }

        /// <summary>
        /// Đảm bảo góc Yaw luôn nằm trong khoảng [0, 360) mượt mà
        /// </summary>
        private void NormalizeYaw()
        {
            if (currentYaw >= 360f) currentYaw -= 360f;
            else if (currentYaw < 0f) currentYaw += 360f;
        }

        /// <summary>
        /// Cập nhật vị trí và góc nhìn camera mượt mà, giữ nhân vật ở chính giữa màn hình theo bề ngang
        /// </summary>
        private void UpdateCameraPosition()
        {
            // 1. Điểm ngắm trọng tâm nhân vật (X = 0 đảm bảo chính giữa ngang)
            Vector3 targetFocusPos = target.position + targetOffset;

            // 2. Hướng và góc xoay mong muốn của Camera
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredDirection = rotation * -Vector3.forward;

            // 3. Xử lý chống xuyên tường (Collision Avoidance qua SphereCast)
            float calculatedDistance = defaultDistance;
            if (enableCollisionAvoidance)
            {
                Ray ray = new Ray(targetFocusPos, desiredDirection);
                RaycastHit hit;

                // Bỏ qua va chạm với chính nhân vật
                int layerMaskWithoutPlayer = collisionLayers & ~(1 << target.gameObject.layer);

                if (Physics.SphereCast(ray, collisionRadius, out hit, defaultDistance, layerMaskWithoutPlayer, QueryTriggerInteraction.Ignore))
                {
                    calculatedDistance = Mathf.Clamp(hit.distance - collisionPadding, minDistance, maxDistance);
                }
            }

            targetDistance = calculatedDistance;
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * distanceSmoothSpeed);

            // 4. Vị trí đích lý tưởng của Camera
            Vector3 targetCamPosition = targetFocusPos + desiredDirection * currentDistance;

            // 5. Smooth Follow theo vị trí nhân vật để di chuyển mượt mà, giữ nhân vật luôn ở trung tâm
            transform.position = Vector3.SmoothDamp(transform.position, targetCamPosition, ref currentFollowVelocity, followSmoothTime);
            transform.rotation = rotation;
        }

        /// <summary>
        /// Đặt lại góc nhìn chéo 45 độ mặc định ra sau lưng nhân vật
        /// </summary>
        public void ResetDiagonalView()
        {
            if (target != null)
            {
                currentYaw = target.eulerAngles.y + defaultYawOffset;
                currentPitch = defaultPitch;
                currentDistance = defaultDistance;
                targetDistance = defaultDistance;
            }
        }

        /// <summary>
        /// Đặt ngay camera vào vị trí chuẩn mà không qua smooth damp (dùng khi mới spawn hoặc teleport)
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 targetFocusPos = target.position + targetOffset;
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredDirection = rotation * -Vector3.forward;

            transform.position = targetFocusPos + desiredDirection * currentDistance;
            transform.rotation = rotation;
            currentFollowVelocity = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Vector3 focus = target.position + targetOffset;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(focus, 0.2f);
                Gizmos.DrawLine(focus, transform.position);
            }
        }
    }
}
