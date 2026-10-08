using UnityEngine;

namespace CatRoom
{
    /// <summary>Tiny looping animation used for decorative moving parts (fish, lamp glow, etc).</summary>
    public class SimpleAnim : MonoBehaviour
    {
        public Vector3 spin;        // degrees per second
        public float bobAmplitude;
        public float bobSpeed = 2f;
        Vector3 basePos;
        float phase;

        void Start()
        {
            basePos = transform.localPosition;
            phase = Random.value * 10f;
        }

        void Update()
        {
            if (spin != Vector3.zero) transform.Rotate(spin * Time.deltaTime, Space.Self);
            if (bobAmplitude > 0f)
                transform.localPosition = basePos + Vector3.up * Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmplitude;
        }
    }

    /// <summary>
    /// Builds the cartoon model for every item out of primitives. Models are centered on their
    /// footprint, sit on y = 0 and face -Z (towards the camera at rotation 0).
    /// </summary>
    public static class ItemModels
    {
        static readonly Color Wood = new Color(0.78f, 0.55f, 0.35f);
        static readonly Color DarkWood = new Color(0.55f, 0.36f, 0.24f);
        static readonly Color Rope = new Color(0.93f, 0.82f, 0.6f);
        static readonly Color Cream = new Color(1f, 0.96f, 0.88f);

