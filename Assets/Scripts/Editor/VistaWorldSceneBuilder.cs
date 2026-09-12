using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using VistaWorld.Player;
using VistaWorld.CameraControl;
using VistaWorld.UI;
using VistaWorld.Platform;
using VistaWorld.Game;

namespace VistaWorld.Editor
{
    /// <summary>
    /// Công cụ 1-Click tự động xây dựng Scene Prototype hoàn chỉnh phong cách Vista World trong Unity Editor.
    /// Tự động tạo: Nhân vật Capsule, Địa hình bệ nhảy, Virtual Joystick, Jump Button, Touch Orbit Field, Camera Third-Person, Crystals & GameManager.
    /// </summary>
    [InitializeOnLoad]
    public static class VistaWorldSceneBuilder
    {
        static VistaWorldSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!System.IO.File.Exists("Assets/Scenes/PrototypeScene.unity"))
                {
                    BuildPrototypeSceneInternal(silent: true);
                }
            };
        }

        [MenuItem("Tools/Vista World/Build Prototype Scene")]
        public static void BuildPrototypeSceneManual()
        {
            BuildPrototypeSceneInternal(silent: false);
        }

        public static void BuildPrototypeSceneInternal(bool silent)
        {
            // 1. Tạo Scene mới
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Đảm bảo các thư mục tồn tại
            EnsureDirectoryExists("Assets/Scenes");
            EnsureDirectoryExists("Assets/Materials");
            EnsureDirectoryExists("Assets/UI");

            // 2. Tạo UI Sprites hình tròn mượt mà (Anti-aliased Sprites)
            Sprite joyBaseSprite = CreateCircleSprite("Assets/UI/Sprite_JoystickBase.png", new Color(0.12f, 0.16f, 0.22f, 0.6f), new Color(1f, 1f, 1f, 0.4f), 0.06f);
            Sprite joyKnobSprite = CreateCircleSprite("Assets/UI/Sprite_JoystickKnob.png", new Color(1f, 1f, 1f, 0.95f), new Color(0.7f, 0.8f, 0.95f, 1f), 0.1f);
            Sprite jumpBtnSprite = CreateCircleSprite("Assets/UI/Sprite_JumpBtn.png", new Color(0.16f, 0.72f, 0.98f, 0.9f), new Color(1f, 1f, 1f, 0.8f), 0.08f);

            // 3. Tạo Materials tươi sáng phong cách Vista World
            Material matPlayer = CreateOrLoadMaterial("Assets/Materials/Mat_PlayerCapsule.mat", new Color(1.0f, 0.45f, 0.2f));
            Material matGround = CreateOrLoadMaterial("Assets/Materials/Mat_GroundGrass.mat", new Color(0.32f, 0.75f, 0.26f));
            Material matPlatform = CreateOrLoadMaterial("Assets/Materials/Mat_FloatingPlatform.mat", new Color(0.2f, 0.62f, 0.92f));
            Material matMoving = CreateOrLoadMaterial("Assets/Materials/Mat_MovingPlatform.mat", new Color(1.0f, 0.82f, 0.18f));
            Material matCrystal = CreateOrLoadMaterial("Assets/Materials/Mat_Crystal.mat", new Color(0.95f, 0.25f, 0.85f), true);

            // 4. Cấu hình Ánh sáng
            SetupLighting();

            // 5. Tạo Sàn đấu & Bệ nhảy Platformer
            GameObject levelRoot = new GameObject("--- LEVEL ENVIRONMENT ---");
            CreateLevelGeometry(levelRoot, matGround, matPlatform, matMoving, matCrystal);

            // 6. Tạo Nhân vật Capsule
            GameObject player = CreatePlayerCapsule(matPlayer);

            // 7. Cấu hình Main Camera & ThirdPersonCamera
            SetupCamera(player);

            // 8. Tạo UI Canvas Android
            Text scoreText = SetupMobileUI(joyBaseSprite, joyKnobSprite, jumpBtnSprite);

            // 9. Tạo GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            SerializedObject soGm = new SerializedObject(gm);
            soGm.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
            soGm.FindProperty("scoreText").objectReferenceValue = scoreText;
            soGm.ApplyModifiedProperties();

            // 10. Lưu Scene
            string scenePath = "Assets/Scenes/PrototypeScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
            {
                EditorUtility.DisplayDialog(
                    "Vista World Prototype",
                    "Đã tạo thành công Scene Prototype tại:\n" + scenePath + "\n\nNhấn nút PLAY để bắt đầu trải nghiệm ngay!",
                    "Bắt đầu ngay"
                );
            }
            else
            {
                Debug.Log("[VistaWorld] Đã tự động khởi tạo Scene Prototype tại " + scenePath);
            }

            Selection.activeGameObject = player;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static Sprite CreateCircleSprite(string path, Color innerColor, Color borderColor, float borderWidth = 0.08f)
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = (size / 2f) - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float normDist = dist / radius;

                    if (normDist > 1.0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (normDist > 1.0f - borderWidth)
                    {
                        float alpha = Mathf.Clamp01((1.0f - normDist) / 0.04f);
                        tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, innerColor);
                    }
                }
            }
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Material CreateOrLoadMaterial(string path, Color color, bool isEmission = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                mat.SetFloat("_Glossiness", 0.4f);
                if (isEmission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 0.8f);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                if (isEmission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 0.8f);
                }
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void SetupLighting()
        {
            RenderSettings.ambientLight = new Color(0.65f, 0.7f, 0.8f);
            Light sun = Object.FindObjectOfType<Light>();
            if (sun == null)
            {
                GameObject sunObj = new GameObject("Directional Light");
                sun = sunObj.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.color = new Color(1f, 0.97f, 0.9f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
        }

        private static void CreateLevelGeometry(GameObject parent, Material matGround, Material matPlatform, Material matMoving, Material matCrystal)
        {
            // Sàn chính (Main Ground)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_Arena";
            ground.transform.SetParent(parent.transform);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(36f, 1f, 36f);
            ground.GetComponent<Renderer>().material = matGround;

            // Bệ nhảy bậc thang (Test Single Jump: 1.2m và 2.8m)
            CreatePlatform(parent.transform, "Platform_Step1", new Vector3(0, 1.2f, 8f), new Vector3(5f, 0.6f, 5f), matPlatform);
            CreateCrystal(parent.transform, new Vector3(0, 2.5f, 8f), matCrystal);

            CreatePlatform(parent.transform, "Platform_Step2", new Vector3(0, 2.8f, 15f), new Vector3(5f, 0.6f, 5f), matPlatform);
            CreateCrystal(parent.transform, new Vector3(0, 4.1f, 15f), matCrystal);

            // Bệ nhảy cao (Test Double Jump: 5.4m)
            CreatePlatform(parent.transform, "Platform_High_DoubleJump", new Vector3(0, 5.4f, 22f), new Vector3(6f, 0.6f, 6f), matPlatform);
            CreateCrystal(parent.transform, new Vector3(0, 6.8f, 22f), matCrystal);

            // Bệ nhảy di động (Test Moving Platform)
            GameObject movingPlat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            movingPlat.name = "MovingPlatform";
            movingPlat.transform.SetParent(parent.transform);
            movingPlat.transform.position = new Vector3(8f, 2.8f, 15f);
            movingPlat.transform.localScale = new Vector3(4.5f, 0.5f, 4.5f);
            movingPlat.GetComponent<Renderer>().material = matMoving;

            BoxCollider triggerCol = movingPlat.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.center = new Vector3(0, 2.2f, 0);
            triggerCol.size = new Vector3(1.05f, 3.5f, 1.05f);

            MovingPlatform mp = movingPlat.AddComponent<MovingPlatform>();
            SerializedObject soMp = new SerializedObject(mp);
            soMp.FindProperty("targetOffset").vector3Value = new Vector3(10f, 0, 0);
            soMp.ApplyModifiedProperties();

            // Đích đến của bệ di động
            CreatePlatform(parent.transform, "Platform_Island_Target", new Vector3(23f, 2.8f, 15f), new Vector3(6f, 0.6f, 6f), matPlatform);
            CreateCrystal(parent.transform, new Vector3(23f, 4.2f, 15f), matCrystal);
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plat.name = name;
            plat.transform.SetParent(parent);
            plat.transform.position = pos;
            plat.transform.localScale = scale;
            plat.GetComponent<Renderer>().material = mat;
            return plat;
        }

        private static GameObject CreateCrystal(Transform parent, Vector3 pos, Material mat)
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crystal.name = "Crystal";
            crystal.transform.SetParent(parent);
            crystal.transform.position = pos;
            crystal.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);
            crystal.transform.rotation = Quaternion.Euler(45f, 45f, 0f);

            Collider col = crystal.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            crystal.GetComponent<Renderer>().material = mat;
            crystal.AddComponent<CollectibleItem>();
            return crystal;
        }

        private static GameObject CreatePlayerCapsule(Material matPlayer)
        {
            GameObject player = new GameObject("PlayerCapsule");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.1f, 0);

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 2.0f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0, 1.0f, 0);
            cc.stepOffset = 0.4f;
            cc.slopeLimit = 45f;

            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "CapsuleModel";
            model.transform.SetParent(player.transform, false);
            model.transform.localPosition = new Vector3(0, 1.0f, 0);
            model.transform.localScale = Vector3.one;

            Collider modelCol = model.GetComponent<Collider>();
            if (modelCol != null) Object.DestroyImmediate(modelCol);

            model.GetComponent<Renderer>().material = matPlayer;

            player.AddComponent<PlayerController>();
            PlayerVisuals pv = player.AddComponent<PlayerVisuals>();
            pv.ModelRoot = model.transform;
            pv.CreateFacialIndicator();

            return player;
        }

        private static void SetupCamera(GameObject player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0, 3.5f, -6f);
            cam.transform.LookAt(player.transform.position + Vector3.up * 1.2f);
            cam.fieldOfView = 60f;

            ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
            tpc.Target = player.transform;
        }

        private static Text SetupMobileUI(Sprite joyBaseSprite, Sprite joyKnobSprite, Sprite jumpBtnSprite)
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            GameObject canvasObj = new GameObject("UI_MobileCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 1. TouchField (Vùng xoay camera nửa phải màn hình)
            GameObject touchFieldObj = new GameObject("TouchField_LookZone");
            touchFieldObj.transform.SetParent(canvasObj.transform, false);
            RectTransform tfRect = touchFieldObj.AddComponent<RectTransform>();
            tfRect.anchorMin = new Vector2(0.35f, 0f);
            tfRect.anchorMax = new Vector2(1f, 1f);
            tfRect.offsetMin = Vector2.zero;
            tfRect.offsetMax = Vector2.zero;

            Image tfImg = touchFieldObj.AddComponent<Image>();
            tfImg.color = new Color(0, 0, 0, 0);
            touchFieldObj.AddComponent<TouchField>();

            // 2. Virtual Joystick
            GameObject joystickObj = new GameObject("VirtualJoystick");
            joystickObj.transform.SetParent(canvasObj.transform, false);
            RectTransform joyRect = joystickObj.AddComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(200f, 200f);
            joyRect.sizeDelta = new Vector2(220f, 220f);

            Image joyBgImg = joystickObj.AddComponent<Image>();
            if (joyBaseSprite != null) joyBgImg.sprite = joyBaseSprite;
            joyBgImg.color = Color.white;

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(joystickObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(95f, 95f);

            Image handleImg = handleObj.AddComponent<Image>();
            if (joyKnobSprite != null) handleImg.sprite = joyKnobSprite;
            handleImg.color = Color.white;

            VirtualJoystick vj = joystickObj.AddComponent<VirtualJoystick>();
            SerializedObject soVj = new SerializedObject(vj);
            soVj.FindProperty("background").objectReferenceValue = joyRect;
            soVj.FindProperty("handle").objectReferenceValue = handleRect;
            soVj.FindProperty("handleRange").floatValue = 90f;
            soVj.ApplyModifiedProperties();

            // 3. Jump Button
            GameObject jumpBtnObj = new GameObject("JumpButton");
            jumpBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform jumpRect = jumpBtnObj.AddComponent<RectTransform>();
            jumpRect.anchorMin = new Vector2(1f, 0f);
            jumpRect.anchorMax = new Vector2(1f, 0f);
            jumpRect.pivot = new Vector2(0.5f, 0.5f);
            jumpRect.anchoredPosition = new Vector2(-160f, 170f);
            jumpRect.sizeDelta = new Vector2(140f, 140f);

            Image jumpImg = jumpBtnObj.AddComponent<Image>();
            if (jumpBtnSprite != null) jumpImg.sprite = jumpBtnSprite;
            jumpImg.color = Color.white;

            jumpBtnObj.AddComponent<JumpButton>();

            GameObject jumpTextObj = new GameObject("Text");
            jumpTextObj.transform.SetParent(jumpBtnObj.transform, false);
            RectTransform textRect = jumpTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text jumpText = jumpTextObj.AddComponent<Text>();
            jumpText.text = "JUMP";
            jumpText.fontSize = 28;
            jumpText.fontStyle = FontStyle.Bold;
            jumpText.alignment = TextAnchor.MiddleCenter;
            jumpText.color = Color.white;

            // 4. Score / Crystal HUD (Góc trái trên)
            GameObject scoreObj = new GameObject("CrystalsHUD");
            scoreObj.transform.SetParent(canvasObj.transform, false);
            RectTransform scoreRect = scoreObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(0f, 1f);
            scoreRect.pivot = new Vector2(0f, 1f);
            scoreRect.anchoredPosition = new Vector2(40f, -30f);
            scoreRect.sizeDelta = new Vector2(300f, 50f);

            Text scoreText = scoreObj.AddComponent<Text>();
            scoreText.text = "Crystals: 0";
            scoreText.fontSize = 32;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.color = new Color(1f, 0.95f, 0.3f);

            // 5. Hướng dẫn trên đỉnh màn hình
            GameObject hintObj = new GameObject("HintsPanel");
            hintObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0, -25f);
            hintRect.sizeDelta = new Vector2(900f, 40f);

            Text hintText = hintObj.AddComponent<Text>();
            hintText.text = "Gạt Joystick/WASD: Di chuyển | Vuốt phải/Chuột phải: Xoay Camera | Nút Jump/Space: Nhảy & Nhảy đúp";
            hintText.fontSize = 18;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(1f, 1f, 1f, 0.95f);

            return scoreText;
        }
    }
}
