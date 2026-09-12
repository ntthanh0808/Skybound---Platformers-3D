using UnityEngine;

namespace VistaWorld.Player
{
    /// <summary>
    /// Xử lý hiệu ứng hình ảnh sống động cho nhân vật Capsule phong cách Vista World:
    /// - Nhún xẹp (Squash) khi tiếp đất
    /// - Kéo dãn (Stretch) khi bật nhảy / nhảy đúp
    /// - Nghiêng người nhẹ theo gia tốc khi chạy
    /// - Tự động tạo đôi mắt/kính định hướng mặt nhân vật nếu chưa có
    /// </summary>
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform chứa mô hình nhân vật để áp dụng scale nhún (nếu để trống sẽ lấy chính transform này)")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private PlayerController playerController;

        public Transform ModelRoot
        {
            get => modelRoot;
            set => modelRoot = value;
        }

        [Header("Squash & Stretch Settings")]
        [Tooltip("Độ dãn tối đa khi bật nhảy")]
        [SerializeField] private Vector3 jumpStretch = new Vector3(0.8f, 1.3f, 0.8f);
        [Tooltip("Độ dãn khi nhảy đúp")]
        [SerializeField] private Vector3 doubleJumpStretch = new Vector3(0.7f, 1.45f, 0.7f);
        [Tooltip("Độ nhún xẹp cơ bản khi tiếp đất")]
        [SerializeField] private Vector3 landSquash = new Vector3(1.3f, 0.7f, 1.3f);
        [Tooltip("Tốc độ đàn hồi trở về hình dạng bình thường")]
        [SerializeField] private float elasticitySpeed = 10.0f;

        [Header("Tilt & Lean Settings")]
        [Tooltip("Góc nghiêng tối đa về phía trước khi chạy nhanh")]
        [SerializeField] private float maxTiltAngle = 12.0f;
        [SerializeField] private float tiltSmoothSpeed = 8.0f;

        [Header("Facial Indicator (Mắt/Kính)")]
        [Tooltip("Tự động tạo cặp mắt hoạt hình nếu chưa có")]
        [SerializeField] private bool autoGenerateEyes = true;

        private Vector3 targetScale = Vector3.one;
        private Vector3 currentScale = Vector3.one;
        private float currentTilt = 0f;

        private void Awake()
        {
            if (modelRoot == null)
            {
                Transform childModel = transform.Find("CapsuleModel");
                if (childModel != null)
                {
                    modelRoot = childModel;
                }
            }
            if (playerController == null) playerController = GetComponent<PlayerController>();

            if (autoGenerateEyes)
            {
                CreateFacialIndicator();
            }
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnJump += HandleJump;
                playerController.OnDoubleJump += HandleDoubleJump;
                playerController.OnLanded += HandleLanded;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnJump -= HandleJump;
                playerController.OnDoubleJump -= HandleDoubleJump;
                playerController.OnLanded -= HandleLanded;
            }
        }

        private void Update()
        {
            UpdateSquashAndStretch();
            UpdateTilt();
        }

        private void HandleJump()
        {
            currentScale = jumpStretch;
        }

        private void HandleDoubleJump()
        {
            currentScale = doubleJumpStretch;
        }

        private void HandleLanded(float fallVelocity)
        {
            // Độ nhún tỉ lệ với tốc độ rơi
            float intensity = Mathf.Clamp01(fallVelocity / 20f);
            float squashY = Mathf.Lerp(0.85f, 0.6f, intensity);
            float squashXZ = Mathf.Lerp(1.15f, 1.4f, intensity);

            currentScale = new Vector3(squashXZ, squashY, squashXZ);
        }

        private void UpdateSquashAndStretch()
        {
            if (modelRoot == null || modelRoot == transform) return;

            // Đàn hồi mượt mà về hình dạng ban đầu (1, 1, 1)
            currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * elasticitySpeed);
            modelRoot.localScale = currentScale;
        }

        private void UpdateTilt()
        {
            if (modelRoot == null || modelRoot == transform || playerController == null) return;

            // Nghiêng người nhẹ khi chạy (chỉ áp dụng lên trục X của model con)
            float moveMagnitude = playerController.CurrentHorizontalVelocity.magnitude;
            float targetTilt = Mathf.Clamp01(moveMagnitude / 8f) * maxTiltAngle;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothSpeed);

            modelRoot.localRotation = Quaternion.Euler(currentTilt, 0f, 0f);
        }

        /// <summary>
        /// Tạo cặp mắt hoạt hình phía trước Capsule để người chơi nhìn rõ hướng mặt
        /// </summary>
        public void CreateFacialIndicator()
        {
            Transform existingFace = transform.Find("FaceIndicator");
            if (existingFace != null) return;

            GameObject face = new GameObject("FaceIndicator");
            face.transform.SetParent(modelRoot, false);
            face.transform.localPosition = new Vector3(0, 0.45f, 0.35f);

            // Mắt trái
            CreateEye(face.transform, new Vector3(-0.16f, 0, 0));
            // Mắt phải
            CreateEye(face.transform, new Vector3(0.16f, 0, 0));
        }

        private void CreateEye(Transform parent, Vector3 localPos)
        {
            // Tròng trắng
            GameObject eyeWhite = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeWhite.name = "EyeWhite";
            eyeWhite.transform.SetParent(parent, false);
            eyeWhite.transform.localPosition = localPos;
            eyeWhite.transform.localScale = new Vector3(0.18f, 0.25f, 0.12f);
            
            // Xóa collider của mắt
            Collider colWhite = eyeWhite.GetComponent<Collider>();
            if (colWhite != null) DestroyImmediate(colWhite);

            // Tròng đen (con ngươi)
            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = "Pupil";
            pupil.transform.SetParent(eyeWhite.transform, false);
            pupil.transform.localPosition = new Vector3(0, 0, 0.45f);
            pupil.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);

            Collider colPupil = pupil.GetComponent<Collider>();
            if (colPupil != null) DestroyImmediate(colPupil);

            // Gán màu đen cho con ngươi
            Renderer pupilRenderer = pupil.GetComponent<Renderer>();
            if (pupilRenderer != null)
            {
                pupilRenderer.material.color = Color.black;
            }
        }
    }
}
