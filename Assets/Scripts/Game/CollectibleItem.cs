using UnityEngine;

namespace VistaWorld.Game
{
    /// <summary>
    /// Vật phẩm đá quý / xu thu thập trôi nổi trên các bệ nhảy (phong cách Vista World).
    /// Tự động xoay tròn và nhấp nhô lơ lửng, phát tín hiệu cộng điểm khi nhân vật chạm vào.
    /// </summary>
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Tốc độ tự xoay quanh trục Y")]
        [SerializeField] private float rotationSpeed = 90.0f;
        [Tooltip("Biên độ nhấp nhô lên xuống")]
        [SerializeField] private float bobbingAmplitude = 0.2f;
        [Tooltip("Tần số nhấp nhô")]
        [SerializeField] private float bobbingFrequency = 2.0f;

        [Header("Value")]
        [SerializeField] private int value = 1;

        private Vector3 startPosition;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            // Xoay tròn
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Nhấp nhô hình sin
            float newY = startPosition.y + Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.GetComponent<Player.PlayerController>() != null)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoin(value);
                }

                // Biến mất khi nhặt
                Destroy(gameObject);
            }
        }
    }
}
