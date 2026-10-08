using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatRoom
{
    /// <summary>Helpers to build a cartoon-styled uGUI entirely from code.</summary>
    public static class UIKit
    {
        public static readonly Color Ink = new Color(0.33f, 0.22f, 0.18f);
        public static readonly Color Paper = new Color(1f, 0.98f, 0.93f);
        public static readonly Color PaperDark = new Color(0.98f, 0.92f, 0.82f);
        public static readonly Color Orange = new Color(1f, 0.63f, 0.33f);
        public static readonly Color Green = new Color(0.46f, 0.8f, 0.48f);
        public static readonly Color Pink = new Color(1f, 0.56f, 0.68f);
        public static readonly Color Blue = new Color(0.45f, 0.7f, 0.96f);
        public static readonly Color Yellow = new Color(1f, 0.82f, 0.3f);
        public static readonly Color Purple = new Color(0.7f, 0.58f, 0.95f);
        public static readonly Color Gray = new Color(0.75f, 0.72f, 0.7f);
        public static readonly Color Shadow = new Color(0.4f, 0.25f, 0.2f, 0.35f);
        public static readonly Color Gold = new Color(1f, 0.78f, 0.2f);

        static Font font;
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>("Fonts/Kanit-Regular");
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Action ClickSound;

        // ---------------- Rect helpers ----------------

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Anchors at a single point (0..1) with a pivot, position and size.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static LayoutElement Layout(this Component c, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefW;
            le.preferredHeight = prefH;
            le.flexibleWidth = flexW;
            le.flexibleHeight = flexH;
            if (prefW >= 0) le.minWidth = prefW;
            if (prefH >= 0) le.minHeight = prefH;
            return le;
        }

        // ---------------- Widgets ----------------

        public static Image Panel(Transform parent, Color color, bool shadow = true, string name = "Panel")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ProcGen.RoundedRect;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.6f;
            img.color = color;
            if (shadow)
            {
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = Shadow;
                s.effectDistance = new Vector2(0, -5);
            }
            return img;
        }

        public static Image Image(Transform parent, Sprite sprite, Color color, string name = "Image")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, string name = "Label")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 0.9f;
            return t;
        }

        public static Text Outlined(this Text t, Color outline, float dist = 2f)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = outline;
            o.effectDistance = new Vector2(dist, -dist);
            return t;
        }

        public static Button Button(Transform parent, string label, Color color, Action onClick, int fontSize = 26, string name = "Button")
        {
            var img = Panel(parent, color, true, name);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            colors.fadeDuration = 0.05f;
            btn.colors = colors;
            btn.targetGraphic = img;
            img.gameObject.AddComponent<ButtonBounce>();
            if (label != null)
            {
                var t = Label(img.transform, label, fontSize, Color.white);
                t.rectTransform.Stretch(8, 2, 8, 2);
                t.Outlined(new Color(0.35f, 0.22f, 0.18f, 0.6f), 1.5f);
            }
            if (onClick != null)
                btn.onClick.AddListener(() =>
                {
                    ClickSound?.Invoke();
                    onClick();
                });
            return btn;
        }

        public static Text ButtonLabel(this Button b) => b.GetComponentInChildren<Text>();

        public static void SetInteractable(this Button b, bool on, Color enabledColor)
        {
            b.interactable = on;
            ((Image)b.targetGraphic).color = on ? enabledColor : Gray;
        }

        /// <summary>A rounded progress bar; returns the fill image (set it with <see cref="SetFill"/>).</summary>
        public static Image Bar(Transform parent, Color fill, string name = "Bar")
        {
            var bg = Panel(parent, new Color(0.9f, 0.85f, 0.78f), false, name);
            bg.raycastTarget = false;
            var f = Panel(bg.transform, fill, false, "Fill");
            f.raycastTarget = false;
            f.pixelsPerUnitMultiplier = 3f;
            SetFill(f, 1f);
            return f;
        }

        public static void SetFill(this Image fill, float value)
        {
            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            rt.offsetMin = new Vector2(3, 3);
            rt.offsetMax = new Vector2(value <= 0.001f ? 3 : -3, -3);
            fill.enabled = value > 0.02f;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, float spacing, TextAnchor align = TextAnchor.MiddleCenter, RectOffset padding = null,
            bool controlW = true, bool controlH = true, bool expandW = false, bool expandH = false)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.padding = padding ?? new RectOffset(0, 0, 0, 0);
            h.childControlWidth = controlW;
            h.childControlHeight = controlH;
            h.childForceExpandWidth = expandW;
            h.childForceExpandHeight = expandH;
            return h;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, float spacing, TextAnchor align = TextAnchor.UpperCenter, RectOffset padding = null,
            bool controlW = true, bool controlH = true, bool expandW = true, bool expandH = false)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.padding = padding ?? new RectOffset(0, 0, 0, 0);
            v.childControlWidth = controlW;
            v.childControlHeight = controlH;
            v.childForceExpandWidth = expandW;
            v.childForceExpandHeight = expandH;
            return v;
        }

        public static ContentSizeFitter Fit(GameObject go, bool horizontal, bool vertical)
        {
            var f = go.AddComponent<ContentSizeFitter>();
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        /// <summary>Scroll view. Returns the content transform (add a layout group + fitter to it).</summary>
        public static RectTransform ScrollView(Transform parent, out ScrollRect scroll, bool horizontal = false)
        {
            var root = Rect("Scroll", parent);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect("Viewport", root).Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0); // catches drags between items
            var content = Rect("Content", viewport);
            if (horizontal)
            {
                content.anchorMin = new Vector2(0, 0);
                content.anchorMax = new Vector2(0, 1);
                content.pivot = new Vector2(0, 0.5f);
            }
            else
            {
                content.anchorMin = new Vector2(0, 1);
                content.anchorMax = new Vector2(1, 1);
                content.pivot = new Vector2(0.5f, 1);
            }
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return content;
        }

        /// <summary>Coin icon: gold disc + ring.</summary>
        public static RectTransform CoinIcon(Transform parent, float size)
        {
            var disc = Image(parent, ProcGen.Circle, Gold, "Coin");
            disc.rectTransform.sizeDelta = new Vector2(size, size);
            var ring = Image(disc.transform, ProcGen.CoinRing, new Color(0.85f, 0.55f, 0.1f), "Ring");
            ring.rectTransform.Stretch(size * 0.12f, size * 0.12f, size * 0.12f, size * 0.12f);
            disc.Layout(size, size);
            return disc.rectTransform;
        }

        public static string Money(long v) => v.ToString("N0");

        /// <summary>Destroys all children, detaching them first so layouts update immediately.</summary>
        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                c.SetParent(null, false);
                UnityEngine.Object.Destroy(c.gameObject);
            }
        }
    }

    /// <summary>Squishy press feedback for buttons.</summary>
    public class ButtonBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Vector3 target = Vector3.one;

        public void OnPointerDown(PointerEventData e) { if (IsInteractable()) target = Vector3.one * 0.93f; }
        public void OnPointerUp(PointerEventData e) { target = Vector3.one; }
        public void OnPointerExit(PointerEventData e) { target = Vector3.one; }

        bool IsInteractable()
        {
            var b = GetComponent<Selectable>();
            return b == null || b.IsInteractable();
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * 20f);
        }
    }
}
