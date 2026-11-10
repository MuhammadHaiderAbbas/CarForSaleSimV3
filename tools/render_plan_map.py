#!/usr/bin/env python3
"""
Renders docs/images/final-map.png (and master-map.png) straight from the plan files —
these are exact, not artistic interpretations. Re-run after any plan edit:

    python3 tools/render_plan_map.py
"""
import math, re, sys
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyArrowPatch
import matplotlib.patheffects as pe

PLAN = sys.argv[1] if len(sys.argv) > 1 else "Assets/CarSales/Sim/World/FinalMapPlan.cs"
OUT  = sys.argv[2] if len(sys.argv) > 2 else ("docs/images/final-map.png" if "FinalMap" in PLAN else "docs/images/master-map.png")
TITLE = "CAR FOR SALE — FINAL MAP (V3)" if "FinalMap" in PLAN else "CAR FOR SALE — MASTER MAP (V2)"

src = open(PLAN, encoding="utf-8").read()

def num(s):
    return float(s[:-1] if s.endswith('f') else s)

# ---- zones (optional 4th is allowed: a zone can carry its own label offset later) ----
zones = []
for m in re.finditer(r'new PlanZone \{ Number = (\d+), Name = "([^"]+)", X0 = (-?[\d.]+f?), Z0 = (-?[\d.]+f?), X1 = (-?[\d.]+f?), Z1 = (-?[\d.]+f?) \}', src):
    zones.append((int(m.group(1)), m.group(2), *[num(v) for v in m.groups()[2:]]))

# ---- sites ----
sites = []
for m in re.finditer(r'S\("([^"]+)", "([^"]+)", (\d+), (-?[\d.]+f?), (-?[\d.]+f?), (-?[\d.]+f?),', src):
    sites.append(dict(id=m.group(1), name=m.group(2), zone=int(m.group(3)), x=num(m.group(4)), z=num(m.group(5))))

# ---- roads (Ring + every PlanRoad) ----
ring_m = re.search(r'public static readonly V2\[\] Ring =\s*\{(.*?)\};', src, re.S)
ring = [(num(a), num(b)) for a, b in re.findall(r'P\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', ring_m.group(1))]
roads = []
for m in re.finditer(r'new PlanRoad \{ Name = "([^"]+)", Width = (-?[\d.]+f?), SpeedLimit = (-?[\d.]+f?), Points = new\[\] \{(.*?)\}\s*\}', src, re.S):
    name, width = m.group(1), num(m.group(2))
    pts = [(num(a), num(b)) for a, b in re.findall(r'P\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', m.group(4))]
    roads.append((name, width, pts))

# ---- roundabouts & light nodes ----
rm = re.search(r'Roundabouts =\s*\{(.*?)\};', src, re.S)
roundabouts = [(num(a), num(b)) for a, b in re.findall(r'PlanRoundabout\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', rm.group(1))]
lm = re.search(r'LightNodes =\s*\{(.*?)\};', src, re.S)
lights = [(num(a), num(b)) for a, b in re.findall(r'P\((-?[\d.]+f?),\s*(-?[\d.]+f?)\)', lm.group(1))]

GLYPH = {
    "Home_Trailer": "H", "Home_House": "H", "Home_Villa": "H",
    "autoworld": "D", "dealer_downtown": "D", "dealer_luxury": "D",
    "AuctionCenter": "A", "UsedCarMarket": "U", "ServiceCentre": "S", "MechanicWorkshop": "W",
    "CarWashCenter": "C", "FuelStation": "F", "PartsDirect": "P", "TyreShop": "P",
    "MapleClose": "M", "BirchClose": "M",
    "RentalApartments": "$", "OfficeTower": "$", "ParkingGarage": "$",
    "PatrolStation": "X", "RentGo": "R", "NorthgateOffices": "O", "OfficesWest": "O",
    "NorthgateSite": "Y", "DocksideSite": "Y",
}
ZONE_COL = {1: "#d9ead3", 2: "#cfe2f3", 3: "#fce8b2", 4: "#f4cccc", 5: "#e0e0e0", 6: "#c9daf8",
            7: "#d9d2e9", 8: "#e2f0cb", 9: "#fff2cc", 10: "#d0e0e3", 11: "#ead1dc", 12: "#ffffff",
            13: "#b7e1cd", 14: "#f9cb9c", 15: "#d5a6bd"}

fig = plt.figure(figsize=(19.2, 11.6), dpi=170)
ax = fig.add_axes([0.035, 0.055, 0.685, 0.90])
ax.set_aspect("equal")
ax.set_facecolor("#f4f6ec")
ax.axis("off")

