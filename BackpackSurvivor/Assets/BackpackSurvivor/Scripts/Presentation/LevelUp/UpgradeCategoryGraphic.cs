using BS.GamePlay.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace BS.Presentation
{
    /// <summary>Five category symbols drawn as geometry, sharing UGUI's default material.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeCategoryGraphic : MaskableGraphic
    {
        [SerializeField] private LevelUpOptionCategory category;
        public LevelUpOptionCategory Category
        {
            get { return category; }
            set { if (category == value) return; category = value; SetVerticesDirty(); }
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            switch (category)
            {
                case LevelUpOptionCategory.Attack:
                    Path(helper, new Vector2(.26f, .80f), new Vector2(.08f, .80f), new Vector2(.08f, .62f));
                    Path(helper, new Vector2(.74f, .80f), new Vector2(.92f, .80f), new Vector2(.92f, .62f));
                    Path(helper, new Vector2(.08f, .38f), new Vector2(.08f, .20f), new Vector2(.26f, .20f));
                    Path(helper, new Vector2(.92f, .38f), new Vector2(.92f, .20f), new Vector2(.74f, .20f));
                    Line(helper, .5f, .20f, .5f, .80f); Line(helper, .22f, .5f, .78f, .5f);
                    break;
                case LevelUpOptionCategory.Survival:
                    Path(helper, new Vector2(.5f, .94f), new Vector2(.85f, .78f), new Vector2(.80f, .38f), new Vector2(.5f, .08f), new Vector2(.2f, .38f), new Vector2(.15f, .78f), new Vector2(.5f, .94f));
                    Line(helper, .5f, .34f, .5f, .72f); Line(helper, .32f, .53f, .68f, .53f);
                    break;
                case LevelUpOptionCategory.Mobility:
                    Path(helper, new Vector2(.16f, .17f), new Vector2(.46f, .50f), new Vector2(.16f, .83f));
                    Path(helper, new Vector2(.51f, .17f), new Vector2(.81f, .50f), new Vector2(.51f, .83f));
                    break;
                case LevelUpOptionCategory.Loot:
                    Path(helper, new Vector2(.15f, .85f), new Vector2(.15f, .34f), new Vector2(.30f, .15f), new Vector2(.70f, .15f), new Vector2(.85f, .34f), new Vector2(.85f, .85f));
                    Path(helper, new Vector2(.65f, .85f), new Vector2(.65f, .42f), new Vector2(.57f, .35f), new Vector2(.43f, .35f), new Vector2(.35f, .42f), new Vector2(.35f, .85f));
                    Line(helper, .15f, .85f, .35f, .85f); Line(helper, .65f, .85f, .85f, .85f);
                    Line(helper, .15f, .68f, .35f, .68f); Line(helper, .65f, .68f, .85f, .68f);
                    break;
                default:
                    Box(helper, .10f, .15f, .32f, .32f); Box(helper, .58f, .15f, .32f, .32f); Box(helper, .10f, .63f, .32f, .32f);
                    Line(helper, .74f, .63f, .74f, .95f); Line(helper, .58f, .79f, .90f, .79f);
                    break;
            }
        }
        private void Box(VertexHelper helper, float x, float y, float w, float h)
        {
            Line(helper, x, y, x + w, y); Line(helper, x + w, y, x + w, y + h);
            Line(helper, x + w, y + h, x, y + h); Line(helper, x, y + h, x, y);
        }
        private void Path(VertexHelper helper, params Vector2[] points)
        {
            for (int i = 1; i < points.Length; i++) Line(helper, points[i - 1].x, points[i - 1].y, points[i].x, points[i].y);
        }
        private void Line(VertexHelper helper, float x1, float y1, float x2, float y2)
        {
            Rect r = GetPixelAdjustedRect();
            Vector2 a = new Vector2(r.x + x1 * r.width, r.y + y1 * r.height);
            Vector2 b = new Vector2(r.x + x2 * r.width, r.y + y2 * r.height);
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * Mathf.Min(r.width, r.height) * .023f;
            int first = helper.currentVertCount;
            helper.AddVert(a - normal, color, Vector2.zero); helper.AddVert(a + normal, color, Vector2.zero);
            helper.AddVert(b + normal, color, Vector2.zero); helper.AddVert(b - normal, color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2); helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
