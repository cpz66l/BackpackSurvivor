using System;
using System.Collections.Generic;
using System.IO;
using BS.GamePlay.Upgrades;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Imports generated illustrations as stable named sprites from one small shared atlas.</summary>
    public static class UpgradeIllustrationImporter
    {
        public const string AtlasPath = "Assets/BackpackSurvivor/Art/UI/Upgrades/UpgradeIllustrations.png";
        [Serializable] class Manifest { public int width, height; public Entry[] sprites; }
        [Serializable] class Entry { public string name; public int[] rect; }

        [MenuItem("Tools/Backpack Survivor/Art/Apply Approved Illustrated Upgrade UI")]
        public static void ApplyApprovedUiAndSave()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != FullMapArtBuilder.ScenePath)
                throw new InvalidOperationException("Open the full art scene outside Play Mode first.");
            FullMapArtBuilder.PreserveUnsavedScene();
            NightUpgradeUIBuilder.ApplyToActiveScene();
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the illustrated upgrade UI.");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Backpack Survivor/Art/Import Upgrade Illustrations")]
        public static void Import()
        {
            string source = Path.GetFullPath(Application.dataPath + "/../../Tools/ArtPipeline/Source/upgrade-illustrations-manifest.json");
            if (!File.Exists(source) || !File.Exists(AtlasPath))
                throw new InvalidOperationException("Run Tools/ArtPipeline/prepare_upgrade_illustrations.py before importing upgrade art.");
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(source));
            if (manifest == null || manifest.sprites == null || manifest.width != 1024 || manifest.height != 1024)
                throw new InvalidOperationException("Upgrade illustrations must use a 1024 x 1024 atlas.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (Entry entry in manifest.sprites)
            {
                if (!names.Add(entry.name) || entry.rect == null || entry.rect.Length != 4 || entry.rect[0] < 0 || entry.rect[1] < 0 ||
                    entry.rect[2] <= 0 || entry.rect[3] <= 0 || entry.rect[0] + entry.rect[2] > manifest.width || entry.rect[1] + entry.rect[3] > manifest.height)
                    throw new InvalidOperationException("Invalid or repeated illustration rectangle: " + entry.name);
            }
            foreach (LevelUpOptionId id in Enum.GetValues(typeof(LevelUpOptionId)))
                if (!names.Contains(id.ToString())) throw new InvalidOperationException("Missing illustration: " + id);
            if (names.Count != Enum.GetValues(typeof(LevelUpOptionId)).Length)
                throw new InvalidOperationException("The atlas must contain exactly one sprite per current upgrade option.");

            AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 1024;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Standalone", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.BC7, textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100, crunchedCompression = false });

            var factories = new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>(); var identities = new List<SpriteNameFileIdPair>();
            foreach (Entry entry in manifest.sprites)
            {
                // Stable IDs keep serialized references valid after the art is regenerated/reimported.
                var id = new GUID(Hash128.Compute("BackpackSurvivor/Upgrade/" + entry.name).ToString());
                rects.Add(new SpriteRect { name = entry.name, spriteID = id, alignment = SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f), rect = new Rect(entry.rect[0], entry.rect[1], entry.rect[2], entry.rect[3]) });
                identities.Add(new SpriteNameFileIdPair(entry.name, id));
            }
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(identities);
            provider.Apply(); importer.SaveAndReimport();
            int count = 0;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AtlasPath)) if (asset is Sprite) count++;
            if (count != names.Count) throw new InvalidOperationException("Unity imported " + count + " sprites; expected " + names.Count);
        }
    }
}