# zones
for num, name, x0, z0, x1, z1 in zones:
    ax.add_patch(mpatches.Rectangle((x0, z0), x1 - x0, z1 - z0, facecolor=ZONE_COL.get(num, "#eeeeee"),
                                    edgecolor="#9aa58f", linewidth=1.1, alpha=0.75, zorder=0))
    ax.text((x0 + x1) / 2, (z0 + z1) / 2, str(num), fontsize=17, fontweight="bold",
            color="#4a5a3a", alpha=0.8, ha="center", va="center", zorder=1)

# ring (test-drive loop) first, thick and dark
rx = [p[0] for p in ring]; rz = [p[1] for p in ring]
ax.plot(rx, rz, color="#37474f", linewidth=13, solid_joinstyle="round", solid_capstyle="round", zorder=2, alpha=0.95)
ax.plot(rx, rz, color="#eceff1", linewidth=1.2, linestyle=(0, (10, 14)), zorder=3, alpha=0.8)

# other roads
for name, width, pts in roads:
    if name == "Test-Drive Loop":
        continue
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    new = name in ("Horizon Drive", "Business Park Road")   # phase-14 roads: distinct colour
    ax.plot(xs, ys, color="#37474f" if not new else "#b03a2e", linewidth=width * 1.15, solid_capstyle="round",
            solid_joinstyle="round", zorder=4, alpha=0.97)
    ax.plot(xs, ys, color="#eceff1", linewidth=1.0, linestyle=(0, (8, 10)), zorder=5, alpha=0.9)

# roundabouts
for x, z in roundabouts:
    ax.add_patch(plt.Circle((x, z), 15, facecolor="#37474f", edgecolor="#eceff1", linewidth=1.6, zorder=6))

# light nodes
for x, z in lights:
    ax.add_patch(plt.Circle((x, z), 5.2, facecolor="#c62828", edgecolor="white", linewidth=1.5, zorder=7))
    ax.text(x, z, "TL", fontsize=4.6, color="white", ha="center", va="center", fontweight="bold", zorder=8)

# sites: glyph dot + number key
for i, s in enumerate(sites, start=1):
    glyph = GLYPH.get(s["id"], "●")
    ax.add_patch(plt.Circle((s["x"], s["z"]), 6.4, facecolor="white", edgecolor="#202020", linewidth=1.3, zorder=8))
    ax.text(s["x"], s["z"], glyph, fontsize=8.5, fontweight="bold", color="#202020", ha="center", va="center", zorder=9)

# ---- side panel: roads + sites key + legend ----
sx = 0.752
fig.text(sx, 0.928, TITLE, fontsize=13.5, fontweight="bold", color="#1b2a1b")
fig.text(sx, 0.897, "Exact render of the plan file (tools/render_plan_map.py).", fontsize=7.5, color="#5a6a5a")

fig.text(sx, 0.852, "ROADS  (dark = master roads,  red = Final-Map additions)", fontsize=8.5, fontweight="bold", color="#1b2a1b")
for i, (name, w, pts) in enumerate(roads):
    ax_patch = "#b03a2e" if name in ("Horizon Drive", "Business Park Road") else "#37474f"
    fig.patches.append(mpatches.Rectangle((sx, 0.822 - i * 0.019), 0.011, 0.0115, transform=fig.transFigure, facecolor=ax_patch, edgecolor="none"))
    fig.text(sx + 0.015, 0.822 - i * 0.019, f"{name}  ({w:.0f} m)", fontsize=7.0, va="center", color="#223022")

ry = 0.822 - len(roads) * 0.019 - 0.012
fig.text(sx, ry, "SITES  (# glyph name — zone)", fontsize=8.5, fontweight="bold", color="#1b2a1b")
for i, s in enumerate(sites):
    glyph = GLYPH.get(s["id"], "●")
    fig.text(sx, ry - 0.0165 * (i + 1), f"{i+1:>2}. {glyph} {s['name']} (zone {s['zone']})", fontsize=6.2, color="#223022")

key_y = ry - 0.0165 * (len(sites) + 2) - 0.012
fig.text(sx, key_y, "GLYPHS:  D dealer  A auction  U used  S service  W workshop  C car wash  F fuel\n"
                     "P parts/tyres  $ investment  H home  G park  T town centre  I industrial  M street\n"
                     "X patrol  R rent-a-car  O office  Y construction", fontsize=6.6, color="#3a4a3a", linespacing=1.3)
fig.text(sx, 0.012, "Scale: ring road is ~1.9 km across.   North = up.   Red dots = junctions with traffic lights.\n"
                     "Zone 12 = the ring road (test-drive loop).   Full details: docs/NEW_MAP.md · docs/CATALOG.md",
         fontsize=6.4, color="#5a6a5a", linespacing=1.35)

plt.savefig(OUT, facecolor="#e9ece0")
print("wrote", OUT)
