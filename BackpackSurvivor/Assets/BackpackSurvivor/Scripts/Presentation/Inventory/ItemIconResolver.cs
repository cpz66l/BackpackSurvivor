using BS.Inventory;
using System.Collections.Generic;
using UnityEngine;
namespace BS.Presentation
{
    public class ItemIconResolver : MonoBehaviour
    {
        [System.Serializable]
        private class IdIconEntry
        {
            public string itemId;
            public Sprite sprite;
        }

        [System.Serializable]
        private class IconEntry
        {
            public ItemTag tag;
            public Sprite sprite;
        }

        [SerializeField] private List<IdIconEntry> idIcons;
        [SerializeField] private List<IconEntry> icons;

        public Sprite GetIcon(Item item)
        {
            if (item == null)
                return null;

            Sprite idIcon = GetIconById(item.Id);
            if (idIcon != null)
                return idIcon;

            return GetFallbackIconByTag(item.Tag);
        }

        private Sprite GetIconById(string itemId)
        {
            if (idIcons == null || idIcons.Count == 0) return null;
            if (string.IsNullOrEmpty(itemId)) return null;

            foreach (var entry in idIcons)
            {
                if (entry == null) continue;
                if (entry.itemId != itemId) continue;

                return entry.sprite;
            }

            return null;
        }

        private Sprite GetFallbackIconByTag(ItemTag tag)
        {
            if (icons == null || icons.Count == 0)
                return null;

            foreach (var entry in icons)
            {
                if (entry == null) continue;
                if (entry.tag != tag) continue;

                return entry.sprite;
            }

            return null;
        }
    }
}
