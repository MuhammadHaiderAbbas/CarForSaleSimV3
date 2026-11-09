using System;
using System.Collections.Generic;

namespace CarSales.Sim
{
    /// <summary>Furniture the interior builder knows how to make (append only).</summary>
    public enum FurnitureType
    {
        Sofa = 0, Armchair = 1, CoffeeTable = 2, TvWall = 3, DiningTable = 4, DiningChair = 5, Bed = 6, Nightstand = 7,
        Wardrobe = 8, Kitchen = 9, KitchenIsland = 10, Fridge = 11, Desk = 12, OfficeChair = 13, Bookcase = 14,
        FloorLamp = 15, Plant = 16, Rug = 17, WallArt = 18, Window = 19, Reception = 20, CoffeeStation = 21, Brochures = 22,
    }

    /// <summary>
    /// One piece of furniture: centre (X, Z) on the floor, turn (Yaw, degrees, front = +Z before turning), size along its own
    /// X and Z (W, D). Y is the height off the floor for wall-mounted things. Variant picks a material (0 = default).
    /// </summary>
    public struct FurnitureItem
    {
        public FurnitureType Type;
        public float X, Z, Yaw, W, D, Y, H;
        public int Seats, Variant;

        public FurnitureItem(FurnitureType type, float x, float z, float yaw, float w = 0f, float d = 0f, int seats = 0, int variant = 0, float y = 0f, float h = 0f)
        { Type = type; X = x; Z = z; Yaw = yaw; W = w; D = d; Seats = seats; Variant = variant; Y = y; H = h; }

        /// <summary>Whether the piece stands on the floor in the way (rugs, art and windows do not).</summary>
        public bool Blocks => Type != FurnitureType.Rug && Type != FurnitureType.WallArt && Type != FurnitureType.Window;

        /// <summary>Chairs tuck under tables and desks, so a seat may overlap them.</summary>
        public bool IsSeat => Type == FurnitureType.DiningChair || Type == FurnitureType.OfficeChair;
        public bool IsTable => Type == FurnitureType.DiningTable || Type == FurnitureType.Desk || Type == FurnitureType.Reception || Type == FurnitureType.CoffeeTable;

        /// <summary>The floor area the piece covers (its own size turned by Yaw), as min/max X and Z.</summary>
        public (float minX, float minZ, float maxX, float maxZ) Footprint()
        {
            var (w, d) = InteriorPlan.Size(this);
            double r = Yaw * Math.PI / 180.0;
            float c = (float)Math.Abs(Math.Cos(r)), s = (float)Math.Abs(Math.Sin(r));
            float hx = (w * c + d * s) / 2f, hz = (w * s + d * c) / 2f;
            return (X - hx, Z - hz, X + hx, Z + hz);
        }
    }

    /// <summary>A rectangle on the floor (obstacles such as car platforms and desks in a showroom).</summary>
    public struct FloorRect
    {
        public float MinX, MinZ, MaxX, MaxZ;
        public string Name;
        public FloorRect(string name, float cx, float cz, float w, float d) { Name = name; MinX = cx - w / 2f; MaxX = cx + w / 2f; MinZ = cz - d / 2f; MaxZ = cz + d / 2f; }
        public bool Overlaps(float minX, float minZ, float maxX, float maxZ, float margin = 0f) =>
            minX < MaxX + margin && maxX > MinX - margin && minZ < MaxZ + margin && maxZ > MinZ - margin;
    }

    /// <summary>The inside of a home: a box of rooms with a door in the south wall, partitions and furniture.</summary>
    public sealed class InteriorLayout
    {
        public int Tier;
        public float SizeX, SizeZ, Height, DoorX;
        /// <summary>Internal walls running north-south at X with a doorway between GapZ0 and GapZ1.</summary>
        public List<(float x, float gapZ0, float gapZ1)> Partitions = new List<(float, float, float)>();
        public List<FurnitureItem> Items = new List<FurnitureItem>();
        public float EntryX => DoorX;
        public float EntryZ => -SizeZ / 2f + 1.1f;
        public int BedIndex => Items.FindIndex(i => i.Type == FurnitureType.Bed);
        /// <summary>Where you wake up: beside the bed, on the room side.</summary>
        public (float x, float z) Wake;
    }

