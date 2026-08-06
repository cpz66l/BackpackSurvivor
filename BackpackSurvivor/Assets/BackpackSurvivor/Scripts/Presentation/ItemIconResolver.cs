using BS.Inventory;
using System.Collections.Generic;
using UnityEngine;
namespace BS.Presentation
{
    public class ItemIconResolver : MonoBehaviour
    {
        [System.Serializable]
        private class IconEntry
        {
            public ItemTag tag;
            public Sprite sprite;
        }

        [SerializeField] private List<IconEntry> icons;

        public Sprite GetIcon(Item item)
        {
            if (icons == null || item == null || icons.Count == 0)
                return null;
            foreach (var entry in icons)
            {
                if (entry.tag == item.Tag)
                    return entry.sprite;
            }

            return null;
        }
    }
}
