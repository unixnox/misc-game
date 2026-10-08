using UnityEngine;
using UnityEngine.UI;

namespace CatRoom
{
    public partial class GameUI
    {
        GameObject catPanel;
        Image catIcon;
        Text catName, catBreed, catStatus, catExtra;
        Image hungerFill, funFill, energyFill, happyFill, trustFill;
        GameObject trustRow, happyRow;
        Button petButton, playButton, adoptButton, renameButton;
        CatController shownCat;

        RectTransform foodPickerRoot, foodPickerList;
        RectTransform catsListRoot;

        // ---------------- Cat panel ----------------

        void BuildCatPanel()
        {
            var p = UIKit.Panel(hudLayer, UIKit.Paper, true, "CatPanel");
            catPanel = p.gameObject;
            var rt = p.rectTransform;
            if (IsPortrait)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(0.5f, 0);
                rt.offsetMin = new Vector2(10, 10);
                rt.offsetMax = new Vector2(-10, 10 + 510);
            }
            else rt.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-16, -10), new Vector2(380, 560));

            UIKit.VLayout(catPanel, 6, TextAnchor.UpperCenter, new RectOffset(18, 18, 14, 16), true, true, true, false);

            // header
            var header = UIKit.Rect("Header", rt);
            header.Layout(-1, 92);
            var iconBg = UIKit.Image(header, ProcGen.Circle, new Color(1f, 0.92f, 0.82f), "IconBg");
            iconBg.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(88, 88));
            catIcon = UIKit.Image(iconBg.transform, null, Color.white, "Icon");
            catIcon.rectTransform.Stretch(2, 2, 2, 2);
            catName = UIKit.Label(header, "", 30, UIKit.Ink, TextAnchor.LowerLeft, "Name");
            catName.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(98, 2), new Vector2(200, 44));
            catName.horizontalOverflow = HorizontalWrapMode.Overflow;
            catBreed = UIKit.Label(header, "", 19, new Color(0.5f, 0.4f, 0.35f), TextAnchor.UpperLeft, "Breed");
            catBreed.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(98, -2), new Vector2(220, 30));
            catBreed.horizontalOverflow = HorizontalWrapMode.Overflow;
            var close = UIKit.Button(header, "X", UIKit.Pink, () => GM.Select(null), 26, "Close");
            ((RectTransform)close.transform).Place(new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(50, 50));

            catStatus = UIKit.Label(rt, "", 20, new Color(0.45f, 0.36f, 0.32f), TextAnchor.MiddleLeft, "Status");
            catStatus.Layout(-1, 28);

            bool portrait = IsPortrait;
            Transform bars = rt;
            if (portrait)
            {
                // two columns of bars to save height
                var grid = UIKit.Rect("Bars", rt);
                grid.Layout(-1, 96);
                var g = grid.gameObject.AddComponent<GridLayoutGroup>();
                g.cellSize = new Vector2(320, 44);
                g.spacing = new Vector2(10, 6);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 2;
                bars = grid;
            }
            hungerFill = StatRow(bars, Loc.T("hunger"), UIKit.Orange, out _);
            funFill = StatRow(bars, Loc.T("fun"), UIKit.Pink, out _);
            energyFill = StatRow(bars, Loc.T("energy"), UIKit.Blue, out _);
            happyFill = StatRow(bars, Loc.T("happiness"), UIKit.Yellow, out happyRow);
            trustFill = StatRow(rt, Loc.T("trust"), new Color(1f, 0.45f, 0.6f), out trustRow);

            catExtra = UIKit.Label(rt, "", 19, new Color(0.45f, 0.36f, 0.32f), TextAnchor.MiddleLeft, "Extra");
            catExtra.Layout(-1, 44);

            var actions = UIKit.Rect("Actions", rt);
            actions.Layout(-1, 66);
            UIKit.HLayout(actions.gameObject, 8, TextAnchor.MiddleCenter, null, true, true, true, true);
            UIKit.Button(actions, Loc.T("feed"), UIKit.Orange, OnFeedClicked, 22, "Feed");
            petButton = UIKit.Button(actions, Loc.T("pet"), UIKit.Pink, () => ShowResult(shownCat, shownCat != null ? shownCat.Pet() : CatController.Result.Busy), 22, "Pet");
            playButton = UIKit.Button(actions, Loc.T("play"), UIKit.Blue, () => ShowResult(shownCat, shownCat != null ? shownCat.Play() : CatController.Result.Busy), 22, "Play");

            adoptButton = UIKit.Button(rt, Loc.T("adopt"), UIKit.Green, OnAdoptClicked, 28, "Adopt");
            adoptButton.Layout(-1, 62);
            renameButton = UIKit.Button(rt, Loc.T("rename"), UIKit.Purple, OnRenameClicked, 20, "Rename");
            renameButton.Layout(-1, 48);

            catPanel.SetActive(false);
        }

        Image StatRow(Transform parent, string label, Color color, out GameObject rowGo)
        {
            var row = UIKit.Rect("Stat", parent);
            row.Layout(-1, 34);
            rowGo = row.gameObject;
            UIKit.HLayout(row.gameObject, 8, TextAnchor.MiddleLeft, null, true, true, false, false);
            var l = UIKit.Label(row, label, 19, UIKit.Ink, TextAnchor.MiddleLeft);
            l.Layout(IsPortrait ? 96 : 104, -1);
            var fill = UIKit.Bar(row, color);
            ((Component)fill.transform.parent).Layout(-1, 24, 1, -1);
            return fill;
        }

        void OnSelectionChanged()
        {
            if (catPanel == null) return;
            shownCat = GM.Selected;
            bool show = shownCat != null && !GM.Placement.EditMode;
            catPanel.SetActive(show);
            CloseFoodPicker();
            if (!show) return;
            catIcon.sprite = GM.Icons.Cat(shownCat.Data.preset);
            UpdateCatPanel();
        }

        void UpdateCatPanel()
        {
            if (catPanel == null || !catPanel.activeSelf) return;
            var c = shownCat;
            if (c == null) { catPanel.SetActive(false); return; }

            var preset = CatAppearance.Get(c.Data.preset);
            catName.text = c.IsStray ? Loc.T("stray_cat") : c.Data.Name;
            catBreed.text = preset.Name;
            catStatus.text = c.StatusText;
            hungerFill.SetFill(c.Data.hunger / 100f);
            funFill.SetFill(c.Data.fun / 100f);
            energyFill.SetFill(c.Data.energy / 100f);
            happyFill.SetFill(c.Happiness / 100f);

            trustRow.SetActive(c.IsStray);
            happyRow.SetActive(!c.IsStray);
            if (c.IsStray)
            {
                trustFill.SetFill(c.Trust / 100f);
                var fav = ItemDatabase.Get(c.Data.favoriteFood);
                string likes = c.FavoriteRevealed && fav != null ? Loc.F("likes", fav.Name) : Loc.T("likes_unknown");
                string when = c.CanAdopt ? Loc.T("adopt_ready") : Loc.F("stay_time", Duration(c.StayTimeLeft));
                catExtra.text = likes + "\n" + when;
            }
            else
            {
                var fav = ItemDatabase.Get(c.Data.favoriteFood);
                catExtra.text = (fav != null ? Loc.F("likes", fav.Name) : "") + "\n" + Loc.F("earning", Mathf.RoundToInt(c.Happiness));
            }

            petButton.interactable = c.PetCooldown <= 0f;
            playButton.interactable = c.PlayCooldown <= 0f;
            adoptButton.gameObject.SetActive(c.IsStray && c.CanAdopt);
            if (c.IsStray && c.CanAdopt) adoptButton.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.03f);
            renameButton.gameObject.SetActive(!c.IsStray);
        }

        void ShowResult(CatController c, CatController.Result r)
        {
            if (c == null) return;
            string n = c.IsStray ? Loc.T("stray_cat") : c.Data.Name;
            switch (r)
            {
                case CatController.Result.Full: Toast(Loc.F("res_full", n)); break;
                case CatController.Result.Cooldown: Toast(Loc.T("res_cooldown")); break;
                case CatController.Result.Tired: Toast(Loc.F("res_tired", n)); break;
                case CatController.Result.Busy: Toast(Loc.T("res_busy")); break;
                case CatController.Result.Hiss: Toast(Loc.F("res_hiss", n)); break;
                case CatController.Result.NoFood: Toast(Loc.T("res_nofood")); break;
            }
        }

        void OnAdoptClicked()
        {
            var c = shownCat;
            if (c == null || !c.IsStray) return;
            if (GM.Cats.RoomFull)
            {
                Toast(Loc.F("room_full", CatManager.MaxCats));
                return;
            }
            if (GM.Cats.Adopt())
            {
                OnSelectionChanged();
                Popup(Loc.T("adopted_title"), Loc.F("adopted_body", c.Data.Name), GM.Icons.Cat(c.Data.preset),
                    new PopupButton(Loc.T("yay"), UIKit.Green, null));
            }
        }

        void OnRenameClicked()
        {
            var c = shownCat;
            if (c == null || c.IsStray) return;
            int next = c.Data.nameIndex;
            for (int i = 0; i < 30; i++)
            {
                next = (next + 1) % CatAppearance.NameCount;
                int candidate = next;
                if (!GM.Cats.Owned.Exists(o => o != c && o.Data.nameIndex == candidate)) break;
            }
            c.Data.nameIndex = next;
            GM.RequestSave();
            GM.Sfx.PlayMeow(c.Data.size);
        }

        // ---------------- Food picker ----------------

        void OnFeedClicked()
        {
            if (shownCat == null) return;
            CloseFoodPicker();
            foodPickerRoot = Modal(popupLayer, "FoodPicker", new Vector2(560, 560), out var panel, CloseFoodPicker);
            Title(panel, Loc.T("choose_food"));
            CloseButton(panel, CloseFoodPicker);
            var list = UIKit.Rect("List", panel).Stretch(16, 16, 16, 80);
            UIKit.VLayout(list.gameObject, 10, TextAnchor.UpperCenter, null, true, true, true, false);
            foodPickerList = list;
            RefreshFoodPicker();
        }

        void CloseFoodPicker()
        {
            if (foodPickerRoot != null) Destroy(foodPickerRoot.gameObject);
            foodPickerRoot = null;
        }

        void RefreshFoodPicker()
        {
            if (foodPickerRoot == null || shownCat == null) return;
            UIKit.ClearChildren(foodPickerList);
            var cat = shownCat;
            foreach (var food in ItemDatabase.ByCategory(ItemCategory.Food))
            {
                var f = food;
                var row = UIKit.Panel(foodPickerList, Color.white, true, f.id);
                row.Layout(-1, 100);
                var icon = UIKit.Image(row.transform, GM.Icons.Item(f), Color.white, "Icon");
                icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(88, 88));
                var name = UIKit.Label(row.transform, f.Name, 24, UIKit.Ink, TextAnchor.LowerLeft);
                name.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(104, 0), new Vector2(240, 36));
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                int count = GM.Count(f.id);
                string sub = f.Desc + "   x" + count;
                if (cat.FavoriteRevealed && cat.Data.favoriteFood == f.id) sub = Loc.T("favorite") + "  " + sub;
                var desc = UIKit.Label(row.transform, sub, 17, new Color(0.45f, 0.36f, 0.32f), TextAnchor.UpperLeft);
                desc.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 1), new Vector2(104, -2), new Vector2(260, 40));
                desc.horizontalOverflow = HorizontalWrapMode.Overflow;

                Button b;
                if (count > 0)
                {
                    b = UIKit.Button(row.transform, Loc.T("give"), UIKit.Green, () => Feed(cat, f), 22, "Give");
                }
                else
                {
                    b = UIKit.Button(row.transform, null, GM.Coins >= f.price ? UIKit.Orange : UIKit.Gray, () =>
                    {
                        if (GM.Buy(f) == GameManager.BuyResult.Ok) Feed(cat, f);
                        else Toast(Loc.T("not_enough"));
                    }, 20, "BuyGive");
                    var r = UIKit.Rect("Row", b.transform).Stretch(4, 2, 4, 2);
                    UIKit.HLayout(r.gameObject, 4, TextAnchor.MiddleCenter);
                    var t = UIKit.Label(r, Loc.T("buy_give"), 18, Color.white);
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    t.Layout(-1, 30);
                    UIKit.CoinIcon(r, 24);
                    var pt = UIKit.Label(r, f.price.ToString(), 20, Color.white);
                    pt.horizontalOverflow = HorizontalWrapMode.Overflow;
                    pt.Layout(-1, 30);
                }
                ((RectTransform)b.transform).Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(150, 64));
            }
        }

        void Feed(CatController cat, ItemDef food)
        {
            if (cat == null) { CloseFoodPicker(); return; }
            var r = cat.Feed(food);
            if (r == CatController.Result.Ok)
            {
                CloseFoodPicker();
                if (cat.IsStray && cat.Data.favoriteFood == food.id) Toast(Loc.T("stray_loves_food"));
            }
            else ShowResult(cat, r);
        }

        // ---------------- My cats list ----------------

        void ShowCatsList()
        {
            CloseCatsList();
            CloseShop();
            catsListRoot = Modal(panelLayer, "CatsList", new Vector2(900, 640), out var panel, CloseCatsList);
            Title(panel, Loc.F("my_cats_title", GM.Cats.Owned.Count, CatManager.MaxCats));
            CloseButton(panel, CloseCatsList);
            var area = UIKit.Rect("Area", panel).Stretch(16, 16, 16, 80);
            var content = UIKit.ScrollView(area, out _);
            ((RectTransform)content.parent.parent).Stretch();
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(196, 250);
            grid.spacing = new Vector2(12, 12);
            grid.padding = new RectOffset(6, 6, 6, 12);
            grid.childAlignment = TextAnchor.UpperCenter;
            UIKit.Fit(content.gameObject, false, true);

            foreach (var cat in GM.Cats.Owned)
            {
                var c = cat;
                var card = UIKit.Button(content, null, Color.white, () => { CloseCatsList(); GM.Select(c); }, 20, "Cat");
                var icon = UIKit.Image(card.transform, GM.Icons.Cat(c.Data.preset), Color.white, "Icon");
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(130, 130));
                var n = UIKit.Label(card.transform, c.Data.Name, 24, UIKit.Ink);
                n.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(186, 34));
                var b = UIKit.Label(card.transform, CatAppearance.Get(c.Data.preset).Name, 17, new Color(0.5f, 0.4f, 0.35f));
                b.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(186, 26));
                var heart = UIKit.Image(card.transform, ProcGen.Heart, UIKit.Pink, "Heart");
                heart.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(14, 14), new Vector2(28, 28));
                var bar = UIKit.Bar(card.transform, UIKit.Yellow);
                ((RectTransform)bar.transform.parent).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 16), new Vector2(132, 24));
                bar.SetFill(c.Happiness / 100f);
            }

            if (GM.Cats.Owned.Count < CatManager.MaxCats)
            {
                var hint = UIKit.Panel(content, new Color(1f, 0.95f, 0.88f), false, "Hint");
                var t = UIKit.Label(hint.transform, Loc.T("adopt_hint"), 18, new Color(0.5f, 0.4f, 0.35f));
                t.rectTransform.Stretch(10, 10, 10, 10);
            }
        }

        void CloseCatsList()
        {
            if (catsListRoot != null) Destroy(catsListRoot.gameObject);
            catsListRoot = null;
        }
    }
}