    /// <summary>
    /// Phase 13c: where every piece of furniture goes in the walk-in homes and the showrooms, as plain data so tests can
    /// prove nothing overlaps and that you can always walk from the door to the bed (and customers to every car).
    /// </summary>
    public static class InteriorPlan
    {
        /// <summary>The size a piece really takes on the floor (its own X and Z), whatever W/D were given.</summary>
        public static (float w, float d) Size(FurnitureItem i)
        {
            switch (i.Type)
            {
                case FurnitureType.Sofa: return (i.W, 0.95f);
                case FurnitureType.Armchair: return (0.95f, 0.95f);
                case FurnitureType.TvWall: return (i.W, 0.45f);
                case FurnitureType.DiningChair: return (0.46f, 0.46f);
                case FurnitureType.Bed: return (i.W + 0.2f, i.D + 0.15f);
                case FurnitureType.Nightstand: return (0.5f, 0.4f);
                case FurnitureType.Wardrobe: return (i.W, 0.6f);
                case FurnitureType.Kitchen: case FurnitureType.KitchenIsland: return (i.W, 0.64f);
                case FurnitureType.Fridge: return (0.72f, 0.68f);
                case FurnitureType.Desk: return (i.W, 0.78f);
                case FurnitureType.OfficeChair: return (0.62f, 0.62f);
                case FurnitureType.Bookcase: return (i.W, 0.36f);
                case FurnitureType.FloorLamp: return (0.45f, 0.45f);
                case FurnitureType.Plant: return (0.5f * Math.Max(0.5f, i.W), 0.5f * Math.Max(0.5f, i.W));
                case FurnitureType.Reception: return (i.W + 0.1f, 0.9f);
                case FurnitureType.CoffeeStation: return (2.4f, 0.58f);   // cabinet and the water cooler beside it
                case FurnitureType.Brochures: return (0.5f, 0.4f);
                default: return (i.W, i.D);
            }
        }

        static FurnitureItem F(FurnitureType t, float x, float z, float yaw, float w = 0f, float d = 0f, int seats = 0, int variant = 0, float y = 0f, float h = 0f) =>
            new FurnitureItem(t, x, z, yaw, w, d, seats, variant, y, h);

