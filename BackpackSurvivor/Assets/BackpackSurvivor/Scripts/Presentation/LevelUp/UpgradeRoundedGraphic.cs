using UnityEngine;
using UnityEngine.UI;

namespace BS.Presentation
{
    /// <summary>A small rounded panel drawn with UGUI geometry and the shared default material.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeRoundedGraphic : MaskableGraphic
    {
        [SerializeField] private float radius = 9f;
        [SerializeField] private float borderWidth = 1.5f;
        [SerializeField] private Color borderColor = new Color(.35f, .47f, .57f, 1f);
        private const int SegmentsPerCorner = 5;
        private const int PointCount = 4 * (SegmentsPerCorner + 1);

        public void Configure(float cornerRadius, float strokeWidth, Color strokeColor)
        {
            radius = cornerRadius; borderWidth = strokeWidth; borderColor = strokeColor;
            SetVerticesDirty();
        }

        public void SetColors(Color fill, Color stroke)
        {
            color = fill;
            if (borderColor == stroke) return;
            borderColor = stroke;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float outerRadius = Mathf.Clamp(radius, 0, Mathf.Min(rect.width, rect.height) * .5f);
            helper.AddVert(rect.center, color, Vector2.zero);
            for (int i = 0; i < PointCount; i++) helper.AddVert(PerimeterPoint(rect, outerRadius, i), color, Vector2.zero);
            for (int i = 0; i < PointCount; i++) helper.AddTriangle(0, i + 1, (i + 1) % PointCount + 1);

            float stroke = Mathf.Clamp(borderWidth, 0, Mathf.Min(rect.width, rect.height) * .5f);
            if (stroke <= 0 || borderColor.a <= 0) return;
            Rect inner = new Rect(rect.x + stroke, rect.y + stroke, rect.width - stroke * 2, rect.height - stroke * 2);
            float innerRadius = Mathf.Max(0, outerRadius - stroke);
            int first = helper.currentVertCount;
            for (int i = 0; i < PointCount; i++)
            {
                helper.AddVert(PerimeterPoint(rect, outerRadius, i), borderColor, Vector2.zero);
                helper.AddVert(PerimeterPoint(inner, innerRadius, i), borderColor, Vector2.zero);
            }
            for (int i = 0; i < PointCount; i++)
            {
                int outer = first + i * 2;
                int next = first + ((i + 1) % PointCount) * 2;
                helper.AddTriangle(outer, next, next + 1);
                helper.AddTriangle(outer, next + 1, outer + 1);
            }
        }

        private static Vector2 PerimeterPoint(Rect rect, float r, int index)
        {
            int corner = index / (SegmentsPerCorner + 1);
            float angle = (90f - corner * 90f - (index % (SegmentsPerCorner + 1)) * 90f / SegmentsPerCorner) * Mathf.Deg2Rad;
            Vector2 center;
            switch (corner)
            {
                case 0: center = new Vector2(rect.xMax - r, rect.yMax - r); break;
                case 1: center = new Vector2(rect.xMax - r, rect.yMin + r); break;
                case 2: center = new Vector2(rect.xMin + r, rect.yMin + r); break;
                default: center = new Vector2(rect.xMin + r, rect.yMax - r); break;
            }
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
    }
}
