using System.Collections.Generic;

namespace CatRoom
{
    public enum Lang { TH = 0, EN = 1 }

    /// <summary>Thai / English strings. Missing keys fall back to the key itself.</summary>
    public static class Loc
    {
        public static Lang Current = Lang.TH;

        public static string T(string key)
        {
            if (Table.TryGetValue(key, out var v)) return v[(int)Current];
            return key;
        }

        public static string F(string key, params object[] args)
        {
            try { return string.Format(T(key), args); }
            catch (System.FormatException) { return T(key); }
        }

        static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // HUD
            { "shop", new[] { "ร้านค้า", "Shop" } },
            { "decorate", new[] { "ตกแต่ง", "Decorate" } },
            { "my_cats", new[] { "แมวของฉัน", "My Cats" } },
            { "settings", new[] { "ตั้งค่า", "Settings" } },
            { "comfort", new[] { "สบาย {0} (x{1})", "Comfort {0} (x{1})" } },
            { "stray_visit", new[] { "แมวจรมาเยี่ยม!", "A stray is here!" } },
            { "stray_arrived", new[] { "มีแมวจรแวะมาที่ประตู! ลองให้อาหารหรือเล่นด้วยดูสิ", "A stray cat came to the door! Try feeding or playing with it." } },
            { "stray_left", new[] { "แมวจรเดินจากไปแล้ว... ครั้งหน้าลองหาอาหารที่มันชอบนะ", "The stray wandered off... Next time, find its favorite food!" } },
            { "stray_cat", new[] { "แมวจร", "Stray Cat" } },
            { "stray_loves_food", new[] { "เมี๊ยว~ ของโปรดเลย! ความไว้ใจเพิ่มขึ้นสองเท่า", "Meow~ its favorite! Trust doubled." } },

            // Shop
            { "tab_furniture", new[] { "เฟอร์นิเจอร์", "Furniture" } },
            { "tab_toys", new[] { "ของเล่น", "Toys" } },
            { "tab_food", new[] { "อาหาร", "Food" } },
            { "tab_wall", new[] { "วอลเปเปอร์", "Wallpaper" } },
            { "tab_floor", new[] { "พื้นห้อง", "Flooring" } },
            { "use", new[] { "ใช้", "Use" } },
            { "in_use", new[] { "ใช้อยู่", "In use" } },
            { "owned_n", new[] { "มี {0}", "x{0}" } },
            { "comfort_plus", new[] { "ความสบาย +{0}", "Comfort +{0}" } },
            { "not_enough", new[] { "เหรียญไม่พอ! ดูแลแมวให้มีความสุขเพื่อหาเหรียญเพิ่มนะ", "Not enough coins! Keep your cats happy to earn more." } },
            { "bought", new[] { "ซื้อ {0} แล้ว!", "Bought {0}!" } },
            { "bought_place", new[] { "ซื้อ {0} แล้ว! กด \"ตกแต่ง\" เพื่อวางในห้อง", "Bought {0}! Tap \"Decorate\" to place it." } },
            { "bought_theme", new[] { "เปลี่ยนเป็น {0} แล้ว!", "Switched to {0}!" } },

            // Decorate
            { "decor_title", new[] { "โหมดตกแต่ง", "Decorating" } },
            { "decor_hint", new[] { "เลือกของจากกระเป๋า หรือแตะของในห้องเพื่อย้าย", "Pick an item from your bag, or tap one in the room to move it." } },
            { "decor_place_hint", new[] { "แตะหรือลากบนพื้นเพื่อวาง (สีเขียว = วางได้)", "Tap or drag on the floor to place (green = OK)." } },
            { "rotate", new[] { "หมุน", "Rotate" } },
            { "store", new[] { "เก็บเข้ากระเป๋า", "Store" } },
            { "place_here", new[] { "วางตรงนี้", "Place" } },
            { "cancel", new[] { "ยกเลิก", "Cancel" } },
            { "done", new[] { "เสร็จ", "Done" } },
            { "buy_more", new[] { "ซื้อเพิ่ม", "Buy more" } },
            { "bag_empty", new[] { "กระเป๋าว่าง ไปซื้อของที่ร้านค้ากัน!", "Your bag is empty. Visit the shop!" } },

            // Cat panel
            { "hunger", new[] { "อิ่มท้อง", "Fullness" } },
            { "fun", new[] { "สนุก", "Fun" } },
            { "energy", new[] { "พลังงาน", "Energy" } },
            { "happiness", new[] { "ความสุข", "Happiness" } },
            { "trust", new[] { "ไว้ใจ", "Trust" } },
            { "feed", new[] { "ให้อาหาร", "Feed" } },
            { "pet", new[] { "ลูบหัว", "Pet" } },
            { "play", new[] { "เล่นด้วย", "Play" } },
            { "adopt", new[] { "รับเลี้ยง!", "Adopt!" } },
            { "rename", new[] { "เปลี่ยนชื่อ", "Rename" } },
            { "likes", new[] { "ของโปรด: {0}", "Favorite: {0}" } },
            { "likes_unknown", new[] { "ของโปรด: ??? (ลองให้อาหารหลาย ๆ แบบ)", "Favorite: ??? (try different foods)" } },
            { "stay_time", new[] { "จะอยู่อีก {0}", "Leaving in {0}" } },
            { "adopt_ready", new[] { "ไว้ใจเราแล้ว! รับมาเลี้ยงได้เลย", "It trusts you now! You can adopt it." } },
            { "earning", new[] { "ความสุข {0}% = ยิ่งสุขยิ่งได้เหรียญ", "Happiness {0}% = more coins" } },
            { "room_full", new[] { "ห้องเต็มแล้ว (เลี้ยงได้สูงสุด {0} ตัว)", "The room is full (max {0} cats)." } },
            { "adopted_title", new[] { "ยินดีด้วย!", "Congratulations!" } },
            { "adopted_body", new[] { "{0} มาเป็นสมาชิกใหม่ของบ้านแล้ว\nดูแลให้มีความสุขนะ", "{0} is now part of the family.\nTake good care of it!" } },
            { "adopt_hint", new[] { "แมวจรจะแวะมาเป็นระยะ ทำให้มันไว้ใจเพื่อรับมาเลี้ยง", "Strays visit from time to time. Earn their trust to adopt them." } },
            { "my_cats_title", new[] { "แมวของฉัน ({0}/{1})", "My Cats ({0}/{1})" } },

