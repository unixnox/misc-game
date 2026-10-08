using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatRoom
{
    /// <summary>
    /// The whole 2D interface, built from code. Split into partial files:
    /// HUD (this file), Shop, Decorate, CatPanel and Popups.
    /// </summary>
    public partial class GameUI : MonoBehaviour
    {
        Canvas canvas;
        CanvasScaler scaler;
        RectTransform root, hudLayer, panelLayer, popupLayer, toastLayer;

        // HUD
        Text coinText, comfortText, catsText;
        RectTransform coinPill;
        GameObject bottomBar;
        Button strayButton;
        double shownCoins;

        // Toast
        RectTransform toastRect;
        Text toastText;
        float toastTimer;
        readonly Queue<string> toastQueue = new Queue<string>();

        bool builtPortrait;
        int builtW, builtH;
        bool subscribed;

        GameManager GM => GameManager.I;

        public static bool IsPortrait => Screen.height > Screen.width;

        /// <summary>Landscape: 720 units tall. Portrait: 720 units wide.</summary>
        public static void ConfigureScaler(CanvasScaler s)
        {
            if (s == null) return;
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (IsPortrait)
            {
                s.referenceResolution = new Vector2(720, 1280);
                s.matchWidthOrHeight = 0f;
            }
            else
            {
                s.referenceResolution = new Vector2(1280, 720);
                s.matchWidthOrHeight = 1f;
            }
        }

        public static Vector2 CanvasSize
        {
            get
            {
                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                return IsPortrait ? new Vector2(720f, 720f / aspect) : new Vector2(720f * aspect, 720f);
            }
        }

        // ---------------- Build ----------------

        public void Build()
        {
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                scaler = gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
                root = (RectTransform)transform;
            }
            ConfigureScaler(scaler);
            builtPortrait = IsPortrait;
            builtW = Screen.width;
            builtH = Screen.height;

            hudLayer = UIKit.Rect("HUD", root).Stretch();
            panelLayer = UIKit.Rect("Panels", root).Stretch();
            popupLayer = UIKit.Rect("Popups", root).Stretch();
            toastLayer = UIKit.Rect("Toasts", root).Stretch();

            BuildHud();
            BuildBottomBar();
            BuildToast();
            BuildCatPanel();
            BuildDecorateBar();

            shownCoins = GM.Coins;

            if (!subscribed)
            {
                subscribed = true;
                GM.SelectionChanged += OnSelectionChanged;
                GM.InventoryChanged += OnInventoryChanged;
                GM.Placement.Changed += RefreshDecorate;
                GM.Cats.StrayArrived += OnStrayArrived;
                GM.Cats.StrayLeft += OnStrayLeft;
                GM.Cats.CatsChanged += OnInventoryChanged;
            }
            OnSelectionChanged();
            RefreshDecorate();
        }

        /// <summary>Destroys and rebuilds the interface (language or orientation change).</summary>
        public void Rebuild()
        {
            UIKit.ClearChildren(root);
            shopRoot = null;
            catsListRoot = null;
            foodPickerRoot = null;
            Build();
            if (GM.Floating != null) GM.Floating.Rebuild();
        }

        void BuildHud()
        {
            // ----- top-left pills -----
            var pills = UIKit.Rect("Pills", hudLayer);
            pills.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -14), new Vector2(10, 56));
            // stacked vertically in portrait, in a row in landscape
            if (IsPortrait) UIKit.VLayout(pills.gameObject, 8, TextAnchor.UpperLeft, null, true, true, false, false);
            else UIKit.HLayout(pills.gameObject, 10, TextAnchor.UpperLeft);
            UIKit.Fit(pills.gameObject, true, IsPortrait);

            coinPill = Pill(pills, out coinText, UIKit.Gold);
            UIKit.CoinIcon(coinPill, 38).SetAsFirstSibling();

            var comfortPill = Pill(pills, out comfortText, UIKit.Pink);
            var heart = UIKit.Image(comfortPill, ProcGen.Heart, UIKit.Pink, "Heart");
            heart.Layout(34, 34);
            heart.transform.SetAsFirstSibling();

            var catsPill = Pill(pills, out catsText, UIKit.Orange);
            var paw = UIKit.Image(catsPill, ProcGen.Paw, UIKit.Orange, "Paw");
            paw.Layout(32, 32);
            paw.transform.SetAsFirstSibling();

            // ----- top-right settings -----
            var settings = UIKit.Button(hudLayer, Loc.T("settings"), UIKit.Purple, ShowSettings, 22, "Settings");
            ((RectTransform)settings.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -14), new Vector2(130, 52));
        }

        RectTransform Pill(Transform parent, out Text text, Color accent)
        {
            var bg = UIKit.Panel(parent, UIKit.Paper, true, "Pill");
            bg.raycastTarget = false;
            var rt = bg.rectTransform;
            UIKit.HLayout(rt.gameObject, 8, TextAnchor.MiddleLeft, new RectOffset(10, 18, 6, 6));
            rt.Layout(-1, 52);
            text = UIKit.Label(rt, "", 26, UIKit.Ink, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.Layout(-1, 40);
            return rt;
        }

        void BuildBottomBar()
        {
            var bar = UIKit.Rect("BottomBar", hudLayer);
            bar.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(560, 124));
            UIKit.HLayout(bar.gameObject, 16, TextAnchor.LowerCenter, null, true, true, false, false);
            bottomBar = bar.gameObject;

            BigButton(bar, Loc.T("shop"), GM.Icons.Item(ItemDatabase.Get("food_dry")), UIKit.Orange, () => ShowShop(ItemCategory.Furniture));
            BigButton(bar, Loc.T("decorate"), GM.Icons.Item(ItemDatabase.Get("sofa")), UIKit.Green, () => GM.Placement.EnterEdit());
            BigButton(bar, Loc.T("my_cats"), GM.Icons.Cat(GM.Data.cats.Count > 0 ? GM.Data.cats[0].preset : 0), UIKit.Blue, ShowCatsList);

            // "a stray is visiting" button
            strayButton = UIKit.Button(hudLayer, null, UIKit.Pink, () =>
            {
                if (GM.Cats.Stray != null) GM.Select(GM.Cats.Stray);
            }, 22, "StrayButton");
            var srt = (RectTransform)strayButton.transform;
            if (IsPortrait) srt.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(300, 64));
            else srt.Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 16), new Vector2(260, 64));
            var row = UIKit.Rect("Row", srt).Stretch(8, 4, 12, 4);
            UIKit.HLayout(row.gameObject, 6, TextAnchor.MiddleCenter);
            var paw = UIKit.Image(row, ProcGen.Paw, Color.white, "Paw");
            paw.Layout(36, 36);
            var t = UIKit.Label(row, Loc.T("stray_visit"), 24, Color.white);
            t.Outlined(new Color(0.5f, 0.2f, 0.3f, 0.6f), 1.5f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.Layout(-1, 40);
            strayButton.gameObject.SetActive(false);
        }

        void BigButton(Transform parent, string label, Sprite icon, Color color, Action onClick)
        {
            var b = UIKit.Button(parent, null, color, onClick, 24, label);
            b.Layout(170, 118);
            var img = UIKit.Image(b.transform, icon, Color.white, "Icon");
            img.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(84, 84));
            var t = UIKit.Label(b.transform, label, 24, Color.white);
            t.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(166, 34));
            t.Outlined(new Color(0.3f, 0.2f, 0.15f, 0.6f), 1.5f);
        }

        void BuildToast()
        {
            var bg = UIKit.Panel(toastLayer, new Color(0.33f, 0.22f, 0.18f, 0.92f), true, "Toast");
            bg.raycastTarget = false;
            toastRect = bg.rectTransform;
            float y = IsPortrait ? -200f : -84f;
            toastRect.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(640, 60));
            UIKit.HLayout(toastRect.gameObject, 0, TextAnchor.MiddleCenter, new RectOffset(24, 24, 10, 10));
            UIKit.Fit(toastRect.gameObject, true, true);
            toastText = UIKit.Label(toastRect, "", 24, Color.white);
            toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastText.Layout(Mathf.Min(620f, CanvasSize.x - 80f), -1);
            toastRect.gameObject.SetActive(false);
        }

        // ---------------- Runtime ----------------

        public void Toast(string message)
        {
            if (toastQueue.Count > 3) toastQueue.Dequeue();
            toastQueue.Enqueue(message);
            if (toastTimer <= 0f) NextToast();
        }

        void NextToast()
        {
            if (toastQueue.Count == 0 || toastRect == null)
            {
                if (toastRect != null) toastRect.gameObject.SetActive(false);
                return;
            }
            toastText.text = toastQueue.Dequeue();
            toastRect.gameObject.SetActive(true);
            toastRect.localScale = Vector3.one * 0.85f;
            toastTimer = 2.8f;
        }

        void Update()
        {
            if (GM == null || !GM.Ready || root == null) return;
            float dt = Time.unscaledDeltaTime;

            if (IsPortrait != builtPortrait || (Screen.width != builtW || Screen.height != builtH) && Mathf.Abs(Screen.width - builtW) + Mathf.Abs(Screen.height - builtH) > 40)
            {
                Rebuild();
                return;
            }

            // coins count up smoothly
            shownCoins = shownCoins < GM.Coins
                ? Math.Min(GM.Coins, shownCoins + Math.Max(1.0, (GM.Coins - shownCoins) * dt * 8.0))
                : GM.Coins;
            coinText.text = UIKit.Money((long)Math.Round(shownCoins));
            int comfort = GM.Room.Comfort;
            comfortText.text = Loc.F("comfort", comfort, GM.Room.ComfortMultiplier.ToString("0.00"));
            catsText.text = GM.Cats.Owned.Count + "/" + CatManager.MaxCats;

            bool edit = GM.Placement.EditMode;
            bool catPanelOpen = catPanel != null && catPanel.activeSelf;
            bottomBar.SetActive(!edit && !(IsPortrait && catPanelOpen));
            bool showStray = !edit && GM.Cats.Stray != null && GM.Selected != GM.Cats.Stray && !(IsPortrait && catPanelOpen);
            strayButton.gameObject.SetActive(showStray);
            if (showStray) strayButton.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.04f);

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastRect.localScale = Vector3.Lerp(toastRect.localScale, Vector3.one, dt * 14f);
                if (toastTimer <= 0f) NextToast();
            }

            UpdateCatPanel();
        }

        // ---------------- Events ----------------

        void OnStrayArrived()
        {
            Toast(Loc.T("stray_arrived"));
        }

        void OnStrayLeft()
        {
            Toast(Loc.T("stray_left"));
        }

        void OnInventoryChanged()
        {
            if (shopRoot != null) RefreshShop();
            if (foodPickerRoot != null) RefreshFoodPicker();
            RefreshDecorate();
        }

        // ---------------- Modal helper ----------------

        /// <summary>Creates a dimmed full-screen blocker with a centered panel.</summary>
        RectTransform Modal(Transform layer, string name, Vector2 maxSize, out RectTransform panel, Action onBackgroundTap = null)
        {
            var dim = UIKit.Rect(name, layer).Stretch();
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.25f, 0.15f, 0.12f, 0.45f);
            if (onBackgroundTap != null)
            {
                var b = dim.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => onBackgroundTap());
            }
            var size = CanvasSize;
            var p = UIKit.Panel(dim, UIKit.Paper, true, "Panel");
            panel = p.rectTransform;
            panel.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(Mathf.Min(maxSize.x, size.x - 28f), Mathf.Min(maxSize.y, size.y - 40f)));
            panel.gameObject.AddComponent<PopIn>();
            panel.gameObject.AddComponent<ClickBlocker>();
            return dim;
        }

        Button CloseButton(RectTransform panel, Action onClose)
        {
            var b = UIKit.Button(panel, "X", UIKit.Pink, onClose, 28, "Close");
            ((RectTransform)b.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(56, 56));
            return b;
        }

        Text Title(RectTransform panel, string text)
        {
            var t = UIKit.Label(panel, text, 34, UIKit.Ink, TextAnchor.MiddleLeft, "Title");
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -12), new Vector2(560, 56));
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        static string Duration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int h = total / 3600, m = (total % 3600) / 60, s = total % 60;
            if (h > 0) return Loc.F("dur_h_m", h, m);
            if (m > 0 && seconds >= 120f) return Loc.F("dur_m", m);
            return m + ":" + s.ToString("00");
        }
    }

    /// <summary>Stops clicks on a panel from reaching the dimmed background behind it.</summary>
    public class ClickBlocker : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
    {
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData) { }
    }

    /// <summary>Little scale-in animation for panels.</summary>
    public class PopIn : MonoBehaviour
    {
        float t;
        void OnEnable() { t = 0f; transform.localScale = Vector3.one * 0.85f; }
        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime * 6f);
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.06f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t) * (t < 1f ? s : 1f);
        }
    }
}
