using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatRoom
{
    public partial class GameUI
    {
        public struct PopupButton
        {
            public string label;
            public Color color;
            public Action action;

            public PopupButton(string label, Color color, Action action)
            {
                this.label = label;
                this.color = color;
                this.action = action;
            }
        }

        public void Popup(string title, string body, Sprite image, params PopupButton[] buttons)
        {
            RectTransform dim = null;
            float height = image != null ? 520 : 400;
            dim = Modal(popupLayer, "Popup", new Vector2(620, height), out var panel);
            UIKit.VLayout(panel.gameObject, 10, TextAnchor.UpperCenter, new RectOffset(28, 28, 24, 24), true, true, true, false);

            var t = UIKit.Label(panel, title, 34, UIKit.Ink);
            t.Layout(-1, 50);
            if (image != null)
            {
                var img = UIKit.Image(panel, image, Color.white, "Image");
                img.Layout(-1, 150);
            }
            var b = UIKit.Label(panel, body, 22, new Color(0.42f, 0.32f, 0.28f));
            b.Layout(-1, -1, -1, 1);

            var row = UIKit.Rect("Buttons", panel);
            row.Layout(-1, 64);
            UIKit.HLayout(row.gameObject, 12, TextAnchor.MiddleCenter, null, true, true, false, true);
            foreach (var pb in buttons)
            {
                var a = pb.action;
                var btn = UIKit.Button(row, pb.label, pb.color, () =>
                {
                    if (dim != null) Destroy(dim.gameObject);
                    a?.Invoke();
                }, 24, "Btn");
                btn.Layout(200, 60);
            }
        }

        public void ShowTutorial()
        {
            Popup(Loc.T("tutorial_title"), Loc.T("tutorial_body"), GM.Icons.Cat(0),
                new PopupButton(Loc.T("lets_go"), UIKit.Green, () => GM.CompleteTutorial()));
        }

        public void ShowWelcomeBack(long earned, float seconds)
        {
            Popup(Loc.T("welcome_back"), Loc.F("welcome_body", UIKit.Money(earned), Duration(seconds)), GM.Icons.Item(ItemDatabase.Get("bed_donut")),
                new PopupButton(Loc.T("yay"), UIKit.Green, null));
        }

        void ShowSettings()
        {
            CloseShop();
            CloseCatsList();
            RectTransform dim = null;
            dim = Modal(popupLayer, "Settings", new Vector2(520, 460), out var panel, () => { if (dim != null) Destroy(dim.gameObject); });
            UIKit.VLayout(panel.gameObject, 14, TextAnchor.UpperCenter, new RectOffset(32, 32, 24, 28), true, true, true, false);
            var title = UIKit.Label(panel, Loc.T("settings_title"), 34, UIKit.Ink);
            title.Layout(-1, 50);

            UIKit.Button(panel, Loc.T("language_toggle"), UIKit.Blue, () =>
            {
                Destroy(dim.gameObject);
                GM.SetLanguage(Loc.Current == Lang.TH ? Lang.EN : Lang.TH);
            }, 24, "Language").Layout(-1, 64);

            Button sound = null;
            sound = UIKit.Button(panel, GM.Data.soundOn ? Loc.T("sound_on") : Loc.T("sound_off"), UIKit.Orange, () =>
            {
                GM.SetSound(!GM.Data.soundOn);
                sound.ButtonLabel().text = GM.Data.soundOn ? Loc.T("sound_on") : Loc.T("sound_off");
            }, 24, "Sound");
            sound.Layout(-1, 64);

            UIKit.Button(panel, Loc.T("reset"), UIKit.Gray, () =>
            {
                Destroy(dim.gameObject);
                Popup(Loc.T("reset"), Loc.T("reset_confirm"), null,
                    new PopupButton(Loc.T("no"), UIKit.Green, null),
                    new PopupButton(Loc.T("yes"), UIKit.Pink, () => GM.ResetGame()));
            }, 22, "Reset").Layout(-1, 56);

            UIKit.Button(panel, Loc.T("close"), UIKit.Green, () => Destroy(dim.gameObject), 24, "Close").Layout(-1, 60);
        }
    }
}
