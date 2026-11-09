using System;
using System.Collections.Generic;

namespace CarSales.Foundation.Graphics
{
    /// <summary>The town's surfaces. Append new ids at the end (materials and saved choices use the numbers).</summary>
    public enum SurfaceId
    {
        Asphalt = 0, Concrete = 1, Paving = 2, Brick = 3, Render = 4, Grass = 5, FloorTile = 6, Marble = 7,
        Wood = 8, RoofMembrane = 9, RoofTiles = 10, Cladding = 11, Soil = 12, Wheat = 13, Fabric = 14,
        BrushedMetal = 15, Foliage = 16, Bark = 17, Carpet = 18, Epoxy = 19,
        FacadeOffice = 20, FacadeApartment = 21, FacadeBrick = 22, GrassMacro = 23,
    }

    /// <summary>How a surface is used: how many metres one texture covers and how shiny it is.</summary>
    public sealed class SurfaceInfo
    {
        public SurfaceId Id;
        public string Name = "";
        /// <summary>Metres covered by one copy of the texture (along U and V).</summary>
        public float TileU = 1f, TileV = 1f;
        /// <summary>Material smoothness scale; the mask texture's alpha holds the variation (0-1) on top of it.</summary>
        public float Smoothness = 0.3f;
        public float NormalStrength = 1f;
        public int Size = 512;
        public bool Emission;
        /// <summary>Gets darker and shinier in the rain.</summary>
        public bool Wettable;
        /// <summary>Neutral detail: the material's colour tints it (walls, fabric).</summary>
        public bool Tintable;
    }

    /// <summary>One generated texture set. Pixels are RGBA, row 0 = the bottom of the texture (V = 0), sRGB except Normal/Mask.</summary>
    public sealed class SurfaceMaps
    {
        public int Size;
        public byte[] Albedo = Array.Empty<byte>();
        public byte[] Normal = Array.Empty<byte>();
        /// <summary>R = metallic, G = ambient occlusion, B = 0, A = smoothness (relative).</summary>
        public byte[] Mask = Array.Empty<byte>();
        /// <summary>Night-time light (windows); empty when the surface has none.</summary>
        public byte[] Emission = Array.Empty<byte>();

        /// <summary>Average albedo (0-1, sRGB) - used to tint neutral textures to a wanted colour.</summary>
        public (float r, float g, float b) AverageAlbedo()
        {
            double r = 0, g = 0, b = 0; int n = Albedo.Length / 4;
            for (int i = 0; i < Albedo.Length; i += 4) { r += Albedo[i]; g += Albedo[i + 1]; b += Albedo[i + 2]; }
            return n == 0 ? (1f, 1f, 1f) : ((float)(r / n / 255.0), (float)(g / n / 255.0), (float)(b / n / 255.0));
        }
    }

    /// <summary>
    /// Phase 13a: procedural, seamlessly tiling PBR textures (albedo, normal, mask, emission) for every town surface,
    /// so nothing is a flat colour any more. Deterministic: the same id, size and seed always give the same pixels.
    /// Scanned textures (Poly Haven / ambientCG) can replace any of them in the Surface Library.
    /// </summary>
    public static class SurfaceGen
    {
        static readonly SurfaceInfo[] Infos =
        {
            new SurfaceInfo { Id = SurfaceId.Asphalt, Name = "Asphalt", TileU = 4f, TileV = 4f, Smoothness = 0.42f, NormalStrength = 1.4f, Wettable = true },
            new SurfaceInfo { Id = SurfaceId.Concrete, Name = "Concrete", TileU = 3f, TileV = 3f, Smoothness = 0.35f, NormalStrength = 1f, Wettable = true },
            new SurfaceInfo { Id = SurfaceId.Paving, Name = "Paving slabs", TileU = 1.2f, TileV = 1.2f, Smoothness = 0.32f, NormalStrength = 1.6f, Wettable = true },
            new SurfaceInfo { Id = SurfaceId.Brick, Name = "Brick", TileU = 0.9f, TileV = 1.2f, Smoothness = 0.22f, NormalStrength = 1.8f, Wettable = true },
            new SurfaceInfo { Id = SurfaceId.Render, Name = "Painted render", TileU = 2.5f, TileV = 2.5f, Smoothness = 0.25f, NormalStrength = 0.6f, Tintable = true },
            new SurfaceInfo { Id = SurfaceId.Grass, Name = "Grass", TileU = 3f, TileV = 3f, Smoothness = 0.14f, NormalStrength = 1.2f },
            new SurfaceInfo { Id = SurfaceId.FloorTile, Name = "Porcelain floor", TileU = 2.4f, TileV = 2.4f, Smoothness = 0.92f, NormalStrength = 0.8f, Size = 1024 },
            new SurfaceInfo { Id = SurfaceId.Marble, Name = "Marble floor", TileU = 2.4f, TileV = 2.4f, Smoothness = 0.95f, NormalStrength = 0.5f, Size = 1024 },
            new SurfaceInfo { Id = SurfaceId.Wood, Name = "Wood", TileU = 1f, TileV = 1f, Smoothness = 0.5f, NormalStrength = 0.7f, Tintable = true },
            new SurfaceInfo { Id = SurfaceId.RoofMembrane, Name = "Flat roof", TileU = 3f, TileV = 3f, Smoothness = 0.3f, NormalStrength = 1f, Wettable = true },
            new SurfaceInfo { Id = SurfaceId.RoofTiles, Name = "Roof tiles", TileU = 1.2f, TileV = 1.2f, Smoothness = 0.35f, NormalStrength = 2f, Tintable = true, Wettable = true },
            new SurfaceInfo { Id = SurfaceId.Cladding, Name = "Metal cladding", TileU = 1.2f, TileV = 1.2f, Smoothness = 0.6f, NormalStrength = 1.5f, Tintable = true },
            new SurfaceInfo { Id = SurfaceId.Soil, Name = "Ploughed soil", TileU = 6f, TileV = 6f, Smoothness = 0.12f, NormalStrength = 1.6f },
            new SurfaceInfo { Id = SurfaceId.Wheat, Name = "Wheat field", TileU = 4f, TileV = 4f, Smoothness = 0.15f, NormalStrength = 1f },
            new SurfaceInfo { Id = SurfaceId.Fabric, Name = "Upholstery", TileU = 0.5f, TileV = 0.5f, Smoothness = 0.2f, NormalStrength = 0.8f, Tintable = true },
            new SurfaceInfo { Id = SurfaceId.BrushedMetal, Name = "Brushed metal", TileU = 1f, TileV = 1f, Smoothness = 0.75f, NormalStrength = 0.3f, Tintable = true },
            new SurfaceInfo { Id = SurfaceId.Foliage, Name = "Foliage", TileU = 1f, TileV = 1f, Smoothness = 0.25f, NormalStrength = 2f },
            new SurfaceInfo { Id = SurfaceId.Bark, Name = "Bark", TileU = 1f, TileV = 1f, Smoothness = 0.1f, NormalStrength = 2f },
            new SurfaceInfo { Id = SurfaceId.Carpet, Name = "Office carpet", TileU = 1f, TileV = 1f, Smoothness = 0.05f, NormalStrength = 0.6f, Tintable = true },
            new SurfaceInfo { Id = SurfaceId.Epoxy, Name = "Workshop floor", TileU = 4f, TileV = 4f, Smoothness = 0.7f, NormalStrength = 0.4f },
            new SurfaceInfo { Id = SurfaceId.FacadeOffice, Name = "Office glass facade", TileU = 12f, TileV = 14.4f, Smoothness = 0.95f, NormalStrength = 1f, Size = 1024, Emission = true },
            new SurfaceInfo { Id = SurfaceId.FacadeApartment, Name = "Apartment facade", TileU = 12f, TileV = 12f, Smoothness = 0.6f, NormalStrength = 1.4f, Size = 1024, Emission = true },
            new SurfaceInfo { Id = SurfaceId.FacadeBrick, Name = "Brick facade", TileU = 12f, TileV = 12f, Smoothness = 0.6f, NormalStrength = 1.6f, Size = 1024, Emission = true },
            new SurfaceInfo { Id = SurfaceId.GrassMacro, Name = "Grass (large-scale variation)", TileU = 70f, TileV = 70f, Smoothness = 1f, NormalStrength = 0f, Size = 256 },
        };

