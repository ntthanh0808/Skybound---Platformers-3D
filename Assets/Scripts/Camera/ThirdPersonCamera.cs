using UnityEngine;
using VistaWorld.UI;

namespace VistaWorld.CameraControl
{
    /// <summary>
    /// Camera góc nhìn thứ ba (Third-Person) mượt mà cho 3D Platformer trên Android.
    /// Hỗ trợ vuốt xoay quanh nhân vật (Orbit), tự động bám theo và tính năng chống xuyên tường (Collision Avoidance).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        [Header("Target & Tracking")]
        [Tooltip("Mục tiêu theo dõi (thường là nhân vật Capsule)")]
        [SerializeField] private Transform target;
        [Tooltip("Độ lệch trọng tâm ngắm vào nhân vật")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.2f, 0);
        [Tooltip("Độ trễ mượt mà khi bám theo nhân vật")]
        [SerializeField] private float followSmoothTime = 0.08f;

        [Header("Distance & Zoom")]
        [Tooltip("Khoảng cách mặc định từ camera tới nhân vật")]
        [SerializeField] private float defaultDistance = 5.5f;
        [Tooltip("Khoảng cách gần nhất khi bị tường ép vào")]
        [SerializeField] private float minDistance = 1.2f;
        [Tooltip("Khoảng cách xa nhất")]
        [SerializeField] private float maxDistance = 8.0f;
        [Tooltip("Tốc độ chuyển đổi khoảng cách camera")]
        [SerializeField] private float distanceSmoothSpeed = 12f;

        [Header("Orbit & Rotation")]
        [Tooltip("Độ nhạy xoay ngang (X)")]
        [SerializeField] private float sensitivityX = 0.2f;
        [Tooltip("Độ nhạy xoay dọc (Y)")]
        [SerializeField] private float sensitivityY = 0.15f;
        [Tooltip("Góc cúi thấp nhất (độ)")]
        [SerializeField] private float minPitch = -15f;
        [Tooltip("Góc ngẩng cao nhất (độ)")]
        [SerializeField] private float maxPitch = 70f;
        [Tooltip("Tự động xoay camera ra sau lưng khi nhân vật di chuyển")]
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
        [SerializeField] private float collisionPadding = 0.15f;

        private float currentYaw = 0f;
        private float currentPitch = 20f;
        private float currentDistance;
        private float targetDistance;
        private Vector3 currentFollowVelocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            currentDistance = defaultDistance;
            targetDistance = defaultDistance;

            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;
        }

        private void Start()
        {
            // Tự động tìm nhân vật mang tag Player nếu chưa gán
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleInput();
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            // Lấy tín hiệu vuốt từ TouchField hoặc chuột
            Vector2 inputDelta = Vector2.zero;
            if (TouchField.Instance != null)
            {
                inputDelta = TouchField.Instance.TouchDist;
            }

            if (inputDelta.sqrMagnitude > 0.001f)
            {
                currentYaw += inputDelta.x * sensitivityX;
                currentPitch -= inputDelta.y * sensitivityY;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
            else if (autoAlignBehindPlayer)
            {
                // Tự động xoay nhẹ theo hướng nhân vật
                float targetYaw = target.eulerAngles.y;
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * autoAlignSpeed);
            }
        }

        private void UpdateCameraPosition()
        {
            // 1. Tính toán tâm ngắm (Look At Point)
            Vector3 targetFocusPos = target.position + targetOffset;

            // 2. Tính toán hướng và vị trí xoay mong muốn
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredDirection = rotation * -Vector3.forward;

            // 3. Xử lý chống xuyên tường (Camera Collision Avoidance)
            float calculatedDistance = defaultDistance;
            if (enableCollisionAvoidance)
            {
                Ray ray = new Ray(targetFocusPos, desiredDirection);
                RaycastHit hit;

                // Bỏ qua va chạm với chính nhân vật
                int layerMaskWithoutPlayer = collisionLayers & ~(1 << target.gameObject.layer);

                if (Physics.SphereCast(ray, collisionRadius, out hit, defaultDistance, layerMaskWithoutPlayer))
                {
                    calculatedDistance = Mathf.Clamp(hit.distance - collisionPadding, minDistance, maxDistance);
                }
            }

            targetDistance = calculatedDistance;
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * distanceSmoothSpeed);

            // 4. Vị trí và góc quay cuối cùng của Camera
            Vector3 targetCamPosition = targetFocusPos + desiredDirection * currentDistance;
            transform.position = Vector3.SmoothDamp(transform.position, targetCamPosition, ref currentFollowVelocity, followSmoothTime);
            transform.rotation = rotation;
        }

        /// <summary>
        /// Đặt nhanh góc nhìn camera ra sau lưng mục tiêu
        /// </summary>
        public void ResetBehindTarget()
        {
            if (target != null)
            {
                currentYaw = target.eulerAngles.y;
                currentPitch = 20f;
            }
        }
    }
}