        // ------------------------------------------------------------------ HOMES
        public static InteriorLayout Home(int tier)
        {
            tier = Math.Max(0, Math.Min(2, tier));
            var L = new InteriorLayout { Tier = tier };
            var it = L.Items;
            if (tier == 0)
            {
                // Studio flat (8 x 6 m): kitchenette west, lounge centre, bed east.
                L.SizeX = 8f; L.SizeZ = 6f; L.Height = 2.6f; L.DoorX = -2.8f;
                it.Add(F(FurnitureType.Kitchen, -3.68f, 0.4f, 90f, 2.4f));
                it.Add(F(FurnitureType.Fridge, -3.62f, 2.3f, 90f));
                it.Add(F(FurnitureType.Sofa, -1.2f, 2.45f, 180f, 2f, seats: 2));
                it.Add(F(FurnitureType.CoffeeTable, -1.2f, 1.35f, 0f, 1f, 0.55f));
                it.Add(F(FurnitureType.Rug, -1.2f, 1.6f, 0f, 2.4f, 1.8f));
                it.Add(F(FurnitureType.TvWall, -0.9f, -2.75f, 0f, 1.5f));
                it.Add(F(FurnitureType.DiningTable, 1.1f, -1.4f, 0f, 0.9f, 0.9f));
                it.Add(F(FurnitureType.DiningChair, 1.1f, -0.75f, 180f));
                it.Add(F(FurnitureType.DiningChair, 1.1f, -2.05f, 0f));
                it.Add(F(FurnitureType.Bed, 2.92f, 1.6f, -90f, 1.4f, 2f));
                it.Add(F(FurnitureType.Nightstand, 3.73f, 0.3f, -90f));
                it.Add(F(FurnitureType.Wardrobe, 2.95f, -2.68f, 0f, 1.4f));
                it.Add(F(FurnitureType.Plant, 0.6f, 2.55f, 0f, 0.8f));
                it.Add(F(FurnitureType.WallArt, 3.98f, 1.6f, -90f, 1.1f, y: 1.6f, h: 0.7f, variant: 1));
                it.Add(F(FurnitureType.Window, 0.5f, 2.99f, 180f, 1.6f, y: 1.55f, h: 1.1f));
                it.Add(F(FurnitureType.Window, 3.99f, -1f, -90f, 1.2f, y: 1.55f, h: 1f));
                L.Wake = (1.3f, 1.5f);
            }
            else if (tier == 1)
            {
                // Family home (12 x 9 m): lounge south-west, kitchen-diner north-west, bedroom east behind a partition.
                L.SizeX = 12f; L.SizeZ = 9f; L.Height = 2.8f; L.DoorX = -5f;
                L.Partitions.Add((2f, -3.45f, -2.55f));
                it.Add(F(FurnitureType.Sofa, -2.4f, -0.35f, 180f, 2.4f, seats: 3));
                it.Add(F(FurnitureType.TvWall, -2.4f, -4.25f, 0f, 1.9f));
                it.Add(F(FurnitureType.CoffeeTable, -2.4f, -1.65f, 0f, 1.2f, 0.6f));
                it.Add(F(FurnitureType.Armchair, 0.4f, -1.7f, -90f, variant: 1));
                it.Add(F(FurnitureType.Rug, -2.4f, -1.7f, 0f, 3f, 2.2f));
                it.Add(F(FurnitureType.FloorLamp, -4.2f, -0.2f, 0f));
                it.Add(F(FurnitureType.Plant, -5.5f, 0.6f, 0f, 1f));
                it.Add(F(FurnitureType.Kitchen, -2.6f, 4.18f, 180f, 3.6f));
                it.Add(F(FurnitureType.Fridge, -5.55f, 4.12f, 180f));
                it.Add(F(FurnitureType.DiningTable, -2.2f, 2.1f, 0f, 1.6f, 0.9f));
                foreach (float dx in new[] { -0.45f, 0.45f })
                {
                    it.Add(F(FurnitureType.DiningChair, -2.2f + dx, 2.75f, 180f, variant: 1));
                    it.Add(F(FurnitureType.DiningChair, -2.2f + dx, 1.45f, 0f, variant: 1));
                }
                it.Add(F(FurnitureType.Bed, 4f, 3.37f, 180f, 1.6f, 2.1f));
                it.Add(F(FurnitureType.Nightstand, 2.75f, 4.25f, 180f));
                it.Add(F(FurnitureType.Nightstand, 5.25f, 4.25f, 180f));
                it.Add(F(FurnitureType.Wardrobe, 5.68f, -1.5f, -90f, 1.8f));
                it.Add(F(FurnitureType.Rug, 4f, 1.4f, 0f, 2.2f, 1.6f, variant: 1));
                it.Add(F(FurnitureType.Plant, 5.55f, -4.05f, 0f, 0.9f));
                it.Add(F(FurnitureType.WallArt, 4f, 4.48f, 180f, 1.4f, y: 1.75f, h: 0.7f, variant: 0));
                it.Add(F(FurnitureType.WallArt, -5.98f, -1.5f, 90f, 1.2f, y: 1.6f, h: 0.8f, variant: 2));
                it.Add(F(FurnitureType.Window, 0.7f, 4.49f, 180f, 1.4f, y: 1.55f, h: 1.1f));
                it.Add(F(FurnitureType.Window, 5.99f, 1f, -90f, 1.6f, y: 1.55f, h: 1.2f));
                it.Add(F(FurnitureType.Window, -5.99f, 1.2f, 90f, 1.4f, y: 1.55f, h: 1.1f));
                L.Wake = (5.3f, 2.1f);
            }
            else
            {
                // Villa (18 x 12 m): lounge with an L of sofas, open kitchen with island, dining for six; master bedroom with a study corner.
                L.SizeX = 18f; L.SizeZ = 12f; L.Height = 3.4f; L.DoorX = -6f;
                L.Partitions.Add((3f, -4.45f, -3.55f));
                it.Add(F(FurnitureType.Sofa, -2.8f, -0.8f, 180f, 3f, seats: 3, variant: 2));
                it.Add(F(FurnitureType.Sofa, -5.6f, -3f, 90f, 2.4f, seats: 2, variant: 2));
                it.Add(F(FurnitureType.CoffeeTable, -2.8f, -2.5f, 0f, 1.4f, 0.8f));
                it.Add(F(FurnitureType.Armchair, 0.3f, -2.7f, -90f, variant: 1));
                it.Add(F(FurnitureType.TvWall, -2.8f, -5.75f, 0f, 2.4f));
                it.Add(F(FurnitureType.Rug, -3f, -2.6f, 0f, 4.2f, 3f, variant: 1));
                it.Add(F(FurnitureType.Bookcase, -8.8f, 1.7f, 90f, 1.8f, h: 2.3f));
                it.Add(F(FurnitureType.FloorLamp, -6.4f, -0.9f, 0f));
                it.Add(F(FurnitureType.Plant, -8.5f, -5.5f, 0f, 1.3f));
                it.Add(F(FurnitureType.Plant, 2.4f, -5.5f, 0f, 1.3f));
                it.Add(F(FurnitureType.Kitchen, -3.5f, 5.68f, 180f, 4.8f));
                it.Add(F(FurnitureType.Fridge, -8.55f, 5.62f, 180f));
                it.Add(F(FurnitureType.Fridge, -7.83f, 5.62f, 180f));
                it.Add(F(FurnitureType.KitchenIsland, -3.5f, 3.1f, 0f, 2.6f));
                it.Add(F(FurnitureType.DiningTable, -3f, 0.9f, 0f, 2.2f, 1f));
                for (int i = -1; i <= 1; i++)
                {
                    it.Add(F(FurnitureType.DiningChair, -3f + i * 0.7f, 1.6f, 180f, variant: 2));
                    it.Add(F(FurnitureType.DiningChair, -3f + i * 0.7f, 0.2f, 0f, variant: 2));
                }
                it.Add(F(FurnitureType.Bed, 6f, 4.85f, 180f, 1.9f, 2.15f));
                it.Add(F(FurnitureType.Nightstand, 4.55f, 5.75f, 180f));
                it.Add(F(FurnitureType.Nightstand, 7.45f, 5.75f, 180f));
                it.Add(F(FurnitureType.Wardrobe, 8.68f, -2.6f, -90f, 2.4f));
                it.Add(F(FurnitureType.Desk, 6f, -5.58f, 0f, 1.4f));
                it.Add(F(FurnitureType.OfficeChair, 6f, -4.8f, 180f, variant: 2));
                it.Add(F(FurnitureType.Armchair, 4.1f, -1.2f, 60f, variant: 3));
                it.Add(F(FurnitureType.Rug, 6f, 2.6f, 0f, 2.8f, 2f, variant: 1));
                it.Add(F(FurnitureType.WallArt, 6f, 5.98f, 180f, 1.8f, y: 1.85f, h: 0.9f, variant: 2));
                it.Add(F(FurnitureType.WallArt, -0.5f, -5.98f, 0f, 1.2f, y: 1.8f, h: 0.8f, variant: 0));
                it.Add(F(FurnitureType.Window, 0.6f, 5.99f, 180f, 2.6f, y: 1.7f, h: 1.6f));
                it.Add(F(FurnitureType.Window, -8.99f, -2.2f, 90f, 2.4f, y: 1.7f, h: 1.6f));
                it.Add(F(FurnitureType.Window, 8.99f, 2.4f, -90f, 2f, y: 1.7f, h: 1.5f));
                L.Wake = (7.3f, 3.2f);
            }
            return L;
        }

