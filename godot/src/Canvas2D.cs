using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace AventurasNivel3
{
    /// <summary>
    /// Imitación del CanvasRenderingContext2D del navegador sobre un CanvasItem de Godot.
    /// Permite que el código de dibujo del juego web se traduzca casi línea por línea:
    /// caminos (moveTo, arcos, curvas), relleno, trazo, recorte, transformaciones,
    /// degradados radiales, patrones, texto y opacidad global.
    /// Solo debe usarse dentro del _Draw del nodo dueño (ver CanvasView).
    /// </summary>
    public class Ctx
    {
        public CanvasItem Ci;
        /// <summary>Transformación base (por ejemplo, para escalar el retrato de 120 a 104 px).</summary>
        public Transform2D Base = Transform2D.Identity;

        // ---- Estado público, con los mismos nombres del canvas ----
        public Paint FillStyle = "#000000";
        public Paint StrokeStyle = "#000000";
        public double LineWidth = 1;
        public string Font = "10px sans-serif";
        public string TextAlign = "start";
        public string TextBaseline = "alphabetic";
        public double GlobalAlpha = 1;
        public string LineCap = "butt";
        public string LineJoin = "miter";

        Transform2D T = Transform2D.Identity;
        float[] dash;
        List<Vector2[]> clip;

        struct State
        {
            public Transform2D T; public Paint Fill, Stroke; public double LineWidth, Alpha; public string Font, Align, Baseline, Cap, Join; public float[] Dash; public List<Vector2[]> Clip;
        }
        readonly Stack<State> stack = new();

        // ---- Camino actual, ya en coordenadas del nodo ----
        readonly List<List<Vector2>> subs = new();
        readonly List<bool> closedFlags = new();
        List<Vector2> cur;

        bool xfDirty = true;
        Transform2D lastXf;

        public Ctx(CanvasItem ci) { Ci = ci; }

        /// <summary>Se llama al inicio de cada _Draw.</summary>
        public void BeginFrame()
        {
            T = Base; stack.Clear(); clip = null; GlobalAlpha = 1; dash = null; xfDirty = true;
            subs.Clear(); closedFlags.Clear(); cur = null;
        }

        // ---------------- Estado ----------------
        public void Save() => stack.Push(new State { T = T, Fill = FillStyle, Stroke = StrokeStyle, LineWidth = LineWidth, Alpha = GlobalAlpha, Font = Font, Align = TextAlign, Baseline = TextBaseline, Cap = LineCap, Join = LineJoin, Dash = dash, Clip = clip });
        public void Restore()
        {
            if (stack.Count == 0) return;
            var s = stack.Pop();
            T = s.T; FillStyle = s.Fill; StrokeStyle = s.Stroke; LineWidth = s.LineWidth; GlobalAlpha = s.Alpha; Font = s.Font; TextAlign = s.Align; TextBaseline = s.Baseline; LineCap = s.Cap; LineJoin = s.Join; dash = s.Dash; clip = s.Clip;
        }
        public void Translate(double x, double y) => T = T * new Transform2D(1, 0, 0, 1, (float)x, (float)y);
        public void Rotate(double a) => T = T * new Transform2D((float)a, Vector2.Zero);
        public void Scale(double sx, double sy) => T = T * new Transform2D((float)sx, 0, 0, (float)sy, 0, 0);
        /// <summary>setTransform(1,0,0,1,0,0): vuelve a la identidad (más la base del nodo).</summary>
        public void ResetTransform() => T = Base;
        public void SetLineDash(double[] d) => dash = (d == null || d.Length == 0) ? null : Array.ConvertAll(d, v => (float)v);

        float Sc => MathF.Sqrt(MathF.Abs(T.Determinant()));
        Vector2 P(double x, double y) => T * new Vector2((float)x, (float)y);

        // ---------------- Caminos ----------------
        public void BeginPath() { subs.Clear(); closedFlags.Clear(); cur = null; }
        void NewSub(Vector2 p) { cur = new List<Vector2> { p }; subs.Add(cur); closedFlags.Add(false); }
        void Add(Vector2 p) { if (cur == null) NewSub(p); else cur.Add(p); }
        public void MoveTo(double x, double y) => NewSub(P(x, y));
        public void LineTo(double x, double y) => Add(P(x, y));
        public void ClosePath()
        {
            if (cur == null) return;
            closedFlags[closedFlags.Count - 1] = true;
            var start = cur[0]; cur = null; // un punto nuevo empezará otro subcamino
            _pendingStart = start;
        }
        Vector2? _pendingStart;
        Vector2 Last()
        {
            if (cur != null && cur.Count > 0) return cur[cur.Count - 1];
            if (_pendingStart.HasValue) return _pendingStart.Value;
            return Vector2.Zero;
        }
        bool HasPoint => (cur != null && cur.Count > 0);

        public void QuadraticCurveTo(double cx, double cy, double x, double y)
        {
            if (!HasPoint) { if (_pendingStart.HasValue) NewSub(_pendingStart.Value); else NewSub(P(cx, cy)); }
            var p0 = Last(); var p1 = P(cx, cy); var p2 = P(x, y);
            int n = Math.Clamp((int)((p0.DistanceTo(p1) + p1.DistanceTo(p2)) / 4f), 4, 32);
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n, u = 1 - t;
                Add(p0 * (u * u) + p1 * (2 * u * t) + p2 * (t * t));
            }
        }
        public void BezierCurveTo(double c1x, double c1y, double c2x, double c2y, double x, double y)
        {
            if (!HasPoint) { if (_pendingStart.HasValue) NewSub(_pendingStart.Value); else NewSub(P(c1x, c1y)); }
            var p0 = Last(); var p1 = P(c1x, c1y); var p2 = P(c2x, c2y); var p3 = P(x, y);
            int n = Math.Clamp((int)((p0.DistanceTo(p1) + p1.DistanceTo(p2) + p2.DistanceTo(p3)) / 4f), 6, 40);
            for (int i = 1; i <= n; i++)
            {
                float t = i / (float)n, u = 1 - t;
                Add(p0 * (u * u * u) + p1 * (3 * u * u * t) + p2 * (3 * u * t * t) + p3 * (t * t * t));
            }
        }
        static float Sweep(float a0, float a1, bool ccw)
        {
            const float TAU = MathF.PI * 2;
            float s;
            if (!ccw)
            {
                s = a1 - a0;
                if (s >= TAU) return TAU;
                s %= TAU; if (s < 0) s += TAU;
                return s;
            }
            s = a0 - a1;
            if (s >= TAU) return -TAU;
            s %= TAU; if (s < 0) s += TAU;
            return -s;
        }
        public void Arc(double x, double y, double r, double a0, double a1, bool ccw = false) => Ellipse(x, y, r, r, 0, a0, a1, ccw);
        public void Ellipse(double x_, double y_, double rx_, double ry_, double rot_, double a0_, double a1_, bool ccw = false)
        {
            float x = (float)x_, y = (float)y_, rot = (float)rot_, a0 = (float)a0_, a1 = (float)a1_;
            float rx = MathF.Max(0, (float)rx_), ry = MathF.Max(0, (float)ry_);
            float sw = Sweep(a0, a1, ccw);
            float rd = MathF.Max(rx, ry) * Sc;
            int n = Math.Clamp((int)MathF.Ceiling(MathF.Abs(sw) * rd / 2.5f), 8, 120);
            float cr = MathF.Cos(rot), sr = MathF.Sin(rot);
            for (int i = 0; i <= n; i++)
            {
                float a = a0 + sw * i / n;
                float ex = MathF.Cos(a) * rx, ey = MathF.Sin(a) * ry;
                var p = P(x + ex * cr - ey * sr, y + ex * sr + ey * cr);
                if (i == 0 && !HasPoint) NewSub(p); else Add(p);
            }
        }
        public void Rect(double x, double y, double w, double h)
        {
            NewSub(P(x, y)); Add(P(x + w, y)); Add(P(x + w, y + h)); Add(P(x, y + h));
            closedFlags[closedFlags.Count - 1] = true; cur = null; _pendingStart = P(x, y);
        }
        public void RoundRect(double x, double y, double w, double h, double r) => RoundRect(x, y, w, h, new[] { r });
        public void RoundRect(double x_, double y_, double w_, double h_, double[] radiiD)
        {
            float x = (float)x_, y = (float)y_, w = (float)w_, h = (float)h_; var radii = Array.ConvertAll(radiiD, v => (float)v);
            if (w < 0) { x += w; w = -w; }
            if (h < 0) { y += h; h = -h; }
            float tl, tr, br, bl;
            switch (radii.Length)
            {
                case 1: tl = tr = br = bl = radii[0]; break;
                case 2: tl = br = radii[0]; tr = bl = radii[1]; break;
                case 3: tl = radii[0]; tr = bl = radii[1]; br = radii[2]; break;
                default: tl = radii[0]; tr = radii[1]; br = radii[2]; bl = radii[3]; break;
            }
            tl = MathF.Max(0, tl); tr = MathF.Max(0, tr); br = MathF.Max(0, br); bl = MathF.Max(0, bl);
            float f = 1;
            if (tl + tr > 0) f = MathF.Min(f, w / (tl + tr));
            if (bl + br > 0) f = MathF.Min(f, w / (bl + br));
            if (tl + bl > 0) f = MathF.Min(f, h / (tl + bl));
            if (tr + br > 0) f = MathF.Min(f, h / (tr + br));
            if (f < 1) { tl *= f; tr *= f; br *= f; bl *= f; }
            cur = null; _pendingStart = null;
            CornerArc(x + tl, y + tl, tl, MathF.PI, MathF.PI * 1.5f, true);
            CornerArc(x + w - tr, y + tr, tr, MathF.PI * 1.5f, MathF.PI * 2, false);
            CornerArc(x + w - br, y + h - br, br, 0, MathF.PI * 0.5f, false);
            CornerArc(x + bl, y + h - bl, bl, MathF.PI * 0.5f, MathF.PI, false);
            closedFlags[closedFlags.Count - 1] = true; _pendingStart = cur[0]; cur = null;
        }
        void CornerArc(float cx, float cy, float r, float a0, float a1, bool first)
        {
            if (r <= 0.01f) { var p = P(cx, cy); if (first) NewSub(p); else Add(p); return; }
            int n = Math.Clamp((int)MathF.Ceiling(r * Sc / 2f), 2, 16);
            for (int i = 0; i <= n; i++)
            {
                float a = a0 + (a1 - a0) * i / n;
                var p = P(cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
                if (first && i == 0) NewSub(p); else Add(p);
            }
        }

        // ---------------- Relleno y trazo ----------------
        public void Fill()
        {
            for (int i = 0; i < subs.Count; i++) if (subs[i].Count >= 3) FillPoly(subs[i].ToArray(), FillStyle);
        }
        public void Stroke()
        {
            for (int i = 0; i < subs.Count; i++)
            {
                var s = subs[i]; if (s.Count < 2) continue;
                var pts = new List<Vector2>(s);
                if (closedFlags[i]) pts.Add(s[0]);
                StrokePoly(pts.ToArray(), closedFlags[i]);
            }
        }
        public void Clip()
        {
            var polys = new List<Vector2[]>();
            foreach (var s in subs) { var c = Clean(s.ToArray()); if (c.Length >= 3) polys.Add(c); }
            if (clip == null) { clip = polys; return; }
            var res = new List<Vector2[]>();
            foreach (var a in clip) foreach (var b in polys) foreach (var r in Geometry2D.IntersectPolygons(a, b)) res.Add(r);
            clip = res;
        }
        public void FillRect(double x, double y, double w, double h)
        {
            if (FillStyle.Pat != null) { PatternRect(FillStyle.Pat, (float)x, (float)y, (float)w, (float)h); return; }
            var poly = new[] { P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h) };
            FillPoly(poly, FillStyle);
        }
        public void StrokeRect(double x, double y, double w, double h)
        {
            StrokePoly(new[] { P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h), P(x, y) }, true);
        }

        static Vector2[] Clean(Vector2[] pts)
        {
            var o = new List<Vector2>(pts.Length);
            foreach (var p in pts) if (o.Count == 0 || o[o.Count - 1].DistanceSquaredTo(p) > 1e-4f) o.Add(p);
            while (o.Count > 2 && o[0].DistanceSquaredTo(o[o.Count - 1]) <= 1e-4f) o.RemoveAt(o.Count - 1);
            return o.ToArray();
        }

        void SetXf(Transform2D x)
        {
            if (!xfDirty && x.IsEqualApprox(lastXf)) return;
            Ci.DrawSetTransformMatrix(x); lastXf = x; xfDirty = false;
        }

        Color WithAlpha(Color c) => new Color(c.R, c.G, c.B, c.A * (float)GlobalAlpha);

        void FillPoly(Vector2[] raw, Paint paint)
        {
            var pts = Clean(raw); if (pts.Length < 3) return;
            Rect2 box = Bounds(pts);
            if (clip == null) { Tri(pts, paint, box); return; }
            foreach (var c in clip) foreach (var r in Geometry2D.IntersectPolygons(pts, c)) { if (Geometry2D.IsPolygonClockwise(r) != Geometry2D.IsPolygonClockwise(pts) && r.Length > 0) { /* posible hueco: se dibuja igual */ } Tri(Clean(r), paint, box); }
        }
        static Rect2 Bounds(Vector2[] pts)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in pts) { x0 = MathF.Min(x0, p.X); y0 = MathF.Min(y0, p.Y); x1 = MathF.Max(x1, p.X); y1 = MathF.Max(y1, p.Y); }
            return new Rect2(x0, y0, MathF.Max(0.001f, x1 - x0), MathF.Max(0.001f, y1 - y0));
        }
        void Tri(Vector2[] pts, Paint paint, Rect2 box)
        {
            if (pts.Length < 3) return;
            int[] idx = Geometry2D.TriangulatePolygon(pts);
            if (idx == null || idx.Length < 3)
            {
                idx = new int[(pts.Length - 2) * 3];
                for (int i = 0; i < pts.Length - 2; i++) { idx[i * 3] = 0; idx[i * 3 + 1] = i + 1; idx[i * 3 + 2] = i + 2; }
            }
            var cols = new Color[pts.Length];
            Rid tex = default; Vector2[] uvs = null;
            if (paint.Grad != null)
            {
                var (t, rect) = paint.Grad.TextureFor(T, box);
                for (int i = 0; i < cols.Length; i++) cols[i] = new Color(1, 1, 1, (float)GlobalAlpha);
                uvs = new Vector2[pts.Length];
                for (int i = 0; i < pts.Length; i++) uvs[i] = (pts[i] - rect.Position) / rect.Size;
                tex = t.GetRid();
            }
            else
            {
                var c = WithAlpha(paint.Col ?? Colors.Black);
                if (c.A <= 0.001f) return;
                for (int i = 0; i < cols.Length; i++) cols[i] = c;
            }
            SetXf(Transform2D.Identity);
            RenderingServer.CanvasItemAddTriangleArray(Ci.GetCanvasItem(), idx, pts, cols, uvs, null, null, tex);
        }

        void StrokePoly(Vector2[] pts, bool closed)
        {
            var c = WithAlpha(StrokeStyle.Col ?? Colors.Black);
            if (c.A <= 0.001f || pts.Length < 2) return;
            float w = MathF.Max(0.6f, (float)LineWidth * Sc);
            var lines = new List<Vector2[]>();
            if (dash != null) lines.AddRange(Dashed(pts, dash, Sc)); else lines.Add(pts);
            if (clip != null)
            {
                var cl = new List<Vector2[]>();
                foreach (var l in lines) foreach (var cp in clip) foreach (var r in Geometry2D.IntersectPolylineWithPolygon(l, cp)) cl.Add(r);
                lines = cl;
            }
            SetXf(Transform2D.Identity);
            bool roundJ = LineJoin == "round" || w >= 2.2f;
            foreach (var l in lines)
            {
                if (l.Length < 2) continue;
                Ci.DrawPolyline(l, c, w, true);
                if (c.A >= 0.999f && roundJ && l.Length <= 48 && w > 1.6f)
                {
                    for (int i = 1; i < l.Length - 1; i++) Ci.DrawCircle(l[i], w * 0.5f, c);
                    if (closed && dash == null) Ci.DrawCircle(l[0], w * 0.5f, c);
                }
                if (LineCap == "round" && !closed && c.A >= 0.999f) { Ci.DrawCircle(l[0], w * 0.5f, c); Ci.DrawCircle(l[l.Length - 1], w * 0.5f, c); }
            }
        }
        static IEnumerable<Vector2[]> Dashed(Vector2[] pts, float[] pattern, float sc)
        {
            var res = new List<Vector2[]>();
            int k = 0; float left = pattern[0] * sc; bool on = true;
            var curL = new List<Vector2> { pts[0] };
            for (int i = 1; i < pts.Length; i++)
            {
                Vector2 a = pts[i - 1], b = pts[i];
                float seg = a.DistanceTo(b);
                while (seg > 0)
                {
                    if (seg < left) { left -= seg; if (on) curL.Add(b); seg = 0; }
                    else
                    {
                        Vector2 m = a + (b - a) * (left / seg);
                        if (on) { curL.Add(m); if (curL.Count >= 2) res.Add(curL.ToArray()); }
                        on = !on; k = (k + 1) % pattern.Length; seg -= left; a = m; left = pattern[k] * sc;
                        curL = new List<Vector2> { m };
                    }
                }
            }
            if (on && curL.Count >= 2) res.Add(curL.ToArray());
            return res;
        }

        void PatternRect(Pattern pat, float x, float y, float w, float h)
        {
            var tex = pat.Tex; float tw = tex.GetWidth(), th = tex.GetHeight();
            if (tw <= 0 || th <= 0) return;
            SetXf(T);
            var mod = new Color(1, 1, 1, (float)GlobalAlpha);
            float sx = MathF.Floor(x / tw) * tw, sy = MathF.Floor(y / th) * th;
            for (float ty = sy; ty < y + h; ty += th)
                for (float tx = sx; tx < x + w; tx += tw)
                {
                    float x0 = MathF.Max(tx, x), y0 = MathF.Max(ty, y), x1 = MathF.Min(tx + tw, x + w), y1 = MathF.Min(ty + th, y + h);
                    if (x1 <= x0 || y1 <= y0) continue;
                    Ci.DrawTextureRectRegion(tex, new Rect2(x0, y0, x1 - x0, y1 - y0), new Rect2(x0 - tx, y0 - ty, x1 - x0, y1 - y0), mod);
                }
        }

        // ---------------- Imágenes ----------------
        public void DrawImage(Texture2D tex, double x, double y)
        {
            if (tex == null) return;
            SetXf(T);
            Ci.DrawTexture(tex, new Vector2((float)x, (float)y), new Color(1, 1, 1, (float)GlobalAlpha));
        }

        // ---------------- Texto ----------------
        public double MeasureText(string s)
        {
            var (f, size) = Fonts.Parse(Font);
            return f.GetStringSize(s, HorizontalAlignment.Left, -1, size).X;
        }
        Vector2 TextPos(Font f, int size, string s, float x, float y)
        {
            float w = f.GetStringSize(s, HorizontalAlignment.Left, -1, size).X;
            if (TextAlign == "center") x -= w / 2; else if (TextAlign == "right" || TextAlign == "end") x -= w;
            float asc = f.GetAscent(size), desc = f.GetDescent(size), k = (asc + desc) > 0 ? size / (asc + desc) : 1;
            float ea = asc * k, ed = desc * k;
            switch (TextBaseline)
            {
                case "middle": y += (ea - ed) / 2; break;
                case "top": case "hanging": y += ea; break;
                case "bottom": case "ideographic": y -= ed; break;
            }
            return new Vector2(x, y);
        }
        public void FillText(string s, double x, double y)
        {
            if (string.IsNullOrEmpty(s)) return;
            var (f, size) = Fonts.Parse(Font);
            var c = WithAlpha(FillStyle.Col ?? Colors.Black);
            SetXf(T);
            Ci.DrawString(f, TextPos(f, size, s, (float)x, (float)y), s, HorizontalAlignment.Left, -1, size, c);
        }
        public void StrokeText(string s, double x, double y)
        {
            if (string.IsNullOrEmpty(s)) return;
            var (f, size) = Fonts.Parse(Font);
            var c = WithAlpha(StrokeStyle.Col ?? Colors.Black);
            SetXf(T);
            Ci.DrawStringOutline(f, TextPos(f, size, s, (float)x, (float)y), s, HorizontalAlignment.Left, -1, size, Math.Max(1, (int)Math.Round(LineWidth)), c);
        }

        // ---------------- Fábricas ----------------
        public RadialGradient CreateRadialGradient(double x0, double y0, double r0, double x1, double y1, double r1) => new RadialGradient((float)x0, (float)y0, (float)r0, (float)x1, (float)y1, (float)r1);
        public Pattern CreatePattern(Texture2D tex) => new Pattern { Tex = tex };
    }

    /// <summary>Lo que se puede poner en fillStyle/strokeStyle: un color CSS, un degradado o un patrón.</summary>
    public class Paint
    {
        public Color? Col; public RadialGradient Grad; public Pattern Pat;
        public static implicit operator Paint(string css) => css == null ? null : new Paint { Col = Css.Parse(css) };
        public static implicit operator Paint(Color c) => new Paint { Col = c };
        public static implicit operator Paint(RadialGradient g) => g == null ? null : new Paint { Grad = g };
        public static implicit operator Paint(Pattern p) => new Paint { Pat = p };
    }

    public class Pattern { public Texture2D Tex; }

    /// <summary>Degradado radial con la misma fórmula de dos círculos del canvas.</summary>
    public class RadialGradient
    {
        readonly float x0, y0, r0, x1, y1, r1;
        readonly List<(float o, Color c)> stops = new();
        static readonly Dictionary<string, (ImageTexture, Vector2)> cache = new();
        public RadialGradient(float x0, float y0, float r0, float x1, float y1, float r1) { this.x0 = x0; this.y0 = y0; this.r0 = r0; this.x1 = x1; this.y1 = y1; this.r1 = r1; }
        public void AddColorStop(double o, string css) { stops.Add(((float)o, Css.Parse(css))); stops.Sort((a, b) => a.o.CompareTo(b.o)); }

        Color At(float t)
        {
            if (stops.Count == 0) return new Color(0, 0, 0, 0);
            if (t <= stops[0].o) return stops[0].c;
            for (int i = 1; i < stops.Count; i++)
                if (t <= stops[i].o)
                {
                    var a = stops[i - 1]; var b = stops[i]; float k = (b.o - a.o) > 0 ? (t - a.o) / (b.o - a.o) : 1;
                    return a.c.Lerp(b.c, k);
                }
            return stops[stops.Count - 1].c;
        }

        /// <summary>Devuelve una textura del degradado que cubre la caja dada (en coordenadas del nodo).</summary>
        public (ImageTexture, Rect2) TextureFor(Transform2D T, Rect2 box)
        {
            Vector2 c0 = T * new Vector2(x0, y0), c1 = T * new Vector2(x1, y1);
            float sc = MathF.Sqrt(MathF.Abs(T.Determinant()));
            float R0 = r0 * sc, R1 = r1 * sc;
            // Todo relativo al centro exterior: así el temblor de pantalla no invalida la caché.
            Vector2 rel = box.Position - c1;
            string key = string.Format(CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2:F1},{3:F1},{4:F1}|{5:F0},{6:F0},{7:F0},{8:F0}|", c0.X - c1.X, c0.Y - c1.Y, R0, R1, 0, rel.X, rel.Y, box.Size.X, box.Size.Y);
            foreach (var s in stops) key += s.o.ToString(CultureInfo.InvariantCulture) + s.c.ToHtml() + ";";
            if (cache.TryGetValue(key, out var hit)) return (hit.Item1, new Rect2(c1 + hit.Item2, box.Size));
            float scale = MathF.Max(1, MathF.Max(box.Size.X, box.Size.Y) / 256f);
            int tw = Math.Max(2, (int)MathF.Ceiling(box.Size.X / scale)), th = Math.Max(2, (int)MathF.Ceiling(box.Size.Y / scale));
            var data = new byte[tw * th * 4];
            Vector2 cd = c1 - c0; float dr = R1 - R0;
            float a = cd.Dot(cd) - dr * dr;
            for (int py = 0; py < th; py++)
                for (int px = 0; px < tw; px++)
                {
                    Vector2 p = box.Position + new Vector2((px + 0.5f) / tw * box.Size.X, (py + 0.5f) / th * box.Size.Y);
                    Vector2 pd = p - c0;
                    float b = pd.Dot(cd) + R0 * dr, c = pd.Dot(pd) - R0 * R0;
                    float t; bool ok = true;
                    if (MathF.Abs(a) < 1e-6f) { if (MathF.Abs(b) < 1e-6f) ok = false; t = ok ? c / (2 * b) : 0; }
                    else
                    {
                        float disc = b * b - a * c;
                        if (disc < 0) { ok = false; t = 0; }
                        else
                        {
                            float sq = MathF.Sqrt(disc);
                            t = (b + sq) / a;
                            if (R0 + t * dr < 0) t = (b - sq) / a;
                            if (R0 + t * dr < 0) ok = false;
                        }
                    }
                    int o = (py * tw + px) * 4;
                    if (!ok) continue; // transparente
                    var col = At(Math.Clamp(t, 0, 1));
                    data[o] = (byte)Math.Round(col.R * 255); data[o + 1] = (byte)Math.Round(col.G * 255); data[o + 2] = (byte)Math.Round(col.B * 255); data[o + 3] = (byte)Math.Round(col.A * 255);
                }
            var img = Image.CreateFromData(tw, th, false, Image.Format.Rgba8, data);
            var tex = ImageTexture.CreateFromImage(img);
            if (cache.Count > 160) cache.Clear();
            cache[key] = (tex, rel);
            return (tex, box);
        }
    }

    /// <summary>Lectura de colores CSS: #rgb, #rrggbb, #rrggbbaa, rgb() y rgba().</summary>
    public static class Css
    {
        static readonly Dictionary<string, Color> cache = new();
        public static Color Parse(string s)
        {
            if (s == null) return Colors.Black;
            if (cache.TryGetValue(s, out var c)) return c;
            c = ParseRaw(s.Trim());
            if (cache.Count > 4000) cache.Clear();
            cache[s] = c;
            return c;
        }
        static float Hex(string s, int i, int n) => Convert.ToInt32(s.Substring(i, n), 16) / (n == 1 ? 15f : 255f);
        static Color ParseRaw(string s)
        {
            if (s.StartsWith("#"))
            {
                var h = s.Substring(1);
                if (h.Length == 3) return new Color(Hex(h, 0, 1), Hex(h, 1, 1), Hex(h, 2, 1));
                if (h.Length == 4) return new Color(Hex(h, 0, 1), Hex(h, 1, 1), Hex(h, 2, 1), Hex(h, 3, 1));
                if (h.Length == 6) return new Color(Hex(h, 0, 2), Hex(h, 2, 2), Hex(h, 4, 2));
                if (h.Length == 8) return new Color(Hex(h, 0, 2), Hex(h, 2, 2), Hex(h, 4, 2), Hex(h, 6, 2));
            }
            if (s.StartsWith("rgb"))
            {
                int a = s.IndexOf('('), b = s.LastIndexOf(')');
                var parts = s.Substring(a + 1, b - a - 1).Split(',');
                float r = float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture) / 255f;
                float g = float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture) / 255f;
                float bl = float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture) / 255f;
                float al = parts.Length > 3 ? float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture) : 1f;
                return new Color(r, g, bl, Math.Clamp(al, 0, 1));
            }
            return Colors.Magenta;
        }
    }

}
