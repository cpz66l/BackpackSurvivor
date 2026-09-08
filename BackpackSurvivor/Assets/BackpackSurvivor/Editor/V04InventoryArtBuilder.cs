using System;
using BS.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Restyles existing inventory bindings; the original 420 x 560 placement/discard rectangle is retained.</summary>
    public static class V04InventoryArtBuilder
    {
        public const string FramePath = "Assets/BackpackSurvivor/Art/UI/V04/InventoryFrame.png";
        public const string ItemVariantPath = "Assets/BackpackSurvivor/Prefabs/UI/V04/ItemView_V04.prefab";
        private const string SourceItemPath = "Assets/BackpackSurvivor/Prefabs/UI/ItemView.prefab";
        private const string FontPath = "Assets/BackpackSurvivor/Art/Font/SourceHanSansCN-Normal SDF.asset";
        private const string DecorationName = "V04InventoryDecoration";
        private static readonly Color Panel = new Color(.066f, .108f, .15f, .965f);
        private static readonly Color Line = new Color(.36f, .48f, .59f, .9f);
        private static readonly Color White = new Color(.94f, .96f, .98f, 1f);
        private static readonly Color Muted = new Color(.65f, .75f, .84f, 1f);
        private static readonly Vector2 GridSize = new Vector2(420, 560);

        [MenuItem("Tools/Backpack Survivor/Art/Apply V04 Inventory")]
        public static void ApplyToActiveScene() => ApplyToActiveScene(FramePath);

        public static void ApplyToActiveScene(string frameSpritePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Apply V04 inventory art outside Play Mode.");

            InventoryUIController controller = FindController();
            SerializedObject bindings = new SerializedObject(controller);
            RectTransform bag = Required<RectTransform>(bindings, "bagPanel");
            RectTransform items = Required<RectTransform>(bindings, "itemLayer");
            CanvasGroup bagGroup = Required<CanvasGroup>(bindings, "bagPanelcanvasGroup");
            TextMeshProUGUI value = Required<TextMeshProUGUI>(bindings, "totalValueText");
            ItemTooltipView tooltip = Required<ItemTooltipView>(bindings, "tooltipView");
            Canvas canvas = bag.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                throw new InvalidOperationException("The existing inventory pointer conversion requires an overlay Canvas.");
            if (!Mathf.Approximately(bindings.FindProperty("step").floatValue, 70f))
                throw new InvalidOperationException("Expected inventory step 70; changing gameplay grid dimensions is outside this art builder.");
            if (Vector2.Distance(bag.rect.size, GridSize) > .1f || bagGroup.transform != bag)
                throw new InvalidOperationException("Expected the existing 420 x 560 BagPanel and its own CanvasGroup.");
            RectTransform cells = bag.Find("CellLayer") as RectTransform;
            GridLayoutGroup grid = cells != null ? cells.GetComponent<GridLayoutGroup>() : null;
            if (grid == null || cells.childCount != 48 || items.parent != bag)
                throw new InvalidOperationException("Expected the existing 48-cell CellLayer and ItemLayer directly under BagPanel.");

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(frameSpritePath);
            if (font == null) throw new InvalidOperationException("Missing shared Chinese font: " + FontPath);
            if (frameSprite == null) throw new InvalidOperationException("Import the approved backpack frame Sprite before applying: " + frameSpritePath);
            Vector4 border = frameSprite.border;
            if (border.x <= 0 || border.y <= 0 || border.z <= 0 || border.w <= 0)
                throw new InvalidOperationException("InventoryFrame needs a four-sided Sprite border describing its transparent grid opening.");
            // Image's sliced border is expressed in UI units, not texture pixels.
            border *= canvas.rootCanvas.referencePixelsPerUnit / frameSprite.pixelsPerUnit;

            ItemView variant = BuildItemVariant(font);
            AlignLayers(cells, items, grid);
            RestyleCells(cells);
            DisableOldFrame(bag);
            BuildDecoration(bag, frameSprite, border, value, font);
            RestyleTooltip(tooltip, font);

            // Rebind only the new visual prefab. All grid, drop, tooltip, audio and icon-resolver references survive.
            bindings.Update();
            bindings.FindProperty("itemViewPrefab").objectReferenceValue = variant;
            bindings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log("V04 inventory art applied: 6 x 8, step 70, 420 x 560 hit rectangle, aligned cell/item origins and ItemView prefab variant.");
        }

        private static InventoryUIController FindController()
        {
            InventoryUIController result = null;
            foreach (InventoryUIController candidate in UnityEngine.Object.FindObjectsByType<InventoryUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != SceneManager.GetActiveScene()) continue;
                if (result != null) throw new InvalidOperationException("More than one InventoryUIController exists in the active scene.");
                result = candidate;
            }
            if (result == null) throw new InvalidOperationException("Open the gameplay scene containing InventoryUIController first.");
            return result;
        }

        private static void AlignLayers(RectTransform cells, RectTransform items, GridLayoutGroup grid)
        {
            // A 2.5 px gutter on each side centres each 65 px cell inside its 70 px logical step.
            TopLeft(cells, new Vector2(2.5f, -2.5f), new Vector2(415, 555));
            TopLeft(items, Vector2.zero, GridSize);
            grid.padding = new RectOffset();
            grid.cellSize = new Vector2(65, 65);
            grid.spacing = new Vector2(5, 5);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            LayoutRebuilder.ForceRebuildLayoutImmediate(cells);
            EditorUtility.SetDirty(grid);
        }

        private static void RestyleCells(RectTransform cells)
        {
            foreach (Transform cell in cells)
            {
                Image old = cell.GetComponent<Image>();
                // UGUI allows one Graphic per object. Cells have no controller bindings to their Image.
                if (old != null) UnityEngine.Object.DestroyImmediate(old);
                UpgradeRoundedGraphic face = cell.GetComponent<UpgradeRoundedGraphic>();
                if (face == null) face = cell.gameObject.AddComponent<UpgradeRoundedGraphic>();
                face.Configure(3f, .8f, new Color(.24f, .34f, .42f, .7f));
                face.color = new Color(.076f, .119f, .158f, .93f);
                face.raycastTarget = false;
                EditorUtility.SetDirty(face);
            }
        }

        private static void DisableOldFrame(RectTransform bag)
        {
            Image oldSurface = bag.GetComponent<Image>();
            if (oldSurface != null) { oldSurface.enabled = false; oldSurface.raycastTarget = false; oldSurface.sprite = null; EditorUtility.SetDirty(oldSurface); }
            Transform oldFrame = bag.Find("BagFrame");
            if (oldFrame == null) return;
            oldFrame.gameObject.SetActive(false);
            Image oldImage = oldFrame.GetComponent<Image>();
            if (oldImage != null) { oldImage.sprite = null; oldImage.raycastTarget = false; EditorUtility.SetDirty(oldImage); }
        }

        private static void BuildDecoration(RectTransform bag, Sprite frameSprite, Vector4 border, TextMeshProUGUI value, TMP_FontAsset font)
        {
            RemoveOwnedChild(bag, DecorationName);
            RectTransform decoration = Rect(DecorationName, bag);
            Stretch(decoration, 0);
            decoration.SetAsFirstSibling();
            float left = border.x, bottom = border.y, right = border.z, top = border.w;
            RectTransform backdrop = Rect("V04InventoryPanel", decoration);
            BottomLeft(backdrop, new Vector2(-left - 18, -bottom - 54), new Vector2(420 + left + right + 36, 560 + bottom + top + 134));
            Rounded(backdrop, 11, 1, Panel, Line, false);

            RectTransform well = Rect("V04InventoryGridWell", decoration);
            Stretch(well, 0);
            Rounded(well, 2, 0, new Color(.039f, .067f, .094f, .985f), Color.clear, false);

            RectTransform frameRect = Rect("V04InventoryFrame", decoration);
            BottomLeft(frameRect, new Vector2(-left, -bottom), new Vector2(420 + left + right, 560 + bottom + top));
            Image frame = frameRect.gameObject.AddComponent<Image>();
            frame.sprite = frameSprite;
            frame.type = Image.Type.Sliced;
            frame.fillCenter = false;
            frame.pixelsPerUnitMultiplier = 1;
            frame.raycastTarget = false;

            TextMeshProUGUI heading = Text("V04InventoryHeading", decoration, "背包", font, 32, White, FontStyles.Bold);
            BottomLeft(heading.rectTransform, new Vector2(-left + 2, 560 + top + 19), new Vector2(116, 44));
            heading.alignment = TextAlignmentOptions.MidlineLeft;

            // Keep the existing TMP reference and its runtime value string; its placement is purely visual.
            value.transform.SetParent(bag, false);
            BottomLeft(value.rectTransform, new Vector2(118, 560 + top + 19), new Vector2(302 + right - 2, 44));
            StyleText(value, font, 22, Muted, FontStyles.Normal);
            value.alignment = TextAlignmentOptions.MidlineRight;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.enableAutoSizing = true;
            value.fontSizeMin = 18;
            value.fontSizeMax = 22;
            value.transform.SetAsLastSibling();

            TextMeshProUGUI footer = Text("V04InventoryInstructions", decoration, "拖拽整理  ·  R 旋转  ·  移出网格丢弃", font, 24, Muted, FontStyles.Normal);
            BottomLeft(footer.rectTransform, new Vector2(-left, -bottom - 43), new Vector2(420 + left + right, 32));
            footer.alignment = TextAlignmentOptions.Center;
        }

        private static ItemView BuildItemVariant(TMP_FontAsset font)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceItemPath);
            if (source == null) throw new InvalidOperationException("Missing original ItemView prefab: " + SourceItemPath);
            EnsureFolder("Assets/BackpackSurvivor/Prefabs/UI/V04");
            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null) throw new InvalidOperationException("Could not instantiate the original ItemView prefab for a variant.");
            try
            {
                instance.name = "ItemView_V04";
                ItemView view = instance.GetComponent<ItemView>();
                if (view == null) throw new InvalidOperationException("Original prefab has no ItemView.");
                SerializedObject data = new SerializedObject(view);
                Image state = Required<Image>(data, "bg");
                TextMeshProUGUI label = Required<TextMeshProUGUI>(data, "label");
                state.enabled = false;
                state.sprite = null;
                state.raycastTarget = false;
                TopLeft((RectTransform)instance.transform, Vector2.zero, new Vector2(70, 70));
                foreach (Graphic graphic in instance.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                StyleText(label, font, 18, White, FontStyles.Normal);
                label.enableAutoSizing = true;
                label.fontSizeMin = 12;
                label.fontSizeMax = 18;
                Image active = Required<Image>(data, "activeWeaponUI");
                active.color = new Color(.91f, .72f, .38f, 1);
                foreach (string starName in new[] { "LevelOne", "LevelTwo", "LevelThree" })
                    Required<Image>(data, starName).color = new Color(.92f, .78f, .48f, 1);

                RectTransform surfaceRect = Rect("V04InventoryItemSurface", instance.transform);
                Stretch(surfaceRect, 2.5f);
                surfaceRect.SetAsFirstSibling();
                UpgradeRoundedGraphic surface = Rounded(surfaceRect, 4, 1.35f, Panel, Line, true);
                V04InventoryItemArt art = instance.AddComponent<V04InventoryItemArt>();
                art.Configure(view, state, surface);
                foreach (Component component in instance.GetComponentsInChildren<Component>(true))
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, ItemVariantPath);
                if (saved == null || PrefabUtility.GetPrefabAssetType(saved) != PrefabAssetType.Variant)
                    throw new InvalidOperationException("V04 ItemView must remain a variant of the original prefab.");
                return saved.GetComponent<ItemView>();
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void RestyleTooltip(ItemTooltipView tooltip, TMP_FontAsset font)
        {
            SerializedObject bindings = new SerializedObject(tooltip);
            RectTransform panel = Required<RectTransform>(bindings, "panel");
            TextMeshProUGUI title = Required<TextMeshProUGUI>(bindings, "titleText");
            TextMeshProUGUI body = Required<TextMeshProUGUI>(bindings, "bodyText");
            // The legacy tooltip stretches across its Canvas. Collapse those anchors before assigning
            // a fixed panel size, otherwise sizeDelta is added to the entire screen dimensions.
            TopLeft(panel, Vector2.zero, new Vector2(346, 254));
            foreach (Image old in panel.GetComponents<Image>()) { old.enabled = false; old.sprite = null; old.raycastTarget = false; EditorUtility.SetDirty(old); }
            RemoveOwnedChild(panel, "V04InventoryTooltipSurface");
            RectTransform surface = Rect("V04InventoryTooltipSurface", panel);
            Stretch(surface, 0);
            surface.SetAsFirstSibling();
            Rounded(surface, 8, 1, new Color(.053f, .089f, .125f, .99f), Line, false);
            StyleText(title, font, 24, White, FontStyles.Bold);
            TopLeft(title.rectTransform, new Vector2(18, -15), new Vector2(310, 42));
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 24;
            StyleText(body, font, 22, Muted, FontStyles.Normal);
            TopLeft(body.rectTransform, new Vector2(18, -69), new Vector2(310, 166));
            body.alignment = TextAlignmentOptions.TopLeft;
            body.lineSpacing = 8;
            if (panel.GetComponent<V04InventoryTooltipClamp>() == null) panel.gameObject.AddComponent<V04InventoryTooltipClamp>();
            foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            EditorUtility.SetDirty(panel);
        }

        private static T Required<T>(SerializedObject data, string property) where T : UnityEngine.Object
        {
            T value = data.FindProperty(property)?.objectReferenceValue as T;
            if (value == null) throw new InvalidOperationException(data.targetObject.name + " has no " + property + " binding.");
            return value;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static UpgradeRoundedGraphic Rounded(RectTransform rect, float radius, float borderWidth, Color fill, Color border, bool raycast)
        {
            if (rect.GetComponent<CanvasRenderer>() == null) rect.gameObject.AddComponent<CanvasRenderer>();
            UpgradeRoundedGraphic graphic = rect.gameObject.AddComponent<UpgradeRoundedGraphic>();
            graphic.Configure(radius, borderWidth, border);
            graphic.color = fill;
            graphic.raycastTarget = raycast;
            return graphic;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color, FontStyles style)
        {
            TextMeshProUGUI result = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            StyleText(result, font, size, color, style);
            result.text = text;
            return result;
        }

        private static void StyleText(TextMeshProUGUI text, TMP_FontAsset font, float size, Color color, FontStyles style)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.margin = Vector4.zero;
            text.characterSpacing = 0;
            text.lineSpacing = 0;
            EditorUtility.SetDirty(text);
        }

        private static void TopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(rect);
        }

        private static void BottomLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void RemoveOwnedChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(slash + 1));
        }
    }
}
