using System.Collections.Generic;
using UnityEngine;

namespace VistaWorld.Platform
{
    /// <summary>
    /// Bệ nhảy di động (Moving Platform) cho 3D Platformer phong cách Vista World.
    /// Di chuyển qua lại giữa các điểm và giữ nhân vật đứng yên vững chắc trên bệ thông qua CharacterController.Move,
    /// không làm thay đổi phân cấp cha-con (tránh méo mó tỷ lệ scale).
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Điểm xuất phát lệch so với vị trí hiện tại")]
        [SerializeField] private Vector3 startOffset = Vector3.zero;
        [Tooltip("Điểm kết thúc lệch so với vị trí hiện tại")]
        [SerializeField] private Vector3 targetOffset = new Vector3(10f, 0, 0);
        [Tooltip("Tốc độ di chuyển của bệ")]
        [SerializeField] private float speed = 2.8f;
        [Tooltip("Thời gian dừng chờ ở mỗi đầu bệ (giây)")]
        [SerializeField] private float waitTimeAtEnds = 0.6f;

        private Vector3 pointA;
        private Vector3 pointB;
        private Vector3 targetPoint;
        private float waitTimer = 0f;
        private Vector3 currentVelocity = Vector3.zero;

        // Danh sách các nhân vật đang đứng trên bệ
        private readonly HashSet<CharacterController> riders = new HashSet<CharacterController>();

        public Vector3 PlatformVelocity => currentVelocity;

        private void Start()
        {
            pointA = transform.position + startOffset;
            pointB = transform.position + targetOffset;
            targetPoint = pointB;
        }

        private void Update()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                currentVelocity = Vector3.zero;
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 nextPos = Vector3.MoveTowards(currentPos, targetPoint, speed * Time.deltaTime);
            Vector3 moveDelta = nextPos - currentPos;

            currentVelocity = Time.deltaTime > 0f ? moveDelta / Time.deltaTime : Vector3.zero;

            // 1. Di chuyển Transform của bệ
            transform.position = nextPos;

            // 2. Di chuyển tất cả nhân vật đang đứng trên bệ theo cùng một vector moveDelta
            if (moveDelta.sqrMagnitude > 0f && riders.Count > 0)
            {
                riders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

                foreach (var rider in riders)
                {
                    rider.Move(moveDelta);
                }
            }

            // Đổi hướng khi chạm đích
            if (Vector3.Distance(transform.position, targetPoint) < 0.02f)
            {
                waitTimer = waitTimeAtEnds;
                targetPoint = (targetPoint == pointA) ? pointB : pointA;
            }
        }

        /// <summary>
        /// Đăng ký nhân vật đứng trên bệ
        /// </summary>
        public void RegisterRider(CharacterController cc)
        {
            if (cc != null && !riders.Contains(cc))
            {
                riders.Add(cc);
            }
        }

        /// <summary>
        /// Hủy đăng ký khi nhân vật rời bệ
        /// </summary>
        public void UnregisterRider(CharacterController cc)
        {
            if (cc != null)
            {
                riders.Remove(cc);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc == null && other.transform.parent != null)
            {
                cc = other.transform.parent.GetComponent<CharacterController>();
            }

            if (cc != null)
            {
                RegisterRider(cc);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc == null && other.transform.parent != null)
            {
                cc = other.transform.parent.GetComponent<CharacterController>();
            }

            if (cc != null && !riders.Contains(cc))
            {
                RegisterRider(cc);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc == null && other.transform.parent != null)
            {
                cc = other.transform.parent.GetComponent<CharacterController>();
            }

            if (cc != null)
            {
                UnregisterRider(cc);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 a = Application.isPlaying ? pointA : transform.position + startOffset;
            Vector3 b = Application.isPlaying ? pointB : transform.position + targetOffset;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireCube(a, Vector3.one * 0.5f);
            Gizmos.DrawWireCube(b, Vector3.one * 0.5f);
        }
    }
}
