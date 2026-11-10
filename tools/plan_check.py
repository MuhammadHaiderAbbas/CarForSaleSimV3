#!/usr/bin/env python3
"""
Validates Assets/CarSales/Sim/World/HarbourPlan.cs the same way the EditMode tests do
(Phase12Tests / Phase14Tests), plus road-network connectivity, so the map can be
checked without opening Unity:

    python3 tools/plan_check.py [plan.cs]

Exit code 0 = every check passed. Prints each failure precisely.
"""
import math, re, sys

PLAN = sys.argv[1] if len(sys.argv) > 1 else "Assets/CarSales/Sim/World/HarbourPlan.cs"
src = open(PLAN, encoding="utf-8").read()

def num(s):
    return float(s[:-1] if s.endswith('f') else s)

fails, notes = [], []
def ok(cond, label):
    (print(f"  ok   {label}") if cond else (fails.append(label), print(f"  FAIL {label}")))
    return cond

# ---------------- parse zones / sites / ring / roads / roundabouts / lights ----------------
zones = []
for m in re.finditer(r'new PlanZone \{ Number = (\d+), Name = "([^"]+)", X0 = (-?[\d.]+f?), Z0 = (-?[\d.]+f?), X1 = (-?[\d.]+f?), Z1 = (-?[\d.]+f?) \}', src):
    zones.append(dict(number=int(m.group(1)), name=m.group(2), x0=num(m.group(3)), z0=num(m.group(4)), x1=num(m.group(5)), z1=num(m.group(6))))

sites = []
for m in re.finditer(r'S\("([^"]+)", "([^"]+)", (\d+), (-?[\d.]+f?), (-?[\d.]+f?), (-?[\d.]+f?),\s*(-?[\d.]+f?), (-?[\d.]+f?), (-?[\d.]+f?), (-?[\d.]+f?)\)', src):
    sites.append(dict(id=m.group(1), name=m.group(2), zone=int(m.group(3)),
                      x=num(m.group(4)), z=num(m.group(5)), yaw=num(m.group(6)),
                      fx0=num(m.group(7)), fz0=num(m.group(8)), fx1=num(m.group(9)), fz1=num(m.group(10))))

ring_m = re.search(r'public static readonly V2\[\] Ring =\s*\{(.*?)\};', src, re.S)
ring = [(num(a), num(b)) for a, b in re.findall(r'P\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', ring_m.group(1))]

roads = []
for m in re.finditer(r'new PlanRoad \{ Name = "([^"]+)", Width = (-?[\d.]+f?), SpeedLimit = (-?[\d.]+f?),(?: Pavements = (true|false),)? Points = new\[\] \{(.*?)\}\s*\}', src, re.S):
    pts = [(num(a), num(b)) for a, b in re.findall(r'P\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', m.group(5))]
    roads.append(dict(name=m.group(1), width=num(m.group(2)), pts=pts))

rm = re.search(r'Roundabouts =\s*\{(.*?)\};', src, re.S)
roundabouts = [(num(a), num(b)) for a, b in re.findall(r'PlanRoundabout\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', rm.group(1))]
lm = re.search(r'LightNodes =\s*\{(.*?)\};', src, re.S)
lights = [(num(a), num(b)) for a, b in re.findall(r'P\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', lm.group(1))]

# ---------------- geometry helpers (same maths as the plan) ----------------
def seg_dist(px, pz, ax, az, bx, bz):
    abx, abz = bx - ax, bz - az
    l2 = abx * abx + abz * abz
    t = 0.0 if l2 < 1e-9 else max(0.0, min(1.0, ((px - ax) * abx + (pz - az) * abz) / l2))
    cx, cz = ax + t * abx, az + t * abz
    return math.hypot(px - cx, pz - cz)

def road_clearance(px, pz):
    best = float("inf")
    for r in roads:
        for i in range(len(r["pts"]) - 1):
            a, b = r["pts"][i], r["pts"][i + 1]
            best = min(best, seg_dist(px, pz, a[0], a[1], b[0], b[1]) - r["width"] / 2.0)
    for (cx, cz) in roundabouts:
        best = min(best, math.hypot(px - cx, pz - cz) - (15.0 + 6.0))
    return best

def box_touches_road(b, margin=0.5):
    x0, z0, x1, z1 = b
    sx = max(1.0, (x1 - x0) / 24.0); sz = max(1.0, (z1 - z0) / 24.0)
    x = x0
    while x <= x1 + 0.01:
        z = z0
        while z <= z1 + 0.01:
            if road_clearance(x, z) < margin:
                return True
            z += sz
        x += sx
    return False

def to_world(s, lx, lz):
    r = math.radians(s["yaw"]); c, sn = math.cos(r), math.sin(r)
    return (s["x"] + lx * c + lz * sn, s["z"] - lx * sn + lz * c)

def world_box(s):
    pts = [to_world(s, s["fx0"], s["fz0"]), to_world(s, s["fx1"], s["fz0"]),
           to_world(s, s["fx0"], s["fz1"]), to_world(s, s["fx1"], s["fz1"])]
    xs = [p[0] for p in pts]; zs = [p[1] for p in pts]
    return (min(xs), min(zs), max(xs), max(zs))

def zone_at(number, p):
    cand = [z for z in zones if z["number"] == number]
    if not cand:
        return None
    inside = [z for z in cand if z["x0"] - 40 <= p[0] <= z["x1"] + 40 and z["z0"] - 40 <= p[1] <= z["z1"] + 40]
    if inside:
        return inside[0]
    return min(cand, key=lambda z: math.hypot((z["x0"] + z["x1"]) / 2 - p[0], (z["z0"] + z["z1"]) / 2 - p[1]))

