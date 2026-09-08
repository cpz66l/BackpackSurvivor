using System;
using System.Collections.Generic;
using System.IO;
using BS.GamePlay.Loot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackSurvivor.EditorTools
{
    public static class V04ArtIterationBuilder
    {
        [MenuItem("Tools/Backpack Survivor/Art/V0.4/Apply Complete Art Iteration")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != FullMapArtBuilder.ScenePath)
                throw new InvalidOperationException("Open 01-Run_ArtFull outside Play Mode.");
            FullMapArtBuilder.PreserveUnsavedScene();
            V04ArtTextureImporter.Import();
            var dropVariant = V04LootBeaconBuilder.ApplyToActiveScene();
            V04HudArtBuilder.ApplyToActiveScene();
            V04InventoryArtBuilder.ApplyToActiveScene();
            V04RunMenusArtBuilder.ApplyToActiveScene();
            V04HudArtBuilder.ConfigureModalCoverage();
            var manager = UnityEngine.Object.FindAnyObjectByType<LootManager>();
            var managerData = new SerializedObject(manager);
            var pool = managerData.FindProperty("dropPool").objectReferenceValue;
            var poolData = new SerializedObject(pool); poolData.FindProperty("prefab").objectReferenceValue = dropVariant; poolData.ApplyModifiedPropertiesWithoutUndo();
            PolishSceneMaterials();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save ArtFull.");
            AddDevelopmentScene(); AssetDatabase.SaveAssets();
        }

        public static void ApplyMainMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before menu art application.");
            string returnScene = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(V04MainMenuArtBuilder.ScenePath);
            V04MainMenuArtBuilder.ApplyToActiveScene();
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save MainMenu.");
            AddDevelopmentScene(); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(returnScene);
        }
        static void AddDevelopmentScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); bool found = false;
            foreach (var item in scenes) if (item.path == FullMapArtBuilder.ScenePath) { item.enabled = true; found = true; }
            if (!found) scenes.Add(new EditorBuildSettingsScene(FullMapArtBuilder.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        static void PolishSceneMaterials()
        {
            const string sourceFolder = "Assets/BackpackSurvivor/Art/Environment/VNext/Materials/FullMap/";
            const string targetFolder = "Assets/BackpackSurvivor/Art/Environment/V04/Materials";
            EnsureFolder(targetFolder);
            var replacements = new Dictionary<string, Material>();
            foreach (string name in new[] { "Concrete", "OldConcrete", "Patch", "Asphalt", "WhitePaint", "AmberPaint" })
            {
                var original = AssetDatabase.LoadAssetAtPath<Material>(sourceFolder + name + ".mat"); if (!original) continue;
                string path = targetFolder + "/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) { material = new Material(original); AssetDatabase.CreateAsset(material, path); }
                EditorUtility.CopySerialized(original, material); material.name = name + "_V04"; material.enableInstancing = true;
                if (material.HasProperty("_SecondaryColor"))
                {
                    Color baseColor = material.GetColor("_BaseColor");
                    material.SetColor("_SecondaryColor", Color.Lerp(baseColor, material.GetColor("_SecondaryColor"), .78f));
                    material.SetFloat("_CrackStrength", material.GetFloat("_CrackStrength") * .85f);
                    material.SetFloat("_BumpScale", material.GetFloat("_BumpScale") * .9f);
                }
                else if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.GetColor("_BaseColor") * .9f);
                EditorUtility.SetDirty(material); replacements[sourceFolder + name + ".mat"] = material; replacements[path] = material;
            }
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!renderer.transform.IsChildOf(GameObject.Find("Map").transform)) continue;
                var materials = renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < materials.Length; i++) if (materials[i] && replacements.TryGetValue(AssetDatabase.GetAssetPath(materials[i]), out var replacement)) { materials[i] = replacement; changed = true; }
                if (changed) { renderer.sharedMaterials = materials; EditorUtility.SetDirty(renderer); }
            }
        }
        public static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]); current = next; }
        }
    }
}
