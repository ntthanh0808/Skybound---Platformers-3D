using UnityEngine;

public class Robot : MonoBehaviour
{
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 100, 0);
    [SerializeField] private Transform[] hands; // Kéo 2 Cube Tay vào đây

    void Update()
    {
        // 1. Xoay Pivot (kéo cả Thân và 2 Tay quay theo quỹ đạo)
        transform.Rotate(rotationSpeed * Time.deltaTime);

        // 2. Khóa góc xoay của 2 Tay để luôn giữ thẳng đứng theo World Axis
        foreach (Transform hand in hands)
        {
            if (hand != null)
            {
                hand.rotation = Quaternion.identity;
            }
        }
    }
}