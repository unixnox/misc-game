using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    /// <summary>Material cache for the cartoon shaders, plus a helper to build models out of primitives.</summary>
    public static class Toon
    {
        static Shader toonShader, outlineShader, ghostShader;
        static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();

        public static readonly Color OutlineColor = new Color(0.3f, 0.2f, 0.17f);

        static void EnsureShaders()
        {
            if (toonShader != null) return;
            toonShader = Shader.Find("CatRoom/Toon");
            outlineShader = Shader.Find("CatRoom/ToonOutline");
            ghostShader = Shader.Find("CatRoom/Ghost");
            // Fallbacks keep the game playable even if the custom shaders failed to compile on a device.
            if (toonShader == null || !toonShader.isSupported) toonShader = Shader.Find("Legacy Shaders/Diffuse");
            if (outlineShader == null || !outlineShader.isSupported) outlineShader = toonShader;
            if (ghostShader == null || !ghostShader.isSupported) ghostShader = Shader.Find("Sprites/Default");
        }

        /// <summary>Shared material for a flat color. Do not modify the returned material.</summary>
        public static Material Get(Color c, bool outline = false, float outlineWidth = 0.015f, float emission = 0f)
        {
            EnsureShaders();
            Color32 c32 = c;
            long key = ((long)c32.r << 32) | ((long)c32.g << 24) | ((long)c32.b << 16)
                       | ((long)Mathf.RoundToInt(outlineWidth * 1000f) << 4) | (outline ? 1L : 0L)
                       | ((long)Mathf.RoundToInt(emission * 10f) << 40);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(outline ? outlineShader : toonShader) { name = "Toon_" + ColorUtility.ToHtmlStringRGB(c) };
            m.color = c;
            if (outline)
            {
                m.SetFloat("_OutlineWidth", outlineWidth);
                m.SetColor("_OutlineColor", OutlineColor);
            }
            if (emission > 0f) m.SetColor("_Emission", c * emission);
            cache[key] = m;
            return m;
        }

        /// <summary>A new (unshared) textured toon material.</summary>
        public static Material Textured(Texture2D tex, Vector2 tiling)
        {
            EnsureShaders();
            var m = new Material(toonShader) { name = "ToonTex" };
            m.mainTexture = tex;
            m.mainTextureScale = tiling;
            return m;
        }

        public static Material Ghost(Color c, Texture2D tex = null)
        {
            EnsureShaders();
            var m = new Material(ghostShader) { name = "Ghost" };
            m.color = c;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        /// <summary>Creates a primitive without a collider, with a toon material.</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            bool outline = false, Vector3 euler = default(Vector3), float outlineWidth = 0.015f)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Get(color, outline, outlineWidth);
            return go;
        }

        /// <summary>Creates an object from a custom mesh with a toon material.</summary>
        public static GameObject MeshObj(Mesh mesh, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            bool outline = false, Vector3 euler = default(Vector3), float outlineWidth = 0.015f)
        {
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Get(color, outline, outlineWidth);
            return go;
        }

        public static Transform Pivot(string name, Transform parent, Vector3 localPos, Vector3 euler = default(Vector3))
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(euler);
            return t;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
