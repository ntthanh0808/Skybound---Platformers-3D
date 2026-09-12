using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VistaWorld.UI
{
    /// <summary>
    /// Virtual Joystick dành cho thiết bị di động Android và Editor.
    /// Tích hợp nội suy mượt mà (Smoothing & Dampening), phản hồi mượt theo mọi hướng (đặc biệt là hướng W/tiến).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public static VirtualJoystick Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("Khung nền joystick")]
        [SerializeField] private RectTransform background;
        [Tooltip("Nút gạt joystick")]
        [SerializeField] private RectTransform handle;

        [Header("Joystick Settings")]
        [Tooltip("Bán kính tối đa nút gạt có thể di chuyển")]
        [SerializeField] private float handleRange = 85f;
        [Tooltip("Vùng chết - khoảng cách tối thiểu để nhận giá trị")]
        [SerializeField] private float deadZone = 0.04f;
        [Tooltip("Tốc độ đàn hồi và mượt mà của cần gạt (càng cao càng nhạy, 15-25 là mượt nhất)")]
        [SerializeField] private float smoothSpeed = 22f;
        [Tooltip("Tự động nhận phím WASD/Mũi tên khi không chạm vào màn hình")]
        [SerializeField] private bool fallbackToKeyboardInEditor = true;

        private Vector2 targetInputVector = Vector2.zero;
        private Vector2 currentInputVector = Vector2.zero;
        private Vector2 targetHandlePosition = Vector2.zero;
        private Canvas parentCanvas;
        private Camera canvasCamera;
        private bool isPointerDown = false;

        /// <summary>
        /// Vector hướng chuẩn hóa mượt mà từ -1 đến 1
        /// </summary>
        public Vector2 Direction => currentInputVector;

        /// <summary>
        /// Trả về true nếu người chơi đang chủ động gạt joystick
        /// </summary>
        public bool IsActive => currentInputVector.sqrMagnitude > deadZone * deadZone;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (background == null) background = GetComponent<RectTransform>();
            if (handle == null && transform.childCount > 0)
            {
                handle = transform.GetChild(0).GetComponent<RectTransform>();
            }

            // Tắt raycastTarget trên handle để không cản trở sự kiện kéo của background
            Image handleImg = handle != null ? handle.GetComponent<Image>() : null;
            if (handleImg != null) handleImg.raycastTarget = false;

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                canvasCamera = parentCanvas.worldCamera;
            }
        }

        private void Update()
        {
            // Xử lý bàn phím khi không chạm màn hình
            if (!isPointerDown && fallbackToKeyboardInEditor)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector2 keyboardInput = new Vector2(h, v);

                if (keyboardInput.sqrMagnitude > 0.01f)
                {
                    targetInputVector = keyboardInput.normalized;
                    targetHandlePosition = targetInputVector * handleRange;
                }
                else
                {
                    targetInputVector = Vector2.zero;
                    targetHandlePosition = Vector2.zero;
                }
            }

            // Nội suy mượt mà (Smoothing) cho cả inputVector và handle vị trí
            float dt = Time.unscaledDeltaTime;
            currentInputVector = Vector2.MoveTowards(currentInputVector, targetInputVector, smoothSpeed * dt);

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.Lerp(handle.anchoredPosition, targetHandlePosition, smoothSpeed * dt);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null) return;

            Vector2 position;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    canvasCamera,
                    out position))
            {
                // Giới hạn bán kính gạt
                position = Vector2.ClampMagnitude(position, handleRange);
                targetHandlePosition = position;

                // Chuẩn hóa vector từ -1 đến 1
                Vector2 rawInput = position / handleRange;
                if (rawInput.magnitude < deadZone)
                {
                    targetInputVector = Vector2.zero;
                }
                else
                {
                    targetInputVector = rawInput;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            targetInputVector = Vector2.zero;
            targetHandlePosition = Vector2.zero;
        }

        private void OnDisable()
        {
            isPointerDown = false;
            targetInputVector = Vector2.zero;
            targetHandlePosition = Vector2.zero;
            currentInputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