        public static IReadOnlyList<SurfaceInfo> All => Infos;

        public static SurfaceInfo Info(SurfaceId id)
        {
            foreach (var i in Infos) if (i.Id == id) return i;
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        /// <summary>Bump when the generators change so cached textures are rebuilt.</summary>
        public const int Version = 1;

        // ------------------------------------------------------------------ PUBLIC
        public static SurfaceMaps Generate(SurfaceId id, int size = 0, int seed = 1)
        {
            var info = Info(id);
            if (size <= 0) size = info.Size;
            if (size < 8 || (size & (size - 1)) != 0) throw new ArgumentException("Size must be a power of two (8 or more).", nameof(size));
            var c = new Canvas(size, info.Emission);
            switch (id)
            {
                case SurfaceId.Asphalt: Asphalt(c, seed); break;
                case SurfaceId.Concrete: Concrete(c, seed); break;
                case SurfaceId.Paving: Paving(c, seed); break;
                case SurfaceId.Brick: Each(c, (i, u, v) => BrickPixel(c, i, u, v, seed, 4, 16)); break;
                case SurfaceId.Render: Render(c, seed); break;
                case SurfaceId.Grass: Grass(c, seed); break;
                case SurfaceId.FloorTile: FloorTile(c, seed, false); break;
                case SurfaceId.Marble: FloorTile(c, seed, true); break;
                case SurfaceId.Wood: Wood(c, seed); break;
                case SurfaceId.RoofMembrane: RoofMembrane(c, seed); break;
                case SurfaceId.RoofTiles: RoofTiles(c, seed); break;
                case SurfaceId.Cladding: Cladding(c, seed); break;
                case SurfaceId.Soil: Soil(c, seed); break;
                case SurfaceId.Wheat: Wheat(c, seed); break;
                case SurfaceId.Fabric: Fabric(c, seed); break;
                case SurfaceId.BrushedMetal: BrushedMetal(c, seed); break;
                case SurfaceId.Foliage: Foliage(c, seed); break;
                case SurfaceId.Bark: Bark(c, seed); break;
                case SurfaceId.Carpet: Carpet(c, seed); break;
                case SurfaceId.Epoxy: Epoxy(c, seed); break;
                case SurfaceId.FacadeOffice: FacadeOffice(c, seed); break;
                case SurfaceId.FacadeApartment: FacadeWindows(c, seed, false); break;
                case SurfaceId.FacadeBrick: FacadeWindows(c, seed, true); break;
                case SurfaceId.GrassMacro: GrassMacro(c, seed); break;
            }
            return c.Finish(info.NormalStrength);
        }

        // ------------------------------------------------------------------ CANVAS
        /// <summary>Float working buffers; Finish() turns the height field into a normal map and packs bytes.</summary>
        sealed class Canvas
        {
            public readonly int N;
            public readonly float[] R, G, B, H, Ao, Smooth, Metal, ER, EG, EB;
            public Canvas(int n, bool emission)
            {
                N = n; int len = n * n;
                R = new float[len]; G = new float[len]; B = new float[len]; H = new float[len];
                Ao = Fill(len, 1f); Smooth = Fill(len, 1f); Metal = new float[len];
                if (emission) { ER = new float[len]; EG = new float[len]; EB = new float[len]; }
            }
            static float[] Fill(int len, float v) { var a = new float[len]; for (int i = 0; i < len; i++) a[i] = v; return a; }

            public void Set(int i, float r, float g, float b) { R[i] = r; G[i] = g; B[i] = b; }

            public SurfaceMaps Finish(float normalStrength)
            {
                int n = N, len = n * n;
                var m = new SurfaceMaps { Size = n, Albedo = new byte[len * 4], Normal = new byte[len * 4], Mask = new byte[len * 4] };
                // Height differences are per pixel; scale so the strength means the same at any resolution.
                float k = normalStrength * n / 64f;
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        int i = y * n + x, o = i * 4;
                        float dx = (H[y * n + (x + 1) % n] - H[y * n + (x - 1 + n) % n]) * 0.5f * k;
                        float dy = (H[((y + 1) % n) * n + x] - H[((y - 1 + n) % n) * n + x]) * 0.5f * k;
                        float nx = -dx, ny = -dy, nz = 1f, l = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                        m.Normal[o] = B8(nx / l * 0.5f + 0.5f); m.Normal[o + 1] = B8(ny / l * 0.5f + 0.5f); m.Normal[o + 2] = B8(nz / l * 0.5f + 0.5f); m.Normal[o + 3] = 255;
                        m.Albedo[o] = B8(R[i]); m.Albedo[o + 1] = B8(G[i]); m.Albedo[o + 2] = B8(B[i]); m.Albedo[o + 3] = 255;
                        m.Mask[o] = B8(Metal[i]); m.Mask[o + 1] = B8(Ao[i]); m.Mask[o + 2] = 0; m.Mask[o + 3] = B8(Smooth[i]);
                    }
                if (ER != null)
                {
                    m.Emission = new byte[len * 4];
                    for (int i = 0; i < len; i++) { int o = i * 4; m.Emission[o] = B8(ER[i]); m.Emission[o + 1] = B8(EG[i]); m.Emission[o + 2] = B8(EB[i]); m.Emission[o + 3] = 255; }
                }
                return m;
            }
        }

