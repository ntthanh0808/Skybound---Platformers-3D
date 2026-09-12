using UnityEngine;
using UnityEngine.EventSystems;

namespace VistaWorld.UI
{
    /// <summary>
    /// Nút bấm Nhảy (Jump / Double Jump) ảo cho màn hình cảm ứng Android.
    /// Hỗ trợ cả phím Spacebar khi test trong Unity Editor.
    /// </summary>
    public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public static JumpButton Instance { get; private set; }

        [Tooltip("Cho phép dùng phím Space trên bàn phím khi test trong Editor")]
        [SerializeField] private bool allowSpacebarInEditor = true;

        private bool isPressedDownThisFrame = false;
        private bool isHeld = false;

        public bool IsHeld => isHeld || (allowSpacebarInEditor && Input.GetKey(KeyCode.Space));

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            if (allowSpacebarInEditor && Input.GetKeyDown(KeyCode.Space))
            {
                isPressedDownThisFrame = true;
            }
        }

        /// <summary>
        /// Kiểm tra và tiêu thụ tín hiệu bấm nhảy trong frame hiện tại
        /// </summary>
        public bool ConsumeJumpPress()
        {
            if (isPressedDownThisFrame)
            {
                isPressedDownThisFrame = false;
                return true;
            }
            return false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressedDownThisFrame = true;
            isHeld = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHeld = false;
        }

        private void OnDisable()
        {
            isPressedDownThisFrame = false;
            isHeld = false;
        }
    }
}
