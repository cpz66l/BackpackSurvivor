using System;
using System.IO;
using BS.Core;
using BS.Data;
using BS.GamePlay.Loot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackSurvivor.EditorTools
{
    public static class RecoveryChestBuilder
    {
        public const string Art = "Assets/BackpackSurvivor/Art/Loot/VNext";
        public const string LibraryPath = Art + "/RecoveryChestLibrary.asset";
        public const string PrefabPath = "Assets/BackpackSurvivor/Prefabs/Loot/VNext/RecoveryChest.prefab";
        public static readonly string[] Tiers = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        public static readonly Color[] Colors = { Color.white, new Color(0, 1, .4698286f),
            new Color(.32229975f, .5812063f, .89433956f), new Color(.15300101f, .25705582f, .93207544f),
            new Color(1, .45464972f, 0) };

        public static void ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var library = BuildLibrary();
            var prefab = BuildPrefab(library);
            int pools = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var spawner in root.GetComponentsInChildren<ChestSpawner>(true))
            {
                var spawnerData = new SerializedObject(spawner);
                var pool = spawnerData.FindProperty("chestPool").objectReferenceValue as ObjectPool;
                if (!pool) throw new InvalidOperationException("Chest spawner has no pool.");
                var data = new SerializedObject(pool);
                data.FindProperty("prefab").objectReferenceValue = prefab;
                data.ApplyModifiedPropertiesWithoutUndo();
                pools++;
            }
            if (pools == 0) throw new InvalidOperationException("No scene chest pool found.");
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        static ChestVisualLibrary BuildLibrary()
        {
            Folder(Art + "/Materials");
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP Lit shader missing.");
            string path = Art + "/Materials/RecoveryChest_Shared.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetTexture("_BaseMap", Palette("BaseColor", true));
            material.SetTexture("_EmissionMap", Palette("Emission", true));
            material.SetTexture("_MetallicGlossMap", Palette("MetallicSmoothness", false));
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", Color.white * 1.5f);
            material.SetFloat("_Metallic", 1f); material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_EMISSION"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.enableInstancing = true;
            material.globalIlluminationFlags = UnityEngine.MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            var library = AssetDatabase.LoadAssetAtPath<ChestVisualLibrary>(LibraryPath);
            if (!library) { library = ScriptableObject.CreateInstance<ChestVisualLibrary>(); AssetDatabase.CreateAsset(library, LibraryPath); }
            library.sharedMaterial = material;
            library.styles = new ChestVisualLibrary.Style[Tiers.Length];
            for (int i = 0; i < Tiers.Length; i++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art + "/Models/" + Tiers[i] + "Chest.glb");
                if (!model) throw new InvalidOperationException("Missing chest GLB: " + Tiers[i]);
                var body = model.transform.Find("Body").GetComponent<MeshFilter>();
                var lid = model.transform.Find("Lid").GetComponent<MeshFilter>();
                if (!body || !lid || model.transform.localScale != Vector3.one || model.transform.localRotation != Quaternion.identity)
                    throw new InvalidOperationException("Chest import must have a unit root with Body/Lid meshes.");
                var style = new ChestVisualLibrary.Style { id = Tiers[i], rarityColor = Colors[i],
                    bundle = AssetDatabase.LoadAssetAtPath<LootTableData>("Assets/BackpackSurvivor/Data/ChestDropLoot/" + Tiers[i] + "ChestLoot.asset"),
                    body = Part(body), lid = Part(lid), bodyBounds = body.sharedMesh.bounds, openAngle = -100f };
                if (!style.bundle) throw new InvalidOperationException("Missing chest loot table: " + Tiers[i]);
                library.styles[i] = style;
            }
            EditorUtility.SetDirty(library);
            return library;
        }

        static ChestVisualLibrary.Part Part(MeshFilter mesh) => new ChestVisualLibrary.Part {
            mesh = mesh.sharedMesh, position = mesh.transform.localPosition,
            rotation = mesh.transform.localRotation, scale = mesh.transform.localScale };

        static Texture2D Palette(string name, bool srgb)
        {
            string path = Art + "/Textures/CHEST_Palette_" + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing chest palette: " + path);
            importer.sRGBTexture = srgb; importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Point;
            importer.isReadable = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static GameObject BuildPrefab(ChestVisualLibrary library)
        {
            Folder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
            var root = new GameObject("RecoveryChest"); root.layer = LayerMask.NameToLayer("Interactable");
            if (root.layer < 0) root.layer = 3;
            try
            {
                var trigger = root.AddComponent<BoxCollider>(); trigger.isTrigger = true;
                trigger.center = new Vector3(0, .05f, 0); trigger.size = new Vector3(1.7f, 1.25f, 1.35f);
                var chest = root.AddComponent<LootChest>();
                var original = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BackpackSurvivor/Prefabs/Chest.prefab");
                if (original) EditorUtility.CopySerialized(original.GetComponent<LootChest>(), chest);
                var presentation = new GameObject("RarityBodyAndHingedLid");
                presentation.layer = root.layer; presentation.transform.SetParent(root.transform, false);
                // Spawner uses y = 0.5; exported meshes use the bottom face as their origin.
                presentation.transform.localPosition = new Vector3(0, -.5f, 0);
                var body = MeshPart("Body", presentation.transform, root.layer);
                var lid = MeshPart("Lid", presentation.transform, root.layer);
                var solid = presentation.AddComponent<BoxCollider>();
                var visual = presentation.AddComponent<LootChestVisual>();
                visual.Configure(library, body, lid, solid);
                var drop = new GameObject("DropPoint").transform; drop.SetParent(root.transform, false);
                drop.localPosition = new Vector3(0, .68f, .08f);
                var data = new SerializedObject(chest);
                data.FindProperty("modelRb").objectReferenceValue = body.GetComponent<Renderer>();
                data.FindProperty("dropPoint").objectReferenceValue = drop;
                data.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static MeshFilter MeshPart(string name, Transform parent, int layer)
        {
            var go = new GameObject(name); go.layer = layer; go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>(); go.AddComponent<MeshRenderer>(); return filter;
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
    }
}
