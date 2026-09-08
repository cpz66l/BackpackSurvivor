using System;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class V04ArtTextureImporter
    {
        public static void Import()
        {
            ImportSprite(V04InventoryArtBuilder.FramePath, new Vector4(39, 33, 39, 33), true);
            ImportSprite(V04MainMenuArtBuilder.BackgroundPath, Vector4.zero, false);
        }
        static void ImportSprite(string path, Vector4 border, bool alpha)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Generated art missing: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.spriteBorder = border;
            importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = alpha; importer.sRGBTexture = true;
            importer.isReadable = false; importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 1024;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = false; importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Standalone", overridden = true,
                maxTextureSize = 1024, format = TextureImporterFormat.BC7, compressionQuality = 100 });
            importer.SaveAndReimport();
        }
    }
}
