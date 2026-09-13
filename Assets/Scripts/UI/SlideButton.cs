using UnityEngine;
using UnityEngine.EventSystems;

namespace VistaWorld.UI
{
    public class SlideButton : MonoBehaviour, IPointerDownHandler
    {
        public static SlideButton Instance { get; private set; }

        [SerializeField] private bool allowCKeyInEditor = true;

        private bool isPressedThisFrame;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Update()
        {
            if (allowCKeyInEditor && Input.GetKeyDown(KeyCode.C))
                isPressedThisFrame = true;
        }

        public bool ConsumeSlidePress()
        {
            if (!isPressedThisFrame)
                return false;

            isPressedThisFrame = false;
            return true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressedThisFrame = true;
        }

        private void OnDisable()
        {
            isPressedThisFrame = false;
        }
    }
}