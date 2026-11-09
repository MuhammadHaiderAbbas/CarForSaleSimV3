using System;
using System.Collections.Generic;

namespace CarSales.Foundation.Graphics
{
    /// <summary>Plain mesh data (no engine types) - the editor copies it into a UnityEngine.Mesh.</summary>
    public sealed class MeshData
    {
        public float[] Positions = Array.Empty<float>();   // xyz
        public float[] Normals = Array.Empty<float>();     // xyz
        public float[] Tangents = Array.Empty<float>();    // xyzw (w = bitangent sign)
        public float[] Uvs = Array.Empty<float>();         // uv (metres)
        public List<int[]> Submeshes = new List<int[]>();
        public int VertexCount => Positions.Length / 3;
    }

    /// <summary>
    /// Phase 13a: replacements for Unity's cube, plane and cylinder whose texture coordinates are in metres, so one
    /// texture repeats at its real size (a brick is a brick on a 2 m wall and on a 40 m wall). Geometry matches the
    /// built-in primitives (unit cube / 10 x 10 plane / 1 x 2 x 1 cylinder) so they can be swapped in place.
    /// </summary>
    public static class MetricMesh
    {
        /// <summary>
        /// Unit cube (-0.5..0.5) whose UVs are in metres for an object scaled to <paramref name="sx"/>, <paramref name="sy"/>,
        /// <paramref name="sz"/>. Walls start at V = 0 on their bottom edge and U runs left to right seen from outside.
        /// With <paramref name="splitCaps"/> the top and bottom are a second submesh (e.g. a façade with a roof material).
        /// </summary>
        public static MeshData Box(float sx, float sy, float sz, bool splitCaps)
        {
            var d = new Builder();
            var sides = new List<int>();
            var caps = splitCaps ? new List<int>() : sides;
            // (normal, right, up, width along right, height along up)
            d.Face(sides, V(0, 0, 1), V(-1, 0, 0), V(0, 1, 0), sx, sy);    // +Z (seen from +Z, right is -X)
            d.Face(sides, V(0, 0, -1), V(1, 0, 0), V(0, 1, 0), sx, sy);    // -Z
            d.Face(sides, V(1, 0, 0), V(0, 0, 1), V(0, 1, 0), sz, sy);     // +X
            d.Face(sides, V(-1, 0, 0), V(0, 0, -1), V(0, 1, 0), sz, sy);   // -X
            d.Face(caps, V(0, 1, 0), V(1, 0, 0), V(0, 0, 1), sx, sz);      // top
            d.Face(caps, V(0, -1, 0), V(1, 0, 0), V(0, 0, -1), sx, sz);    // bottom
            var m = d.ToData();
            m.Submeshes.Add(sides.ToArray());
            if (splitCaps) m.Submeshes.Add(caps.ToArray());
            return m;
        }

        /// <summary>A 10 x 10 unit plane facing +Y (like Unity's), <paramref name="cells"/> quads a side, UVs in metres for the given scale.</summary>
        public static MeshData Plane(float scaleX, float scaleZ, int cells = 10)
        {
            if (cells < 1) cells = 1;
            var d = new Builder();
            var tris = new List<int>();
            float step = 10f / cells;
            for (int z = 0; z <= cells; z++)
                for (int x = 0; x <= cells; x++)
                {
                    float px = -5f + x * step, pz = -5f + z * step;
                    d.Vertex(px, 0f, pz, 0f, 1f, 0f, 1f, 0f, 0f, -1f, (px + 5f) * scaleX, (pz + 5f) * scaleZ);   // bitangent +Z
                }
            int row = cells + 1;
            for (int z = 0; z < cells; z++)
                for (int x = 0; x < cells; x++)
                {
                    int a = z * row + x, b = a + 1, c = a + row, e = c + 1;
                    // Clockwise seen from above (+Y): a -> c -> e, a -> e -> b.
                    tris.Add(a); tris.Add(c); tris.Add(e);
                    tris.Add(a); tris.Add(e); tris.Add(b);
                }
            var m = d.ToData();
            m.Submeshes.Add(tris.ToArray());
            return m;
        }

