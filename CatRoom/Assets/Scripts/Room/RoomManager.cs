using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    /// <summary>A furniture or toy instance placed on the room grid.</summary>
    public class PlacedItem : MonoBehaviour
    {
        public ItemDef def;
        public int x, z, rot;
        public CatController occupant;

        public int FootW => rot % 2 == 0 ? def.w : def.d;
        public int FootD => rot % 2 == 0 ? def.d : def.w;

        public bool Perch => def.useHeight > 0.05f;

        /// <summary>Where a cat stands/sits while using the item.</summary>
        public Vector3 UsePoint
        {
            get
            {
                var p = transform.position;
                if (Perch) return new Vector3(p.x, def.useHeight, p.z);
                return ApproachPoint;
            }
        }

        /// <summary>A floor point just in front of the item.</summary>
        public Vector3 ApproachPoint
        {
            get
            {
                float depth = def.d * 0.5f + 0.35f;
                var p = transform.position + transform.rotation * new Vector3(0, 0, -depth);
                p.y = 0;
                return RoomManager.ClampToFloor(p);
            }
        }
    }

    public class RoomManager : MonoBehaviour
    {
        public const int Size = 8;
        public const float Margin = 0.35f;
        static readonly Vector2Int[] DoorCells = { new Vector2Int(0, 1), new Vector2Int(0, 2) };

        public readonly List<PlacedItem> Items = new List<PlacedItem>();
        public event Action Changed;

        Material wallMat, floorMat;
        Transform doorPivot;
        float doorTimer;
        float doorAngle;
        GameObject gridOverlay;

        public Vector3 DoorInside => new Vector3(0.6f, 0f, 2f);
        public Vector3 DoorThreshold => new Vector3(0.12f, 0f, 2f);

        public int Comfort
        {
            get
            {
                int c = 0;
                foreach (var i in Items) c += i.def.comfort;
                return c;
            }
        }

        /// <summary>Coin multiplier from room comfort: +2% per point, up to x3.</summary>
        public float ComfortMultiplier => Mathf.Min(3f, 1f + Comfort * 0.02f);

        // ---------------- Building ----------------

        public void Build(SaveData data)
        {
            BuildShell();
            ApplyWallpaper(ItemDatabase.Get(data.wallpaper) ?? ItemDatabase.Get(ItemDatabase.DefaultWallpaper));
            ApplyFlooring(ItemDatabase.Get(data.flooring) ?? ItemDatabase.Get(ItemDatabase.DefaultFlooring));

            foreach (var p in data.placed)
            {
                var def = ItemDatabase.Get(p.id);
                if (def == null || !def.Placeable) continue;
                if (CanPlace(def, p.x, p.z, p.rot)) Place(def, p.x, p.z, p.rot, false);
                else GameManager.I.AddItem(def.id, 1, false); // invalid spot: return to the bag
            }
        }

        void BuildShell()
        {
            var shell = new GameObject("RoomShell").transform;
            shell.SetParent(transform, false);

            var slabColor = new Color(0.98f, 0.82f, 0.66f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(3.85f, -0.32f, 4.15f), new Vector3(8.5f, 0.6f, 8.5f), slabColor);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(3.85f, -0.66f, 4.15f), new Vector3(8.3f, 0.1f, 8.3f), slabColor * 0.8f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(shell, false);
            floor.transform.localPosition = new Vector3(Size / 2f, -0.01f, Size / 2f);
            floor.transform.localScale = new Vector3(Size, 0.02f, Size);
            floorMat = Toon.Textured(Texture2D.whiteTexture, new Vector2(4, 4));
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.layer = 0;

            wallMat = Toon.Textured(Texture2D.whiteTexture, new Vector2(4, 2));
            var back = Toon.Prim(PrimitiveType.Cube, shell, new Vector3(3.85f, 2f, 8.15f), new Vector3(8.3f, 4f, 0.3f), Color.white);
            back.GetComponent<Renderer>().sharedMaterial = wallMat;
            var left = Toon.Prim(PrimitiveType.Cube, shell, new Vector3(-0.15f, 2f, 4f), new Vector3(0.3f, 4f, 8f), Color.white);
            left.GetComponent<Renderer>().sharedMaterial = wallMat;

            var trim = new Color(0.82f, 0.58f, 0.4f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(3.8f, 4.06f, 8.15f), new Vector3(8.5f, 0.14f, 0.42f), trim);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(-0.15f, 4.06f, 3.95f), new Vector3(0.42f, 0.14f, 8.1f), trim);
            var baseboard = new Color(1f, 0.97f, 0.92f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(4f, 0.09f, 7.98f), new Vector3(8f, 0.18f, 0.05f), baseboard);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(0.02f, 0.09f, 4f), new Vector3(0.05f, 0.18f, 8f), baseboard);

            BuildWindow(shell);
            BuildDoor(shell);
            BuildWallDecor(shell);

            gridOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            gridOverlay.name = "GridOverlay";
            UnityEngine.Object.Destroy(gridOverlay.GetComponent<Collider>());
            gridOverlay.transform.SetParent(transform, false);
            gridOverlay.transform.localPosition = new Vector3(Size / 2f, 0.03f, Size / 2f);
            gridOverlay.transform.localRotation = Quaternion.Euler(90, 0, 0);
            gridOverlay.transform.localScale = new Vector3(Size, Size, 1);
            gridOverlay.GetComponent<Renderer>().sharedMaterial = Toon.Ghost(new Color(1f, 1f, 1f, 0.55f), ProcGen.Grid(Size));
            gridOverlay.SetActive(false);
        }

        void BuildWindow(Transform shell)
        {
            var frame = new Color(1f, 0.98f, 0.95f);
            var sky = new Color(0.62f, 0.85f, 1f);
            var w = Toon.Pivot("Window", shell, new Vector3(5.6f, 2.3f, 7.99f));
            var pane = Toon.Prim(PrimitiveType.Cube, w, Vector3.zero, new Vector3(1.7f, 1.3f, 0.02f), sky);
            pane.GetComponent<Renderer>().sharedMaterial = Toon.Get(sky, false, 0.015f, 0.6f);
            Toon.Prim(PrimitiveType.Sphere, w, new Vector3(-0.45f, 0.25f, -0.02f), new Vector3(0.5f, 0.22f, 0.02f), Color.white);
            Toon.Prim(PrimitiveType.Sphere, w, new Vector3(-0.2f, 0.32f, -0.02f), new Vector3(0.4f, 0.25f, 0.02f), Color.white);
            Toon.Prim(PrimitiveType.Sphere, w, new Vector3(0.45f, -0.15f, -0.02f), new Vector3(0.45f, 0.18f, 0.02f), Color.white);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(0, 0, -0.04f), new Vector3(0.07f, 1.3f, 0.05f), frame);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(0, 0, -0.04f), new Vector3(1.7f, 0.07f, 0.05f), frame);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(0, 0.68f, -0.04f), new Vector3(1.86f, 0.1f, 0.08f), frame, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(0, -0.68f, -0.04f), new Vector3(1.86f, 0.1f, 0.08f), frame, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(-0.88f, 0, -0.04f), new Vector3(0.1f, 1.4f, 0.08f), frame, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(0.88f, 0, -0.04f), new Vector3(0.1f, 1.4f, 0.08f), frame, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(0, -0.76f, -0.12f), new Vector3(2.0f, 0.06f, 0.26f), frame, true, default, 0.01f);
            var curtain = new Color(1f, 0.7f, 0.74f);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(-1.08f, 0.0f, -0.08f), new Vector3(0.36f, 1.7f, 0.06f), curtain, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, w, new Vector3(1.08f, 0.0f, -0.08f), new Vector3(0.36f, 1.7f, 0.06f), curtain, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cylinder, w, new Vector3(0, 0.9f, -0.1f), new Vector3(0.04f, 1.35f, 0.04f), new Color(0.82f, 0.58f, 0.4f), false, new Vector3(0, 0, 90));
            // a small potted cactus on the sill
            Toon.Prim(PrimitiveType.Cylinder, w, new Vector3(0.55f, -0.62f, -0.14f), new Vector3(0.14f, 0.08f, 0.14f), new Color(0.88f, 0.52f, 0.36f), true, default, 0.008f);
            Toon.Prim(PrimitiveType.Sphere, w, new Vector3(0.55f, -0.46f, -0.14f), new Vector3(0.1f, 0.2f, 0.1f), new Color(0.45f, 0.78f, 0.42f), true, default, 0.008f);
        }

        void BuildDoor(Transform shell)
        {
            var outside = new Color(0.72f, 0.9f, 1f);
            var opening = Toon.Prim(PrimitiveType.Cube, shell, new Vector3(0.005f, 1.25f, 2f), new Vector3(0.01f, 2.5f, 1.2f), outside);
            opening.GetComponent<Renderer>().sharedMaterial = Toon.Get(outside, false, 0.015f, 0.5f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(0.12f, 0.03f, 2f), new Vector3(0.25f, 0.06f, 1.2f), new Color(0.6f, 0.75f, 0.55f));
            var frameC = new Color(0.98f, 0.92f, 0.82f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(0.03f, 2.55f, 2f), new Vector3(0.08f, 0.12f, 1.42f), frameC, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(0.03f, 1.25f, 1.33f), new Vector3(0.08f, 2.6f, 0.1f), frameC, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, shell, new Vector3(0.03f, 1.25f, 2.67f), new Vector3(0.08f, 2.6f, 0.1f), frameC, true, default, 0.01f);

            doorPivot = Toon.Pivot("DoorPivot", shell, new Vector3(0.05f, 0f, 2.6f));
            var doorC = new Color(0.86f, 0.6f, 0.4f);
            Toon.Prim(PrimitiveType.Cube, doorPivot, new Vector3(0f, 1.25f, -0.6f), new Vector3(0.06f, 2.48f, 1.18f), doorC, true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, doorPivot, new Vector3(0.035f, 1.75f, -0.6f), new Vector3(0.02f, 0.8f, 0.8f), doorC * 0.9f);
            Toon.Prim(PrimitiveType.Cube, doorPivot, new Vector3(0.035f, 0.7f, -0.6f), new Vector3(0.02f, 0.8f, 0.8f), doorC * 0.9f);
            Toon.Prim(PrimitiveType.Sphere, doorPivot, new Vector3(0.07f, 1.2f, -1.05f), Vector3.one * 0.09f, new Color(1f, 0.85f, 0.3f), true, default, 0.008f);
            // cat flap
            Toon.Prim(PrimitiveType.Cube, doorPivot, new Vector3(0.04f, 0.25f, -0.6f), new Vector3(0.02f, 0.35f, 0.4f), new Color(0.55f, 0.38f, 0.28f));
        }

        void BuildWallDecor(Transform shell)
        {
            // cat portrait on the left wall
            var pic = Toon.Pivot("Portrait", shell, new Vector3(0.02f, 2.4f, 5.4f));
            Toon.Prim(PrimitiveType.Cube, pic, Vector3.zero, new Vector3(0.05f, 0.9f, 1.1f), new Color(0.82f, 0.58f, 0.4f), true, default, 0.01f);
            Toon.Prim(PrimitiveType.Cube, pic, new Vector3(0.02f, 0, 0), new Vector3(0.03f, 0.72f, 0.92f), new Color(1f, 0.95f, 0.8f));
            var orange = new Color(1f, 0.66f, 0.3f);
            Toon.Prim(PrimitiveType.Sphere, pic, new Vector3(0.05f, -0.05f, 0), new Vector3(0.02f, 0.38f, 0.44f), orange);
            Toon.MeshObj(ProcGen.Cone, pic, new Vector3(0.05f, 0.1f, -0.13f), new Vector3(0.02f, 0.18f, 0.14f), orange, false, new Vector3(0, 0, 0));
            Toon.MeshObj(ProcGen.Cone, pic, new Vector3(0.05f, 0.1f, 0.13f), new Vector3(0.02f, 0.18f, 0.14f), orange, false, new Vector3(0, 0, 0));
            Toon.Prim(PrimitiveType.Sphere, pic, new Vector3(0.07f, -0.03f, -0.08f), new Vector3(0.01f, 0.06f, 0.05f), new Color(0.2f, 0.15f, 0.15f));
            Toon.Prim(PrimitiveType.Sphere, pic, new Vector3(0.07f, -0.03f, 0.08f), new Vector3(0.01f, 0.06f, 0.05f), new Color(0.2f, 0.15f, 0.15f));

            // wall clock on the back wall
            var clock = Toon.Pivot("Clock", shell, new Vector3(2.2f, 2.9f, 7.98f));
            Toon.Prim(PrimitiveType.Cylinder, clock, Vector3.zero, new Vector3(0.6f, 0.03f, 0.6f), Color.white, true, new Vector3(90, 0, 0), 0.015f);
            var hands = Toon.Pivot("Hands", clock, new Vector3(0, 0, -0.04f));
            Toon.Prim(PrimitiveType.Cube, hands, new Vector3(0, 0.1f, 0), new Vector3(0.03f, 0.2f, 0.01f), new Color(0.3f, 0.22f, 0.2f));
            var minute = Toon.Pivot("Minute", clock, new Vector3(0, 0, -0.05f));
            Toon.Prim(PrimitiveType.Cube, minute, new Vector3(0, 0.12f, 0), new Vector3(0.02f, 0.25f, 0.01f), new Color(0.3f, 0.22f, 0.2f));
            minute.gameObject.AddComponent<SimpleAnim>().spin = new Vector3(0, 0, -30f);
        }

        // ---------------- Themes ----------------

        public void ApplyWallpaper(ItemDef def)
        {
            if (def == null) return;
            var old = wallMat.mainTexture;
            wallMat.mainTexture = ProcGen.Wallpaper(def);
            if (old != null && old != Texture2D.whiteTexture) Destroy(old);
        }

        public void ApplyFlooring(ItemDef def)
        {
            if (def == null) return;
            var old = floorMat.mainTexture;
            floorMat.mainTexture = ProcGen.Flooring(def);
            if (old != null && old != Texture2D.whiteTexture) Destroy(old);
        }

        public void ShowGrid(bool on)
        {
            if (gridOverlay != null) gridOverlay.SetActive(on);
        }

        // ---------------- Grid placement ----------------

        public static int FootW(ItemDef def, int rot) => rot % 2 == 0 ? def.w : def.d;
        public static int FootD(ItemDef def, int rot) => rot % 2 == 0 ? def.d : def.w;

        public static Vector3 FootprintCenter(ItemDef def, int x, int z, int rot)
        {
            return new Vector3(x + FootW(def, rot) * 0.5f, 0f, z + FootD(def, rot) * 0.5f);
        }

        /// <summary>The bottom-left cell for an item whose footprint is centered near a world point.</summary>
        public static Vector2Int CellFor(ItemDef def, int rot, Vector3 world)
        {
            int fw = FootW(def, rot), fd = FootD(def, rot);
            int x = Mathf.RoundToInt(world.x - fw * 0.5f);
            int z = Mathf.RoundToInt(world.z - fd * 0.5f);
            x = Mathf.Clamp(x, 0, Size - fw);
            z = Mathf.Clamp(z, 0, Size - fd);
            return new Vector2Int(x, z);
        }

        public bool CanPlace(ItemDef def, int x, int z, int rot, PlacedItem ignore = null)
        {
            int fw = FootW(def, rot), fd = FootD(def, rot);
            if (x < 0 || z < 0 || x + fw > Size || z + fd > Size) return false;
            for (int cx = x; cx < x + fw; cx++)
            for (int cz = z; cz < z + fd; cz++)
            {
                if (!def.flat)
                    foreach (var dc in DoorCells)
                        if (dc.x == cx && dc.y == cz) return false;
                foreach (var it in Items)
                {
                    if (it == ignore || it.def.flat != def.flat) continue;
                    if (cx >= it.x && cx < it.x + it.FootW && cz >= it.z && cz < it.z + it.FootD) return false;
                }
            }
            return true;
        }

        public PlacedItem Place(ItemDef def, int x, int z, int rot, bool notify = true)
        {
            var model = ItemModels.Build(def);
            model.transform.SetParent(transform, false);
            var item = model.AddComponent<PlacedItem>();
            item.def = def;
            SetTransform(item, x, z, rot);
            AddCollider(model);
            Items.Add(item);
            if (notify) Changed?.Invoke();
            return item;
        }

        public void Move(PlacedItem item, int x, int z, int rot)
        {
            if (item.occupant != null) item.occupant.DropToFloor();
            SetTransform(item, x, z, rot);
            Changed?.Invoke();
        }

        public void Remove(PlacedItem item)
        {
            if (item.occupant != null) item.occupant.DropToFloor();
            Items.Remove(item);
            Destroy(item.gameObject);
            Changed?.Invoke();
        }

        static void SetTransform(PlacedItem item, int x, int z, int rot)
        {
            item.x = x; item.z = z; item.rot = ((rot % 4) + 4) % 4;
            item.transform.localPosition = FootprintCenter(item.def, x, z, item.rot);
            item.transform.localRotation = Quaternion.Euler(0, item.rot * 90f, 0);
        }

        static void AddCollider(GameObject model)
        {
            var bounds = new Bounds(model.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            var box = model.AddComponent<BoxCollider>();
            var size = model.transform.InverseTransformVector(bounds.size);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Max(0.15f, Mathf.Abs(size.y)), Mathf.Abs(size.z));
            box.center = model.transform.InverseTransformPoint(bounds.center);
            box.size = size;
        }

        public List<PlacedItemData> ToSaveData()
        {
            var list = new List<PlacedItemData>();
            foreach (var i in Items) list.Add(new PlacedItemData { id = i.def.id, x = i.x, z = i.z, rot = i.rot });
            return list;
        }

        // ---------------- Queries ----------------

        public PlacedItem FindFree(Predicate<PlacedItem> match, Vector3 near)
        {
            PlacedItem best = null;
            float bestScore = float.MaxValue;
            foreach (var it in Items)
            {
                if (it.occupant != null || !match(it)) continue;
                // a bit of randomness so cats don't always pick the same thing
                float score = Vector3.Distance(near, it.transform.position) + UnityEngine.Random.value * 3f;
                if (score < bestScore) { bestScore = score; best = it; }
            }
            return best;
        }

        public PlacedItem NearestBowl(Vector3 near)
        {
            PlacedItem best = null;
            float bestD = float.MaxValue;
            foreach (var it in Items)
            {
                if (it.def.use != UseKind.Bowl) continue;
                float d = Vector3.Distance(near, it.transform.position);
                if (d < bestD) { bestD = d; best = it; }
            }
            return best;
        }

        public static Vector3 ClampToFloor(Vector3 p)
        {
            p.x = Mathf.Clamp(p.x, Margin, Size - Margin);
            p.z = Mathf.Clamp(p.z, Margin, Size - Margin);
            return p;
        }

        public static Vector3 RandomFloorPoint()
        {
            return new Vector3(UnityEngine.Random.Range(0.8f, Size - 0.8f), 0f, UnityEngine.Random.Range(0.8f, Size - 0.8f));
        }

        public static bool RaycastFloor(Ray ray, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = Vector3.zero;
            return false;
        }

        // ---------------- Door ----------------

        public void OpenDoor(float seconds)
        {
            doorTimer = Mathf.Max(doorTimer, seconds);
        }

        void Update()
        {
            if (doorPivot == null) return;
            doorTimer -= Time.deltaTime;
            float target = doorTimer > 0f ? -80f : 0f;
            doorAngle = Mathf.MoveTowards(doorAngle, target, 220f * Time.deltaTime);
            doorPivot.localRotation = Quaternion.Euler(0, doorAngle, 0);
        }
    }
}
