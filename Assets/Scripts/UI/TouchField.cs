using UnityEngine;
using UnityEngine.EventSystems;

namespace VistaWorld.UI
{
    /// <summary>
    /// Vùng cảm ứng (nửa phải màn hình) để vuốt ngón tay xoay góc nhìn Camera Third-Person trên Android.
    /// Tự động hỗ trợ kéo chuột phải trong Unity Editor.
    /// </summary>
    public class TouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public static TouchField Instance { get; private set; }

        [Header("Editor Controls")]
        [Tooltip("Cho phép kéo chuột phải trong Unity Editor để xoay camera")]
        [SerializeField] private bool allowMouseDragInEditor = true;
        [SerializeField] private float mouseSensitivity = 2f;

        private Vector2 touchDist;
        private Vector2 pointerOld;
        private int pointerId;
        private bool isPressed;

        public Vector2 TouchDist => touchDist;
        public bool IsPressed => isPressed;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            if (isPressed)
            {
                if (pointerId >= 0 && pointerId < Input.touches.Length)
                {
                    touchDist = Input.touches[pointerId].position - pointerOld;
                    pointerOld = Input.touches[pointerId].position;
                }
                else
                {
                    Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                    touchDist = currentMousePos - pointerOld;
                    pointerOld = currentMousePos;
                }
            }
            else
            {
                // Hỗ trợ chuột phải trong Unity Editor khi không chạm vào UI
                if (allowMouseDragInEditor && Input.GetMouseButton(1))
                {
                    touchDist = new Vector2(
                        Input.GetAxis("Mouse X") * mouseSensitivity * 10f,
                        Input.GetAxis("Mouse Y") * mouseSensitivity * 10f
                    );
                }
                else
                {
                    touchDist = Vector2.zero;
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            pointerId = eventData.pointerId;
            pointerOld = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            touchDist = Vector2.zero;
        }

        private void OnDisable()
        {
            isPressed = false;
            touchDist = Vector2.zero;
        }
    }
}