        static byte B8(float v) => (byte)(v <= 0f ? 0 : v >= 1f ? 255 : (int)(v * 255f + 0.5f));

        // ------------------------------------------------------------------ NOISE (all tile seamlessly)
        static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u + 2654435769u;
                h ^= h >> 15; h *= 0x85EBCA77u;
                h ^= h >> 13; h *= 0xC2B2AE3Du;
                return h ^ (h >> 16);
            }
        }

        /// <summary>0..1 from a lattice point.</summary>
        static float Rand(int x, int y, int seed) => (Hash(x, y, seed) & 0xFFFFFF) / 16777216f;

        static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }
        static float Fade(float t) => t * t * (3f - 2f * t);
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        static float Smooth(float e0, float e1, float x) { float t = Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }
        static float Frac(float v) => v - (float)Math.Floor(v);

        /// <summary>Value noise on a periodic lattice: u, v in [0, 1) map to <paramref name="period"/> cells.</summary>
        static float Value(float u, float v, int period, int seed) => Value(u, v, period, period, seed);

        /// <summary>Value noise stretched along one axis (different cell counts across U and V); still tiles.</summary>
        static float Value(float u, float v, int periodU, int periodV, int seed)
        {
            float x = u * periodU, y = v * periodV;
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = Fade(x - x0), fy = Fade(y - y0);
            int ax = Mod(x0, periodU), ay = Mod(y0, periodV), bx = Mod(x0 + 1, periodU), by = Mod(y0 + 1, periodV);
            return Lerp(Lerp(Rand(ax, ay, seed), Rand(bx, ay, seed), fx), Lerp(Rand(ax, by, seed), Rand(bx, by, seed), fx), fy);
        }

        /// <summary>Fractal sum of value noise, 0..1.</summary>
        static float Fbm(float u, float v, int period, int octaves, int seed, float gain = 0.5f) => Fbm(u, v, period, period, octaves, seed, gain);

        static float Fbm(float u, float v, int periodU, int periodV, int octaves, int seed, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Value(u, v, periodU << o, periodV << o, seed + o * 101) * amp;
                norm += amp; amp *= gain;
            }
            return sum / norm;
        }

        /// <summary>Periodic cellular noise: distance to the nearest and second-nearest feature point (in cell units) and the nearest cell's id.</summary>
        static void Worley(float u, float v, int period, int seed, out float f1, out float f2, out uint cell)
        {
            float x = u * period, y = v * period;
            int cx = (int)Math.Floor(x), cy = (int)Math.Floor(y);
            f1 = f2 = 9f; cell = 0;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j, wx = Mod(gx, period), wy = Mod(gy, period);
                    uint h = Hash(wx, wy, seed);
                    float px = gx + (h & 0xFFFF) / 65536f, py = gy + (h >> 16) / 65536f;
                    float d = (float)Math.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1) { f2 = f1; f1 = d; cell = h; } else if (d < f2) f2 = d;
                }
        }

        static float CellRand(uint cell, int salt) => (Hash((int)cell, salt, 7) & 0xFFFF) / 65536f;

        /// <summary>Calls <paramref name="px"/> for every pixel with (index, u, v), u/v at pixel centres in [0, 1).</summary>
        static void Each(Canvas c, Action<int, float, float> px)
        {
            int n = c.N; float inv = 1f / n;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px(y * n + x, (x + 0.5f) * inv, (y + 0.5f) * inv);
        }

        static float SrgbMix(float a, float b, float t) => Lerp(a, b, Clamp01(t));

        // ------------------------------------------------------------------ SURFACES
        static void Asphalt(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float macro = Fbm(u, v, 3, 4, seed);                     // worn / patched areas
                float tar = Smooth(0.58f, 0.66f, Fbm(u, v, 5, 3, seed + 5));
                Worley(u, v, 150, seed + 9, out float f1, out _, out uint cell);
                float stone = 1f - Smooth(0.05f, 0.42f, f1);              // aggregate chips
                float bright = CellRand(cell, 1);
                float grain = Fbm(u, v, 96, 2, seed + 13);
                float baseL = 0.21f + (macro - 0.5f) * 0.06f + (grain - 0.5f) * 0.03f;
                float l = Lerp(baseL, 0.24f + bright * 0.2f, stone * 0.8f);
                l = Lerp(l, 0.15f, tar * 0.6f);
                float warm = (CellRand(cell, 2) - 0.5f) * 0.02f * stone;
                c.Set(i, l + warm, l, l - warm * 0.5f + 0.005f);
                c.H[i] = stone * 0.7f + grain * 0.3f - tar * 0.15f;
                c.Ao[i] = 1f - (1f - stone) * 0.18f;
                c.Smooth[i] = Clamp01(0.55f + tar * 0.45f - stone * 0.2f + (macro - 0.5f) * 0.2f);
            });
        }

        static void Concrete(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float blotch = Fbm(u, v, 4, 5, seed);
                float fine = Fbm(u, v, 64, 3, seed + 3);
                Worley(u, v, 110, seed + 7, out float f1, out _, out _);
                float pore = 1f - Smooth(0.0f, 0.16f, f1);
                float edge = Math.Min(Math.Min(u, 1f - u), Math.Min(v, 1f - v));
                float joint = 1f - Smooth(0.0015f, 0.004f, edge);           // expansion joint at the tile edge
                float l = 0.5f + (blotch - 0.5f) * 0.16f + (fine - 0.5f) * 0.06f - pore * 0.12f - joint * 0.22f;
                c.Set(i, l, l * 0.995f, l * 0.97f);
                c.H[i] = fine * 0.35f - pore * 0.5f - joint * 0.9f;
                c.Ao[i] = 1f - joint * 0.5f - pore * 0.2f;
                c.Smooth[i] = Clamp01(0.75f + (blotch - 0.5f) * 0.5f - pore * 0.3f);
            });
        }

        static void Paving(Canvas c, int seed)
        {
            const int slabs = 2;                                           // 2 x 2 slabs of 0.6 m
            Each(c, (i, u, v) =>
            {
                float su = u * slabs, sv = v * slabs;
                int ix = (int)su, iy = (int)sv;
                float fu = su - ix, fv = sv - iy;
                float e = Math.Min(Math.Min(fu, 1f - fu), Math.Min(fv, 1f - fv));
                float grout = 1f - Smooth(0.006f, 0.014f, e);
                float bevel = Smooth(0.006f, 0.04f, e);
                float tone = Rand(ix, iy, seed) - 0.5f;
                float speck = Fbm(u, v, 128, 2, seed + 4);
                float dirt = Fbm(u, v, 6, 4, seed + 8);
                float l = 0.58f + tone * 0.09f + (speck - 0.5f) * 0.08f - (dirt - 0.5f) * 0.1f;
                float r = l * 1.0f, g = l * 0.97f, b = l * 0.92f;
                c.Set(i, SrgbMix(r, 0.3f, grout), SrgbMix(g, 0.29f, grout), SrgbMix(b, 0.27f, grout));
                c.H[i] = bevel * 0.8f + speck * 0.2f;
                c.Ao[i] = Lerp(1f, 0.55f, grout);
                c.Smooth[i] = Clamp01(0.7f + (speck - 0.5f) * 0.3f - grout * 0.4f);
            });
        }

        /// <summary>One pixel of running-bond brick: <paramref name="across"/> bricks and <paramref name="courses"/> (even) courses per texture.</summary>
        static void BrickPixel(Canvas c, int i, float u, float v, int seed, int across, int courses)
        {
            float cv = v * courses; int row = (int)Math.Floor(cv); float fv = cv - row;
            float cu = u * across + (Mod(row, 2) == 1 ? 0.5f : 0f); int col = (int)Math.Floor(cu); float fu = cu - col;
            int wrapCol = Mod(col, across);
            float mortarU = 0.045f, mortarV = 0.13f;                           // 10 mm of 225 / 75 mm
            float eu = Math.Min(fu, 1f - fu) / mortarU, ev = Math.Min(fv, 1f - fv) / mortarV;
            float inBrick = Smooth(0.45f, 0.7f, Math.Min(eu, ev));
            float tone = Rand(wrapCol, Mod(row, courses), seed) - 0.5f;
            float burnt = Rand(wrapCol, Mod(row, courses), seed + 3) > 0.86f ? 1f : 0f;
            float grain = Fbm(u, v, 64, 3, seed + 5);
            float r = 0.56f + tone * 0.1f - burnt * 0.18f + (grain - 0.5f) * 0.08f;
            float g = 0.26f + tone * 0.05f - burnt * 0.08f + (grain - 0.5f) * 0.05f;
            float b = 0.19f + tone * 0.03f - burnt * 0.04f + (grain - 0.5f) * 0.04f;
            float mortar = 0.66f + (grain - 0.5f) * 0.1f;
            c.Set(i, Lerp(mortar, r, inBrick), Lerp(mortar * 0.97f, g, inBrick), Lerp(mortar * 0.92f, b, inBrick));
            c.H[i] = inBrick * 0.85f + grain * 0.15f * inBrick;
            c.Ao[i] = Lerp(0.6f, 1f, inBrick);
            c.Smooth[i] = Lerp(0.3f, 0.6f + (grain - 0.5f) * 0.4f, inBrick);
        }

        static void Render(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float blotch = Fbm(u, v, 4, 4, seed);
                float grain = Fbm(u, v, 128, 2, seed + 2);
                float streak = Fbm(u, v, 24, 6, 3, seed + 6);                  // faint vertical weathering
                float l = 0.9f + (blotch - 0.5f) * 0.05f + (grain - 0.5f) * 0.05f - Smooth(0.55f, 0.8f, streak) * 0.05f;
                c.Set(i, l, l, l * 0.985f);
                c.H[i] = grain * 0.8f + blotch * 0.2f;
                c.Smooth[i] = Clamp01(0.8f + (grain - 0.5f) * 0.4f);
            });
        }

        static void Grass(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float patch = Fbm(u, v, 3, 4, seed);
                float clump = Fbm(u, v, 12, 3, seed + 3);
                // Blades: streaky noise in two crossing directions.
                float bladeA = Value(u, v, 256, 48, seed + 5);
                float bladeB = Value(u, v, 48, 240, seed + 9);
                float blade = Math.Max(bladeA, bladeB);
                float dry = Smooth(0.5f, 0.9f, patch) * 0.6f;
                float shade = 0.7f + blade * 0.45f + (clump - 0.5f) * 0.25f;
                float r = Lerp(0.22f, 0.38f, dry) * shade, g = Lerp(0.36f, 0.4f, dry) * shade, b = Lerp(0.11f, 0.16f, dry) * shade;
                float clover = Smooth(0.72f, 0.8f, Fbm(u, v, 40, 2, seed + 12));
                g += clover * 0.04f;
                c.Set(i, r, g, b);
                c.H[i] = blade * 0.7f + clump * 0.3f;
                c.Ao[i] = 0.7f + blade * 0.3f;
                c.Smooth[i] = Clamp01(0.4f + blade * 0.6f);
            });
        }

        static void FloorTile(Canvas c, int seed, bool marble)
        {
            const int tiles = 2;                                         // 2 x 2 large-format tiles (1.2 m)
            Each(c, (i, u, v) =>
            {
                float su = u * tiles, sv = v * tiles;
                int ix = (int)su, iy = (int)sv;
                float fu = su - ix, fv = sv - iy;
                float e = Math.Min(Math.Min(fu, 1f - fu), Math.Min(fv, 1f - fv));
                float grout = 1f - Smooth(0.0012f, 0.0035f, e);
                float tone = Rand(ix, iy, seed) - 0.5f;
                float cloud = Fbm(u + ix * 0.37f, v + iy * 0.21f, 6, 5, seed + 3);
                float l, r, g, b;
                if (marble)
                {
                    float turb = Fbm(u, v, 4, 6, seed + 7);
                    float vein = 1f - Smooth(0.0f, 0.08f, Math.Abs((float)Math.Sin((u * 3f + v * 5f + turb * 6f) * Math.PI)));
                    float vein2 = 1f - Smooth(0.0f, 0.03f, Math.Abs((float)Math.Sin((u * 7f - v * 4f + turb * 9f) * Math.PI)));
                    l = 0.9f + (cloud - 0.5f) * 0.06f - vein * 0.3f - vein2 * 0.12f;
                    r = l; g = l * 0.99f; b = l * 0.975f;
                }
                else
                {
                    l = 0.74f + tone * 0.03f + (cloud - 0.5f) * 0.08f;
                    r = l; g = l * 0.995f; b = l * 0.985f;
                }
                c.Set(i, SrgbMix(r, 0.45f, grout), SrgbMix(g, 0.44f, grout), SrgbMix(b, 0.43f, grout));
                c.H[i] = 1f - grout;
                c.Ao[i] = Lerp(1f, 0.7f, grout);
                c.Smooth[i] = Lerp(0.93f + (cloud - 0.5f) * 0.1f, 0.35f, grout);
            });
        }

        static void Wood(Canvas c, int seed)
        {
            const int planks = 5;                                        // planks run along U, 0.2 m wide
            Each(c, (i, u, v) =>
            {
                float pv = v * planks; int p = (int)pv; float fp = pv - p;
                float offset = Rand(p, 0, seed) * 0.7f;
                float gap = 1f - Smooth(0.0f, 0.035f, Math.Min(fp, 1f - fp));
                float uu = Frac(u + offset);
                float warp = Fbm(uu, v, 3, 3, seed + p) * 4f;
                float rings = Frac((v * planks * 6f + warp + Value(uu, v, 8, seed + 30) * 0.6f));
                float grain = Smooth(0.0f, 0.5f, rings) * Smooth(1f, 0.6f, rings);
                float fibre = Value(uu, v, 6, 420, seed + 9);
                float tone = Rand(p, 1, seed) - 0.5f;
                float l = 0.72f + tone * 0.1f + (grain - 0.5f) * 0.14f + (fibre - 0.5f) * 0.05f;
                c.Set(i, l * (1f - gap * 0.5f), l * 0.94f * (1f - gap * 0.55f), l * 0.86f * (1f - gap * 0.6f));
                c.H[i] = (1f - gap) * 0.7f + grain * 0.2f + fibre * 0.1f;
                c.Ao[i] = 1f - gap * 0.45f;
                c.Smooth[i] = Clamp01(0.85f + (grain - 0.5f) * 0.25f - gap * 0.5f);
            });
        }

        static void RoofMembrane(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                Worley(u, v, 220, seed, out float f1, out _, out uint cell);
                float stone = 1f - Smooth(0.1f, 0.5f, f1);
                float stain = Fbm(u, v, 3, 4, seed + 4);
                float l = 0.24f + stone * (0.08f + CellRand(cell, 1) * 0.12f) - (stain - 0.5f) * 0.08f;
                c.Set(i, l, l * 0.98f, l * 0.96f);
                c.H[i] = stone;
                c.Ao[i] = 0.8f + stone * 0.2f;
                c.Smooth[i] = Clamp01(0.5f - stone * 0.2f + (stain - 0.5f) * 0.3f);
            });
        }

        static void RoofTiles(Canvas c, int seed)
        {
            const int rows = 4, across = 4;                              // interlocking tiles 0.3 m
            Each(c, (i, u, v) =>
            {
                float rv = v * rows; int row = (int)rv; float fv = rv - row;
                float ru = u * across + (row % 2) * 0.5f; int col = (int)Math.Floor(ru); float fu = ru - col;
                float profile = (float)Math.Sin(fu * Math.PI);           // the tile's curve
                float overlap = Smooth(0.0f, 0.12f, fv) * (1f - Smooth(0.88f, 1f, fv) * 0.6f);
                float tone = Rand(Mod(col, across), row, seed) - 0.5f;
                float moss = Smooth(0.68f, 0.8f, Fbm(u, v, 5, 4, seed + 3)) * (1f - fv) * 0.6f;
                float l = 0.86f + tone * 0.12f;
                float shade = 0.78f + profile * 0.22f;
                c.Set(i, Lerp(l * shade, 0.45f, moss * 0.4f), Lerp(l * shade, 0.5f, moss * 0.4f), Lerp(l * shade, 0.32f, moss * 0.4f));
                c.H[i] = profile * 0.6f + overlap * 0.4f;
                c.Ao[i] = Lerp(0.55f, 1f, overlap) * (0.75f + profile * 0.25f);
                c.Smooth[i] = Clamp01(0.7f + tone * 0.3f - moss * 0.6f);
            });
        }

        static void Cladding(Canvas c, int seed)
        {
            const int ribs = 6;                                          // trapezoidal ribs every 0.2 m
            Each(c, (i, u, v) =>
            {
                float ru = Frac(u * ribs);
                float rib = Smooth(0.08f, 0.18f, ru) * (1f - Smooth(0.42f, 0.52f, ru));
                float streak = Fbm(u, v, 32, 6, 3, seed);
                float l = 0.86f + rib * 0.06f - (streak - 0.5f) * 0.06f;
                c.Set(i, l, l, l * 1.01f);
                c.H[i] = rib;
                c.Ao[i] = 0.85f + rib * 0.15f;
                c.Metal[i] = 0.7f;
                c.Smooth[i] = Clamp01(0.8f + (streak - 0.5f) * 0.3f);
            });
        }

        static void Soil(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float furrow = (float)Math.Sin(v * 30f * Math.PI * 2f + Fbm(u, v, 4, 3, seed) * 2f) * 0.5f + 0.5f;
                Worley(u, v, 90, seed + 3, out float f1, out _, out uint cell);
                float clod = 1f - Smooth(0.1f, 0.5f, f1);
                float damp = Fbm(u, v, 3, 4, seed + 6);
                float l = 0.3f + furrow * 0.08f + clod * 0.05f * CellRand(cell, 2) - (damp - 0.5f) * 0.1f;
                c.Set(i, l * 1.12f, l * 0.88f, l * 0.66f);
                c.H[i] = furrow * 0.7f + clod * 0.3f;
                c.Ao[i] = 0.7f + furrow * 0.3f;
                c.Smooth[i] = Clamp01(0.3f + (damp - 0.5f) * 0.6f);
            });
        }

        static void Wheat(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float row = (float)Math.Sin(v * 24f * Math.PI * 2f) * 0.5f + 0.5f;
                float stalk = Value(u, v, 300, seed);
                float patch = Fbm(u, v, 3, 4, seed + 2);
                float l = 0.55f + stalk * 0.25f + (patch - 0.5f) * 0.15f;
                c.Set(i, l * 1.12f * (0.8f + row * 0.2f), l * 0.95f * (0.8f + row * 0.2f), l * 0.5f);
                c.H[i] = stalk * 0.6f + row * 0.4f;
                c.Ao[i] = 0.65f + row * 0.35f;
                c.Smooth[i] = 0.5f + stalk * 0.5f;
            });
        }

        static void Fabric(Canvas c, int seed)
        {
            const int threads = 90;
            Each(c, (i, u, v) =>
            {
                float wu = (float)Math.Sin(u * threads * Math.PI * 2f), wv = (float)Math.Sin(v * threads * Math.PI * 2f);
                bool over = ((int)(u * threads) + (int)(v * threads)) % 2 == 0;
                float weave = over ? wu * 0.5f + 0.5f : wv * 0.5f + 0.5f;
                float fuzz = Fbm(u, v, 64, 3, seed);
                float l = 0.82f + weave * 0.1f + (fuzz - 0.5f) * 0.08f;
                c.Set(i, l, l, l);
                c.H[i] = weave * 0.7f + fuzz * 0.3f;
                c.Ao[i] = 0.8f + weave * 0.2f;
                c.Smooth[i] = 0.5f + weave * 0.3f;
            });
        }

        static void BrushedMetal(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float brush = Value(u, v, 4, 512, seed) * 0.6f + Value(u, v, 8, 96, seed + 3) * 0.4f;
                float scratch = Smooth(0.93f, 0.97f, Value(u, v, 160, 24, seed + 7));
                float l = 0.85f + (brush - 0.5f) * 0.08f + scratch * 0.06f;
                c.Set(i, l, l, l * 1.01f);
                c.H[i] = brush * 0.3f - scratch * 0.3f;
                c.Metal[i] = 1f;
                c.Smooth[i] = Clamp01(0.85f + (brush - 0.5f) * 0.2f - scratch * 0.3f);
            });
        }

        static void Foliage(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                Worley(u, v, 26, seed, out float f1, out float f2, out uint cell);
                float leaf = Smooth(0.0f, 0.25f, f2 - f1);                 // leaf edges
                float dome = 1f - Smooth(0.0f, 0.75f, f1);
                Worley(u, v, 64, seed + 4, out float g1, out float g2, out uint cell2);
                float small = Smooth(0.0f, 0.2f, g2 - g1);
                float tone = CellRand(cell, 3) - 0.5f, tone2 = CellRand(cell2, 5) - 0.5f;
                float light = dome * 0.6f + 0.4f;
                float r = (0.24f + tone * 0.07f + tone2 * 0.03f) * light, g = (0.42f + tone * 0.09f + tone2 * 0.04f) * light, b = (0.13f + tone * 0.03f) * light;
                float gap = 1f - leaf * small;
                c.Set(i, Lerp(r, 0.07f, gap * 0.55f), Lerp(g, 0.12f, gap * 0.55f), Lerp(b, 0.05f, gap * 0.55f));
                c.H[i] = dome * leaf * small;
                c.Ao[i] = Lerp(0.45f, 1f, dome * leaf);
                c.Smooth[i] = 0.4f + dome * 0.6f;
            });
        }

        static void Bark(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                // Vertical ridges with deep fissures between them.
                float ridges = Fbm(u, v, 14, 3, 4, seed + 3);
                float fissure = Fbm(u, v, 22, 4, 3, seed + 5);
                float crack = 1f - Smooth(0.02f, 0.09f, Math.Abs(fissure - 0.5f));
                float l = 0.26f + ridges * 0.16f - crack * 0.12f;
                c.Set(i, l * 1.05f, l * 0.88f, l * 0.72f);
                c.H[i] = ridges - crack * 0.6f;
                c.Ao[i] = 1f - crack * 0.5f;
                c.Smooth[i] = 0.4f + ridges * 0.4f;
            });
        }

        static void Carpet(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float tuft = Value(u, v, 256, seed) * 0.6f + Value(u, v, 128, seed + 1) * 0.4f;
                float pattern = Fbm(u, v, 8, 2, seed + 5);
                float l = 0.78f + (tuft - 0.5f) * 0.18f + (pattern - 0.5f) * 0.06f;
                c.Set(i, l, l, l);
                c.H[i] = tuft;
                c.Ao[i] = 0.8f + tuft * 0.2f;
                c.Smooth[i] = 0.3f + tuft * 0.4f;
            });
        }

        static void Epoxy(Canvas c, int seed)
        {
            Each(c, (i, u, v) =>
            {
                float wear = Fbm(u, v, 4, 5, seed);
                float fleck = Value(u, v, 300, seed + 3) > 0.92f ? 1f : 0f;
                float oil = Smooth(0.68f, 0.74f, Fbm(u, v, 6, 4, seed + 9));
                float tyre = Smooth(0.62f, 0.7f, Value(u, v, 2, 6, seed + 12)) * 0.6f;
                float l = 0.46f + (wear - 0.5f) * 0.08f + fleck * 0.06f - oil * 0.07f - tyre * 0.05f;
                c.Set(i, l * 0.94f, l * 0.98f, l);
                c.H[i] = wear * 0.3f;
                c.Smooth[i] = Clamp01(0.85f - (wear - 0.5f) * 0.6f - tyre * 0.3f + oil * 0.15f);
            });
        }

        static void GrassMacro(Canvas c, int seed)
        {
            // Neutral-grey large-scale variation, used as a x2 detail map so the ground never looks tiled.
            Each(c, (i, u, v) =>
            {
                float a = Fbm(u, v, 3, 5, seed), b = Fbm(u, v, 5, 3, seed + 3);
                float l = 0.5f + (a - 0.5f) * 0.28f;
                c.Set(i, l + (b - 0.5f) * 0.1f, l, l - (b - 0.5f) * 0.06f);
                c.H[i] = 0f;
            });
        }

        // ------------------------------------------------------------------ FACADES
        /// <summary>Glass curtain wall: 4 bays (3 m) x 4 floors (3.6 m), aluminium mullions, dark spandrel bands, lit offices at night.</summary>
        static void FacadeOffice(Canvas c, int seed)
        {
            const int bays = 4, floors = 4, panesPerBay = 2;
            Each(c, (i, u, v) =>
            {
                float fu = u * bays * panesPerBay; int pane = (int)fu; float pu = fu - pane;
                float fv = v * floors; int floor = (int)fv; float pv = fv - floor;
                float spandrel = 1f - Smooth(0.22f, 0.235f, pv);              // 0.85 m band at the slab
                float mullion = 1f - Smooth(0.012f, 0.022f, Math.Min(pu, 1f - pu));
                float transom = 1f - Smooth(0.008f, 0.016f, Math.Abs(pv - 0.235f));
                float frame = Math.Max(mullion, transom);
                float sky = 0.25f + pv * 0.25f + (Rand(pane, floor, seed) - 0.5f) * 0.06f;   // fake sky gradient in the glass
                float r = 0.13f + sky * 0.15f, g = 0.18f + sky * 0.2f, b = 0.24f + sky * 0.26f;
                float sr = 0.12f, sg = 0.13f, sb = 0.15f;
                float mr = 0.62f, mg = 0.64f, mb = 0.66f;
                float rr = Lerp(r, sr, spandrel), gg = Lerp(g, sg, spandrel), bb = Lerp(b, sb, spandrel);
                c.Set(i, Lerp(rr, mr, frame), Lerp(gg, mg, frame), Lerp(bb, mb, frame));
                c.H[i] = frame * 0.8f + spandrel * 0.3f;
                c.Ao[i] = 1f - (1f - frame) * 0.05f;
                c.Metal[i] = Lerp(Lerp(0.3f, 0.1f, spandrel), 0.9f, frame);
                c.Smooth[i] = Lerp(Lerp(1f, 0.55f, spandrel), 0.7f, frame);
                // Night: whole offices (a bay on a floor) are lit or dark; blinds part-way down on some.
                int bay = pane / panesPerBay;
                float lit = Rand(bay, floor, seed + 17) < 0.42f ? 1f : 0f;
                // Some offices have their blinds part-way down: a pale band from the top of the glass.
                float drop = Rand(bay, floor, seed + 23) < 0.4f ? Rand(bay, floor, seed + 27) * 0.5f : 0f;
                float blind = drop > 0f ? Smooth(1f - drop - 0.004f, 1f - drop + 0.004f, pv) * (1f - spandrel) * (1f - frame) : 0f;
                var cr0 = c.R[i]; var cg0 = c.G[i]; var cb0 = c.B[i];
                c.Set(i, Lerp(cr0, 0.62f, blind), Lerp(cg0, 0.6f, blind), Lerp(cb0, 0.56f, blind));
                c.Smooth[i] = Lerp(c.Smooth[i], 0.3f, blind);
                c.Metal[i] = Lerp(c.Metal[i], 0f, blind);
                float glow = lit * (1f - spandrel) * (1f - frame) * (1f - blind * 0.5f) * (0.75f + Rand(pane, floor, seed + 29) * 0.25f);
                bool cool = Rand(bay, floor, seed + 31) < 0.5f;
                c.ER[i] = glow * (cool ? 0.85f : 1f); c.EG[i] = glow * (cool ? 0.9f : 0.82f); c.EB[i] = glow * (cool ? 1f : 0.58f);
            });
        }

        /// <summary>Punched windows (3 m bays x 3 m floors, 4 x 4) in render or brick, with frames, sills, curtains and lit rooms at night.</summary>
        static void FacadeWindows(Canvas c, int seed, bool brick)
        {
            const int bays = 4, floors = 4;
            Each(c, (i, u, v) =>
            {
                float bu = u * bays; int bay = (int)bu; float x = bu - bay;           // 0..1 across a 3 m bay
                float fv = v * floors; int floor = (int)fv; float y = fv - floor;     // 0..1 up a 3 m floor
                // Wall first.
                if (brick) BrickPixel(c, i, u, v, seed, 53, 160);
                else
                {
                    float blotch = Fbm(u, v, 6, 4, seed), grain = Fbm(u, v, 256, 2, seed + 2);
                    float l = 0.82f + (blotch - 0.5f) * 0.06f + (grain - 0.5f) * 0.05f;
                    c.Set(i, l, l * 0.975f, l * 0.93f);
                    c.H[i] = grain * 0.4f;
                    c.Smooth[i] = 0.35f;
                }
                // Window opening: 1.5 m x 1.6 m, sill at 0.85 m.
                float wx0 = 0.25f, wx1 = 0.75f, wy0 = 0.28f, wy1 = 0.82f;
                bool inWin = x > wx0 && x < wx1 && y > wy0 && y < wy1;
                bool sill = x > wx0 - 0.03f && x < wx1 + 0.03f && y > wy0 - 0.035f && y <= wy0;
                if (sill)
                {
                    c.Set(i, 0.8f, 0.79f, 0.77f); c.H[i] = 1f; c.Ao[i] = 1f; c.Smooth[i] = 0.5f; c.Metal[i] = 0f;
                    return;
                }
                if (!inWin) { c.Metal[i] = 0f; return; }
                float ex = Math.Min(x - wx0, wx1 - x), ey = Math.Min(y - wy0, wy1 - y);
                float edge = Math.Min(ex, ey * 0.55f);
                bool frame = edge < 0.022f || Math.Abs(x - 0.5f) < 0.012f;           // uPVC frame and centre mullion
                float reveal = 1f - Smooth(0.0f, 0.02f, edge);
                if (frame)
                {
                    c.Set(i, 0.9f, 0.9f, 0.89f); c.H[i] = 0.55f; c.Ao[i] = 0.9f; c.Smooth[i] = 0.7f; c.Metal[i] = 0f;
                    return;
                }
                float curtainSide = Rand(bay, floor, seed + 41);
                float curtainW = 0.04f + Rand(bay, floor, seed + 43) * 0.08f;
                bool curtain = ex < curtainW && curtainSide < 0.7f;
                float tint = Rand(bay, floor, seed + 47);
                float cr = 0.55f + tint * 0.3f, cg = 0.45f + (1f - tint) * 0.25f, cb = 0.35f + tint * 0.15f;
                float sky = 0.22f + (y - wy0) * 0.35f;
                float gr = 0.1f + sky * 0.12f, gg = 0.13f + sky * 0.15f, gb = 0.17f + sky * 0.2f;
                c.Set(i, curtain ? cr * 0.6f : gr, curtain ? cg * 0.6f : gg, curtain ? cb * 0.6f : gb);
                c.H[i] = 0.05f;
                c.Ao[i] = Lerp(1f, 0.6f, reveal);
                c.Metal[i] = curtain ? 0f : 0.25f;
                c.Smooth[i] = curtain ? 0.2f : 1f;
                float lit = Rand(bay, floor, seed + 53) < 0.38f ? 1f : 0f;
                float glow = lit * (0.7f + Rand(bay, floor, seed + 59) * 0.3f) * (curtain ? 0.55f : 1f);
                c.ER[i] = glow * (curtain ? cr : 1f); c.EG[i] = glow * (curtain ? cg : 0.8f); c.EB[i] = glow * (curtain ? cb : 0.52f);
            });
        }
    }
}