        public static GameObject Build(ItemDef def)
        {
            var root = new GameObject(def.id);
            var t = root.transform;
            switch (def.id)
            {
                case "bed_basic": BedBasic(t); break;
                case "bed_donut": BedDonut(t); break;
                case "box": Box(t); break;
                case "food_bowl": Bowl(t); break;
                case "scratch_post": ScratchPost(t); break;
                case "cat_tree": CatTree(t); break;
                case "sofa": Sofa(t); break;
                case "plant": Plant(t); break;
                case "lamp": Lamp(t); break;
                case "bookshelf": Bookshelf(t); break;
                case "fish_tank": FishTank(t); break;
                case "rug_round": RugRound(t); break;
                case "rug_fish": RugFish(t); break;
                case "toy_ball": ToyBall(t); break;
                case "toy_mouse": ToyMouse(t); break;
                case "toy_tunnel": ToyTunnel(t); break;
                case "toy_wand": ToyWand(t); break;
                case "food_dry": FoodBag(t); break;
                case "food_wet": FoodCan(t); break;
                case "food_fish": FoodFish(t); break;
                case "food_treat": FoodTreat(t); break;
                default:
                    if (def.category == ItemCategory.Wallpaper) ThemeSwatch(t, def, true);
                    else if (def.category == ItemCategory.Flooring) ThemeSwatch(t, def, false);
                    else Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.25f, 0), Vector3.one * 0.5f, Color.magenta);
                    break;
            }
            return root;
        }

        // ---------------- Furniture ----------------

        static void BedBasic(Transform t)
        {
            var pink = new Color(1f, 0.72f, 0.76f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.05f, 0), new Vector3(0.82f, 0.05f, 0.82f), pink * 0.92f, true);
            Toon.MeshObj(ProcGen.Torus, t, new Vector3(0, 0.12f, 0), new Vector3(0.8f, 0.55f, 0.8f), pink, true);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.1f, 0), new Vector3(0.6f, 0.03f, 0.6f), new Color(1f, 0.9f, 0.92f));
        }

        static void BedDonut(Transform t)
        {
            var dough = new Color(0.86f, 0.6f, 0.36f);
            var icing = new Color(1f, 0.58f, 0.75f);
            Toon.MeshObj(ProcGen.Torus, t, new Vector3(0, 0.16f, 0), new Vector3(0.95f, 0.9f, 0.95f), dough, true);
            Toon.MeshObj(ProcGen.Torus, t, new Vector3(0, 0.22f, 0), new Vector3(0.93f, 0.6f, 0.93f), icing, true);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.08f, 0), new Vector3(0.62f, 0.05f, 0.62f), Cream);
            Color[] sprinkle = { Color.white, new Color(0.5f, 0.85f, 1f), new Color(1f, 0.95f, 0.4f), new Color(0.6f, 0.95f, 0.6f) };
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                var p = new Vector3(Mathf.Cos(a) * 0.47f, 0.33f, Mathf.Sin(a) * 0.47f);
                Toon.Prim(PrimitiveType.Cube, t, p, new Vector3(0.07f, 0.025f, 0.025f), sprinkle[i % sprinkle.Length], false, new Vector3(0, i * 47f, 0));
            }
        }

        static void Box(Transform t)
        {
            var c = new Color(0.86f, 0.66f, 0.43f);
            const float s = 0.72f, h = 0.36f, th = 0.04f;
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.02f, 0), new Vector3(s, th, s), c * 0.9f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, h / 2, -s / 2), new Vector3(s, h, th), c, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, h / 2, s / 2), new Vector3(s, h, th), c, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(-s / 2, h / 2, 0), new Vector3(th, h, s), c, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(s / 2, h / 2, 0), new Vector3(th, h, s), c, true, default, 0.01f);
            // flaps
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, h + 0.08f, -s / 2 - 0.1f), new Vector3(s, 0.03f, 0.26f), c * 1.05f, true, new Vector3(-50, 0, 0), 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, h + 0.08f, s / 2 + 0.1f), new Vector3(s, 0.03f, 0.26f), c * 1.05f, true, new Vector3(50, 0, 0), 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.2f, -s / 2 - 0.022f), new Vector3(0.25f, 0.08f, 0.005f), new Color(0.55f, 0.4f, 0.28f));
        }

        static void Bowl(Transform t)
        {
            var c = new Color(0.55f, 0.75f, 1f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.06f, 0), new Vector3(0.42f, 0.06f, 0.42f), c, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.1f, 0), new Vector3(0.32f, 0.03f, 0.32f), new Color(0.72f, 0.45f, 0.26f));
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.05f, 0.13f, 0.02f), new Vector3(0.12f, 0.05f, 0.12f), new Color(0.8f, 0.52f, 0.3f));
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.07f, -0.2f), new Vector3(0.12f, 0.05f, 0.01f), Color.white);
        }

        static void ScratchPost(Transform t)
        {
            var carpet = new Color(0.72f, 0.62f, 0.9f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.05f, 0), new Vector3(0.7f, 0.1f, 0.7f), carpet, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.55f, 0), new Vector3(0.2f, 0.45f, 0.2f), Rope, true, default, 0.012f);
            for (int i = 0; i < 5; i++)
                Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.2f + i * 0.17f, 0), new Vector3(0.21f, 0.008f, 0.21f), Rope * 0.85f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.02f, 0), new Vector3(0.42f, 0.03f, 0.42f), carpet, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.17f, 0.88f, -0.05f), new Vector3(0.008f, 0.12f, 0.008f), Color.white);
            var ball = Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.17f, 0.74f, -0.05f), Vector3.one * 0.1f, new Color(1f, 0.5f, 0.6f), true, default, 0.008f);
            ball.AddComponent<SimpleAnim>().bobAmplitude = 0.02f;
        }

        static void CatTree(Transform t)
        {
            var carpet = new Color(0.62f, 0.78f, 0.95f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.05f, 0), new Vector3(0.9f, 0.1f, 0.9f), carpet, true, default, 0.012f);
            // cubby house
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(-0.15f, 0.33f, 0.1f), new Vector3(0.55f, 0.46f, 0.55f), carpet * 0.95f, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.15f, 0.32f, -0.18f), new Vector3(0.26f, 0.005f, 0.26f), new Color(0.2f, 0.17f, 0.25f), false, new Vector3(90, 0, 0));
            // posts
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.25f, 0.8f, -0.2f), new Vector3(0.15f, 0.75f, 0.15f), Rope, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(-0.15f, 1.05f, 0.1f), new Vector3(0.13f, 0.5f, 0.13f), Rope, true, default, 0.01f);
            // middle platform
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.2f, 0.95f, -0.15f), new Vector3(0.45f, 0.06f, 0.45f), carpet, true, default, 0.01f);
            // top bed
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.55f, 0), new Vector3(0.82f, 0.04f, 0.82f), carpet, true, default, 0.012f);
            Toon.MeshObj(ProcGen.Torus, t, new Vector3(0, 1.6f, 0), new Vector3(0.78f, 0.4f, 0.78f), new Color(1f, 0.8f, 0.85f), true, default, 0.01f);
            var toy = Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.4f, 1.25f, -0.25f), Vector3.one * 0.09f, new Color(1f, 0.85f, 0.3f), true, default, 0.008f);
            toy.AddComponent<SimpleAnim>().bobAmplitude = 0.03f;
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.4f, 1.42f, -0.25f), new Vector3(0.008f, 0.12f, 0.008f), Color.white);
        }

        static void Sofa(Transform t)
        {
            var c = new Color(0.45f, 0.74f, 0.76f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.26f, 0.02f), new Vector3(1.8f, 0.3f, 0.78f), c * 0.9f, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(-0.42f, 0.45f, -0.04f), new Vector3(0.82f, 0.12f, 0.64f), c * 1.08f, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.42f, 0.45f, -0.04f), new Vector3(0.82f, 0.12f, 0.64f), c * 1.08f, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.66f, 0.3f), new Vector3(1.8f, 0.6f, 0.2f), c, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(-0.92f, 0.45f, 0.02f), new Vector3(0.18f, 0.42f, 0.78f), c, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.92f, 0.45f, 0.02f), new Vector3(0.18f, 0.42f, 0.78f), c, true, default, 0.012f);
            foreach (var x in new[] { -0.8f, 0.8f })
            foreach (var z in new[] { -0.3f, 0.33f })
                Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(x, 0.05f, z), new Vector3(0.08f, 0.05f, 0.08f), DarkWood);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.55f, 0.62f, 0.15f), new Vector3(0.35f, 0.3f, 0.12f), new Color(1f, 0.8f, 0.5f), true, new Vector3(-15, 10, 8), 0.01f);
        }

        static void Plant(Transform t)
        {
            var leaf = new Color(0.45f, 0.78f, 0.42f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.18f, 0), new Vector3(0.36f, 0.18f, 0.36f), new Color(0.88f, 0.52f, 0.36f), true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.38f, 0), new Vector3(0.4f, 0.03f, 0.4f), new Color(0.95f, 0.6f, 0.42f), true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.4f, 0), new Vector3(0.32f, 0.01f, 0.32f), new Color(0.42f, 0.3f, 0.22f));
            for (int i = 0; i < 7; i++)
            {
                float a = i * 360f / 7f;
                var pivot = Toon.Pivot("leaf", t, new Vector3(0, 0.42f, 0), new Vector3(0, a, 0));
                float tilt = 25f + (i % 3) * 12f;
                Toon.Prim(PrimitiveType.Sphere, pivot, Quaternion.Euler(tilt, 0, 0) * new Vector3(0, 0.25f, 0),
                    new Vector3(0.16f, 0.42f, 0.07f), leaf * (0.9f + (i % 2) * 0.15f), true, new Vector3(tilt, 0, 0), 0.01f);
            }
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.75f, 0), new Vector3(0.15f, 0.4f, 0.15f), leaf * 1.1f, true, default, 0.01f);
        }

        static void Lamp(Transform t)
        {
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.03f, 0), new Vector3(0.34f, 0.03f, 0.34f), DarkWood, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.72f, 0), new Vector3(0.04f, 0.7f, 0.04f), DarkWood);
            var bulb = Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.38f, 0), Vector3.one * 0.16f, new Color(1f, 0.95f, 0.7f));
            bulb.GetComponent<Renderer>().sharedMaterial = Toon.Get(new Color(1f, 0.95f, 0.7f), false, 0.015f, 0.8f);
            var shade = Toon.MeshObj(ProcGen.Cone, t, new Vector3(0, 1.32f, 0), new Vector3(0.6f, 0.38f, 0.6f), new Color(1f, 0.9f, 0.62f), true, default, 0.012f);
            shade.GetComponent<Renderer>().sharedMaterial = Toon.Get(new Color(1f, 0.9f, 0.62f), true, 0.012f, 0.35f);
        }

        static void Bookshelf(Transform t)
        {
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.7f, 0.2f), new Vector3(1.8f, 1.4f, 0.06f), DarkWood);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(-0.88f, 0.7f, 0f), new Vector3(0.06f, 1.4f, 0.46f), Wood, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.88f, 0.7f, 0f), new Vector3(0.06f, 1.4f, 0.46f), Wood, true, default, 0.01f);
            Color[] books = { new Color(0.95f, 0.45f, 0.45f), new Color(0.45f, 0.65f, 0.95f), new Color(0.98f, 0.85f, 0.4f),
                              new Color(0.55f, 0.85f, 0.55f), new Color(0.85f, 0.6f, 0.95f), new Color(1f, 0.7f, 0.5f) };
            for (int s = 0; s < 4; s++)
            {
                float y = 0.03f + s * 0.46f;
                Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, y, 0), new Vector3(1.8f, 0.06f, 0.46f), Wood, true, default, 0.01f);
                if (s == 3) break;
                float x = -0.78f;
                int k = s * 3;
                while (x < 0.7f)
                {
                    float w = 0.08f + ((k * 7) % 3) * 0.03f;
                    float h = 0.26f + ((k * 5) % 4) * 0.04f;
                    if ((k % 7) != 3)
                        Toon.Prim(PrimitiveType.Cube, t, new Vector3(x + w / 2, y + 0.03f + h / 2, -0.02f), new Vector3(w, h, 0.3f), books[k % books.Length]);
                    x += w + 0.015f;
                    k++;
                }
            }
        }

        static void FishTank(Transform t)
        {
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.22f, 0), new Vector3(0.82f, 0.44f, 0.6f), Wood, true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.47f, 0), new Vector3(0.78f, 0.06f, 0.56f), new Color(0.98f, 0.9f, 0.65f));
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(-0.22f, 0.55f, 0.1f), new Vector3(0.08f, 0.25f, 0.08f), new Color(0.4f, 0.8f, 0.45f));
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(-0.15f, 0.52f, 0.12f), new Vector3(0.07f, 0.18f, 0.07f), new Color(0.35f, 0.72f, 0.4f));
            var swim = Toon.Pivot("swim", t, new Vector3(0, 0.75f, 0));
            swim.gameObject.AddComponent<SimpleAnim>().spin = new Vector3(0, 60f, 0);
            Color[] fish = { new Color(1f, 0.55f, 0.2f), new Color(1f, 0.85f, 0.25f) };
            for (int i = 0; i < 2; i++)
            {
                var f = Toon.Pivot("fish", swim, new Vector3(i == 0 ? 0.2f : -0.18f, i * 0.1f - 0.05f, 0), new Vector3(0, i == 0 ? 0 : 180, 0));
                Toon.Prim(PrimitiveType.Sphere, f, Vector3.zero, new Vector3(0.05f, 0.08f, 0.14f), fish[i]);
                Toon.MeshObj(ProcGen.Cone, f, new Vector3(0, 0, -0.06f), new Vector3(0.09f, 0.08f, 0.09f), fish[i], false, new Vector3(-90, 0, 0));
            }
            var glass = Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.77f, 0), new Vector3(0.8f, 0.62f, 0.58f), Color.white);
            glass.GetComponent<Renderer>().sharedMaterial = Toon.Ghost(new Color(0.55f, 0.88f, 1f, 0.35f));
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 1.09f, 0), new Vector3(0.84f, 0.04f, 0.62f), DarkWood, true, default, 0.01f);
        }

        static void RugRound(Transform t)
        {
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.012f, 0), new Vector3(1.85f, 0.008f, 1.85f), new Color(1f, 0.74f, 0.6f));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.016f, 0), new Vector3(1.4f, 0.008f, 1.4f), new Color(1f, 0.86f, 0.74f));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.02f, 0), new Vector3(0.7f, 0.008f, 0.7f), new Color(1f, 0.74f, 0.6f));
        }

        static void RugFish(Transform t)
        {
            var c = new Color(0.55f, 0.75f, 0.98f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.18f, 0.012f, 0), new Vector3(1.35f, 0.02f, 0.8f), c);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(-0.68f, 0.012f, 0), new Vector3(0.42f, 0.02f, 0.42f), c, false, new Vector3(0, 45, 0));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.6f, 0.024f, -0.1f), new Vector3(0.12f, 0.004f, 0.12f), Color.white);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.6f, 0.028f, -0.1f), new Vector3(0.06f, 0.004f, 0.06f), new Color(0.2f, 0.2f, 0.3f));
            for (int i = 0; i < 3; i++)
                Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.25f - i * 0.22f, 0.024f, 0), new Vector3(0.03f, 0.004f, 0.5f), c * 0.85f);
        }

        // ---------------- Toys ----------------

        static void ToyBall(Transform t)
        {
            var c = new Color(1f, 0.5f, 0.62f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.15f, 0), Vector3.one * 0.3f, c, true, default, 0.01f);
            Toon.MeshObj(ProcGen.Torus, t, new Vector3(0, 0.15f, 0), new Vector3(0.3f, 0.1f, 0.3f), c * 0.85f, false, new Vector3(30, 0, 20));
            Toon.MeshObj(ProcGen.Torus, t, new Vector3(0, 0.15f, 0), new Vector3(0.3f, 0.1f, 0.3f), c * 0.85f, false, new Vector3(-40, 60, 0));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.2f, 0.02f, -0.12f), new Vector3(0.015f, 0.15f, 0.015f), c * 0.85f, false, new Vector3(90, 30, 0));
        }

        static void ToyMouse(Transform t)
        {
            var c = new Color(0.72f, 0.72f, 0.78f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.08f, 0), new Vector3(0.18f, 0.15f, 0.3f), c, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.06f, 0.15f, -0.08f), new Vector3(0.07f, 0.07f, 0.02f), new Color(1f, 0.7f, 0.78f), true, default, 0.006f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(-0.06f, 0.15f, -0.08f), new Vector3(0.07f, 0.07f, 0.02f), new Color(1f, 0.7f, 0.78f), true, default, 0.006f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.08f, -0.16f), Vector3.one * 0.03f, new Color(1f, 0.5f, 0.6f));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.04f, 0.03f, 0.25f), new Vector3(0.015f, 0.12f, 0.015f), new Color(1f, 0.6f, 0.7f), false, new Vector3(90, 20, 0));
        }

        static void ToyTunnel(Transform t)
        {
            var c = new Color(0.7f, 0.55f, 0.92f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.3f, 0), new Vector3(0.58f, 0.85f, 0.58f), c, true, new Vector3(0, 0, 90), 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.3f, 0), new Vector3(0.46f, 0.86f, 0.46f), new Color(0.25f, 0.18f, 0.32f), false, new Vector3(0, 0, 90));
            for (int i = -2; i <= 2; i++)
                Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(i * 0.32f, 0.3f, 0), new Vector3(0.6f, 0.02f, 0.6f), c * 0.85f, false, new Vector3(0, 0, 90));
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.02f, 0), new Vector3(1.6f, 0.04f, 0.3f), c * 0.8f);
        }

        static void ToyWand(Transform t)
        {
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.04f, 0), new Vector3(0.3f, 0.04f, 0.3f), new Color(0.98f, 0.8f, 0.5f), true, default, 0.01f);
            var pole = Toon.Pivot("pole", t, new Vector3(0, 0.08f, 0), new Vector3(0, 0, -20));
            Toon.Prim(PrimitiveType.Cylinder, pole, new Vector3(0, 0.45f, 0), new Vector3(0.03f, 0.45f, 0.03f), new Color(0.95f, 0.4f, 0.5f));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0.38f, 0.62f, 0), new Vector3(0.006f, 0.22f, 0.006f), Color.white);
            var feathers = Toon.Pivot("feathers", t, new Vector3(0.38f, 0.4f, 0));
            feathers.gameObject.AddComponent<SimpleAnim>().spin = new Vector3(0, 90f, 0);
            Color[] fc = { new Color(0.4f, 0.8f, 1f), new Color(1f, 0.85f, 0.3f), new Color(1f, 0.5f, 0.7f) };
            for (int i = 0; i < 3; i++)
                Toon.Prim(PrimitiveType.Sphere, feathers, Quaternion.Euler(0, i * 120f, 0) * new Vector3(0.04f, -0.06f, 0),
                    new Vector3(0.05f, 0.16f, 0.02f), fc[i], false, new Vector3(0, i * 120f, 20));
        }

        // ---------------- Food (icons only) ----------------

        static void FoodBag(Transform t)
        {
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.32f, 0), new Vector3(0.48f, 0.64f, 0.24f), new Color(1f, 0.62f, 0.3f), true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0, 0.68f, 0), new Vector3(0.44f, 0.08f, 0.1f), new Color(0.95f, 0.55f, 0.25f), true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.32f, -0.125f), new Vector3(0.28f, 0.005f, 0.28f), Color.white, false, new Vector3(90, 0, 0));
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.32f, -0.13f), new Vector3(0.12f, 0.06f, 0.01f), new Color(0.5f, 0.75f, 0.95f));
        }

        static void FoodCan(Transform t)
        {
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.12f, 0), new Vector3(0.45f, 0.12f, 0.45f), new Color(0.85f, 0.87f, 0.92f), true, default, 0.012f);
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.12f, 0), new Vector3(0.46f, 0.07f, 0.46f), new Color(0.55f, 0.8f, 0.6f));
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.25f, 0), new Vector3(0.36f, 0.01f, 0.36f), new Color(0.72f, 0.74f, 0.8f));
        }

        static void FoodFish(Transform t)
        {
            var c = new Color(0.62f, 0.72f, 0.85f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.05f, 0.12f, 0), new Vector3(0.62f, 0.22f, 0.2f), c, true, default, 0.012f);
            Toon.MeshObj(ProcGen.Cone, t, new Vector3(-0.22f, 0.12f, 0), new Vector3(0.22f, 0.2f, 0.08f), c * 0.9f, true, new Vector3(0, 0, 90), 0.01f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.26f, 0.15f, -0.08f), Vector3.one * 0.05f, new Color(0.15f, 0.15f, 0.2f));
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(0.08f, 0.17f, -0.03f), new Vector3(0.4f, 0.06f, 0.14f), new Color(0.4f, 0.5f, 0.68f));
        }

        static void FoodTreat(Transform t)
        {
            Toon.Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.3f, 0), new Vector3(0.14f, 0.3f, 0.14f), new Color(1f, 0.6f, 0.75f), true, new Vector3(0, 0, -15), 0.01f);
            Toon.Prim(PrimitiveType.Cube, t, new Vector3(0.08f, 0.6f, 0), new Vector3(0.16f, 0.05f, 0.06f), new Color(1f, 0.85f, 0.9f), true, new Vector3(0, 0, -15), 0.008f);
            Toon.Prim(PrimitiveType.Sphere, t, new Vector3(-0.03f, 0.32f, -0.07f), Vector3.one * 0.07f, new Color(0.98f, 0.95f, 0.5f), false, default);
        }

        static void ThemeSwatch(Transform t, ItemDef def, bool wall)
        {
            var go = Toon.Prim(PrimitiveType.Cube, t, wall ? new Vector3(0, 0.45f, 0) : new Vector3(0, 0.04f, 0),
                wall ? new Vector3(0.9f, 0.9f, 0.05f) : new Vector3(0.9f, 0.05f, 0.9f), Color.white);
            var tex = wall ? ProcGen.Wallpaper(def) : ProcGen.Flooring(def);
            go.GetComponent<Renderer>().sharedMaterial = Toon.Textured(tex, new Vector2(1.5f, 1.5f));
        }
    }
}
