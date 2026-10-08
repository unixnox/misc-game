using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatRoom
{
    public partial class GameUI
    {
        static readonly ItemCategory[] ShopTabs = { ItemCategory.Furniture, ItemCategory.Toy, ItemCategory.Food, ItemCategory.Wallpaper, ItemCategory.Flooring };

        RectTransform shopRoot, shopGrid;
        ScrollRect shopScroll;
        ItemCategory shopTab;
        Text shopCoins;
        readonly List<Button> shopTabButtons = new List<Button>();

        static string TabKey(ItemCategory c)
        {
            switch (c)
            {
                case ItemCategory.Furniture: return "tab_furniture";
                case ItemCategory.Toy: return "tab_toys";
                case ItemCategory.Food: return "tab_food";
                case ItemCategory.Wallpaper: return "tab_wall";
                default: return "tab_floor";
            }
        }

        public void ShowShop(ItemCategory tab)
        {
            CloseShop();
            CloseCatsList();
            shopTab = tab;
            shopRoot = Modal(panelLayer, "Shop", new Vector2(1120, 700), out var panel);
            Title(panel, Loc.T("shop"));
            CloseButton(panel, CloseShop);

            var coinBox = UIKit.Panel(panel, UIKit.PaperDark, false, "Coins");
            coinBox.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -14), new Vector2(190, 52));
            var row = UIKit.Rect("Row", coinBox.rectTransform).Stretch(10, 4, 10, 4);
            UIKit.HLayout(row.gameObject, 6, TextAnchor.MiddleRight);
            UIKit.CoinIcon(row, 34);
            shopCoins = UIKit.Label(row, UIKit.Money(GM.Coins), 26, UIKit.Ink, TextAnchor.MiddleRight);
            shopCoins.horizontalOverflow = HorizontalWrapMode.Overflow;
            shopCoins.Layout(-1, 40);

            var tabs = UIKit.Rect("Tabs", panel);
            tabs.anchorMin = new Vector2(0, 1);
            tabs.anchorMax = new Vector2(1, 1);
            tabs.pivot = new Vector2(0.5f, 1);
            tabs.offsetMin = new Vector2(16, -136);
            tabs.offsetMax = new Vector2(-16, -78);
            UIKit.HLayout(tabs.gameObject, 8, TextAnchor.MiddleCenter, null, true, true, true, true);
            shopTabButtons.Clear();
            foreach (var cat in ShopTabs)
            {
                var c = cat;
                var b = UIKit.Button(tabs, Loc.T(TabKey(c)), UIKit.Gray, () => { shopTab = c; RefreshShop(); shopScroll.verticalNormalizedPosition = 1f; },
                    IsPortrait ? 19 : 22, "Tab");
                shopTabButtons.Add(b);
            }

            var area = UIKit.Rect("Area", panel).Stretch(16, 16, 16, 146);
            shopGrid = UIKit.ScrollView(area, out shopScroll);
            ((RectTransform)shopGrid.parent.parent).Stretch();
            var grid = shopGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(196, 292);
            grid.spacing = new Vector2(12, 12);
            grid.padding = new RectOffset(6, 6, 6, 12);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            UIKit.Fit(shopGrid.gameObject, false, true);

            RefreshShop();
        }

        void CloseShop()
        {
            if (shopRoot != null) Destroy(shopRoot.gameObject);
            shopRoot = null;
        }

        void RefreshShop()
        {
            if (shopRoot == null) return;
            for (int i = 0; i < ShopTabs.Length && i < shopTabButtons.Count; i++)
                ((Image)shopTabButtons[i].targetGraphic).color = ShopTabs[i] == shopTab ? UIKit.Orange : new Color(0.84f, 0.76f, 0.68f);
            shopCoins.text = UIKit.Money(GM.Coins);

            UIKit.ClearChildren(shopGrid);
            foreach (var def in ItemDatabase.ByCategory(shopTab)) ShopCard(def);
        }

        int PlacedCount(string id)
        {
            int n = 0;
            foreach (var it in GM.Room.Items) if (it.def.id == id) n++;
            return n;
        }

        void ShopCard(ItemDef def)
        {
            var card = UIKit.Panel(shopGrid, Color.white, true, def.id);
            UIKit.VLayout(card.gameObject, 2, TextAnchor.UpperCenter, new RectOffset(10, 10, 8, 10), true, true, true, false);

            var iconBg = UIKit.Panel(card.transform, new Color(1f, 0.95f, 0.88f), false, "IconBg");
            iconBg.raycastTarget = false;
            iconBg.Layout(-1, 132);
            var icon = UIKit.Image(iconBg.transform, GM.Icons.Item(def), Color.white, "Icon");
            icon.rectTransform.Stretch(6, 4, 6, 4);

            var name = UIKit.Label(card.transform, def.Name, 22, UIKit.Ink);
            name.Layout(-1, 32);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 14;
            name.resizeTextMaxSize = 22;

            string info = def.Desc;
            if (def.Placeable && def.comfort > 0) info += "\n" + Loc.F("comfort_plus", def.comfort);
            var desc = UIKit.Label(card.transform, info, 16, new Color(0.45f, 0.36f, 0.32f));
            desc.Layout(-1, 46);
            desc.resizeTextForBestFit = true;
            desc.resizeTextMinSize = 11;
            desc.resizeTextMaxSize = 16;

            if (def.IsTheme && GM.OwnsTheme(def.id))
            {
                bool inUse = GM.Data.wallpaper == def.id || GM.Data.flooring == def.id;
                var use = UIKit.Button(card.transform, inUse ? Loc.T("in_use") : Loc.T("use"), inUse ? UIKit.Gray : UIKit.Blue, () =>
                {
                    GM.ApplyTheme(def);
                    RefreshShop();
                }, 22, "Use");
                use.interactable = !inUse;
                use.Layout(-1, 50);
            }
            else
            {
                bool afford = GM.Coins >= def.price;
                var buy = UIKit.Button(card.transform, null, afford ? UIKit.Green : UIKit.Gray, () => OnBuy(def), 22, "Buy");
                buy.Layout(-1, 50);
                var row = UIKit.Rect("Row", buy.transform).Stretch(6, 4, 6, 4);
                UIKit.HLayout(row.gameObject, 6, TextAnchor.MiddleCenter);
                UIKit.CoinIcon(row, 30);
                var price = UIKit.Label(row, def.price.ToString(), 24, Color.white);
                price.Outlined(new Color(0.3f, 0.2f, 0.15f, 0.6f), 1.5f);
                price.horizontalOverflow = HorizontalWrapMode.Overflow;
                price.Layout(-1, 40);
            }

            if (!def.IsTheme)
            {
                int owned = GM.Count(def.id) + PlacedCount(def.id);
                if (owned > 0)
                {
                    var badge = UIKit.Panel(card.transform, UIKit.Orange, false, "Owned");
                    badge.raycastTarget = false;
                    badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                    badge.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-6, -6), new Vector2(76, 32));
                    var bt = UIKit.Label(badge.transform, Loc.F("owned_n", owned), 18, Color.white);
                    bt.rectTransform.Stretch();
                }
            }
        }

        void OnBuy(ItemDef def)
        {
            var r = GM.Buy(def);
            switch (r)
            {
                case GameManager.BuyResult.NotEnough:
                    Toast(Loc.T("not_enough"));
                    break;
                case GameManager.BuyResult.Ok:
                    if (def.Placeable) Toast(Loc.F("bought_place", def.Name));
                    else if (def.IsTheme) Toast(Loc.F("bought_theme", def.Name));
                    else Toast(Loc.F("bought", def.Name));
                    RefreshShop();
                    break;
            }
        }
    }
}
