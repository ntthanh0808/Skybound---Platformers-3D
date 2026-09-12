using UnityEngine;
using UnityEngine.UI;
using VistaWorld.Player;

namespace VistaWorld.Game
{
    /// <summary>
    /// Quản lý trạng thái trò chơi cho prototype Vista World:
    /// - Tự động hồi sinh nhân vật khi rơi khỏi bệ xuống vực (Void Fall)
    /// - Quản lý điểm số / Đá quý (Crystals / Coins)
    /// - Cập nhật thông tin HUD
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Respawn Settings")]
        [Tooltip("Nhân vật cần quản lý")]
        [SerializeField] private PlayerController player;
        [Tooltip("Vị trí hồi sinh ban đầu")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1.5f, 0);
        [Tooltip("Độ cao ngưỡng rơi xuống vực để kích hoạt hồi sinh")]
        [SerializeField] private float fallThresholdY = -12.0f;

        [Header("Collectibles / Score")]
        [SerializeField] private int collectedCoins = 0;
        [SerializeField] private Text scoreText;

        public int Coins => collectedCoins;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            if (player != null)
            {
                spawnPosition = player.transform.position;
            }
        }

        private void Update()
        {
            // Tự động hồi sinh khi rơi khỏi sàn đấu
            if (player != null && player.transform.position.y < fallThresholdY)
            {
                RespawnPlayer();
            }
        }

        /// <summary>
        /// Đưa nhân vật trở lại điểm xuất phát một cách an toàn
        /// </summary>
        public void RespawnPlayer()
        {
            if (player == null) return;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = spawnPosition;
            player.transform.rotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;
        }

        /// <summary>
        /// Cộng điểm đá quý/xu khi nhặt được
        /// </summary>
        public void AddCoin(int amount = 1)
        {
            collectedCoins += amount;
            if (scoreText != null)
            {
                scoreText.text = "Crystals: " + collectedCoins;
            }
        }
    }
}