# ---------------- checks ----------------
print(f"Harbour plan check — {PLAN}\n")
print(f"parsed: {len(zones)} zones, {len(sites)} sites, {len(roads)} roads, {len(roundabouts)} roundabouts, {len(lights)} light nodes\n")

for z in zones:
    ok(z["x0"] < z["x1"] and z["z0"] < z["z1"], f"zone {z['number']} {z['name']!r} has positive size")
numbers = sorted({z["number"] for z in zones})
ok(numbers == list(range(1, 16)), f"zones numbered 1..15 (got {numbers})")
ok(sum(1 for z in zones if z["number"] == 14) == 2, "two zones numbered 14 (expansion plots)")
ok("Business Park" in next(z for z in zones if z["number"] == 15)["name"], "zone 15 is the business park")

print()
for s in sites:
    zid = zone_at(s["zone"], (s["x"], s["z"]))
    ok(zid is not None and zid["x0"] - 40 <= s["x"] <= zid["x1"] + 40 and zid["z0"] - 40 <= s["z"] <= zid["z1"] + 40,
       f"{s['name']}: position in zone {s['zone']}")
    box = world_box(s)
    ok(not box_touches_road(box), f"{s['name']}: footprint off the roads")
    front = to_world(s, (s["fx0"] + s["fx1"]) / 2.0, s["fz0"] - 2.0)
    d = road_clearance(front[0], front[1])
    ok(d < 14.0, f"{s['name']}: entrance {d:.1f} m from a road (<14 m)")

print()
for i in range(len(sites)):
    for j in range(i + 1, len(sites)):
        a, b = world_box(sites[i]), world_box(sites[j])
        if a[0] < b[2] and b[0] < a[2] and a[1] < b[3] and b[1] < a[3]:
            fails.append(f"{sites[i]['name']} overlaps {sites[j]['name']}")
            print(f"  FAIL {sites[i]['name']} overlaps {sites[j]['name']}")
print("  ok   no two site footprints overlap" if not any('overlaps' in f for f in fails) else "")

print()
# Business Park Road serves the three south-side sites; Horizon Drive reaches the ring.
by_name = {r["name"]: r for r in roads}
for req in ("Horizon Drive", "Business Park Road"):
    ok(req in by_name, f"{req} exists")
bpr = by_name.get("Business Park Road")
if bpr:
    for sid in ("NorthgateOffices", "OfficesWest", "RentGo"):
        s = next(s for s in sites if s["id"] == sid)
        front = to_world(s, (s["fx0"] + s["fx1"]) / 2.0, s["fz0"] - 2.0)
        d = min(seg_dist(front[0], front[1], p[0], p[1], q[0], q[1])
                for p, q in zip(bpr["pts"], bpr["pts"][1:]))
        ok(d < 12.0, f"{s['name']}: front {d:.1f} m from Business Park Road (<12 m)")

hd = by_name.get("Horizon Drive")
if hd:
    south = hd["pts"][-1]
    best = min(seg_dist(south[0], south[1], ring[i][0], ring[i][1], ring[i + 1][0], ring[i + 1][1])
               for i in range(len(ring) - 1))
    ok(best < 6.0, f"Horizon Drive meets the ring road ({best:.1f} m)")

print()
# Ring closed + perimeter + map frame
per = sum(math.dist(ring[i], ring[i + 1]) for i in range(len(ring) - 1))
ok(math.dist(ring[0], ring[-1]) < 0.01, "ring is a closed loop")
print(f"  note perimeter ≈ {per:.0f} m")
ok(per > 1500, "ring is at least 1.5 km (test-drive loop)")
frame_half = 525.0
for r in roads:
    for p in r["pts"]:
        ok(abs(p[0]) <= frame_half + 1 and abs(p[1]) <= frame_half + 1, f"road point {p} inside the map frame")

# Connectivity: every road shares a vertex (≤1 m) with the rest of the network.
nodes = []
parent = []
def node(p):
    for i, n in enumerate(nodes):
        if math.dist(n, p) < 1.0:
            return i
    nodes.append(p)
    parent.append(len(nodes) - 1)
    return len(nodes) - 1
def find(i):
    while parent[i] != i:
        parent[i] = parent[parent[i]]; i = parent[i]
    return i
edges = []
for r in roads:
    for i in range(len(r["pts"]) - 1):
        a, b = node(r["pts"][i]), node(r["pts"][i + 1])
        edges.append((a, b))
        ra, rb = find(a), find(b)
        if ra != rb: parent[ra] = rb
verts = []
for r in roads: verts += r["pts"]
for r in roads:
    pass
roots = {find(i) for i in range(len(nodes))}
ok(len(roots) == 1, f"the whole road network is one connected town ({len(nodes)} junctions)")

# Light nodes sit on a shared junction (within 1 m of vertices of at least two roads)
for p in lights:
    count = sum(1 for r in roads for q in r["pts"] if math.dist(q, p) < 1.0)
    ok(count >= 2, f"light node {p} is a shared junction of {count} road vertices")

print()
if fails:
    print(f"FAILED: {len(fails)} problem(s):")
    for f in fails:
        print("  -", f)
    sys.exit(1)
print("ALL CHECKS PASSED")
sys.exit(0)