            // Status
            { "st_sleep", new[] { "หลับปุ๋ย Zzz", "Sleeping Zzz" } },
            { "st_eat", new[] { "กินอย่างเอร็ดอร่อย", "Munching happily" } },
            { "st_purr", new[] { "ครืดคราด~ มีความสุข", "Purring~" } },
            { "st_play", new[] { "เล่นสนุกสุด ๆ", "Having fun" } },
            { "st_hiss", new[] { "ขู่ฟ่อ! ยังไม่ไว้ใจ", "Hiss! Not ready yet" } },
            { "st_using", new[] { "อยู่กับ{0}", "Enjoying the {0}" } },
            { "st_walk", new[] { "เดินเล่น", "Strolling" } },
            { "st_wants_home", new[] { "อยากอยู่ด้วยแล้ว!", "Wants to stay!" } },
            { "st_shy", new[] { "ยังเขินอาย", "Still shy" } },
            { "st_relax", new[] { "ชิลล์ ๆ", "Chilling" } },

            // Results
            { "res_full", new[] { "{0} อิ่มแล้วจ้า", "{0} is full." } },
            { "res_cooldown", new[] { "ใจเย็น ๆ นะ รอแป๊บนึง", "Easy! Wait a moment." } },
            { "res_tired", new[] { "{0} เหนื่อยแล้ว ให้พักก่อนนะ", "{0} is too tired. Let it rest." } },
            { "res_busy", new[] { "รอสักครู่", "Just a moment." } },
            { "res_hiss", new[] { "ฟ่อ! {0} ยังกลัวอยู่ ลองให้อาหารก่อนนะ", "Hiss! The {0} is still scared. Try food first." } },
            { "res_nofood", new[] { "ไม่มีอาหารชนิดนี้", "You have none of that food." } },

            // Food picker
            { "choose_food", new[] { "เลือกอาหาร", "Choose food" } },
            { "give", new[] { "ให้", "Give" } },
            { "buy_give", new[] { "ซื้อให้", "Buy" } },
            { "favorite", new[] { "[ของโปรด!]", "[Favorite!]" } },

            // Popups
            { "yay", new[] { "เย้!", "Yay!" } },
            { "lets_go", new[] { "เริ่มเลย!", "Let's go!" } },
            { "close", new[] { "ปิด", "Close" } },
            { "yes", new[] { "ใช่", "Yes" } },
            { "no", new[] { "ไม่", "No" } },
            { "welcome_back", new[] { "ยินดีต้อนรับกลับ!", "Welcome back!" } },
            { "welcome_body", new[] { "ระหว่างที่คุณไม่อยู่ ({1})\nแมว ๆ หาเหรียญได้ {0} เหรียญ!", "While you were away ({1})\nyour cats earned {0} coins!" } },
            { "tutorial_title", new[] { "ยินดีต้อนรับสู่ห้องแมวเหมียว!", "Welcome to Cozy Cat Room!" } },
            { "tutorial_body", new[] {
                "แตะที่แมวเพื่อให้อาหาร ลูบหัว และเล่นด้วย\nแมวที่มีความสุขจะให้เหรียญ แตะเหรียญเพื่อรับ x2!\nใช้เหรียญซื้อของแต่งห้อง อาหาร และของเล่น\nแมวจรจะแวะมา ทำให้มันไว้ใจแล้วรับมาเลี้ยงได้",
                "Tap a cat to feed, pet and play with it.\nHappy cats drop coins. Tap them for x2!\nSpend coins on furniture, food and toys.\nStray cats will visit. Earn their trust to adopt them." } },
            { "dur_h_m", new[] { "{0} ชม. {1} นาที", "{0}h {1}m" } },
            { "dur_m", new[] { "{0} นาที", "{0} min" } },

            // Settings
            { "settings_title", new[] { "ตั้งค่า", "Settings" } },
            { "language_toggle", new[] { "ภาษา: ไทย  >  English", "Language: English  >  ไทย" } },
            { "sound_on", new[] { "เสียง: เปิด", "Sound: On" } },
            { "sound_off", new[] { "เสียง: ปิด", "Sound: Off" } },
            { "reset", new[] { "เริ่มเกมใหม่", "Start over" } },
            { "reset_confirm", new[] { "ลบข้อมูลทั้งหมดแล้วเริ่มใหม่ใช่ไหม?\n(ย้อนกลับไม่ได้)", "Delete all progress and start over?\n(This cannot be undone.)" } },
        };
    }
}
