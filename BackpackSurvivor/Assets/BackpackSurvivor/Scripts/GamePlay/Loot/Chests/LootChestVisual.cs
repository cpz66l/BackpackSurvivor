using BS.Data;
using UnityEngine;

namespace BS.GamePlay.Loot
{
    /// <summary>Two shared meshes per pooled chest; no material instances or animated rigidbodies.</summary>
    public sealed class LootChestVisual : MonoBehaviour
    {
        [SerializeField] private ChestVisualLibrary library;
        [SerializeField] private MeshFilter body;
        [SerializeField] private MeshFilter lid;
        [SerializeField] private BoxCollider bodyCollider;
        [SerializeField, Min(.05f)] private float openDuration = .32f;
        private MeshRenderer bodyRenderer, lidRenderer;
        private MaterialPropertyBlock properties;
        private Quaternion closedRotation;
        private float animationTime;
        private ChestVisualLibrary.Style style;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public string StyleId => style == null ? string.Empty : style.id;
        public float OpenProgress => Mathf.Clamp01(animationTime / openDuration);

        public void Configure(ChestVisualLibrary value, MeshFilter bodyPart, MeshFilter lidPart, BoxCollider solid)
        {
            library = value; body = bodyPart; lid = lidPart; bodyCollider = solid;
            SetStyle(null, Color.white);
        }

        private void CacheRenderers()
        {
            if (!bodyRenderer && body) bodyRenderer = body.GetComponent<MeshRenderer>();
            if (!lidRenderer && lid) lidRenderer = lid.GetComponent<MeshRenderer>();
            if (properties == null) properties = new MaterialPropertyBlock();
        }

        public void SetStyle(LootTableData bundle, Color color)
        {
            if (!library || !body || !lid) return;
            style = library.Resolve(bundle, color);
            if (style == null) return;
            CacheRenderers();
            ApplyPart(body, style.body); ApplyPart(lid, style.lid);
            bodyRenderer.sharedMaterial = library.sharedMaterial;
            lidRenderer.sharedMaterial = library.sharedMaterial;
            closedRotation = style.lid.rotation;
            if (bodyCollider)
            {
                bodyCollider.center = style.bodyBounds.center;
                bodyCollider.size = style.bodyBounds.size;
            }
            ResetClosed();
        }

        private static void ApplyPart(MeshFilter target, ChestVisualLibrary.Part part)
        {
            target.sharedMesh = part.mesh;
            target.transform.localPosition = part.position;
            target.transform.localRotation = part.rotation;
            target.transform.localScale = part.scale;
        }

        public void ResetClosed()
        {
            CacheRenderers();
            animationTime = 0;
            if (lid && style != null) lid.transform.localRotation = closedRotation;
            if (bodyCollider) bodyCollider.enabled = true;
            if (bodyRenderer) bodyRenderer.SetPropertyBlock(null);
            if (lidRenderer) lidRenderer.SetPropertyBlock(null);
            enabled = false;
        }

        public void Open()
        {
            CacheRenderers();
            if (style == null && library) SetStyle(null, Color.white);
            if (bodyCollider) bodyCollider.enabled = false;
            animationTime = 0;
            if (library && library.sharedMaterial && library.sharedMaterial.HasProperty(EmissionColor))
            {
                properties.Clear();
                properties.SetColor(EmissionColor, library.sharedMaterial.GetColor(EmissionColor) * .16f);
                if (bodyRenderer) bodyRenderer.SetPropertyBlock(properties);
                if (lidRenderer) lidRenderer.SetPropertyBlock(properties);
            }
            enabled = style != null && lid;
        }

        private void Update()
        {
            animationTime = Mathf.Min(animationTime + Time.deltaTime, openDuration);
            float t = OpenProgress;
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            lid.transform.localRotation = closedRotation * Quaternion.Euler(style.openAngle * eased, 0, 0);
            if (t >= 1) enabled = false;
        }
    }
}
