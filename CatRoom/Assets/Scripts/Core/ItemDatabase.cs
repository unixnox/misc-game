using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    public enum ItemCategory { Furniture, Toy, Food, Wallpaper, Flooring }

    /// <summary>What a cat does when it uses a placed item.</summary>
    public enum UseKind { None, Sleep, Play, Scratch, Climb, Sit, Watch, Bowl }

    public class ItemDef
    {
        public string id;
        public string th, en;
        public string descTh, descEn;
        public ItemCategory category;
        public int price;

        // Placeable items (Furniture / Toy)
        public int w = 1, d = 1;
        public bool flat;            // rugs: other items can sit on top of them
        public int comfort;          // room comfort -> coin multiplier and stray trust
        public UseKind use;
        public float useHeight;      // where a cat sits when using it
        public float useTime = 8f;
        public float hungerRate, funRate, energyRate; // per second while used

        // Food
        public float hungerGain, funGain, trustGain;

        // Wallpaper / Flooring
        public Color colorA = Color.white, colorB = Color.white;
        public int pattern;

        public string Name => Loc.Current == Lang.TH ? th : en;
        public string Desc => Loc.Current == Lang.TH ? descTh : descEn;
        public bool Placeable => category == ItemCategory.Furniture || category == ItemCategory.Toy;
        public bool IsTheme => category == ItemCategory.Wallpaper || category == ItemCategory.Flooring;
    }

    public static class ItemDatabase
    {
        public const string DefaultWallpaper = "wall_cream";
        public const string DefaultFlooring = "floor_wood";

        public static readonly List<ItemDef> All = new List<ItemDef>();
        static readonly Dictionary<string, ItemDef> byId = new Dictionary<string, ItemDef>();

        static ItemDatabase()
        {
            // ---------- Furniture ----------
            Add(new ItemDef { id = "bed_basic", th = "เบาะนอนแมว", en = "Cat Cushion", descTh = "เบาะนุ่ม ๆ ให้แมวงีบ", descEn = "A soft cushion for naps",
                category = ItemCategory.Furniture, price = 60, comfort = 2, use = UseKind.Sleep, useHeight = 0.12f, useTime = 14f, energyRate = 3.5f });
            Add(new ItemDef { id = "bed_donut", th = "ที่นอนโดนัท", en = "Donut Bed", descTh = "นอนสบายกว่า ฟื้นพลังเร็ว", descEn = "Extra cozy, restores energy fast",
                category = ItemCategory.Furniture, price = 180, comfort = 5, use = UseKind.Sleep, useHeight = 0.14f, useTime = 16f, energyRate = 6f, funRate = 0.3f });
            Add(new ItemDef { id = "box", th = "กล่องกระดาษ", en = "Cardboard Box", descTh = "ถูกแต่แมวรักที่สุด!", descEn = "Cheap, but cats love it!",
                category = ItemCategory.Furniture, price = 20, comfort = 1, use = UseKind.Play, useHeight = 0.08f, useTime = 9f, funRate = 2.2f, energyRate = 1f });
            Add(new ItemDef { id = "food_bowl", th = "ชามอาหาร", en = "Food Bowl", descTh = "แมวจะมากินอาหารที่ชาม", descEn = "Cats come here to eat",
                category = ItemCategory.Furniture, price = 40, comfort = 1, use = UseKind.Bowl, useHeight = 0f });
            Add(new ItemDef { id = "scratch_post", th = "เสาลับเล็บ", en = "Scratching Post", descTh = "ลับเล็บให้หายเครียด", descEn = "Scratch the stress away",
                category = ItemCategory.Furniture, price = 80, comfort = 2, use = UseKind.Scratch, useHeight = 0f, useTime = 7f, funRate = 3.5f, energyRate = -0.5f });
            Add(new ItemDef { id = "cat_tree", th = "คอนโดแมว", en = "Cat Tree", descTh = "ปีนป่ายและนอนบนที่สูง", descEn = "Climb high and nap on top",
                category = ItemCategory.Furniture, price = 350, comfort = 8, use = UseKind.Climb, useHeight = 1.62f, useTime = 14f, funRate = 2.5f, energyRate = 2.5f });
            Add(new ItemDef { id = "sofa", th = "โซฟา", en = "Sofa", descTh = "ที่นั่งแสนสบายของทุกคน", descEn = "A comfy seat for everyone",
                category = ItemCategory.Furniture, price = 220, w = 2, comfort = 6, use = UseKind.Sit, useHeight = 0.5f, useTime = 12f, energyRate = 2.5f, funRate = 0.5f });
            Add(new ItemDef { id = "plant", th = "ต้นไม้กระถาง", en = "Potted Plant", descTh = "เพิ่มความสดชื่นให้ห้อง", descEn = "Freshens up the room",
                category = ItemCategory.Furniture, price = 50, comfort = 3 });
            Add(new ItemDef { id = "lamp", th = "โคมไฟตั้งพื้น", en = "Floor Lamp", descTh = "แสงอุ่น ๆ ชวนผ่อนคลาย", descEn = "Warm, relaxing light",
                category = ItemCategory.Furniture, price = 70, comfort = 3 });
            Add(new ItemDef { id = "bookshelf", th = "ชั้นหนังสือ", en = "Bookshelf", descTh = "แมวชอบนั่งดูจากที่สูง", descEn = "Cats like to watch from up high",
                category = ItemCategory.Furniture, price = 150, w = 2, comfort = 4, use = UseKind.Watch, useHeight = 1.42f, useTime = 10f, funRate = 1.5f, energyRate = 1f });
            Add(new ItemDef { id = "fish_tank", th = "ตู้ปลา", en = "Fish Tank", descTh = "ดูปลาว่ายได้ทั้งวัน", descEn = "Fish-watching all day long",
                category = ItemCategory.Furniture, price = 400, comfort = 10, use = UseKind.Watch, useHeight = 0f, useTime = 12f, funRate = 3f });
            Add(new ItemDef { id = "rug_round", th = "พรมกลม", en = "Round Rug", descTh = "วางของทับได้ แมวชอบกลิ้ง", descEn = "Furniture can go on top",
                category = ItemCategory.Furniture, price = 90, w = 2, d = 2, flat = true, comfort = 4 });
            Add(new ItemDef { id = "rug_fish", th = "พรมลายปลา", en = "Fish Rug", descTh = "พรมรูปปลาตัวโต", descEn = "A big fish-shaped rug",
                category = ItemCategory.Furniture, price = 120, w = 2, d = 1, flat = true, comfort = 4 });

            // ---------- Toys ----------
            Add(new ItemDef { id = "toy_ball", th = "ลูกบอลไหมพรม", en = "Yarn Ball", descTh = "กลิ้งไปกลิ้งมาสนุกสุด ๆ", descEn = "Roll it around!",
                category = ItemCategory.Toy, price = 25, comfort = 1, use = UseKind.Play, useHeight = 0f, useTime = 7f, funRate = 3f, energyRate = -0.6f });
            Add(new ItemDef { id = "toy_mouse", th = "หนูของเล่น", en = "Toy Mouse", descTh = "ล่าเหยื่อแบบปลอดภัย", descEn = "Safe hunting practice",
                category = ItemCategory.Toy, price = 35, comfort = 1, use = UseKind.Play, useHeight = 0f, useTime = 7f, funRate = 3.5f, energyRate = -0.6f });
            Add(new ItemDef { id = "toy_tunnel", th = "อุโมงค์แมว", en = "Cat Tunnel", descTh = "มุดเข้ามุดออกไม่เบื่อ", descEn = "In and out, never bored",
                category = ItemCategory.Toy, price = 120, w = 2, comfort = 3, use = UseKind.Play, useHeight = 0f, useTime = 9f, funRate = 4f, energyRate = -0.4f });
            Add(new ItemDef { id = "toy_wand", th = "ไม้ตกแมว", en = "Feather Wand", descTh = "ขนนกพลิ้ว ๆ ล่อแมว", descEn = "Fluttery feather teaser",
                category = ItemCategory.Toy, price = 60, comfort = 2, use = UseKind.Play, useHeight = 0f, useTime = 8f, funRate = 4.5f, energyRate = -0.8f });

            // ---------- Food ----------
            Add(new ItemDef { id = "food_dry", th = "อาหารเม็ด", en = "Dry Food", descTh = "อิ่ม +35", descEn = "Fullness +35",
                category = ItemCategory.Food, price = 10, hungerGain = 35, funGain = 0, trustGain = 12 });
            Add(new ItemDef { id = "food_wet", th = "อาหารเปียก", en = "Wet Food", descTh = "อิ่ม +50 สนุก +5", descEn = "Fullness +50, Fun +5",
                category = ItemCategory.Food, price = 20, hungerGain = 50, funGain = 5, trustGain = 16 });
            Add(new ItemDef { id = "food_fish", th = "ปลาทู", en = "Mackerel", descTh = "อิ่ม +60 สนุก +10", descEn = "Fullness +60, Fun +10",
                category = ItemCategory.Food, price = 30, hungerGain = 60, funGain = 10, trustGain = 20 });
            Add(new ItemDef { id = "food_treat", th = "ขนมแมวเลีย", en = "Lickable Treat", descTh = "อิ่ม +15 สนุก +25", descEn = "Fullness +15, Fun +25",
                category = ItemCategory.Food, price = 15, hungerGain = 15, funGain = 25, trustGain = 18 });

            // ---------- Wallpaper (pattern: 0 plain, 1 stripes, 2 dots, 3 hearts-ish checks) ----------
            Add(Theme("wall_cream", "วอลเปเปอร์ครีม", "Cream Wallpaper", ItemCategory.Wallpaper, 0, new Color(1f, 0.93f, 0.82f), new Color(0.98f, 0.88f, 0.76f), 0));
            Add(Theme("wall_mint", "ลายทางมินต์", "Mint Stripes", ItemCategory.Wallpaper, 100, new Color(0.78f, 0.95f, 0.88f), new Color(0.66f, 0.89f, 0.8f), 1));
            Add(Theme("wall_pink", "จุดชมพู", "Pink Polka", ItemCategory.Wallpaper, 120, new Color(1f, 0.85f, 0.88f), new Color(1f, 0.97f, 0.98f), 2));
            Add(Theme("wall_sky", "ฟ้าลายตาราง", "Sky Plaid", ItemCategory.Wallpaper, 150, new Color(0.78f, 0.88f, 1f), new Color(0.66f, 0.79f, 0.97f), 3));
            Add(Theme("wall_lemon", "ลายทางเลมอน", "Lemon Stripes", ItemCategory.Wallpaper, 150, new Color(1f, 0.96f, 0.7f), new Color(1f, 0.9f, 0.55f), 1));

            // ---------- Flooring (pattern: 0 planks, 1 checker, 2 carpet, 3 tatami) ----------
            Add(Theme("floor_wood", "พื้นไม้", "Wood Floor", ItemCategory.Flooring, 0, new Color(0.87f, 0.66f, 0.45f), new Color(0.78f, 0.56f, 0.37f), 0));
            Add(Theme("floor_checker", "กระเบื้องหมากรุก", "Checker Tiles", ItemCategory.Flooring, 150, new Color(1f, 0.97f, 0.92f), new Color(0.98f, 0.72f, 0.66f), 1));
            Add(Theme("floor_carpet", "พรมขนนุ่ม", "Fluffy Carpet", ItemCategory.Flooring, 120, new Color(0.8f, 0.74f, 0.95f), new Color(0.74f, 0.68f, 0.9f), 2));
            Add(Theme("floor_tatami", "เสื่อทาทามิ", "Tatami Mats", ItemCategory.Flooring, 200, new Color(0.84f, 0.85f, 0.6f), new Color(0.6f, 0.5f, 0.32f), 3));
        }

        static ItemDef Theme(string id, string th, string en, ItemCategory cat, int price, Color a, Color b, int pattern)
        {
            return new ItemDef { id = id, th = th, en = en, descTh = cat == ItemCategory.Wallpaper ? "เปลี่ยนลายผนังห้อง" : "เปลี่ยนพื้นห้อง",
                descEn = cat == ItemCategory.Wallpaper ? "Change the walls" : "Change the floor",
                category = cat, price = price, colorA = a, colorB = b, pattern = pattern };
        }

        static void Add(ItemDef def)
        {
            All.Add(def);
            byId[def.id] = def;
        }

        public static ItemDef Get(string id)
        {
            if (id == null) return null;
            byId.TryGetValue(id, out var def);
            return def;
        }

        public static List<ItemDef> ByCategory(ItemCategory cat)
        {
            var list = new List<ItemDef>();
            foreach (var d in All) if (d.category == cat) list.Add(d);
            return list;
        }
    }
}
