using UnityEngine;
using UnityEngine.UI;

namespace CatRoom
{
    public partial class GameUI
    {
        GameObject decorateBar;
        RectTransform decorateList;
        GameObject decorateActions, decorateBag;
        Text decorateHint;
        Button storeButton;

        void BuildDecorateBar()
        {
            var size = CanvasSize;
            var bar = UIKit.Panel(hudLayer, UIKit.Paper, true, "DecorateBar");
            decorateBar = bar.gameObject;
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            float side = IsPortrait ? 10f : Mathf.Max(16f, (size.x - 1100f) / 2f);
            rt.offsetMin = new Vector2(side, 10);
            rt.offsetMax = new Vector2(-side, 10 + 236);

            // header: title + hint + done
            var title = UIKit.Label(rt, Loc.T("decor_title"), 28, UIKit.Ink, TextAnchor.MiddleLeft, "Title");
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -6), new Vector2(260, 44));
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            decorateHint = UIKit.Label(rt, "", 19, new Color(0.45f, 0.36f, 0.32f), TextAnchor.MiddleLeft, "Hint");
            var hrt = decorateHint.rectTransform;
            hrt.anchorMin = new Vector2(0, 1);
            hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0, 1);
            hrt.offsetMin = new Vector2(IsPortrait ? 20 : 250, -50);
            hrt.offsetMax = new Vector2(-150, -6);
            if (IsPortrait)
            {
                hrt.offsetMin = new Vector2(20, -82);
                hrt.offsetMax = new Vector2(-150, -44);
            }

            var done = UIKit.Button(rt, Loc.T("done"), UIKit.Green, () => GM.Placement.ExitEdit(), 24, "Done");
            ((RectTransform)done.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -8), new Vector2(124, 52));

            float top = IsPortrait ? 86f : 58f;

            // bag (horizontal list of owned placeables)
            var bag = UIKit.Rect("Bag", rt).Stretch(12, 10, 12, top);
            decorateBag = bag.gameObject;
            decorateList = UIKit.ScrollView(bag, out _, true);
            ((RectTransform)decorateList.parent.parent).Stretch();
            UIKit.HLayout(decorateList.gameObject, 10, TextAnchor.MiddleLeft, new RectOffset(4, 4, 4, 4), true, true, false, true);
            UIKit.Fit(decorateList.gameObject, true, false);

            // actions while an item is held
            var actions = UIKit.Rect("Actions", rt).Stretch(12, 14, 12, top + 6);
            decorateActions = actions.gameObject;
            UIKit.HLayout(actions.gameObject, 12, TextAnchor.MiddleCenter, null, true, true, false, false);
            int fs = IsPortrait ? 20 : 24;
            float bw = IsPortrait ? 160 : 190;
            UIKit.Button(actions, Loc.T("rotate"), UIKit.Blue, () => GM.Placement.Rotate(), fs, "Rotate").Layout(bw, 80);
            storeButton = UIKit.Button(actions, Loc.T("store"), UIKit.Orange, () => GM.Placement.StoreMoving(), fs, "Store");
            storeButton.Layout(bw, 80);
            UIKit.Button(actions, Loc.T("place_here"), UIKit.Green, () => GM.Placement.ConfirmHere(), fs, "Place").Layout(bw, 80);
            UIKit.Button(actions, Loc.T("cancel"), UIKit.Gray, () => GM.Placement.CancelGhost(), fs, "Cancel").Layout(IsPortrait ? 120 : 150, 80);

            decorateBar.SetActive(false);
        }

        void RefreshDecorate()
        {
            if (decorateBar == null || GM.Placement == null) return;
            var pc = GM.Placement;
            decorateBar.SetActive(pc.EditMode);
            if (!pc.EditMode) return;

            CloseShop();
            CloseCatsList();
            CloseFoodPicker();

            bool holding = pc.HasGhost;
            decorateActions.SetActive(holding);
            decorateBag.SetActive(!holding);
            storeButton.gameObject.SetActive(pc.MovingExisting);
            decorateHint.text = holding ? Loc.T("decor_place_hint") : Loc.T("decor_hint");
            if (holding) return;

            UIKit.ClearChildren(decorateList);
            int shown = 0;
            foreach (var def in ItemDatabase.All)
            {
                if (!def.Placeable) continue;
                int n = GM.Count(def.id);
                if (n <= 0) continue;
                shown++;
                var d = def;
                var card = UIKit.Button(decorateList, null, Color.white, () => GM.Placement.BeginPlaceNew(d), 20, def.id);
                card.Layout(130, 150);
                var icon = UIKit.Image(card.transform, GM.Icons.Item(def), Color.white, "Icon");
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(104, 104));
                var name = UIKit.Label(card.transform, def.Name, 17, UIKit.Ink);
                name.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 4), new Vector2(124, 40));
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 11;
                name.resizeTextMaxSize = 17;
                var badge = UIKit.Panel(card.transform, UIKit.Orange, false, "Count");
                badge.raycastTarget = false;
                badge.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-4, -4), new Vector2(46, 30));
                UIKit.Label(badge.transform, "x" + n, 18, Color.white).rectTransform.Stretch();
            }

            // shortcut to the shop
            var more = UIKit.Button(decorateList, Loc.T("buy_more"), UIKit.Orange, () =>
            {
                GM.Placement.ExitEdit();
                ShowShop(ItemCategory.Furniture);
            }, 20, "BuyMore");
            more.Layout(130, 150);

            if (shown == 0) decorateHint.text = Loc.T("bag_empty");
        }
    }
}
