using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    /// <summary>Renders 3D item and cat models into sprites for the shop and menus.</summary>
    public class IconRenderer : MonoBehaviour
    {
        public const int IconLayer = 30;
        const int Size = 192;
        static readonly Vector3 StudioPos = new Vector3(0f, -300f, 0f);

        Camera cam;
        RenderTexture rt;
        readonly Dictionary<string, Sprite> items = new Dictionary<string, Sprite>();
        readonly Dictionary<int, Sprite> cats = new Dictionary<int, Sprite>();

        public void Init()
        {
            rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { name = "IconRT" };
            rt.Create();
            var go = new GameObject("IconCamera");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << IconLayer;
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.transform.rotation = Quaternion.Euler(28f, -40f, 0f);
        }

        public Sprite Item(ItemDef def)
        {
            if (def == null) return null;
            if (items.TryGetValue(def.id, out var s) && s != null) return s;
            var model = ItemModels.Build(def);
            s = Capture(model, def.id);
            items[def.id] = s;
            return s;
        }

        public Sprite Cat(int preset)
        {
            if (cats.TryGetValue(preset, out var s) && s != null) return s;
            var holder = new GameObject("CatIcon");
            CatVisual.Build(holder.transform, preset, 1f);
            holder.transform.rotation = Quaternion.Euler(0, 20f, 0);
            s = Capture(holder, "cat" + preset);
            cats[preset] = s;
            return s;
        }

        Sprite Capture(GameObject model, string name)
        {
            model.transform.position = StudioPos;
            Toon.SetLayerRecursive(model, IconLayer);

            var renderers = model.GetComponentsInChildren<Renderer>();
            var b = new Bounds(StudioPos, Vector3.one * 0.1f);
            if (renderers.Length > 0) b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            float radius = Mathf.Max(0.25f, b.extents.magnitude);
            cam.orthographicSize = radius * 0.95f;
            cam.transform.position = b.center - cam.transform.forward * 15f;

            var prev = RenderTexture.active;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "Icon_" + name, wrapMode = TextureWrapMode.Clamp };
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply(true);
            RenderTexture.active = prev;

            model.SetActive(false);
            Destroy(model);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
        }
    }
}