        // ------------------------------------------------------------------ SHOWROOMS
        /// <summary>Inside of the showroom per building level: x from -HalfWidth to HalfWidth, z from Front to Back.</summary>
        public static (float halfWidth, float front, float back) ShowroomRoom(int level) =>
            level == 1 ? (11.85f, 10.15f, 23.85f) : level == 2 ? (15.85f, 10.2f, 25.85f) : (17.4f, 9.2f, 26.85f);

        /// <summary>Car platforms (and the sales computer) that furniture must keep clear of.</summary>
        public static List<FloorRect> ShowroomObstacles(int level)
        {
            var r = new List<FloorRect> { new FloorRect("Computer", 0f, 21.5f, 1.8f, 0.9f) };
            if (level == 1) foreach (float x in new[] { -6f, 6f }) r.Add(new FloorRect("Platform", x, 17f, 6f, 6f));
            else if (level == 2) foreach (float x in new[] { -12f, -6f, 6f, 12f }) r.Add(new FloorRect("Platform", x, 17f, 5.6f, 5.6f));
            else
            {
                foreach (var (x, z) in new[] { (-6f, 17f), (6f, 17f), (-12f, 14f), (12f, 14f), (-12f, 22f), (12f, 22f) }) r.Add(new FloorRect("Platform", x, z, 5.4f, 5.4f));
                r.Add(new FloorRect("CoffeeBar", 14.5f, 19f, 0.9f, 3.4f));
            }
            return r;
        }

