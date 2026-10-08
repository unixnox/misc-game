using System;
using System.Collections.Generic;

namespace CatRoom
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long coins = 150;
        public long lifetimeCoins;
        public string wallpaper = ItemDatabase.DefaultWallpaper;
        public string flooring = ItemDatabase.DefaultFlooring;
        public List<PlacedItemData> placed = new List<PlacedItemData>();
        public List<InventoryEntry> inventory = new List<InventoryEntry>();
        public List<string> ownedThemes = new List<string>();
        public List<CatData> cats = new List<CatData>();
        public int adoptedCount;
        public long lastSaveUnix;
        public int language = -1; // -1 = auto
        public bool soundOn = true;
        public bool tutorialDone;

        public static SaveData CreateNew()
        {
            var d = new SaveData();
            d.ownedThemes.Add(ItemDatabase.DefaultWallpaper);
            d.ownedThemes.Add(ItemDatabase.DefaultFlooring);
            d.placed.Add(new PlacedItemData { id = "bed_basic", x = 5, z = 6, rot = 0 });
            d.placed.Add(new PlacedItemData { id = "food_bowl", x = 2, z = 6, rot = 0 });
            d.placed.Add(new PlacedItemData { id = "rug_round", x = 3, z = 3, rot = 0 });
            d.placed.Add(new PlacedItemData { id = "toy_ball", x = 4, z = 4, rot = 0 });
            d.inventory.Add(new InventoryEntry { id = "food_dry", count = 5 });
            d.inventory.Add(new InventoryEntry { id = "food_treat", count = 1 });
            d.inventory.Add(new InventoryEntry { id = "plant", count = 1 });
            d.cats.Add(CatData.CreateStarter());
            return d;
        }
    }

    [Serializable]
    public class PlacedItemData
    {
        public string id;
        public int x, z, rot;
    }

    [Serializable]
    public class InventoryEntry
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class CatData
    {
        public string uid;
        public int nameIndex;
        public int preset;
        public float size = 1f;
        public float hunger = 80f;    // fullness: 100 = full
        public float fun = 80f;
        public float energy = 80f;
        public float affection = 40f;
        public string favoriteFood;
        public float posX = 4f, posZ = 4f;

        public string Name => CatAppearance.CatName(nameIndex);

        public static CatData CreateStarter()
        {
            return new CatData { uid = Guid.NewGuid().ToString("N"), nameIndex = 0, preset = 0, size = 1f,
                favoriteFood = "food_fish", affection = 60f };
        }

        public static CatData CreateRandomStray(System.Random rng)
        {
            var foods = new[] { "food_dry", "food_wet", "food_fish", "food_treat" };
            return new CatData
            {
                uid = Guid.NewGuid().ToString("N"),
                nameIndex = rng.Next(1, CatAppearance.NameCount),
                preset = CatAppearance.RandomPreset(rng),
                size = 0.88f + (float)rng.NextDouble() * 0.22f,
                hunger = 25f + (float)rng.NextDouble() * 20f,
                fun = 30f, energy = 55f, affection = 10f,
                favoriteFood = foods[rng.Next(foods.Length)],
                posX = 1.5f, posZ = 2f,
            };
        }
    }
}
