using UnityEngine;

namespace BS.Presentation
{
    /// <summary>Keeps the existing pointer-following inventory tooltip inside an overlay Canvas viewport.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class V04InventoryTooltipClamp : MonoBehaviour
    {
        [SerializeField] private float screenMargin = 12f;
        private RectTransform panel;
        private readonly Vector3[] corners = new Vector3[4];

        private void Awake() => panel = (RectTransform)transform;

        private void LateUpdate()
        {
            if (panel == null) panel = (RectTransform)transform;
            panel.GetWorldCorners(corners);
            // The inventory controller already requires ScreenSpaceOverlay for its pointer coordinates.
            float dx = 0, dy = 0;
            if (corners[2].x > Screen.width - screenMargin) dx = Screen.width - screenMargin - corners[2].x;
            if (corners[0].x + dx < screenMargin) dx = screenMargin - corners[0].x;
            if (corners[2].y > Screen.height - screenMargin) dy = Screen.height - screenMargin - corners[2].y;
            if (corners[0].y + dy < screenMargin) dy = screenMargin - corners[0].y;
            if (dx != 0 || dy != 0) panel.position += new Vector3(dx, dy, 0);
        }
    }
}
