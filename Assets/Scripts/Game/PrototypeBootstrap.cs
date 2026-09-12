using UnityEngine;
using UnityEngine.UI;
using VistaWorld.Player;
using VistaWorld.CameraControl;
using VistaWorld.UI;
using VistaWorld.Platform;

namespace VistaWorld.Game
{
    /// <summary>
    /// Runtime Bootstrap: Tự động khởi tạo toàn bộ môi trường chơi prototype khi bấm Play
    /// nếu Scene hiện tại chưa có nhân vật hoặc chưa được dựng sẵn qua Editor Menu.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart()
        {
            if (FindObjectOfType<PlayerController>() != null)
            {
                // Đã có nhân vật trong Scene, không cần bootstrap tự động
                return;
            }

            Debug.Log("<color=cyan>[VistaWorld]</color> Phát hiện Scene chưa có thiết lập, đang tự động khởi tạo môi trường chơi...");
            BuildRuntimeEnvironment();
        }

        public static void BuildRuntimeEnvironment()
        {
            // 1. Ánh sáng
            Light sun = FindObjectOfType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Directional Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.2f;

            // 2. Vật liệu cơ bản
            Material matGround = new Material(Shader.Find("Standard")) { color = new Color(0.32f, 0.75f, 0.26f) };
            Material matPlatform = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.62f, 0.92f) };
            Material matMoving = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.82f, 0.18f) };
            Material matPlayer = new Material(Shader.Find("Standard")) { color = new Color(1.0f, 0.45f, 0.2f) };
            Material matCrystal = new Material(Shader.Find("Standard")) { color = new Color(0.95f, 0.25f, 0.85f) };

            // 3. Địa hình bệ nhảy
            GameObject level = new GameObject("--- LEVEL ENVIRONMENT ---");

            // Sàn chính
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_Arena";
            ground.transform.SetParent(level.transform);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(36f, 1f, 36f);
            ground.GetComponent<Renderer>().material = matGround;

            // Bậc thang 1 (Single Jump: 1.2m)
            CreatePlat(level.transform, "Plat_1", new Vector3(0, 1.2f, 8f), new Vector3(5f, 0.6f, 5f), matPlatform);
            CreateCryst(level.transform, new Vector3(0, 2.5f, 8f), matCrystal);

            // Bậc thang 2 (2.8m)
            CreatePlat(level.transform, "Plat_2", new Vector3(0, 2.8f, 15f), new Vector3(5f, 0.6f, 5f), matPlatform);
            CreateCryst(level.transform, new Vector3(0, 4.1f, 15f), matCrystal);

            // Bệ nhảy cao (Double Jump: 5.4m)
            CreatePlat(level.transform, "Plat_High", new Vector3(0, 5.4f, 22f), new Vector3(6f, 0.6f, 6f), matPlatform);
            CreateCryst(level.transform, new Vector3(0, 6.8f, 22f), matCrystal);

            // Bệ di động
            GameObject movingPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            movingPlat.name = "MovingPlatform";
            movingPlat.transform.SetParent(level.transform);
            movingPlat.transform.position = new Vector3(8f, 2.8f, 15f);
            movingPlat.transform.localScale = new Vector3(4.5f, 0.5f, 4.5f);
            movingPlat.GetComponent<Renderer>().material = matMoving;

            BoxCollider triggerCol = movingPlat.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = new Vector3(0, 2.2f, 0);
            triggerCol.size = new Vector3(1.05f, 3.5f, 1.05f);
            movingPlat.AddComponent<MovingPlatform>();

            // Đích đến
            CreatePlat(level.transform, "Plat_Island_Target", new Vector3(23f, 2.8f, 15f), new Vector3(6f, 0.6f, 6f), matPlatform);
            CreateCryst(level.transform, new Vector3(23f, 4.2f, 15f), matCrystal);

            // 4. Nhân vật Capsule
            GameObject player = new GameObject("PlayerCapsule");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.1f, 0);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1.0f, 0);
            cc.stepOffset = 0.4f;

            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "CapsuleModel";
            model.transform.SetParent(player.transform, false);
            model.transform.localPosition = new Vector3(0, 1.0f, 0);
            Destroy(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().material = matPlayer;

            PlayerController pc = player.AddComponent<PlayerController>();
            PlayerVisuals pv = player.AddComponent<PlayerVisuals>();
            pv.ModelRoot = model.transform;
            pv.CreateFacialIndicator();

            // 5. Main Camera & ThirdPersonCamera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 3.5f, -6f);
            ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.Target = player.transform;

            // 6. UI Canvas
            SetupUI();

            // 7. GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            Debug.Log("<color=green>[VistaWorld]</color> Khởi tạo thành công môi trường prototype!");
        }

        private static void CreatePlat(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plat.name = name;
            plat.transform.SetParent(parent);
            plat.transform.position = pos;
            plat.transform.localScale = scale;
            plat.GetComponent<Renderer>().material = mat;
        }

        private static void CreateCryst(Transform parent, Vector3 pos, Material mat)
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "Crystal";
            crystal.transform.SetParent(parent);
            crystal.transform.position = pos;
            crystal.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            crystal.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
            crystal.GetComponent<Collider>().isTrigger = true;
            crystal.GetComponent<Renderer>().material = mat;
            crystal.AddComponent<CollectibleItem>();
        }

        private static void SetupUI()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            GameObject canvasObj = new GameObject("UI_MobileCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // TouchField
            GameObject tfObj = new GameObject("TouchField_LookZone");
            tfObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tfRect = tfObj.AddComponent<RectTransform>();
            tfRect.anchorMin = new Vector2(0.35f, 0f);
            tfRect.anchorMax = new Vector2(1f, 1f);
            tfRect.offsetMin = Vector2.zero;
            tfRect.offsetMax = Vector2.zero;
            Image tfImg = tfObj.AddComponent<Image>();
            tfImg.color = new Color(0, 0, 0, 0);
            tfObj.AddComponent<TouchField>();

            // Virtual Joystick
            GameObject joyObj = new GameObject("VirtualJoystick");
            joyObj.transform.SetParent(canvasObj.transform, false);
            RectTransform joyRect = joyObj.AddComponent<RectTransform>();
            joyRect.anchorMin = Vector2.zero;
            joyRect.anchorMax = Vector2.zero;
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(200f, 200f);
            joyRect.sizeDelta = new Vector2(220f, 220f);
            Image joyBg = joyObj.AddComponent<Image>();
            joyBg.color = new Color(0.12f, 0.16f, 0.22f, 0.65f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(joyObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(95f, 95f);
            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = new Color(1f, 1f, 1f, 0.95f);

            joyObj.AddComponent<VirtualJoystick>();

            // Jump Button
            GameObject jumpObj = new GameObject("JumpButton");
            jumpObj.transform.SetParent(canvasObj.transform, false);
            RectTransform jumpRect = jumpObj.AddComponent<RectTransform>();
            jumpRect.anchorMin = new Vector2(1f, 0f);
            jumpRect.anchorMax = new Vector2(1f, 0f);
            jumpRect.pivot = new Vector2(0.5f, 0.5f);
            jumpRect.anchoredPosition = new Vector2(-160f, 170f);
            jumpRect.sizeDelta = new Vector2(140f, 140f);
            Image jumpImg = jumpObj.AddComponent<Image>();
            jumpImg.color = new Color(0.16f, 0.72f, 0.98f, 0.9f);
            jumpObj.AddComponent<JumpButton>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(jumpObj.transform, false);
            RectTransform tr = textObj.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            Text txt = textObj.AddComponent<Text>();
            txt.text = "JUMP";
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            // Hints
            GameObject hintObj = new GameObject("HintsPanel");
            hintObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hr = hintObj.AddComponent<RectTransform>();
            hr.anchorMin = new Vector2(0.5f, 1f);
            hr.anchorMax = new Vector2(0.5f, 1f);
            hr.pivot = new Vector2(0.5f, 1f);
            hr.anchoredPosition = new Vector2(0, -25f);
            hr.sizeDelta = new Vector2(900f, 40f);
            Text ht = hintObj.AddComponent<Text>();
            ht.text = "Gạt Joystick/WASD: Di chuyển | Vuốt phải/Chuột phải: Xoay Camera | Nút Jump/Space: Nhảy & Nhảy đúp";
            ht.fontSize = 18;
            ht.alignment = TextAnchor.MiddleCenter;
            ht.color = new Color(1f, 1f, 1f, 0.95f);
        }
    }
}
