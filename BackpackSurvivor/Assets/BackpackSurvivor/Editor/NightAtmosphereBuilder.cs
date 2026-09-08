using System;
using System.IO;
using BS.GamePlay.Player;
using BS.Presentation.EnvironmentEffects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BackpackSurvivor.EditorTools
{
    public static class NightAtmosphereBuilder
    {
        public const string RootName = "NightAtmosphere_Local_176ParticleCap";
        public const string AssetFolder = "Assets/BackpackSurvivor/Art/Effects/Night";
        public const string MaterialPath = AssetFolder + "/NightAir_Shared.mat";
        public const string TexturePath = AssetFolder + "/NightAir_Radial64.png";

        [MenuItem("Tools/Backpack Survivor/Art/Apply Lightweight Night Atmosphere")]
        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building the night atmosphere.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open the target map scene first.");
            Shader shader = Shader.Find("BackpackSurvivor/Effects/Night Air");
            if (!shader) throw new InvalidOperationException("Night Air shader has not imported yet.");

            EnsureFolder(AssetFolder);
            Texture2D sprite = BuildRadialTexture();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                material = new Material(shader) { name = "NightAir_Shared" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", sprite);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_NearFadeStart", 8f);
            material.SetFloat("_NearFadeEnd", 16f);
            material.SetFloat("_GroundHeight", 0f);
            EditorUtility.SetDirty(material);

            // Rebuild only this helper's owned root in the active scene.
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == RootName && root.GetComponent<NightAtmosphere>()) UnityEngine.Object.DestroyImmediate(root);
            var atmosphereRoot = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(atmosphereRoot, scene);
            var controller = atmosphereRoot.AddComponent<NightAtmosphere>();
            var dust = CreateSystem("WindDust_120Max", atmosphereRoot.transform);
            var drizzle = CreateSystem("FineDrizzle_56Max", atmosphereRoot.transform);

            Transform player = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var candidate = root.GetComponentInChildren<PlayerController>(true);
                if (candidate) { player = candidate.transform; break; }
            }
            controller.Configure(player, dust, drizzle, material);
            ApplyNightLighting(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("NIGHT_ATMOSPHERE_READY; 2 particle systems, 176 combined hard limit, 16 particles/s; no new realtime lights.");
        }

        private static ParticleSystem CreateSystem(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<ParticleSystem>();
        }

        private static void ApplyNightLighting(Scene scene)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.46f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.31f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.18f, 0.24f);
            RenderSettings.reflectionIntensity = 0.38f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.13f, 0.18f, 0.25f);
            // Normal gameplay is mostly inside the clear range; only distant aprons recede.
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 108f;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional && light.name == "Directional Light")
                    {
                        light.color = new Color(0.76f, 0.86f, 1f);
                        light.intensity = 0.82f;
                        light.shadowStrength = 0.68f;
                        EditorUtility.SetDirty(light);
                    }
                    else if (light.type == LightType.Spot &&
                             (light.name == "WarmServiceSpot_NoShadows" || light.name == "CoolServiceSpot_NoShadows"))
                    {
                        light.intensity = light.name.StartsWith("Warm", StringComparison.Ordinal) ? 5.6f : 5.1f;
                        light.shadows = LightShadows.None;
                        EditorUtility.SetDirty(light);
                    }
                }
            }
        }

        private static Texture2D BuildRadialTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float px = (x + 0.5f) / size * 2f - 1f;
                        float py = (y + 0.5f) / size * 2f - 1f;
                        float radial = Mathf.Clamp01(1f - Mathf.Sqrt(px * px + py * py));
                        byte alpha = (byte)Mathf.RoundToInt(radial * radial * (3f - 2f * radial) * 255f);
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }
                texture.SetPixels32(pixels); texture.Apply(false, false);
                File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = size;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
