using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Casino.Editor
{
    public static class BlackjackChipSampleInstaller
    {
        private const string ModelPath = "Assets/Casino/Art/Models/Blackjack/ChouMa.fbx";
        private const string ChipPrefabPath = "Assets/Casino/Prefabs/Blackjack/Chip.prefab";
        private const string ScenePath = "Assets/Casino/Scenes/BlackjackGreybox.unity";
        private const string MaterialsFolder = "Assets/Casino/Art/Materials";
        private const string LocalChipAnchorPath = "Blackjack_Greybox_Root/Seats/Seat_0_Local/ChipStackAnchor";

        public static void Install()
        {
            EnsureFolder("Assets/Casino/Prefabs", "Blackjack");
            EnsureFolder("Assets/Casino/Art", "Materials");

            var chipPrefab = BuildChipPrefab();
            PlaceSampleStack(chipPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static GameObject BuildChipPrefab()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                throw new System.InvalidOperationException("Chip model is missing: " + ModelPath);
            }

            var red = CreateOrUpdateMaterial("M_Chip_Red", new Color32(178, 39, 46, 255));
            var cream = CreateOrUpdateMaterial("M_Chip_Cream", new Color32(237, 226, 197, 255));
            var dark = CreateOrUpdateMaterial("M_Chip_Dark", new Color32(38, 42, 45, 255));

            var root = new GameObject("Chip");
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            modelInstance.name = "ChouMa_Model";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            ApplyMaterials(modelInstance, red, cream, dark);
            OrientFlatOnTable(modelInstance);
            FitToTableChipSize(modelInstance, targetDiameter: 0.32f);
            MoveModelBottomCenterToRoot(modelInstance);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ChipPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void PlaceSampleStack(GameObject chipPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var anchor = GameObject.Find(LocalChipAnchorPath);
            if (anchor == null)
            {
                throw new System.InvalidOperationException("Chip anchor is missing: " + LocalChipAnchorPath);
            }

            RemoveLooseChipSamples();

            var existingStack = anchor.transform.Find("Chip_Sample_Stack");
            if (existingStack != null)
            {
                Object.DestroyImmediate(existingStack.gameObject);
            }

            var stack = new GameObject("Chip_Sample_Stack");
            stack.transform.SetParent(anchor.transform, false);
            stack.transform.localPosition = Vector3.zero;
            stack.transform.localRotation = Quaternion.identity;

            for (var index = 0; index < 3; index++)
            {
                var chip = (GameObject)PrefabUtility.InstantiatePrefab(chipPrefab, scene);
                chip.name = "Chip_" + index;
                chip.transform.SetParent(stack.transform, false);
                chip.transform.localPosition = new Vector3(index * 0.018f, index * 0.035f, -index * 0.012f);
                chip.transform.localRotation = Quaternion.Euler(0f, index * 11f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void RemoveLooseChipSamples()
        {
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            var looseSamples = new System.Collections.Generic.List<GameObject>();
            for (var index = 0; index < allObjects.Length; index++)
            {
                var candidate = allObjects[index];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.name == "ChouMa" && candidate.transform.parent == null)
                {
                    looseSamples.Add(candidate);
                }
            }

            for (var index = 0; index < looseSamples.Count; index++)
            {
                if (looseSamples[index] != null)
                {
                    Object.DestroyImmediate(looseSamples[index]);
                }
            }
        }

        private static void ApplyMaterials(GameObject target, Material red, Material cream, Material dark)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(includeInactive: true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                {
                    renderer.sharedMaterial = red;
                    continue;
                }

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials.Length == 1)
                    {
                        materials[materialIndex] = red;
                    }
                    else if (materialIndex == 0)
                    {
                        materials[materialIndex] = red;
                    }
                    else if (materialIndex == 1)
                    {
                        materials[materialIndex] = cream;
                    }
                    else
                    {
                        materials[materialIndex] = dark;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void OrientFlatOnTable(GameObject modelInstance)
        {
            var bounds = CalculateBounds(modelInstance);
            var smallestAxis = GetSmallestAxis(bounds.size);
            if (smallestAxis == Axis.Y)
            {
                return;
            }

            if (smallestAxis == Axis.Z)
            {
                modelInstance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                modelInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
        }

        private static void FitToTableChipSize(GameObject modelInstance, float targetDiameter)
        {
            var bounds = CalculateBounds(modelInstance);
            var planarDiameter = Mathf.Max(bounds.size.x, bounds.size.z);
            if (planarDiameter <= 0.0001f)
            {
                return;
            }

            var scale = targetDiameter / planarDiameter;
            modelInstance.transform.localScale *= scale;
        }

        private static void MoveModelBottomCenterToRoot(GameObject modelInstance)
        {
            var bounds = CalculateBounds(modelInstance);
            var rootPosition = modelInstance.transform.parent.position;
            var offset = rootPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            modelInstance.transform.position += offset;
        }

        private static Bounds CalculateBounds(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static Axis GetSmallestAxis(Vector3 size)
        {
            if (size.y <= size.x && size.y <= size.z)
            {
                return Axis.Y;
            }

            return size.x <= size.z ? Axis.X : Axis.Z;
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

        private enum Axis
        {
            X,
            Y,
            Z
        }
    }
}
