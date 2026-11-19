using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RecolecEco
{
    /// <summary>
    /// Generación de mallas low-poly originales (casco de la chalana, rocas, terreno, agua, etc.).
    /// Usado por el constructor del editor; las mallas se guardan como assets del proyecto.
    /// </summary>
    public static class MeshGen
    {
        /// <summary>Ayudante para construir mallas con sombreado plano y color por vértice.</summary>
        public class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Color> c = new List<Color>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 d, Color col)
            {
                Vector3 nn = Vector3.Cross(b - a, d - a);
                if (nn.sqrMagnitude < 1e-12f) return;
                nn.Normalize();
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(d);
                n.Add(nn); n.Add(nn); n.Add(nn);
                c.Add(col); c.Add(col); c.Add(col);
                uv.Add(new Vector2(a.x, a.z)); uv.Add(new Vector2(b.x, b.z)); uv.Add(new Vector2(d.x, d.z));
                t.Add(i); t.Add(i + 1); t.Add(i + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
            {
                Tri(a, b, d, col);
                Tri(a, d, e, col);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0);
                m.RecalculateBounds();
                m.RecalculateTangents();
                return m;
            }
        }

        // ---------------------------------------------------------------- Casco de chalana
        public struct HullSection
        {
            public float z, halfWidth, bottom, top;
            public HullSection(float z, float hw, float b, float t) { this.z = z; halfWidth = hw; bottom = b; top = t; }
        }

        public static readonly HullSection[] ChalanaSections =
        {
            new HullSection(-3.5f, 1.02f, -0.48f, 0.78f),
            new HullSection(-2.6f, 1.15f, -0.56f, 0.74f),
            new HullSection(-1.4f, 1.22f, -0.60f, 0.72f),
            new HullSection(-0.2f, 1.22f, -0.60f, 0.73f),
            new HullSection(1.0f, 1.12f, -0.56f, 0.78f),
            new HullSection(2.1f, 0.88f, -0.46f, 0.88f),
            new HullSection(2.9f, 0.52f, -0.28f, 1.02f),
            new HullSection(3.5f, 0.14f, 0.02f, 1.18f),
            new HullSection(3.75f, 0.0f, 0.35f, 1.30f),
        };

        static Vector3 Profile(HullSection s, float side, float k)
        {
            // k: 0 = quilla, 1 = borda. Perfil con pantoque redondeado.
            float y = Mathf.Lerp(s.bottom, s.top, k);
            float w;
            if (k < 0.25f) w = Mathf.Lerp(0.0f, 0.82f, Mathf.Sqrt(k / 0.25f));
            else w = Mathf.Lerp(0.82f, 1.0f, Mathf.Clamp01((k - 0.25f) / 0.45f));
            return new Vector3(side * s.halfWidth * w, y, s.z);
        }

        /// <summary>Casco con franjas de color (antiincrustante, blanco, color principal, franja, borda).</summary>
        public static Mesh ChalanaHull(Color bottomCol, Color whiteCol, Color mainCol, Color stripeCol, Color rimCol, Color insideCol, float scale = 1f)
        {
            var b = new Builder();
            float[] ks = { 0f, 0.12f, 0.25f, 0.36f, 0.42f, 0.70f, 0.80f, 0.92f, 1f };
            Color[] cols = { bottomCol, bottomCol, bottomCol, bottomCol, whiteCol, mainCol, stripeCol, mainCol, rimCol };
            var S = ChalanaSections;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < S.Length - 1; i++)
                {
                    for (int j = 0; j < ks.Length - 1; j++)
                    {
                        Vector3 a = Profile(S[i], side, ks[j]) * scale;
                        Vector3 bb = Profile(S[i + 1], side, ks[j]) * scale;
                        Vector3 cc = Profile(S[i + 1], side, ks[j + 1]) * scale;
                        Vector3 d = Profile(S[i], side, ks[j + 1]) * scale;
                        Color col = cols[j];
                        if (side > 0) b.Quad(a, bb, cc, d, col); else b.Quad(a, d, cc, bb, col);
                        // Cara interior (visible desde arriba), solo por encima de cubierta
                        if (ks[j] >= 0.42f)
                        {
                            if (side > 0) b.Quad(a, d, cc, bb, insideCol); else b.Quad(a, bb, cc, d, insideCol);
                        }
                    }
                }
            }
            // Espejo de popa
            var s0 = S[0];
            for (int j = 0; j < ks.Length - 1; j++)
            {
                Vector3 l0 = Profile(s0, -1, ks[j]) * scale, l1 = Profile(s0, -1, ks[j + 1]) * scale;
                Vector3 r0 = Profile(s0, 1, ks[j]) * scale, r1 = Profile(s0, 1, ks[j + 1]) * scale;
                b.Quad(l0, l1, r1, r0, j < 4 ? bottomCol : (j == 6 ? stripeCol : mainCol));
            }
            // Cubierta
            Color deck = new Color(0.72f, 0.52f, 0.32f);
            for (int i = 0; i < S.Length - 1; i++)
            {
                float k = 0.42f;
                Vector3 a = Profile(S[i], -1, k) * scale, bb = Profile(S[i + 1], -1, k) * scale;
                Vector3 cc = Profile(S[i + 1], 1, k) * scale, d = Profile(S[i], 1, k) * scale;
                b.Quad(a, bb, cc, d, i % 2 == 0 ? deck : deck * 0.93f + new Color(0, 0, 0, 0.07f));
            }
            return b.ToMesh("ChalanaHull");
        }

        public static float HullDeckY(float z, float scale = 1f)
        {
            var S = ChalanaSections;
            for (int i = 0; i < S.Length - 1; i++)
                if (z <= S[i + 1].z)
                {
                    float t = Mathf.InverseLerp(S[i].z, S[i + 1].z, z);
                    return Mathf.Lerp(Mathf.Lerp(S[i].bottom, S[i].top, 0.42f), Mathf.Lerp(S[i + 1].bottom, S[i + 1].top, 0.42f), t) * scale;
                }
            return 0.2f * scale;
        }

        // ---------------------------------------------------------------- Rocas
        public static Mesh Rock(int seed, float roughness, Color baseCol, Color topCol, int subdiv = 1, Vector3? stretch = null)
        {
            var rng = new System.Random(seed);
            GetIcosphere(subdiv, out var verts, out var tris);
            Vector3 st = stretch ?? Vector3.one;
            float ox = (float)rng.NextDouble() * 100f, oz = (float)rng.NextDouble() * 100f;
            for (int i = 0; i < verts.Count; i++)
            {
                var p = verts[i];
                float nz = Mathf.PerlinNoise(p.x * 1.7f + ox, p.z * 1.7f + oz) * 2f - 1f;
                float r = 1f + nz * roughness + ((float)rng.NextDouble() - 0.5f) * roughness * 0.6f;
                p *= r;
                if (p.y < -0.2f) p.y = -0.2f - (p.y + 0.2f) * 0.3f; // base aplanada
                verts[i] = Vector3.Scale(p, st);
            }
            var b = new Builder();
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 a = verts[tris[i]], bb = verts[tris[i + 1]], c = verts[tris[i + 2]];
                Vector3 nrm = Vector3.Cross(bb - a, c - a).normalized;
                float up = Mathf.Clamp01(nrm.y);
                float jitter = ((float)rng.NextDouble() - 0.5f) * 0.08f;
                Color col = Color.Lerp(baseCol, topCol, up * up) + new Color(jitter, jitter, jitter, 0);
                b.Tri(a, bb, c, col);
            }
            return b.ToMesh("Rock_" + seed);
        }

        static void GetIcosphere(int subdiv, out List<Vector3> verts, out List<int> tris)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            tris = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int s = 0; s < subdiv; s++)
            {
                var cache = new Dictionary<long, int>();
                var nt = new List<int>();
                var vv = verts;
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                    if (cache.TryGetValue(key, out int r)) return r;
                    vv.Add(((vv[a] + vv[b]) * 0.5f).normalized);
                    cache[key] = vv.Count - 1;
                    return vv.Count - 1;
                }
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = nt;
            }
        }

        // ---------------------------------------------------------------- Terreno
        public static Mesh Terrain(float minX, float maxX, float minZ, float maxZ, float cell, Func<float, float, float> height, Func<float, float, float, float, Color> color)
        {
            var b = new Builder();
            int nx = Mathf.CeilToInt((maxX - minX) / cell), nz = Mathf.CeilToInt((maxZ - minZ) / cell);
            var h = new float[nx + 1, nz + 1];
            for (int i = 0; i <= nx; i++)
                for (int j = 0; j <= nz; j++)
                    h[i, j] = height(minX + i * cell, minZ + j * cell);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x0 = minX + i * cell, z0 = minZ + j * cell, x1 = x0 + cell, z1 = z0 + cell;
                    float h00 = h[i, j], h10 = h[i + 1, j], h01 = h[i, j + 1], h11 = h[i + 1, j + 1];
                    if (Mathf.Max(Mathf.Max(h00, h10), Mathf.Max(h01, h11)) < -3.5f) continue; // fondo oculto bajo el agua
                    Vector3 a = new Vector3(x0, h00, z0), bb = new Vector3(x1, h10, z0), c = new Vector3(x1, h11, z1), d = new Vector3(x0, h01, z1);
                    bool flip = ((i + j) & 1) == 0;
                    if (flip)
                    {
                        AddTerrainTri(b, a, d, c, color);
                        AddTerrainTri(b, a, c, bb, color);
                    }
                    else
                    {
                        AddTerrainTri(b, a, d, bb, color);
                        AddTerrainTri(b, bb, d, c, color);
                    }
                }
            return b.ToMesh("Terrain");
        }

        static void AddTerrainTri(Builder b, Vector3 a, Vector3 c, Vector3 d, Func<float, float, float, float, Color> color)
        {
            Vector3 nrm = Vector3.Cross(c - a, d - a).normalized;
            Vector3 center = (a + c + d) / 3f;
            b.Tri(a, c, d, color(center.x, center.y, center.z, 1f - nrm.y));
        }

        // ---------------------------------------------------------------- Agua
        public static Mesh WaterGrid(float size, int cells)
        {
            var m = new Mesh { name = "WaterGrid", indexFormat = IndexFormat.UInt32 };
            int n = cells + 1;
            var v = new Vector3[n * n];
            var nn = new Vector3[n * n];
            var uv = new Vector2[n * n];
            float half = size * 0.5f, step = size / cells;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    v[j * n + i] = new Vector3(-half + i * step, 0, -half + j * step);
                    nn[j * n + i] = Vector3.up;
                    uv[j * n + i] = new Vector2(i / (float)cells, j / (float)cells);
                }
            var t = new int[cells * cells * 6];
            int k = 0;
            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                {
                    int a = j * n + i;
                    t[k++] = a; t[k++] = a + n; t[k++] = a + n + 1;
                    t[k++] = a; t[k++] = a + n + 1; t[k++] = a + 1;
                }
            m.vertices = v; m.normals = nn; m.uv = uv; m.triangles = t;
            m.bounds = new Bounds(Vector3.zero, new Vector3(size, 4f, size));
            return m;
        }

        // ---------------------------------------------------------------- Formas simples
        public static Mesh Ring(float inner, float outer, int seg, string name = "Ring")
        {
            var b = new Builder();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 i0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), i1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                b.Quad(i0 * inner, i1 * inner, i1 * outer, i0 * outer, Color.white);
            }
            // invertir para que mire hacia arriba
            var m = b.ToMesh(name);
            var tri = m.triangles;
            for (int i = 0; i < tri.Length; i += 3) (tri[i + 1], tri[i + 2]) = (tri[i + 2], tri[i + 1]);
            m.triangles = tri;
            var nr = m.normals; for (int i = 0; i < nr.Length; i++) nr[i] = Vector3.up; m.normals = nr;
            return m;
        }

        public static Mesh Torus(float radius, float tube, int seg, int sides, string name = "Torus")
        {
            var b = new Builder();
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    Vector3 P(int ii, int jj)
                    {
                        float u = ii * Mathf.PI * 2f / seg, w = jj * Mathf.PI * 2f / sides;
                        Vector3 c = new Vector3(Mathf.Cos(u), 0, Mathf.Sin(u)) * radius;
                        return c + (new Vector3(Mathf.Cos(u), 0, Mathf.Sin(u)) * Mathf.Cos(w) + Vector3.up * Mathf.Sin(w)) * tube;
                    }
                    b.Quad(P(i, j), P(i, j + 1), P(i + 1, j + 1), P(i + 1, j), Color.white);
                }
            return b.ToMesh(name);
        }

        /// <summary>Cono abierto (bolsa de red) apuntando hacia -Y.</summary>
        public static Mesh NetBag(float radius, float depth, int seg)
        {
            var b = new Builder();
            Vector3 tip = new Vector3(0, -depth, 0);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                Vector3 p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius, p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius;
                Vector3 m0 = Vector3.Lerp(p0, tip, 0.5f) * 1.08f, m1 = Vector3.Lerp(p1, tip, 0.5f) * 1.08f;
                b.Quad(p0, m0, m1, p1, Color.white);
                b.Tri(m0, tip, m1, Color.white);
                // cara interior
                b.Quad(p0, p1, m1, m0, Color.white);
                b.Tri(m0, m1, tip, Color.white);
            }
            return b.ToMesh("NetBag");
        }

        /// <summary>Techo a dos aguas (prisma triangular) de ancho 1, largo 1, alto 1.</summary>
        public static Mesh GableRoof()
        {
            var b = new Builder();
            Vector3 a = new Vector3(-0.5f, 0, -0.5f), bb = new Vector3(0.5f, 0, -0.5f), c = new Vector3(0, 1, -0.5f);
            Vector3 d = new Vector3(-0.5f, 0, 0.5f), e = new Vector3(0.5f, 0, 0.5f), f = new Vector3(0, 1, 0.5f);
            b.Tri(a, c, bb, Color.white);
            b.Tri(d, e, f, Color.white);
            b.Quad(a, d, f, c, Color.white);
            b.Quad(bb, c, f, e, Color.white);
            b.Quad(a, bb, e, d, Color.white);
            return b.ToMesh("GableRoof");
        }

        /// <summary>Pirámide de base cuadrada (techo, boya).</summary>
        public static Mesh Pyramid()
        {
            var b = new Builder();
            Vector3 top = new Vector3(0, 1, 0);
            Vector3[] p = { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) };
            for (int i = 0; i < 4; i++) b.Tri(p[i], top, p[(i + 1) % 4], Color.white);
            b.Quad(p[0], p[1], p[2], p[3], Color.white);
            return b.ToMesh("Pyramid");
        }

        /// <summary>Banderín triangular plano (doble cara).</summary>
        public static Mesh Pennant()
        {
            var b = new Builder();
            Vector3 a = new Vector3(0, 0, 0), bb = new Vector3(0, -0.5f, 0), c = new Vector3(0.9f, -0.25f, 0);
            Vector3 a2 = new Vector3(0, 0, 0.01f), b2 = new Vector3(0, -0.5f, 0.01f), c2 = new Vector3(0.9f, -0.25f, 0.01f);
            b.Tri(a, c, bb, Color.white);
            b.Tri(a2, b2, c2, Color.white);
            return b.ToMesh("Pennant");
        }

        /// <summary>Pez low-poly (cuerpo + cola) a lo largo de +Z.</summary>
        public static Mesh Fish(Color body, Color belly)
        {
            var b = new Builder();
            Vector3 nose = new Vector3(0, 0, 0.5f), tail = new Vector3(0, 0, -0.35f);
            Vector3 top = new Vector3(0, 0.16f, 0.05f), bot = new Vector3(0, -0.13f, 0.05f), l = new Vector3(-0.09f, 0, 0.05f), r = new Vector3(0.09f, 0, 0.05f);
            b.Tri(nose, top, r, body); b.Tri(nose, r, bot, belly); b.Tri(nose, bot, l, belly); b.Tri(nose, l, top, body);
            b.Tri(tail, r, top, body); b.Tri(tail, bot, r, belly); b.Tri(tail, l, bot, belly); b.Tri(tail, top, l, body);
            Vector3 t1 = new Vector3(0, 0.18f, -0.6f), t2 = new Vector3(0, -0.18f, -0.6f);
            b.Tri(tail, t1, t2, body); b.Tri(tail, t2, t1, body);
            return b.ToMesh("Fish");
        }
    }
}
