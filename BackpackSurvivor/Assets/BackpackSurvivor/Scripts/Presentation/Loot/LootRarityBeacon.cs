using BS.Inventory;
using UnityEngine;

namespace BS.Presentation
{
    /// <summary>Optional pooled equipment presentation; never writes gameplay or physics state.</summary>
    [DisallowMultipleComponent]
    public sealed class LootRarityBeacon : MonoBehaviour
    {
        [SerializeField] private MeshRenderer beaconRenderer;
        [SerializeField] private Collider landingState;
        [SerializeField] private Material[] rarityMaterials = new Material[5];
        private int styleIndex = -1;

        public int StyleIndex => styleIndex;
        public bool IsVisible => beaconRenderer && beaconRenderer.enabled && gameObject.activeInHierarchy;
        public Material SharedMaterial => beaconRenderer ? beaconRenderer.sharedMaterial : null;
        public Mesh SharedMesh
        {
            get
            {
                var filter = beaconRenderer ? beaconRenderer.GetComponent<MeshFilter>() : null;
                return filter ? filter.sharedMesh : null;
            }
        }

        // The builder passes the existing interaction collider as a read-only flight-state source.
        public void Configure(MeshRenderer renderer, Collider existingCollider, Material[] materials)
        {
            beaconRenderer = renderer;
            landingState = existingCollider;
            rarityMaterials = materials;
            ResetVisual();
        }

        public void SetRarity(Rarity rarity)
        {
            int index = (int)rarity;
            if (!beaconRenderer || rarityMaterials == null || index < 0 || index >= rarityMaterials.Length || !rarityMaterials[index])
            { ResetVisual(); return; }
            styleIndex = index;
            beaconRenderer.sharedMaterial = rarityMaterials[index];
            // Initialize is immediately followed by PlayScatterFlight on normal drops.
            // LateUpdate reads the final collider state before the frame is rendered.
            RefreshVisibility();
        }

        public void ResetVisual()
        {
            styleIndex = -1;
            if (!beaconRenderer) return;
            beaconRenderer.enabled = false;
            if (rarityMaterials != null && rarityMaterials.Length > 0 && rarityMaterials[0])
                beaconRenderer.sharedMaterial = rarityMaterials[0];
        }

        public void RefreshVisibility()
        {
            if (!beaconRenderer) return;
            // DropItem already disables its collider during scatter flight and restores it on landing.
            // This observer never changes the collider, trajectory, pickup range or collection state.
            beaconRenderer.enabled = styleIndex >= 0 && isActiveAndEnabled && landingState && landingState.enabled;
        }

        private void OnEnable() { ResetVisual(); }
        private void OnDisable() { ResetVisual(); }
        private void LateUpdate() { RefreshVisibility(); }
    }
}
