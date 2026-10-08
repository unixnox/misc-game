using UnityEngine;

namespace CatRoom
{
    /// <summary>Fur presets (including Thai breeds) and the list of cat names.</summary>
    public class CatPreset
    {
        public string th, en;
        public Color body, head, legs, tail, tailTip, muzzle, ears;
        public Color eyeL, eyeR;
        public Color patch;          // patch over one eye/ear (alpha 0 = none)
        public int weight;           // spawn weight for strays

        public string Name => Loc.Current == Lang.TH ? th : en;
    }

    public static class CatAppearance
    {
        static readonly Color Amber = new Color(0.98f, 0.75f, 0.2f);
        static readonly Color Green = new Color(0.45f, 0.82f, 0.38f);
        static readonly Color Blue = new Color(0.35f, 0.62f, 0.98f);

        public static readonly CatPreset[] Presets =
        {
            P("ส้ม", "Orange", C(1f, 0.66f, 0.3f), C(1f, 0.66f, 0.3f), C(1f, 0.92f, 0.82f), C(0.95f, 0.55f, 0.22f), C(1f, 0.92f, 0.82f), C(1f, 0.95f, 0.88f), Amber, Amber, Color.clear, 10),
            P("เทาลายเสือ", "Gray Tabby", C(0.66f, 0.66f, 0.7f), C(0.6f, 0.6f, 0.65f), C(0.85f, 0.85f, 0.88f), C(0.45f, 0.45f, 0.5f), C(0.35f, 0.35f, 0.4f), C(0.95f, 0.95f, 0.95f), Green, Green, Color.clear, 10),
            P("ดำ", "Black", C(0.2f, 0.19f, 0.23f), C(0.2f, 0.19f, 0.23f), C(0.2f, 0.19f, 0.23f), C(0.2f, 0.19f, 0.23f), C(0.2f, 0.19f, 0.23f), C(0.28f, 0.27f, 0.31f), Amber, Amber, Color.clear, 8),
            P("ทักซิโด้", "Tuxedo", C(0.22f, 0.21f, 0.25f), C(0.22f, 0.21f, 0.25f), C(1f, 1f, 1f), C(0.22f, 0.21f, 0.25f), C(1f, 1f, 1f), C(1f, 1f, 1f), Green, Green, Color.clear, 8),
            P("สามสี", "Calico", C(1f, 0.98f, 0.95f), C(1f, 0.98f, 0.95f), C(1f, 0.98f, 0.95f), C(0.25f, 0.22f, 0.24f), C(0.98f, 0.62f, 0.28f), C(1f, 1f, 1f), Amber, Amber, C(0.98f, 0.62f, 0.28f), 7),
            P("วิเชียรมาศ", "Siamese", C(0.98f, 0.93f, 0.82f), C(0.98f, 0.93f, 0.82f), C(0.36f, 0.26f, 0.22f), C(0.36f, 0.26f, 0.22f), C(0.3f, 0.21f, 0.18f), C(0.42f, 0.31f, 0.26f), Blue, Blue, Color.clear, 4),
            P("สีสวาด (โคราช)", "Korat", C(0.56f, 0.62f, 0.72f), C(0.56f, 0.62f, 0.72f), C(0.56f, 0.62f, 0.72f), C(0.5f, 0.56f, 0.66f), C(0.62f, 0.68f, 0.78f), C(0.66f, 0.72f, 0.8f), Green, Green, Color.clear, 3),
            P("ขาวมณี", "Khao Manee", C(1f, 1f, 1f), C(1f, 1f, 1f), C(1f, 1f, 1f), C(1f, 1f, 1f), C(1f, 1f, 1f), C(1f, 1f, 1f), Blue, Amber, Color.clear, 2),
            P("ครีม", "Cream", C(1f, 0.88f, 0.7f), C(1f, 0.88f, 0.7f), C(1f, 0.95f, 0.86f), C(0.96f, 0.8f, 0.6f), C(1f, 0.95f, 0.86f), C(1f, 0.97f, 0.92f), Amber, Amber, Color.clear, 7),
        };

        static readonly string[,] Names =
        {
            { "มะลิ", "Mali" }, { "ส้มโอ", "Pomelo" }, { "ขนมปัง", "Bun" }, { "โมจิ", "Mochi" }, { "ถั่วแดง", "Red Bean" },
            { "ทองหยิบ", "Thong Yip" }, { "ลูกชิ้น", "Meatball" }, { "ข้าวปั้น", "Onigiri" }, { "นมสด", "Milky" }, { "เต้าหู้", "Tofu" },
            { "มะม่วง", "Mango" }, { "ชาไทย", "Thai Tea" }, { "บราวนี่", "Brownie" }, { "คุกกี้", "Cookie" }, { "ซูชิ", "Sushi" },
            { "พุดดิ้ง", "Pudding" }, { "ลาเต้", "Latte" }, { "งาดำ", "Sesame" }, { "ข้าวเหนียว", "Sticky Rice" }, { "ทาร์ต", "Tart" },
            { "ขนมชั้น", "Layer Cake" }, { "เมฆ", "Cloud" }, { "ถุงเท้า", "Socks" }, { "ปุยฝ้าย", "Cotton" }, { "ทับทิม", "Ruby" },
        };

        public static int NameCount => Names.GetLength(0);

        public static string CatName(int index)
        {
            index = Mathf.Clamp(index, 0, NameCount - 1);
            return Names[index, Loc.Current == Lang.TH ? 0 : 1];
        }

        public static CatPreset Get(int index) => Presets[Mathf.Clamp(index, 0, Presets.Length - 1)];

        public static int RandomPreset(System.Random rng)
        {
            int total = 0;
            foreach (var p in Presets) total += p.weight;
            int r = rng.Next(total);
            for (int i = 0; i < Presets.Length; i++)
            {
                r -= Presets[i].weight;
                if (r < 0) return i;
            }
            return 0;
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);

        static CatPreset P(string th, string en, Color body, Color head, Color legs, Color tail, Color tailTip, Color muzzle,
            Color eyeL, Color eyeR, Color patch, int weight)
        {
            return new CatPreset { th = th, en = en, body = body, head = head, legs = legs, tail = tail, tailTip = tailTip,
                muzzle = muzzle, ears = tail, eyeL = eyeL, eyeR = eyeR, patch = patch, weight = weight };
        }
    }
}