        /// <summary>The showroom's furniture for a building level (reception, lounge, sales desks, coffee point, art).</summary>
        public static List<FurnitureItem> Showroom(int level)
        {
            level = Math.Max(1, Math.Min(3, level));
            var (side, _, wall) = ShowroomRoom(level);
            var it = new List<FurnitureItem>();
            float rx = level == 3 ? -6.6f : -8.2f, rz = level == 3 ? 23.3f : 22.4f, rw = level == 1 ? 3.4f : level == 2 ? 4f : 3.2f;
            it.Add(F(FurnitureType.Reception, rx, rz, 180f, rw));
            it.Add(F(FurnitureType.OfficeChair, rx, rz + 0.95f, 180f));
            float lx = level == 1 ? 8.4f : level == 2 ? 10.8f : 12f, sofaZ = wall - 0.6f;
            it.Add(F(FurnitureType.Sofa, lx, sofaZ, 180f, level == 1 ? 2.2f : 2.6f, seats: level == 1 ? 2 : 3));
            it.Add(F(FurnitureType.CoffeeTable, lx, sofaZ - (level == 3 ? 1.05f : 1.25f), 0f, 1.1f, 0.6f));
            float armDx = level == 1 ? 2.1f : 2.3f, armZ = level == 3 ? sofaZ - 0.3f : sofaZ - 1.3f;   // level 3: clear of the platform in front
            it.Add(F(FurnitureType.Armchair, lx - armDx, armZ, 90f));
            it.Add(F(FurnitureType.Armchair, lx + armDx, armZ, -90f));
            if (level < 3)
            {
                it.Add(F(FurnitureType.Rug, lx, sofaZ - 1.1f, 0f, 3.4f, 2.4f));
                it.Add(F(FurnitureType.FloorLamp, lx + 2.9f, wall - 0.45f, 0f));
            }
            it.Add(F(FurnitureType.WallArt, lx, wall - 0.02f, 180f, 1.6f, y: 2.4f, h: 1f, variant: (level + 1) % 3));
            it.Add(F(FurnitureType.OfficeChair, 1f, 20.75f, -20f));
            if (level >= 2)
                foreach (float x in new[] { -3.4f, 3.4f })
                {
                    it.Add(F(FurnitureType.Desk, x, 22.3f, 180f, 1.4f));
                    it.Add(F(FurnitureType.OfficeChair, x, 21.5f, 0f));
                    foreach (float dx in new[] { -0.4f, 0.4f }) it.Add(F(FurnitureType.DiningChair, x + dx, 23.15f, 180f, variant: 3));
                }
            it.Add(F(FurnitureType.CoffeeStation, -side + 0.4f, level == 3 ? 10.95f : 14.95f, 90f));
            it.Add(F(FurnitureType.Brochures, -3.6f, level == 3 ? 10.2f : 11.2f, 180f));
            if (level < 3)
            {
                it.Add(F(FurnitureType.WallArt, -side + 0.02f, 19.5f, 90f, 1.4f, y: 2.4f, h: 0.9f, variant: 0));
                it.Add(F(FurnitureType.WallArt, side - 0.02f, 15.5f, -90f, 1.4f, y: 2.4f, h: 0.9f, variant: 1));
            }
            else
                foreach (var (x, z) in new[] { (-16.9f, 26.2f), (16.9f, 10.3f), (16.9f, 26.2f) }) it.Add(F(FurnitureType.Plant, x, z, 0f, 1.2f));
            return it;
        }

