using BS.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace BS.Presentation
{
    /// <summary>Displays ItemView's existing rarity and placement state without changing inventory behaviour.</summary>
    public sealed class V04InventoryItemArt : MonoBehaviour
    {
        [SerializeField] private ItemView itemView;
        [SerializeField] private Image stateImage;
        [SerializeField] private UpgradeRoundedGraphic surface;

        private Color previousState;
        private Rarity previousRarity;
        private bool hasPainted;
        private static readonly Color ValidState = new Color(.1f, .55f, .15f, .8f);
        private static readonly Color InvalidState = new Color(.6f, .12f, .12f, .8f);
        private static readonly Color BaseFill = new Color(.075f, .125f, .17f, .98f);

        public void Configure(ItemView view, Image source, UpgradeRoundedGraphic target)
        {
            itemView = view;
            stateImage = source;
            surface = target;
            hasPainted = false;
        }

        private void OnEnable() => hasPainted = false;

        private void LateUpdate()
        {
            if (itemView == null || itemView.Item == null || stateImage == null || surface == null) return;
            Color state = stateImage.color;
            Rarity rarity = itemView.Item.Rarity;
            if (hasPainted && state == previousState && rarity == previousRarity) return;
            previousState = state;
            previousRarity = rarity;
            hasPainted = true;

            Color border = RarityColor(rarity);
            Color fill = Color.Lerp(BaseFill, border, .085f);
            if (state == ValidState)
            {
                border = new Color(.48f, .86f, .66f, 1f);
                fill = new Color(.105f, .235f, .205f, .98f);
            }
            else if (state == InvalidState)
            {
                border = new Color(.95f, .43f, .43f, 1f);
                fill = new Color(.285f, .115f, .145f, .98f);
            }
            fill.a = .98f;
            surface.SetColors(fill, border);
        }

        private static Color RarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return new Color(.42f, .72f, .55f, .95f);
                case Rarity.Rare: return new Color(.36f, .61f, .9f, .95f);
                case Rarity.Epic: return new Color(.72f, .49f, .85f, .95f);
                case Rarity.Legendary: return new Color(.9f, .39f, .42f, .95f);
                default: return new Color(.63f, .71f, .77f, .9f);
            }
        }
    }
}
