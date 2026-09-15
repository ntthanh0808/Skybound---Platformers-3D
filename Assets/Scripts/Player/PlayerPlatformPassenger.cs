using UnityEngine;

public class PlayerPlatformPassenger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu vật thể bước lên có Tag là "MovingPlatform"
        if (other.CompareTag("MovingPlatform"))
        {
            // Tự động làm con của khối đó để quay/di chuyển theo
            transform.SetParent(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            // Rời khỏi khối thì bỏ làm con
            transform.SetParent(null);
        }
    }
}