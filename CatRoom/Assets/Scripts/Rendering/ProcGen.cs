using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    /// <summary>Procedural meshes and textures so the project needs no imported art.</summary>
    public static class ProcGen
    {
        static Mesh cone, torus, halfSphere;

        // ---------------- Meshes ----------------

        /// <summary>Cone with its base on y=0 and tip at y=1, radius 0.5.</summary>
        public static Mesh Cone
        {
            get
            {
                if (cone != null) return cone;
                const int seg = 10;
                var verts = new List<Vector3>();
                var tris = new List<int>();
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                    var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0, Mathf.Sin(a0) * 0.5f);
                    var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0, Mathf.Sin(a1) * 0.5f);
                    int b = verts.Count;
                    verts.Add(p0); verts.Add(Vector3.up); verts.Add(p1);
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    b = verts.Count;
                    verts.Add(p0); verts.Add(p1); verts.Add(Vector3.zero);
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                }
                cone = new Mesh { name = "Cone" };
                cone.SetVertices(verts);
                cone.SetTriangles(tris, 0);
                cone.RecalculateNormals();
                cone.RecalculateBounds();
                return cone;
            }
        }

        /// <summary>Torus lying flat (XZ plane), major radius 0.5, minor radius 0.18.</summary>
        public static Mesh Torus
        {
            get
            {
                if (torus != null) return torus;
                const int seg = 24, side = 12;
                const float R = 0.5f, r = 0.18f;
                var verts = new Vector3[(seg + 1) * (side + 1)];
                var normals = new Vector3[verts.Length];
                var uvs = new Vector2[verts.Length];
                for (int i = 0; i <= seg; i++)
                {
                    float u = i * Mathf.PI * 2f / seg;
                    var center = new Vector3(Mathf.Cos(u) * R, 0, Mathf.Sin(u) * R);
                    for (int j = 0; j <= side; j++)
                    {
                        float v = j * Mathf.PI * 2f / side;
                        var n = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(v), Mathf.Sin(u) * Mathf.Cos(v));
                        int idx = i * (side + 1) + j;
                        verts[idx] = center + n * r;
                        normals[idx] = n;
                        uvs[idx] = new Vector2((float)i / seg, (float)j / side);
                    }
                }
                var tris = new List<int>();
                for (int i = 0; i < seg; i++)
                for (int j = 0; j < side; j++)
                {
                    int a = i * (side + 1) + j, b = (i + 1) * (side + 1) + j;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
                }
                torus = new Mesh { name = "Torus", vertices = verts, normals = normals, uv = uvs };
                torus.SetTriangles(tris, 0);
                torus.RecalculateBounds();
                return torus;
            }
        }

        /// <summary>Upper hemisphere, radius 0.5, flat side at y=0.</summary>
        public static Mesh HalfSphere
        {
            get
            {
                if (halfSphere != null) return halfSphere;
                const int seg = 20, rings = 8;
                var verts = new List<Vector3>();
                var normals = new List<Vector3>();
                var tris = new List<int>();
                for (int r = 0; r <= rings; r++)
                {
                    float phi = r * Mathf.PI * 0.5f / rings;
                    for (int s = 0; s <= seg; s++)
                    {
                        float th = s * Mathf.PI * 2f / seg;
                        var n = new Vector3(Mathf.Cos(th) * Mathf.Cos(phi), Mathf.Sin(phi), Mathf.Sin(th) * Mathf.Cos(phi));
                        verts.Add(n * 0.5f);
                        normals.Add(n);
                    }
                }
                for (int r = 0; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = r * (seg + 1) + s, b = (r + 1) * (seg + 1) + s;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
                // bottom cap
                int c = verts.Count;
                verts.Add(Vector3.zero); normals.Add(Vector3.down);
                for (int s = 0; s <= seg; s++)
                {
                    float th = s * Mathf.PI * 2f / seg;
                    verts.Add(new Vector3(Mathf.Cos(th) * 0.5f, 0, Mathf.Sin(th) * 0.5f));
                    normals.Add(Vector3.down);
                }
                for (int s = 0; s < seg; s++) { tris.Add(c); tris.Add(c + 1 + s); tris.Add(c + 2 + s); }
                halfSphere = new Mesh { name = "HalfSphere" };
                halfSphere.SetVertices(verts);
                halfSphere.SetNormals(normals);
                halfSphere.SetTriangles(tris, 0);
                halfSphere.RecalculateBounds();
                return halfSphere;
            }
        }

        // ---------------- Textures ----------------

        static Texture2D NewTex(int w, int h, bool repeat = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2,
            };
            return t;
        }

        public static Texture2D Wallpaper(ItemDef def)
        {
            const int S = 128;
            var t = NewTex(S, S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color c = def.colorA;
                switch (def.pattern)
                {
                    case 1: // vertical stripes
                        c = (x / 16) % 2 == 0 ? def.colorA : def.colorB;
                        break;
                    case 2: // polka dots
                    {
                        float dx = (x % 32) - 16f, dy = (y % 32) - 16f;
                        float ox = ((x + 16) % 32) - 16f, oy = ((y + 16) % 32) - 16f;
                        bool dot = dx * dx + dy * dy < 25f || ox * ox + oy * oy < 25f;
                        c = dot ? def.colorB : def.colorA;
                        break;
                    }
                    case 3: // plaid
                    {
                        bool a = (x / 16) % 4 == 0, b = (y / 16) % 4 == 0;
                        c = a && b ? def.colorB * 0.92f : (a || b ? def.colorB : def.colorA);
                        c.a = 1f;
                        break;
                    }
                    default:
                    {
                        // subtle vertical texture
                        float n = Mathf.PerlinNoise(x * 0.05f, y * 0.4f);
                        c = Color.Lerp(def.colorA, def.colorB, n * 0.6f);
                        break;
                    }
                }
                px[y * S + x] = c;
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        public static Texture2D Flooring(ItemDef def)
        {
            const int S = 128;
            var t = NewTex(S, S);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color c;
                switch (def.pattern)
                {
                    case 1: // checker
                        c = ((x / 64) + (y / 64)) % 2 == 0 ? def.colorA : def.colorB;
                        if (x % 64 == 0 || y % 64 == 0) c *= 0.92f;
                        break;
                    case 2: // carpet
                    {
                        float n = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                        c = Color.Lerp(def.colorA, def.colorB, n);
                        break;
                    }
                    case 3: // tatami: mat with dark border
                    {
                        bool border = (y % 64) < 4 || (x % 128) < 4;
                        float n = Mathf.PerlinNoise(x * 0.8f, y * 0.05f) * 0.15f;
                        c = border ? def.colorB : def.colorA * (0.92f + n);
                        break;
                    }
                    default: // planks
                    {
                        int plank = y / 32;
                        int offset = (plank % 2) * 48;
                        bool seam = (y % 32) < 2 || ((x + offset) % 96) < 2;
                        float grain = Mathf.PerlinNoise(x * 0.06f + plank * 7.3f, y * 0.6f);
                        float tone = 0.94f + ((plank * 37) % 5) * 0.025f;
                        c = Color.Lerp(def.colorA, def.colorB, grain * 0.7f) * tone;
                        if (seam) c = def.colorB * 0.8f;
                        break;
                    }
                }
                c.a = 1f;
                px[y * S + x] = c;
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        public static Texture2D Grid(int cells)
        {
            const int P = 32;
            int S = P * cells;
            var t = NewTex(S, S, false);
            t.filterMode = FilterMode.Bilinear;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                bool line = x % P == 0 || y % P == 0 || x % P == P - 1 || y % P == P - 1;
                px[y * S + x] = line ? new Color(1, 1, 1, 0.9f) : new Color(1, 1, 1, 0.08f);
            }
            t.SetPixels(px);
            t.Apply(true);
            return t;
        }

        // ---------------- UI sprites ----------------

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        static Sprite MakeSprite(string key, int size, System.Func<float, float, float> alpha, Vector4 border)
        {
            if (sprites.TryGetValue(key, out var s) && s != null) return s;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)));
            t.SetPixels(px);
            t.Apply();
            s = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s.name = key;
            sprites[key] = s;
            return s;
        }

        /// <summary>9-sliced rounded rectangle.</summary>
        public static Sprite RoundedRect
        {
            get
            {
                const int S = 64; const float R = 24f;
                return MakeSprite("rounded", S, (x, y) =>
                {
                    float dx = Mathf.Max(0, Mathf.Max(R - x, x - (S - R)));
                    float dy = Mathf.Max(0, Mathf.Max(R - y, y - (S - R)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    return R - d + 0.5f;
                }, new Vector4(R + 2, R + 2, R + 2, R + 2));
            }
        }

        public static Sprite Circle
        {
            get
            {
                const int S = 64;
                return MakeSprite("circle", S, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(S / 2f, S / 2f));
                    return S / 2f - d;
                }, Vector4.zero);
            }
        }

        public static Sprite Heart
        {
            get
            {
                const int S = 64;
                return MakeSprite("heart", S, (x, y) =>
                {
                    // classic implicit heart curve
                    float u = (x / S - 0.5f) * 2.6f, v = (y / S - 0.42f) * 2.6f;
                    float a = u * u + v * v - 1f;
                    float f = a * a * a - u * u * v * v * v;
                    return f < 0 ? Mathf.Clamp01(-f * 40f) : 0f;
                }, Vector4.zero);
            }
        }

        /// <summary>Paw print.</summary>
        public static Sprite Paw
        {
            get
            {
                const int S = 64;
                return MakeSprite("paw", S, (x, y) =>
                {
                    float best = -999f;
                    best = Mathf.Max(best, Ellipse(x, y, 32, 22, 15, 12));
                    best = Mathf.Max(best, Ellipse(x, y, 14, 38, 7, 8));
                    best = Mathf.Max(best, Ellipse(x, y, 26, 48, 7, 8));
                    best = Mathf.Max(best, Ellipse(x, y, 38, 48, 7, 8));
                    best = Mathf.Max(best, Ellipse(x, y, 50, 38, 7, 8));
                    return best;
                }, Vector4.zero);
            }
        }

        /// <summary>Coin: filled disc with an inner ring.</summary>
        public static Sprite CoinRing
        {
            get
            {
                const int S = 64;
                return MakeSprite("coinring", S, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                    float ring = Mathf.Abs(d - 20f);
                    return 2.2f - ring;
                }, Vector4.zero);
            }
        }

        static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            return (1f - d) * Mathf.Min(rx, ry) + 0.5f;
        }
    }
}
