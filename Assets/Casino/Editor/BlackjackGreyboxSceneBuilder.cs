using Casino.Presentation.Blackjack;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Casino.Editor
{
    public static class BlackjackGreyboxSceneBuilder
    {
        public const string ScenePath = "Assets/Casino/Scenes/BlackjackGreybox.unity";

        private const string MaterialsFolder = "Assets/Casino/Art/Materials";

        public static void Build()
        {
            EnsureProjectFolders();

            var felt = CreateOrUpdateMaterial("M_Greybox_Blackjack_Felt", new Color32(22, 104, 75, 255));
            var rail = CreateOrUpdateMaterial("M_Greybox_Dark_Rail", new Color32(54, 42, 45, 255));
            var floor = CreateOrUpdateMaterial("M_Greybox_Room_Floor", new Color32(54, 57, 61, 255));
            var wall = CreateOrUpdateMaterial("M_Greybox_Back_Wall", new Color32(70, 55, 61, 255));
            var cardZone = CreateOrUpdateMaterial("M_Greybox_Card_Zone", new Color32(231, 221, 182, 255));
            var betSpot = CreateOrUpdateMaterial("M_Greybox_Bet_Spot", new Color32(191, 74, 59, 255));
            var seat = CreateOrUpdateMaterial("M_Greybox_Player_Seat", new Color32(54, 86, 112, 255));
            var marker = CreateOrUpdateMaterial("M_Greybox_Anchor_Marker", new Color32(240, 191, 83, 255));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var root = CreateEmpty("Blackjack_Greybox_Root", null, Vector3.zero);
            BuildRoom(root.transform, floor, wall);
            BuildTable(root.transform, felt, rail);
            BuildDealer(root.transform, cardZone, betSpot, marker);
            BuildSeats(root.transform, seat, cardZone, betSpot, marker);
            BuildCameraAndLights(root.transform);
            BuildUi(root.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets", "Casino");
            EnsureFolder("Assets/Casino", "Scenes");
            EnsureFolder("Assets/Casino", "Prefabs");
            EnsureFolder("Assets/Casino/Prefabs", "Blackjack");
            EnsureFolder("Assets/Casino", "Art");
            EnsureFolder("Assets/Casino/Art", "Materials");
            EnsureFolder("Assets/Casino/Art", "Models");
            EnsureFolder("Assets/Casino/Art/Models", "Blackjack");
        }

        private static void EnsureFolder(string parent, string folder)
        {
            var path = parent + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static Material CreateOrUpdateMaterial(string name, Color color)
        {
            var path = MaterialsFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                material = new Material(shader)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildRoom(Transform root, Material floor, Material wall)
        {
            var room = CreateEmpty("Room", root, Vector3.zero);
            CreateCube("Floor_Placeholder", room.transform, new Vector3(0f, -0.05f, 0f), new Vector3(10f, 0.1f, 8f), floor);
            CreateCube("BackWall_Placeholder", room.transform, new Vector3(0f, 1.5f, 3.45f), new Vector3(10f, 3f, 0.18f), wall);
            CreateCube("LeftWall_Placeholder", room.transform, new Vector3(-5f, 1.5f, 0f), new Vector3(0.18f, 3f, 8f), wall);
            CreateCube("RightWall_Placeholder", room.transform, new Vector3(5f, 1.5f, 0f), new Vector3(0.18f, 3f, 8f), wall);
        }

        private static void BuildTable(Transform root, Material felt, Material rail)
        {
            var table = CreateEmpty("Table", root, Vector3.zero);
            CreateCube("Blackjack_Table_Base_Placeholder", table.transform, new Vector3(0f, 0.45f, 0f), new Vector3(7.8f, 0.55f, 3.8f), rail);
            CreateCube("Blackjack_Felt_Surface", table.transform, new Vector3(0f, 0.77f, 0f), new Vector3(7.1f, 0.08f, 3.15f), felt);
            CreateCube("Dealer_Edge_Rail_Placeholder", table.transform, new Vector3(0f, 0.94f, 1.75f), new Vector3(7.8f, 0.22f, 0.24f), rail);
            CreateCube("Player_Edge_Rail_Placeholder", table.transform, new Vector3(0f, 0.94f, -1.75f), new Vector3(7.8f, 0.22f, 0.24f), rail);
            CreateCube("Left_Edge_Rail_Placeholder", table.transform, new Vector3(-3.9f, 0.94f, 0f), new Vector3(0.24f, 0.22f, 3.4f), rail);
            CreateCube("Right_Edge_Rail_Placeholder", table.transform, new Vector3(3.9f, 0.94f, 0f), new Vector3(0.24f, 0.22f, 3.4f), rail);
        }

        private static void BuildDealer(Transform root, Material cardZone, Material betSpot, Material marker)
        {
            var dealer = CreateEmpty("Dealer", root, Vector3.zero);
            var anchor = CreateEmpty("Dealer_Anchor", dealer.transform, new Vector3(0f, 0.82f, 2.15f));
            anchor.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            CreateCube("Dealer_CardZone", dealer.transform, new Vector3(-0.45f, 0.86f, 0.96f), new Vector3(1.45f, 0.035f, 0.55f), cardZone);
            CreateCube("Dealer_HoleCardZone", dealer.transform, new Vector3(1.05f, 0.865f, 0.96f), new Vector3(0.72f, 0.035f, 0.55f), cardZone);
            CreateCube("Dealer_ChipTray", dealer.transform, new Vector3(-2.65f, 0.89f, 1.08f), new Vector3(1.15f, 0.16f, 0.55f), betSpot);
            CreateCube("Discard_Tray", dealer.transform, new Vector3(2.65f, 0.89f, 1.08f), new Vector3(1.15f, 0.16f, 0.55f), marker);
        }

        private static void BuildSeats(Transform root, Material seat, Material cardZone, Material betSpot, Material marker)
        {
            var seats = CreateEmpty("Seats", root, Vector3.zero);
            CreateSeat(seats.transform, "Seat_0_Local", new Vector3(0f, 0f, -2.55f), seat, cardZone, betSpot, marker);
            CreateSeat(seats.transform, "Seat_1_LeftNear", new Vector3(-1.55f, 0f, -2.25f), seat, cardZone, betSpot, marker);
            CreateSeat(seats.transform, "Seat_2_LeftFar", new Vector3(-2.95f, 0f, -1.25f), seat, cardZone, betSpot, marker);
            CreateSeat(seats.transform, "Seat_3_FarLeft", new Vector3(-3.15f, 0f, 0.55f), seat, cardZone, betSpot, marker);
            CreateSeat(seats.transform, "Seat_4_FarRight", new Vector3(3.15f, 0f, 0.55f), seat, cardZone, betSpot, marker);
            CreateSeat(seats.transform, "Seat_5_RightNear", new Vector3(1.55f, 0f, -2.25f), seat, cardZone, betSpot, marker);
        }

        private static void CreateSeat(
            Transform seats,
            string name,
            Vector3 seatPosition,
            Material seatMaterial,
            Material cardZoneMaterial,
            Material betSpotMaterial,
            Material markerMaterial)
        {
            var seatRoot = CreateEmpty(name, seats, seatPosition);
            var facing = Vector3.zero - seatPosition;
            facing.y = 0f;
            seatRoot.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

            CreateCylinder("Seat_Placeholder", seatRoot.transform, Vector3.zero, new Vector3(0.72f, 0.18f, 0.72f), seatMaterial);
            CreateEmpty("AvatarAnchor", seatRoot.transform, new Vector3(0f, 0.72f, -0.18f));
            CreateEmpty("CameraAnchor", seatRoot.transform, new Vector3(0f, 1.35f, -0.56f));

            var cardZonePosition = Vector3.Lerp(seatPosition, Vector3.zero, 0.5f);
            cardZonePosition.y = 0.87f;
            var betSpotPosition = Vector3.Lerp(seatPosition, Vector3.zero, 0.68f);
            betSpotPosition.y = 0.89f;

            var cardZone = CreateCube("CardZone", seatRoot.transform, Vector3.zero, new Vector3(1.16f, 0.035f, 0.54f), cardZoneMaterial);
            cardZone.transform.position = cardZonePosition;
            cardZone.transform.rotation = seatRoot.transform.rotation;

            var betSpot = CreateCylinder("BetSpot", seatRoot.transform, Vector3.zero, new Vector3(0.52f, 0.03f, 0.52f), betSpotMaterial);
            betSpot.transform.position = betSpotPosition;

            var chipAnchor = CreateEmpty("ChipStackAnchor", seatRoot.transform, Vector3.zero);
            chipAnchor.transform.position = betSpotPosition + new Vector3(0f, 0.08f, 0f);

            var promptAnchor = CreateEmpty("ActionPromptAnchor", seatRoot.transform, Vector3.zero);
            promptAnchor.transform.position = cardZonePosition + new Vector3(0f, 0.32f, 0f);

            var seatMarker = CreateCube("SeatNumberMarker", seatRoot.transform, new Vector3(0f, 0.22f, 0f), new Vector3(0.22f, 0.04f, 0.22f), markerMaterial);
            seatMarker.transform.rotation = seatRoot.transform.rotation;
        }

        private static void BuildCameraAndLights(Transform root)
        {
            var cameraRig = CreateEmpty("Camera", root, Vector3.zero);
            var cameraAnchor = CreateEmpty("LocalSeat_CameraAnchor", cameraRig.transform, new Vector3(0f, 2.2f, -4.75f));
            cameraAnchor.transform.LookAt(new Vector3(0f, 0.82f, 0.1f), Vector3.up);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(cameraRig.transform, false);
            cameraObject.transform.position = cameraAnchor.transform.position;
            cameraObject.transform.rotation = cameraAnchor.transform.rotation;
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;

            var lighting = CreateEmpty("Lighting", root, Vector3.zero);
            var keyLight = new GameObject("Key_Light");
            keyLight.transform.SetParent(lighting.transform, false);
            keyLight.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            var directional = keyLight.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.15f;

            var fillLight = new GameObject("Table_Fill_Light");
            fillLight.transform.SetParent(lighting.transform, false);
            fillLight.transform.position = new Vector3(0f, 3.1f, -1f);
            var point = fillLight.AddComponent<Light>();
            point.type = LightType.Point;
            point.range = 7f;
            point.intensity = 2.2f;
            point.color = new Color32(255, 221, 168, 255);
        }

        private static void BuildUi(Transform root)
        {
            var ui = CreateEmpty("UI", root, Vector3.zero);
            var screen = CreateEmpty("BlackjackGreyboxScreen", ui.transform, Vector3.zero);
            screen.AddComponent<BlackjackGreyboxScreen>();
        }

        private static GameObject CreateEmpty(string name, Transform parent, Vector3 localPosition)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            return gameObject;
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localScale = localScale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            return cylinder;
        }
    }
}