        // ------------------------------------------------------------------ WALKING
        /// <summary>
        /// True when someone <paramref name="radius"/> wide can walk from (x0, z0) to (x1, z1) inside the room without
        /// touching furniture, outer walls or partitions (doorways excepted). A 10 cm grid flood fill.
        /// </summary>
        public static bool CanWalk(float minX, float minZ, float maxX, float maxZ, IEnumerable<(float minX, float minZ, float maxX, float maxZ)> obstacles,
                                   float x0, float z0, float x1, float z1, float radius)
        {
            const float cell = 0.1f;
            int nx = (int)Math.Ceiling((maxX - minX) / cell), nz = (int)Math.Ceiling((maxZ - minZ) / cell);
            if (nx <= 0 || nz <= 0) return false;
            var blocked = new bool[nx, nz];
            var obs = new List<(float, float, float, float)>(obstacles);
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = minX + (i + 0.5f) * cell, z = minZ + (j + 0.5f) * cell;
                    bool b = x < minX + radius || x > maxX - radius || z < minZ + radius || z > maxZ - radius;
                    if (!b)
                        foreach (var (ax, az, bx, bz) in obs)
                            if (x > ax - radius && x < bx + radius && z > az - radius && z < bz + radius) { b = true; break; }
                    blocked[i, j] = b;
                }
            int Ix(float x) => Math.Max(0, Math.Min(nx - 1, (int)((x - minX) / cell)));
            int Iz(float z) => Math.Max(0, Math.Min(nz - 1, (int)((z - minZ) / cell)));
            int si = Ix(x0), sj = Iz(z0), ti = Ix(x1), tj = Iz(z1);
            if (blocked[si, sj] || blocked[ti, tj]) return false;
            var seen = new bool[nx, nz];
            var q = new Queue<(int, int)>();
            q.Enqueue((si, sj)); seen[si, sj] = true;
            while (q.Count > 0)
            {
                var (i, j) = q.Dequeue();
                if (i == ti && j == tj) return true;
                foreach (var (di, dj) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int a = i + di, b = j + dj;
                    if (a < 0 || b < 0 || a >= nx || b >= nz || seen[a, b] || blocked[a, b]) continue;
                    seen[a, b] = true;
                    q.Enqueue((a, b));
                }
            }
            return false;
        }

        /// <summary>Obstacles of a home: blocking furniture plus the partition walls (minus their doorways).</summary>
        public static List<(float minX, float minZ, float maxX, float maxZ)> HomeObstacles(InteriorLayout L)
        {
            var list = new List<(float, float, float, float)>();
            foreach (var i in L.Items) if (i.Blocks) list.Add(i.Footprint());
            float hz = L.SizeZ / 2f;
            foreach (var (x, g0, g1) in L.Partitions)
            {
                list.Add((x - 0.07f, -hz, x + 0.07f, g0));
                list.Add((x - 0.07f, g1, x + 0.07f, hz));
            }
            return list;
        }
    }
}
