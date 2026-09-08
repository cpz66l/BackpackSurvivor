using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackSurvivor.EditorTools
{
    public static class NightContentBuilder
    {
        [MenuItem("Tools/Backpack Survivor/Art/Apply Chests Night and Upgrade UI to Full Map")]
        public static void ApplyAndSave()
        {
            RequireFullMap();
            FullMapArtBuilder.PreserveUnsavedScene();
            ApplyToActiveScene();
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new InvalidOperationException("Could not save the full art scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("NIGHT_CONTENT_READY=" + FullMapArtBuilder.ScenePath);
        }

        public static void ApplyToActiveScene()
        {
            RequireFullMap();
            RecoveryChestBuilder.ApplyToActiveScene();
            NightAtmosphereBuilder.ApplyToActiveScene();
            NightUpgradeUIBuilder.ApplyToActiveScene();
        }

        static void RequireFullMap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != FullMapArtBuilder.ScenePath)
                throw new InvalidOperationException("Open the full 120m art scene outside Play Mode first.");
        }
    }
}
