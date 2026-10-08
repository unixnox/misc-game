using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatRoom
{
    /// <summary>
    /// Screen-space overlay that tracks world positions: coin bubbles above happy cats,
    /// thought icons, stray trust bars, floating hearts and "+coins" texts.
    /// </summary>
    public class FloatingUI : MonoBehaviour
    {
        const float BubbleLifetime = 14f;

        class Bubble
        {
            public CatController cat;
            public RectTransform rt;
            public Text text;
            public int amount;
            public float age;
        }

        class CatTag
        {
            public RectTransform thought;
            public Image thoughtIcon;
            public Text thoughtText;
            public RectTransform trust;
            public Image trustFill;
            public Text name;
        }

        class Fx
        {
            public RectTransform rt;
            public Graphic g;
            public Vector2 velocity;
            public float life, maxLife;
        }

        Canvas canvas;
        CanvasScaler scaler;
        RectTransform root;
        readonly Dictionary<CatController, Bubble> bubbles = new Dictionary<CatController, Bubble>();
        readonly Dictionary<CatController, CatTag> tags = new Dictionary<CatController, CatTag>();
        readonly List<Fx> fx = new List<Fx>();
        readonly List<CatController> scratch = new List<CatController>();

        GameManager GM => GameManager.I;
        float Scale => canvas != null ? canvas.scaleFactor : 1f;

        public void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            scaler = gameObject.AddComponent<CanvasScaler>();
            GameUI.ConfigureScaler(scaler);
            gameObject.AddComponent<GraphicRaycaster>();
            root = (RectTransform)transform;
        }

        // ---------------- Coins ----------------

        public void AddCoinBubble(CatController cat, int amount)
        {
            if (bubbles.TryGetValue(cat, out var b))
            {
                b.amount += amount;
                b.age = Mathf.Min(b.age, BubbleLifetime * 0.5f);
                b.text.text = "+" + b.amount;
                return;
            }
            b = new Bubble { cat = cat, amount = amount };
            var btn = UIKit.Button(root, null, new Color(1f, 0.97f, 0.85f), null, 20, "CoinBubble");
            b.rt = (RectTransform)btn.transform;
            b.rt.sizeDelta = new Vector2(104, 52);
            b.rt.pivot = new Vector2(0.5f, 0f);
            var row = UIKit.Rect("Row", b.rt).Stretch(8, 4, 8, 4);
            UIKit.HLayout(row.gameObject, 4, TextAnchor.MiddleCenter);
            UIKit.CoinIcon(row, 34);
            b.text = UIKit.Label(row, "+" + amount, 24, UIKit.Ink);
            b.text.Layout(-1, 40);
            b.text.horizontalOverflow = HorizontalWrapMode.Overflow;
            btn.onClick.AddListener(() => Collect(b, true));
            bubbles[cat] = b;
            GM.Sfx.Play(Sound.Pop, 0.35f, 1.2f);
        }

        void Collect(Bubble b, bool tapped)
        {
            if (!bubbles.ContainsKey(b.cat) && b.cat != null) return;
            int total = tapped ? b.amount * 2 : b.amount;
            GM.AddCoins(total);
            if (tapped)
            {
                GM.Sfx.Play(Sound.Coin, 0.7f);
                SpawnText(b.rt.position, "+" + total + (tapped ? " x2!" : ""), UIKit.Gold);
            }
            Destroy(b.rt.gameObject);
            bubbles.Remove(b.cat);
        }

        /// <summary>Collect every bubble on screen (used by the HUD "collect all" helper).</summary>
        public int PendingCoins
        {
            get
            {
                int s = 0;
                foreach (var b in bubbles.Values) s += b.amount;
                return s;
            }
        }

        // ---------------- Effects ----------------

        public void SpawnHearts(Vector3 world, int count)
        {
            var sp = WorldToScreen(world, out bool visible);
            if (!visible) return;
            for (int i = 0; i < count; i++)
            {
                var img = UIKit.Image(root, ProcGen.Heart, Color.Lerp(UIKit.Pink, new Color(1f, 0.4f, 0.5f), Random.value), "Heart");
                img.rectTransform.sizeDelta = Vector2.one * Random.Range(26f, 40f);
                img.rectTransform.position = sp + new Vector2(Random.Range(-30f, 30f), Random.Range(0f, 20f)) * Scale;
                fx.Add(new Fx { rt = img.rectTransform, g = img, velocity = new Vector2(Random.Range(-30f, 30f), Random.Range(90f, 150f)), life = 0f, maxLife = Random.Range(1.0f, 1.6f) });
            }
        }

        public void SpawnText(Vector3 screenPos, string s, Color c)
        {
            var t = UIKit.Label(root, s, 30, c, TextAnchor.MiddleCenter, "FxText");
            t.Outlined(UIKit.Ink, 2f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.rectTransform.sizeDelta = new Vector2(220, 50);
            t.rectTransform.position = screenPos;
            fx.Add(new Fx { rt = t.rectTransform, g = t, velocity = new Vector2(0, 110f), maxLife = 1.2f });
        }

        Vector2 WorldToScreen(Vector3 world, out bool visible)
        {
            var cam = GM.MainCamera;
            var p = cam.WorldToScreenPoint(world);
            visible = p.z > 0;
            return new Vector2(p.x, p.y);
        }

        // ---------------- Per-cat tags ----------------

        CatTag GetTag(CatController cat)
        {
            if (tags.TryGetValue(cat, out var tag)) return tag;
            tag = new CatTag();

            var bubble = UIKit.Panel(root, Color.white, true, "Thought");
            bubble.raycastTarget = false;
            tag.thought = bubble.rectTransform;
            tag.thought.sizeDelta = new Vector2(58, 58);
            tag.thought.pivot = new Vector2(0.5f, 0f);
            tag.thoughtIcon = UIKit.Image(tag.thought, null, Color.white, "Icon");
            tag.thoughtIcon.rectTransform.Stretch(6, 6, 6, 6);
            tag.thoughtText = UIKit.Label(tag.thought, "Zz", 24, UIKit.Blue);
            tag.thoughtText.rectTransform.Stretch();

            var trustBg = UIKit.Panel(root, new Color(1f, 1f, 1f, 0.9f), true, "Trust");
            trustBg.raycastTarget = false;
            tag.trust = trustBg.rectTransform;
            tag.trust.sizeDelta = new Vector2(120, 40);
            tag.trust.pivot = new Vector2(0.5f, 0f);
            var heart = UIKit.Image(tag.trust, ProcGen.Heart, UIKit.Pink, "Heart");
            heart.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(26, 26));
            var bar = UIKit.Bar(tag.trust, UIKit.Pink);
            ((RectTransform)bar.transform.parent).Stretch(36, 12, 10, 12);
            tag.trustFill = bar;

            var name = UIKit.Label(root, "", 22, Color.white, TextAnchor.MiddleCenter, "Name");
            name.Outlined(UIKit.Ink, 2f);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.rectTransform.sizeDelta = new Vector2(200, 30);
            name.rectTransform.pivot = new Vector2(0.5f, 0f);
            tag.name = name;

            tags[cat] = tag;
            return tag;
        }

        void Update()
        {
            if (GM == null || !GM.Ready || GM.MainCamera == null) return;
            float dt = Time.unscaledDeltaTime;
            float s = Scale;
            bool hide = GM.Placement != null && GM.Placement.EditMode;

            // cats: owned + stray
            scratch.Clear();
            scratch.AddRange(GM.Cats.Owned);
            if (GM.Cats.Stray != null) scratch.Add(GM.Cats.Stray);

            foreach (var cat in scratch)
            {
                if (cat == null) continue;
                var tag = GetTag(cat);
                var sp = WorldToScreen(cat.Visual.HeadTop, out bool vis);
                vis &= !hide;

                // thought bubble
                var thought = cat.Thought;
                bool showThought = vis && thought != CatThought.None && !bubbles.ContainsKey(cat);
                tag.thought.gameObject.SetActive(showThought);
                if (showThought)
                {
                    tag.thought.position = sp + new Vector2(34f, 6f + Mathf.Sin(Time.unscaledTime * 3f) * 4f) * s;
                    tag.thoughtText.gameObject.SetActive(thought == CatThought.Sleepy);
                    tag.thoughtIcon.gameObject.SetActive(thought != CatThought.Sleepy);
                    switch (thought)
                    {
                        case CatThought.Hungry: tag.thoughtIcon.sprite = GM.Icons.Item(ItemDatabase.Get("food_bowl")); tag.thoughtIcon.color = Color.white; break;
                        case CatThought.Bored: tag.thoughtIcon.sprite = GM.Icons.Item(ItemDatabase.Get("toy_ball")); tag.thoughtIcon.color = Color.white; break;
                        case CatThought.Love: tag.thoughtIcon.sprite = ProcGen.Heart; tag.thoughtIcon.color = UIKit.Pink; break;
                    }
                }

                // stray trust bar
                bool showTrust = vis && cat.IsStray;
                tag.trust.gameObject.SetActive(showTrust);
                if (showTrust)
                {
                    tag.trust.position = sp + new Vector2(0, 40f) * s;
                    tag.trustFill.SetFill(cat.Trust / 100f);
                }

                // name label on the selected cat
                bool showName = vis && GM.Selected == cat;
                tag.name.gameObject.SetActive(showName);
                if (showName)
                {
                    tag.name.text = cat.IsStray ? Loc.T("stray_cat") : cat.Data.Name;
                    tag.name.rectTransform.position = sp + new Vector2(0, cat.IsStray ? 84f : 40f) * s;
                }
            }

            // remove tags of cats that are gone
            var dead = new List<CatController>();
            foreach (var kv in tags) if (kv.Key == null) dead.Add(kv.Key);
            foreach (var k in dead)
            {
                var tag = tags[k];
                Destroy(tag.thought.gameObject);
                Destroy(tag.trust.gameObject);
                Destroy(tag.name.gameObject);
                tags.Remove(k);
            }

            // coin bubbles
            var expired = new List<Bubble>();
            foreach (var b in bubbles.Values)
            {
                if (b.cat == null) { expired.Add(b); continue; }
                b.age += dt;
                var sp = WorldToScreen(b.cat.Visual.HeadTop, out bool vis);
                b.rt.gameObject.SetActive(vis && !hide);
                b.rt.position = sp + new Vector2(0, 8f + Mathf.Sin(Time.unscaledTime * 2.5f + b.amount) * 5f) * s;
                if (b.age > BubbleLifetime) expired.Add(b);
            }
            foreach (var b in expired)
            {
                if (b.cat == null)
                {
                    GM.AddCoins(b.amount);
                    Destroy(b.rt.gameObject);
                    var key = default(CatController);
                    foreach (var kv in bubbles) if (kv.Value == b) key = kv.Key;
                    bubbles.Remove(key);
                }
                else Collect(b, false);
            }

            // effects
            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i];
                f.life += dt;
                if (f.rt == null || f.life >= f.maxLife)
                {
                    if (f.rt != null) Destroy(f.rt.gameObject);
                    fx.RemoveAt(i);
                    continue;
                }
                f.rt.position += (Vector3)(f.velocity * s * dt);
                var c = f.g.color;
                c.a = 1f - Mathf.Clamp01((f.life - f.maxLife * 0.5f) / (f.maxLife * 0.5f));
                f.g.color = c;
            }
        }

        public void Rebuild()
        {
            GameUI.ConfigureScaler(scaler);
        }
    }
}
