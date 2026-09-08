using BS.Data;
using System;
using UnityEngine;

namespace BS.GamePlay.Loot
{
    [CreateAssetMenu(menuName = "Backpack Survivor/Chest Visual Library")]
    public sealed class ChestVisualLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Part
        {
            public Mesh mesh;
            public Vector3 position;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
        }

        [Serializable]
        public sealed class Style
        {
            public string id;
            public LootTableData bundle;
            public Color rarityColor;
            public Part body = new Part();
            public Part lid = new Part();
            public Bounds bodyBounds;
            public float openAngle = -100f;
        }

        public Material sharedMaterial;
        public Style[] styles = Array.Empty<Style>();

        public Style Resolve(LootTableData bundle, Color color)
        {
            Style nearest = null;
            float distance = float.MaxValue;
            foreach (Style style in styles)
            {
                if (style == null) continue;
                if (bundle && style.bundle == bundle) return style;
                // Color fallback keeps custom loot tables usable without localized name matching.
                Vector3 difference = new Vector3(color.r - style.rarityColor.r,
                    color.g - style.rarityColor.g, color.b - style.rarityColor.b);
                float candidate = difference.sqrMagnitude;
                if (candidate < distance) { nearest = style; distance = candidate; }
            }
            return nearest;
        }
    }
}
