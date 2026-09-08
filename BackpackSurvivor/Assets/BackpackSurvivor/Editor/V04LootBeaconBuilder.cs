using System;
using System.Collections.Generic;
using System.IO;
using BS.GamePlay.Loot;
using BS.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BackpackSurvivor.EditorTools
{
    public static class V04LootBeaconBuilder
    {
        public const string ArtFolder = "Assets/BackpackSurvivor/Art/Effects/Loot/V04";
        public const string PrefabPath = "Assets/BackpackSurvivor/Prefabs/Loot/V04/DropItem.prefab";
        public const string OriginalPrefabPath = "Assets/BackpackSurvivor/Prefabs/Loot/DropItem.prefab";
        public const string MeshPath = ArtFolder + "/LootBeacon_4Triangles.asset";
        public const string ShaderPath = ArtFolder + "/LootRarityBeacon.shader";
        public static readonly string[] Names = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        public static readonly Color[] Colors = { Color.white, Color.green, Color.blue, new Color(.6f,.2f,.9f), Color.red };
        public static readonly float[] Heights = { 2f, 2.3f, 2.6f, 2.9f, 3.2f };
        public static readonly float[] Widths = { .36f, .40f, .44f, .48f, .52f };
        public static readonly float[] Intensities = { .24f, .28f, .34f, .40f, .44f };
        const string ChildName = "V04RarityBeacon";

        public static string MaterialPath(int rarity) => ArtFolder + "/LootBeacon_" + Names[rarity] + ".mat";

        [MenuItem("Tools/Backpack Survivor/Art/V04/Build Equipment Beacon Variant")]
        public static void BuildFromMenu() { ApplyToActiveScene(); }

        // Resource-only operation. The caller binds its chosen scene's LootManager dropPool.
        public static GameObject ApplyToActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build equipment beacon assets outside Play Mode.");
            EnsureFolder(ArtFolder); EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\','/'));
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (!shader) throw new InvalidOperationException("Import LootRarityBeacon.shader before building the variant.");
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity.ToString() == "Error") throw new InvalidOperationException("Beacon shader: " + message.message);
            Mesh mesh = BuildMesh();
            var materials = new Material[Names.Length];
            for (int i=0; i<materials.Length; i++)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(i));
                if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath(i)); }
                material.name = "LootBeacon_" + Names[i]; material.shader = shader;
                material.SetColor("_BaseColor", Colors[i]); material.SetFloat("_BeamHeight", Heights[i]);
                material.SetFloat("_BeamWidth", Widths[i]); material.SetFloat("_BaseRadius", .32f + .025f*i);
                material.SetFloat("_Opacity", Intensities[i]); material.SetFloat("_GroundHeight", 0);
                material.SetFloat("_FadeStart", 45); material.SetFloat("_FadeEnd", 65);
                material.enableInstancing = true; material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                EditorUtility.SetDirty(material); materials[i] = material;
            }
            GameObject result = BuildVariant(mesh,materials);
            AssetDatabase.SaveAssets();
            return result;
        }

        static Mesh BuildMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh,MeshPath); }
            mesh.name = "LootBeacon_4Triangles"; mesh.Clear();
            mesh.vertices = new[] { new Vector3(-.5f,0,0), new Vector3(.5f,0,0), new Vector3(-.5f,1,0), new Vector3(.5f,1,0),
                new Vector3(-.5f,0,-.5f), new Vector3(.5f,0,-.5f), new Vector3(-.5f,0,.5f), new Vector3(.5f,0,.5f) };
            mesh.SetUVs(0,new List<Vector3> { new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,1,0),new Vector3(1,1,0),
                new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(0,1,1),new Vector3(1,1,1) });
            mesh.triangles = new[] { 0,2,1,1,2,3,4,5,6,5,7,6 };
            // The shader anchors to the map floor, while legacy drop transforms may sit at y=0.5 or 1.
            // This tiny shared mesh needs conservative bounds for its world-space displaced quads.
            mesh.bounds = new Bounds(new Vector3(0,.5f,0),new Vector3(1.3f,7f,1.3f));
            EditorUtility.SetDirty(mesh); return mesh;
        }

        static GameObject BuildVariant(Mesh mesh, Material[] materials)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(OriginalPrefabPath);
            if (!original || !original.GetComponent<DropItem>()) throw new InvalidOperationException("Original DropItem prefab missing.");
            GameObject root;
            var preview = default(UnityEngine.SceneManagement.Scene);
            if (existing) root = PrefabUtility.LoadPrefabContents(PrefabPath);
            else
            {
                preview = EditorSceneManager.NewPreviewScene();
                root = PrefabUtility.InstantiatePrefab(original,preview) as GameObject;
            }
            try
            {
                if (!root) throw new InvalidOperationException("Could not create the isolated DropItem prefab variant.");
                Transform child = root.transform.Find(ChildName);
                if (!child)
                {
                    child = new GameObject(ChildName).transform;
                    child.SetParent(root.transform,false);
                }
                child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
                child.gameObject.layer = root.layer;
                var filter = child.GetComponent<MeshFilter>(); if (!filter) filter = child.gameObject.AddComponent<MeshFilter>();
                var renderer = child.GetComponent<MeshRenderer>(); if (!renderer) renderer = child.gameObject.AddComponent<MeshRenderer>();
                filter.sharedMesh = mesh; renderer.sharedMaterial = materials[0]; renderer.enabled = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                var beacon = root.GetComponent<LootRarityBeacon>(); if (!beacon) beacon = root.AddComponent<LootRarityBeacon>();
                beacon.Configure(renderer,root.GetComponent<Collider>(),materials);
                var serialized = new SerializedObject(root.GetComponent<DropItem>());
                serialized.FindProperty("rarityBeacon").objectReferenceValue = beacon;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(root);
                else
                {
                    if (root) UnityEngine.Object.DestroyImmediate(root);
                    if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
                }
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
