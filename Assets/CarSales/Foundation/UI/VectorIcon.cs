using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CarSales.Foundation
{
    /// <summary>
    /// A tiny SVG reader and rasteriser for the interface icons (no Unity, no packages): paths (M L H V C S Q T A Z,
    /// absolute and relative), rect, circle, ellipse and polygon, with matrix transforms; fill="none" is skipped.
    /// Fills with the non-zero rule and anti-aliases by supersampling. Output is an alpha mask, top row first.
    /// </summary>
    public static class VectorIcon
    {
        public struct Pt { public float X, Y; public Pt(float x, float y) { X = x; Y = y; } }

        static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

        // ------------------------------------------------------------------ PARSING
        /// <summary>Polygons (flattened curves) of every filled shape in the SVG, in the SVG's own units.</summary>
        public static List<List<Pt>> Parse(string svg, out float viewW, out float viewH)
        {
            viewW = viewH = 24f;
            var vb = Regex.Match(svg, "viewBox=\"([^\"]+)\"");
            if (vb.Success)
            {
                var n = Numbers(vb.Groups[1].Value);
                if (n.Count == 4) { viewW = n[2]; viewH = n[3]; }
            }
            var polys = new List<List<Pt>>();
            foreach (Match el in Regex.Matches(svg, "<(path|rect|circle|ellipse|polygon)\\b([^>]*)/?>"))
            {
                string tag = el.Groups[1].Value, attrs = el.Groups[2].Value;
                if (Attr(attrs, "fill") == "none") continue;
                var shapes = new List<List<Pt>>();
                switch (tag)
                {
                    case "path": shapes = PathPolys(Attr(attrs, "d") ?? ""); break;
                    case "rect":
                    {
                        float x = F(attrs, "x"), y = F(attrs, "y"), w = F(attrs, "width"), h = F(attrs, "height");
                        if (w <= 0f || h <= 0f) break;
                        shapes.Add(new List<Pt> { new Pt(x, y), new Pt(x + w, y), new Pt(x + w, y + h), new Pt(x, y + h) });
                        break;
                    }
                    case "circle": shapes.Add(Ellipse(F(attrs, "cx"), F(attrs, "cy"), F(attrs, "r"), F(attrs, "r"))); break;
                    case "ellipse": shapes.Add(Ellipse(F(attrs, "cx"), F(attrs, "cy"), F(attrs, "rx"), F(attrs, "ry"))); break;
                    case "polygon":
                    {
                        var n = Numbers(Attr(attrs, "points") ?? "");
                        var poly = new List<Pt>();
                        for (int i = 0; i + 1 < n.Count; i += 2) poly.Add(new Pt(n[i], n[i + 1]));
                        if (poly.Count > 2) shapes.Add(poly);
                        break;
                    }
                }
                var tr = Attr(attrs, "transform");
                if (!string.IsNullOrEmpty(tr) && tr.StartsWith("matrix"))
                {
                    var m = Numbers(tr.Substring(6));
                    if (m.Count == 6)
                        foreach (var s in shapes)
                            for (int i = 0; i < s.Count; i++)
                                s[i] = new Pt(m[0] * s[i].X + m[2] * s[i].Y + m[4], m[1] * s[i].X + m[3] * s[i].Y + m[5]);
                }
                // The 24x24 "M0 0h24v24H0z" background box is always fill="none"; a filled full-size box would hide the icon.
                foreach (var s in shapes) if (s.Count > 2) polys.Add(s);
            }
            return polys;
        }

        static string Attr(string attrs, string name)
        {
            var m = Regex.Match(attrs, "(?:^|\\s)" + name + "=\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        static float F(string attrs, string name) { var v = Attr(attrs, name); return v != null && float.TryParse(v, NumberStyles.Float, Ci, out var f) ? f : 0f; }

        static List<float> Numbers(string s)
        {
            var list = new List<float>();
            foreach (Match m in Regex.Matches(s, "-?(?:\\d+\\.?\\d*|\\.\\d+)(?:[eE][-+]?\\d+)?"))
                list.Add(float.Parse(m.Value, Ci));
            return list;
        }

        static List<Pt> Ellipse(float cx, float cy, float rx, float ry)
        {
            var p = new List<Pt>();
            int n = 48;
            for (int i = 0; i < n; i++) { double a = i * 2 * Math.PI / n; p.Add(new Pt(cx + rx * (float)Math.Cos(a), cy + ry * (float)Math.Sin(a))); }
            return p;
        }

        /// <summary>Splits path data into flattened sub-paths.</summary>
        public static List<List<Pt>> PathPolys(string d)
        {
            var polys = new List<List<Pt>>();
            var tokens = Regex.Matches(d, "[MmLlHhVvCcSsQqTtAaZz]|-?(?:\\d+\\.?\\d*|\\.\\d+)(?:[eE][-+]?\\d+)?");
            int i = 0;
            char cmd = 'M';
            float x = 0, y = 0, sx = 0, sy = 0, lcx = 0, lcy = 0;   // current, sub-path start, last control
            char prev = ' ';
            List<Pt> cur = null;
            bool IsCmd(int k) => k < tokens.Count && char.IsLetter(tokens[k].Value[0]) && tokens[k].Value != "e";
            float Num() => float.Parse(tokens[i++].Value, Ci);
            void Start() { if (cur != null && cur.Count > 2) polys.Add(cur); cur = new List<Pt> { new Pt(x, y) }; }
            void To(float nx, float ny) { if (cur == null) Start(); cur.Add(new Pt(nx, ny)); x = nx; y = ny; }

            while (i < tokens.Count)
            {
                if (IsCmd(i)) cmd = tokens[i++].Value[0];
                else if (cmd == 'M') cmd = 'L'; else if (cmd == 'm') cmd = 'l';   // extra pairs after a move are lines
                bool rel = char.IsLower(cmd);
                char up = char.ToUpperInvariant(cmd);
                if (up != 'Z' && i >= tokens.Count) break;
                switch (up)
                {
                    case 'M':
                    {
                        float nx = Num(), ny = Num();
                        if (rel) { nx += x; ny += y; }
                        x = nx; y = ny; sx = x; sy = y;
                        Start();
                        break;
                    }
                    case 'L': { float nx = Num(), ny = Num(); if (rel) { nx += x; ny += y; } To(nx, ny); break; }
                    case 'H': { float nx = Num(); if (rel) nx += x; To(nx, y); break; }
                    case 'V': { float ny = Num(); if (rel) ny += y; To(x, ny); break; }
                    case 'C':
                    case 'S':
                    {
                        float c1x, c1y;
                        if (up == 'C') { c1x = Num(); c1y = Num(); if (rel) { c1x += x; c1y += y; } }
                        else
                        {
                            bool smooth = "CcSs".IndexOf(prev) >= 0;
                            c1x = smooth ? 2 * x - lcx : x; c1y = smooth ? 2 * y - lcy : y;
                        }
                        float c2x = Num(), c2y = Num(), ex = Num(), ey = Num();
                        if (rel) { c2x += x; c2y += y; ex += x; ey += y; }
                        Cubic(x, y, c1x, c1y, c2x, c2y, ex, ey, To);
                        lcx = c2x; lcy = c2y;
                        break;
                    }
                    case 'Q':
                    case 'T':
                    {
                        float qx, qy;
                        if (up == 'Q') { qx = Num(); qy = Num(); if (rel) { qx += x; qy += y; } }
                        else { bool smooth = "QqTt".IndexOf(prev) >= 0; qx = smooth ? 2 * x - lcx : x; qy = smooth ? 2 * y - lcy : y; }
                        float ex = Num(), ey = Num();
                        if (rel) { ex += x; ey += y; }
                        float x0 = x, y0 = y;
                        Cubic(x0, y0, x0 + 2f / 3f * (qx - x0), y0 + 2f / 3f * (qy - y0), ex + 2f / 3f * (qx - ex), ey + 2f / 3f * (qy - ey), ex, ey, To);
                        lcx = qx; lcy = qy;
                        break;
                    }
                    case 'A':
                    {
                        float rx = Num(), ry = Num(), rot = Num(), large = Num(), sweep = Num(), ex = Num(), ey = Num();
                        if (rel) { ex += x; ey += y; }
                        Arc(x, y, rx, ry, rot, large != 0f, sweep != 0f, ex, ey, To);
                        break;
                    }
                    case 'Z':
                        if (cur != null && cur.Count > 2) polys.Add(cur);
                        cur = null;
                        x = sx; y = sy;
                        break;
                    default: i++; break;
                }
                prev = cmd;
            }
            if (cur != null && cur.Count > 2) polys.Add(cur);
            return polys;
        }

        static void Cubic(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, Action<float, float> to)
        {
            const int n = 12;
            for (int k = 1; k <= n; k++)
            {
                float t = k / (float)n, u = 1 - t;
                to(u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x3,
                   u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y3);
            }
        }

        /// <summary>SVG elliptical arc (endpoint form) flattened to lines.</summary>
        static void Arc(float x1, float y1, float rx, float ry, float rotDeg, bool large, bool sweep, float x2, float y2, Action<float, float> to)
        {
            if (rx == 0f || ry == 0f) { to(x2, y2); return; }
            rx = Math.Abs(rx); ry = Math.Abs(ry);
            double phi = rotDeg * Math.PI / 180, cp = Math.Cos(phi), sp = Math.Sin(phi);
            double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
            double x1p = cp * dx + sp * dy, y1p = -sp * dx + cp * dy;
            double lam = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
            if (lam > 1) { double s = Math.Sqrt(lam); rx *= (float)s; ry *= (float)s; }
            double num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
            double den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
            double coef = (large != sweep ? 1 : -1) * Math.Sqrt(Math.Max(0, num / den));
            double cxp = coef * rx * y1p / ry, cyp = -coef * ry * x1p / rx;
            double cx = cp * cxp - sp * cyp + (x1 + x2) / 2.0, cy = sp * cxp + cp * cyp + (y1 + y2) / 2.0;
            double Ang(double ux, double uy, double vx, double vy)
            {
                double a = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
                return a;
            }
            double t1 = Ang(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
            double dt = Ang((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
            if (!sweep && dt > 0) dt -= 2 * Math.PI; else if (sweep && dt < 0) dt += 2 * Math.PI;
            int n = Math.Max(4, (int)(Math.Abs(dt) / (Math.PI / 16)));
            for (int k = 1; k <= n; k++)
            {
                double t = t1 + dt * k / n;
                double ex = rx * Math.Cos(t), ey = ry * Math.Sin(t);
                to((float)(cp * ex - sp * ey + cx), (float)(sp * ex + cp * ey + cy));
            }
        }

        // ------------------------------------------------------------------ RASTERISING
        /// <summary>Renders the SVG into a size x size alpha mask (0-255), top row first, with a little padding.</summary>
        public static byte[] Rasterize(string svg, int size, int supersample = 4, float padding = 0.04f)
        {
            var polys = Parse(svg, out float vw, out float vh);
            var alpha = new byte[size * size];
            if (polys.Count == 0) return alpha;
            float scale = size * (1f - 2f * padding) / Math.Max(vw, vh);
            float ox = size * padding, oy = size * padding;
            // Edges in pixel space.
            var edges = new List<(float x0, float y0, float x1, float y1, int dir)>();
            foreach (var p in polys)
                for (int k = 0; k < p.Count; k++)
                {
                    var a = p[k]; var b = p[(k + 1) % p.Count];
                    float ax = ox + a.X * scale, ay = oy + a.Y * scale, bx = ox + b.X * scale, by = oy + b.Y * scale;
                    if (Math.Abs(ay - by) < 1e-6f) continue;
                    edges.Add(ay < by ? (ax, ay, bx, by, 1) : (bx, by, ax, ay, -1));
                }
            int ss = Math.Max(1, supersample);
            var cover = new int[size * size];
            var xs = new List<(float x, int dir)>();
            for (int row = 0; row < size * ss; row++)
            {
                float sy = (row + 0.5f) / ss;
                xs.Clear();
                foreach (var e in edges)
                    if (sy >= e.y0 && sy < e.y1) xs.Add((e.x0 + (sy - e.y0) / (e.y1 - e.y0) * (e.x1 - e.x0), e.dir));
                if (xs.Count < 2) continue;
                xs.Sort((p, q) => p.x.CompareTo(q.x));
                int wind = 0;
                int py = row / ss;
                for (int k = 0; k < xs.Count - 1; k++)
                {
                    wind += xs[k].dir;
                    if (wind == 0) continue;   // non-zero fill rule
                    int sx0 = (int)Math.Ceiling(xs[k].x * ss - 0.5f), sx1 = (int)Math.Floor(xs[k + 1].x * ss - 0.5f);
                    for (int sx = Math.Max(0, sx0); sx <= Math.Min(size * ss - 1, sx1); sx++) cover[py * size + sx / ss]++;
                }
            }
            int full = ss * ss;
            for (int k = 0; k < alpha.Length; k++) alpha[k] = (byte)Math.Min(255, cover[k] * 255 / full);
            return alpha;
        }
    }
}