        /// <summary>
        /// Cylinder like Unity's (radius 0.5, height 2 = y -1..1) with UVs in metres: U around the side, V up it;
        /// the caps are mapped flat. <paramref name="sx"/>/<paramref name="sy"/>/<paramref name="sz"/> are the object's scale.
        /// </summary>
        public static MeshData Cylinder(float sx, float sy, float sz, int segments = 24)
        {
            if (segments < 3) segments = 3;
            var d = new Builder();
            var tris = new List<int>();
            float radiusM = 0.25f * (sx + sz);                   // average radius in metres
            float circumference = (float)(2 * Math.PI) * radiusM, height = 2f * sy;
            for (int i = 0; i <= segments; i++)
            {
                double a = 2 * Math.PI * i / segments;
                float cx = (float)Math.Cos(a), cz = (float)Math.Sin(a);
                float u = circumference * i / segments;
                // Tangent follows increasing angle: d/da (cos, sin) = (-sin, cos).
                d.Vertex(cx * 0.5f, -1f, cz * 0.5f, cx, 0f, cz, -cz, 0f, cx, 0f, u, 0f);
                d.Vertex(cx * 0.5f, 1f, cz * 0.5f, cx, 0f, cz, -cz, 0f, cx, 0f, u, height);
            }
            for (int i = 0; i < segments; i++)
            {
                // Seen from outside, increasing angle runs left-to-right: a/b = bottom/top left, c/e = bottom/top right (clockwise = front).
                int a = i * 2, b = a + 1, c = a + 2, e = a + 3;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(c); tris.Add(b); tris.Add(e);
            }
            d.FixTangentSigns();   // bitangent must point up (+Y) so V increases upwards
            int capStart = d.Count;
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? 1f : -1f, ny = y;
                int centre = d.Count;
                d.Vertex(0f, y, 0f, 0f, ny, 0f, 1f, 0f, 0f, 1f, 0.5f * sx, 0.5f * sz);
                for (int i = 0; i <= segments; i++)
                {
                    double a = 2 * Math.PI * i / segments;
                    float cx = (float)Math.Cos(a) * 0.5f, cz = (float)Math.Sin(a) * 0.5f;
                    d.Vertex(cx, y, cz, 0f, ny, 0f, 1f, 0f, 0f, 1f, (cx + 0.5f) * sx, (cz + 0.5f) * sz);
                }
                for (int i = 0; i < segments; i++)
                {
                    int p = centre + 1 + i, q = p + 1;
                    if (cap == 0) { tris.Add(centre); tris.Add(q); tris.Add(p); }   // top, clockwise from above
                    else { tris.Add(centre); tris.Add(p); tris.Add(q); }
                }
            }
            d.FixTangentSigns(capStart);
            var m = d.ToData();
            m.Submeshes.Add(tris.ToArray());
            return m;
        }

        /// <summary>Cache key for a scale (rounded to 1 cm) so equal-sized objects share one mesh.</summary>
        public static string Key(string kind, float sx, float sy, float sz, bool split = false)
        {
            static int R(float v) => (int)Math.Round(Math.Abs(v) * 100f);
            return $"{kind}_{R(sx)}_{R(sy)}_{R(sz)}{(split ? "_s" : "")}";
        }

        // ------------------------------------------------------------------ HELPERS
        static float[] V(float x, float y, float z) => new[] { x, y, z };

        sealed class Builder
        {
            readonly List<float> p = new List<float>(), n = new List<float>(), t = new List<float>(), uv = new List<float>();
            public int Count => p.Count / 3;

            public void Vertex(float px, float py, float pz, float nx, float ny, float nz, float tx, float ty, float tz, float tw, float u, float v)
            {
                p.Add(px); p.Add(py); p.Add(pz);
                n.Add(nx); n.Add(ny); n.Add(nz);
                t.Add(tx); t.Add(ty); t.Add(tz); t.Add(tw);
                uv.Add(u); uv.Add(v);
            }

            /// <summary>A quad on the unit cube: corners bottom-left, top-left, top-right, bottom-right seen from outside (clockwise = front).</summary>
            public void Face(List<int> tris, float[] normal, float[] right, float[] up, float width, float height)
            {
                int start = Count;
                float sign = Dot(Cross(normal, right), up) < 0f ? -1f : 1f;
                for (int k = 0; k < 4; k++)
                {
                    float ru = k >= 2 ? 0.5f : -0.5f, uu = k == 1 || k == 2 ? 0.5f : -0.5f;
                    float x = normal[0] * 0.5f + right[0] * ru + up[0] * uu;
                    float y = normal[1] * 0.5f + right[1] * ru + up[1] * uu;
                    float z = normal[2] * 0.5f + right[2] * ru + up[2] * uu;
                    Vertex(x, y, z, normal[0], normal[1], normal[2], right[0], right[1], right[2], sign, (ru + 0.5f) * Math.Abs(width), (uu + 0.5f) * Math.Abs(height));
                }
                tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
            }

            /// <summary>Sets each tangent's w so that cross(normal, tangent) * w points along increasing V (Unity's convention).</summary>
            public void FixTangentSigns(int from = 0)
            {
                // For the generated shapes V increases with +Y on sides and +Z on caps; derive the sign from that.
                for (int i = from; i < Count; i++)
                {
                    float[] nn = { n[i * 3], n[i * 3 + 1], n[i * 3 + 2] }, tt = { t[i * 4], t[i * 4 + 1], t[i * 4 + 2] };
                    float[] b = Cross(nn, tt);
                    float[] upDir = Math.Abs(nn[1]) > 0.5f ? new[] { 0f, 0f, 1f } : new[] { 0f, 1f, 0f };
                    t[i * 4 + 3] = Dot(b, upDir) < 0f ? -1f : 1f;
                }
            }

            public MeshData ToData() => new MeshData { Positions = p.ToArray(), Normals = n.ToArray(), Tangents = t.ToArray(), Uvs = uv.ToArray() };
        }

        static float[] Cross(float[] a, float[] b) => new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        static float Dot(float[] a, float[] b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
    }
}
