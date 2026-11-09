using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventurasNivel3
{
    /// <summary>Opciones de drawCreature (el objeto "o" de game.js).</summary>
    public class CO { public int seed; public bool moving, still, talking, big; public double[] look; }
    /// <summary>Opciones de drawCat.</summary>
    public class CatO { public double s = 1, face = 1; public bool moving, sit, happy, sad, meow, purr; public int seed; }

    public partial class Game
    {
        // ---------------- Dibujo: utilidades ----------------
        void circle(double x, double y, double r, Paint fill = null, string stroke = null, double lw = 0)
        {
            ctx.BeginPath(); ctx.Arc(x, y, Math.Max(0.1, r), 0, Math.PI * 2);
            if (fill != null) { ctx.FillStyle = fill; ctx.Fill(); }
            if (stroke != null) { ctx.StrokeStyle = stroke; ctx.LineWidth = lw != 0 ? lw : 2; ctx.Stroke(); }
        }
        void ellipse(double x, double y, double rx, double ry, Paint fill = null, string stroke = null, double lw = 0)
        {
            ctx.BeginPath(); ctx.Ellipse(x, y, Math.Max(0.1, rx), Math.Max(0.1, ry), 0, 0, Math.PI * 2);
            if (fill != null) { ctx.FillStyle = fill; ctx.Fill(); }
            if (stroke != null) { ctx.StrokeStyle = stroke; ctx.LineWidth = lw != 0 ? lw : 2; ctx.Stroke(); }
        }
        void rr(double x, double y, double w, double h, double r, Paint fill = null, string stroke = null, double lw = 0) => rr(x, y, w, h, new[] { r }, fill, stroke, lw);
        void rr(double x, double y, double w, double h, double[] r, Paint fill = null, string stroke = null, double lw = 0)
        {
            ctx.BeginPath(); ctx.RoundRect(x, y, w, h, r);
            if (fill != null) { ctx.FillStyle = fill; ctx.Fill(); }
            if (stroke != null) { ctx.StrokeStyle = stroke; ctx.LineWidth = lw != 0 ? lw : 2; ctx.Stroke(); }
        }
        void txt(string s, double x, double y, string font, string color, string align = null, string bas = null)
        {
            ctx.Font = font; ctx.FillStyle = color; ctx.TextAlign = align ?? "center"; ctx.TextBaseline = bas ?? "middle"; ctx.FillText(s, x, y);
        }
        void label(string s, double x, double y)
        {
            ctx.Font = "700 12px " + FONT; ctx.TextAlign = "center"; ctx.TextBaseline = "middle"; ctx.LineJoin = "round"; ctx.LineWidth = 4; ctx.StrokeStyle = "rgba(42,34,56,.85)"; ctx.StrokeText(s, x, y); ctx.FillStyle = COL.cream; ctx.FillText(s, x, y);
        }
        void tag(string s, double x, double y, string bg, string fg = null)
        {
            ctx.Font = "700 12px " + FONT; double w = ctx.MeasureText(s) + 16; rr(x - w / 2, y - 10, w, 20, 10, bg, OUT, 2); txt(s, x, y + 0.5, "700 12px " + FONT, fg ?? COL.white);
        }
        List<string> wrapLines(string s, string font, double max)
        {
            ctx.Font = font; var words = s.Split(' '); var lines = new List<string>(); string cur = "";
            foreach (var w in words) { string t = cur != "" ? cur + " " + w : w; if (ctx.MeasureText(t) > max && cur != "") { lines.Add(cur); cur = w; } else cur = t; }
            if (cur != "") lines.Add(cur);
            return lines;
        }
        void drawEye(double x, double y, double s)
        {
            ctx.BeginPath(); ctx.MoveTo(x - s, y); ctx.QuadraticCurveTo(x, y - s * 0.95, x + s, y); ctx.QuadraticCurveTo(x, y + s * 0.95, x - s, y); ctx.ClosePath();
            ctx.FillStyle = COL.white; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = Math.Max(1.5, s * 0.12); ctx.Stroke();
            circle(x, y, s * 0.42, COL.adiv); circle(x, y, s * 0.2, OUT); circle(x - s * 0.15, y - s * 0.15, s * 0.09, COL.white);
        }
        void drawArrow(double cx, double cy, string d, string color, double s)
        {
            double ang = d switch { "R" => 0, "D" => Math.PI / 2, "L" => Math.PI, _ => -Math.PI / 2 };
            ctx.Save(); ctx.Translate(cx, cy); ctx.Rotate(ang); ctx.FillStyle = color; ctx.BeginPath();
            ctx.MoveTo(-s * 0.5, -s * 0.15); ctx.LineTo(s * 0.08, -s * 0.15); ctx.LineTo(s * 0.08, -s * 0.4); ctx.LineTo(s * 0.55, 0); ctx.LineTo(s * 0.08, s * 0.4); ctx.LineTo(s * 0.08, s * 0.15); ctx.LineTo(-s * 0.5, s * 0.15); ctx.ClosePath();
            ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = 2; ctx.Stroke(); ctx.Restore();
        }
        void bubble(string s, double x, double y, string col = null)
        {
            var lines = wrapLines(s, "700 13px " + FONT, 280); ctx.Font = "700 13px " + FONT;
            double w = Math.Min(300, lines.Max(l => ctx.MeasureText(l)) + 22);
            double bx = Math.Max(8, Math.Min(W - w - 8, x - w / 2)); double h = lines.Count * 17 + 12;
            rr(bx, y - h, w, h, 9, COL.cream, col ?? OUT, 2.5);
            ctx.BeginPath(); ctx.MoveTo(x - 7, y - 1); ctx.LineTo(x + 7, y - 1); ctx.LineTo(x, y + 8); ctx.ClosePath(); ctx.FillStyle = col ?? OUT; ctx.Fill();
            for (int i = 0; i < lines.Count; i++) txt(lines[i], bx + w / 2, y - h + 15 + i * 17, "700 13px " + FONT, OUT);
        }

        // ---------------- Personajes ----------------
        void drawCreature(string k, double x, double y, double r, CO o = null)
        {
            o ??= new CO(); var c = CK.ContainsKey(k) ? CK[k] : CK["c1"]; double t = S.t + o.seed * 41;
            bool isA = k == "adivino";
            double bob = o.still ? 0 : (o.moving ? Math.Abs(Math.Sin(t * 0.32)) * r * 0.16 : Math.Sin(t * 0.07) * r * 0.05);
            double rx = r * (isA ? 0.9 : 1), ry = r * (isA ? 1.1 : 0.94);
            ellipse(x, y + ry * 0.95, rx * 0.95, r * 0.28, "rgba(30,20,40,.28)");
            double by = y - bob;
            double fs = o.moving ? Math.Sin(t * 0.32) * r * 0.1 : 0;
            ellipse(x - rx * 0.45, y + ry * 0.8 - fs, r * 0.26, r * 0.17, c.dark, OUT, Math.Max(1.2, r * 0.07));
            ellipse(x + rx * 0.45, y + ry * 0.8 + fs, r * 0.26, r * 0.17, c.dark, OUT, Math.Max(1.2, r * 0.07));
            ctx.BeginPath(); ctx.Ellipse(x, by, rx, ry, 0, 0, Math.PI * 2); ctx.FillStyle = c.body; ctx.Fill();
            ctx.Save(); ctx.Clip();
            ctx.FillStyle = "rgba(0,0,0,.12)"; ctx.BeginPath(); ctx.Ellipse(x + rx * 0.32, by + ry * 0.62, rx * 0.95, ry * 0.55, 0, 0, Math.PI * 2); ctx.Fill();
            bodyAcc(k, x, by, rx, ry, r);
            ctx.Restore();
            ctx.BeginPath(); ctx.Ellipse(x, by, rx, ry, 0, 0, Math.PI * 2); ctx.LineWidth = Math.Max(2, r * 0.11); ctx.StrokeStyle = OUT; ctx.Stroke();
            ellipse(x - rx * 0.42, by - ry * 0.5, rx * 0.2, ry * 0.11, "rgba(255,255,255,.5)");
            face(k, x, by, rx, ry, r, o, t);
            headAcc(k, x, by, rx, ry, r, o);
        }
        void face(string k, double x, double by, double rx, double ry, double r, CO o, double t)
        {
            var lk = o.look ?? new double[] { 0, 0 }; bool blink = ((t + o.seed * 13) % 230) < 5;
            if (k == "adivino")
            {
                double ey0 = by - ry * 0.06;
                if (blink) { ctx.BeginPath(); ctx.MoveTo(x - r * 0.5, ey0); ctx.LineTo(x + r * 0.5, ey0); ctx.StrokeStyle = OUT; ctx.LineWidth = r * 0.1; ctx.Stroke(); } else { drawEye(x + lk[0] * r * 0.06, ey0, r * 0.52); }
                if (o.talking && Math.Sin(t * 0.7) > 0) ellipse(x, by + ry * 0.55, r * 0.12, r * 0.08, OUT);
                return;
            }
            double ex = rx * 0.36, ey = by - ry * 0.1, ew = r * 0.19, eh = r * 0.24;
            foreach (int s in new[] { -1, 1 })
            {
                double cx = x + s * ex;
                if (blink) { ctx.BeginPath(); ctx.MoveTo(cx - ew, ey); ctx.LineTo(cx + ew, ey); ctx.StrokeStyle = OUT; ctx.LineWidth = Math.Max(1.5, r * 0.08); ctx.Stroke(); }
                else { ellipse(cx, ey, ew, eh, COL.white, OUT, Math.Max(1.2, r * 0.06)); circle(cx + lk[0] * ew * 0.35, ey + lk[1] * eh * 0.3 + eh * 0.12, ew * 0.62, OUT); circle(cx + lk[0] * ew * 0.35 - ew * 0.22, ey + lk[1] * eh * 0.3 - eh * 0.06, ew * 0.24, COL.white); }
            }
            ellipse(x - rx * 0.62, by + ry * 0.22, r * 0.15, r * 0.08, "rgba(255,105,135,.45)"); ellipse(x + rx * 0.62, by + ry * 0.22, r * 0.15, r * 0.08, "rgba(255,105,135,.45)");
            if (o.talking && Math.Sin(t * 0.7) > 0) ellipse(x, by + ry * 0.24, r * 0.1, r * 0.08, OUT);
            else { ctx.BeginPath(); ctx.Arc(x, by + ry * 0.12, r * 0.12, 0.2 * Math.PI, 0.8 * Math.PI); ctx.StrokeStyle = OUT; ctx.LineWidth = Math.Max(1.5, r * 0.07); ctx.Stroke(); }
            if (k == "ernesto")
            {
                ctx.StrokeStyle = OUT; ctx.LineWidth = Math.Max(1.2, r * 0.07);
                foreach (int s in new[] { -1, 1 }) { ctx.BeginPath(); ctx.Arc(x + s * ex, ey, ew * 1.3, 0, Math.PI * 2); ctx.Stroke(); }
                ctx.BeginPath(); ctx.MoveTo(x - ex + ew * 1.3, ey); ctx.LineTo(x + ex - ew * 1.3, ey); ctx.Stroke();
            }
        }
        void bodyAcc(string k, double x, double by, double rx, double ry, double r)
        {
            if (k == "player") { ctx.FillStyle = "#2fa59c"; ctx.FillRect(x - rx, by + ry * 0.34, rx * 2, ry * 0.24); ctx.FillStyle = "rgba(255,255,255,.25)"; ctx.FillRect(x - rx, by + ry * 0.34, rx * 2, ry * 0.06); }
            else if (k == "rosa")
            {
                ctx.FillStyle = "#a57ad8"; ctx.BeginPath(); ctx.Ellipse(x, by - ry * 0.92, rx * 1.15, ry * 0.62, 0, 0, Math.PI * 2); ctx.Fill();
                foreach (var ab in new[] { new[] { -0.5, -0.55 }, new[] { 0, -0.68 }, new[] { 0.45, -0.52 }, new[] { -0.2, -0.42 }, new[] { 0.25, -0.75 } }) circle(x + ab[0] * rx, by + ab[1] * ry, r * 0.06, "#f6efe0");
            }
            else if (k == "mario") { rr(x - rx * 0.55, by + ry * 0.12, rx * 1.1, ry * 1.0, r * 0.14, "#4f9a52"); ctx.FillStyle = "rgba(255,255,255,.3)"; ctx.FillRect(x - rx * 0.25, by + ry * 0.42, rx * 0.5, ry * 0.05); }
            else if (k == "ernesto")
            {
                ctx.FillStyle = "#f6efe0"; ctx.BeginPath(); ctx.MoveTo(x - rx * 0.3, by + ry * 0.38); ctx.LineTo(x, by + ry * 0.62); ctx.LineTo(x + rx * 0.3, by + ry * 0.38); ctx.ClosePath(); ctx.Fill();
                ctx.FillStyle = "#d2483a"; ctx.BeginPath(); ctx.MoveTo(x - rx * 0.08, by + ry * 0.42); ctx.LineTo(x + rx * 0.08, by + ry * 0.42); ctx.LineTo(x, by + ry * 0.9); ctx.ClosePath(); ctx.Fill();
            }
            else if (k == "adivino") { foreach (var ab in new[] { new[] { -0.5, 0.5 }, new[] { 0.3, 0.65 }, new[] { 0.6, 0.25 }, new[] { -0.15, 0.85 } }) circle(x + ab[0] * rx, by + ab[1] * ry, r * 0.06, "#ffd36b"); }
        }
        void headAcc(string k, double x, double by, double rx, double ry, double r, CO o)
        {
            double lw = Math.Max(1.5, r * 0.09);
            if (k == "player") { ctx.BeginPath(); ctx.Ellipse(x - rx * 0.12, by - ry * 0.8, rx * 0.62, ry * 0.26, -0.25, 0, Math.PI * 2); ctx.FillStyle = "#2f5f7a"; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = lw; ctx.Stroke(); circle(x + rx * 0.05, by - ry * 1.04, r * 0.08, "#2f5f7a", OUT, lw * 0.7); }
            else if (k == "rosa")
            {
                ctx.FillStyle = "#a57ad8"; ctx.StrokeStyle = OUT; ctx.LineWidth = lw * 0.8;
                foreach (var v in new[] { new[] { 0.72, -0.38, 0.5 }, new[] { 0.9, -0.18, -0.4 } }) { ctx.BeginPath(); ctx.Ellipse(x + v[0] * rx, by + v[1] * ry, r * 0.17, r * 0.1, v[2], 0, Math.PI * 2); ctx.Fill(); ctx.Stroke(); }
            }
            else if (k == "mario") { ellipse(x, by - ry * 0.78, rx * 1.08, ry * 0.24, "#e6c27a", OUT, lw); rr(x - rx * 0.48, by - ry * 1.22, rx * 0.96, ry * 0.46, new[] { r * 0.2, r * 0.2, 2, 2 }, "#e6c27a", OUT, lw); ctx.FillStyle = "#c8553d"; ctx.FillRect(x - rx * 0.47, by - ry * 0.92, rx * 0.94, ry * 0.11); }
            else if (k == "tito") { ctx.BeginPath(); ctx.Ellipse(x, by - ry * 0.72, rx * 0.78, ry * 0.4, 0, Math.PI, 0); ctx.ClosePath(); ctx.FillStyle = "#e0513f"; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = lw; ctx.Stroke(); rr(x - rx * 1.1, by - ry * 0.82, rx * 0.45, ry * 0.16, r * 0.06, "#e0513f", OUT, lw * 0.8); circle(x, by - ry * 1.1, r * 0.07, "#f6efe0", OUT, lw * 0.6); }
            else if (k == "taq") { ellipse(x + rx * 0.15, by - ry * 0.7, rx * 0.85, ry * 0.2, "#2f6f74", OUT, lw); ctx.BeginPath(); ctx.Ellipse(x, by - ry * 0.78, rx * 0.7, ry * 0.3, 0, Math.PI, 0); ctx.ClosePath(); ctx.FillStyle = "#3d8f94"; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = lw; ctx.Stroke(); }
            else if (k == "ernesto") { ctx.StrokeStyle = OUT; ctx.LineWidth = lw; ctx.BeginPath(); ctx.MoveTo(x - r * 0.1, by - ry); ctx.QuadraticCurveTo(x + r * 0.1, by - ry * 1.35, x + r * 0.25, by - ry * 1.05); ctx.Stroke(); }
            else if (k == "adivino")
            {
                ctx.BeginPath(); ctx.MoveTo(x - rx * 0.95, by - ry * 0.6); ctx.QuadraticCurveTo(x - rx * 0.1, by - ry * 1.1, x + rx * 0.45, by - ry * 2.15); ctx.QuadraticCurveTo(x + rx * 0.35, by - ry * 1.2, x + rx * 0.95, by - ry * 0.6); ctx.ClosePath();
                ctx.FillStyle = COL.adivDark; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = lw; ctx.Stroke();
                ellipse(x, by - ry * 0.62, rx * 1.05, ry * 0.18, "#5d3aa6", OUT, lw);
                star(x + rx * 0.05, by - ry * 1.15, r * 0.14, "#ffd36b"); star(x + rx * 0.32, by - ry * 1.6, r * 0.09, "#ffd36b");
                if (!o.big)
                {
                    double bx = x + rx * 1.3, bby = by + ry * 0.35, br = r * 0.4; var g = ctx.CreateRadialGradient(bx - br * 0.3, bby - br * 0.3, br * 0.1, bx, bby, br); g.AddColorStop(0, "#fbf7ff"); g.AddColorStop(1, "#b59ce4");
                    circle(bx, bby, br * 1.5, "rgba(190,160,255,.18)"); circle(bx, bby, br, g, OUT, lw * 0.8); rr(bx - br * 0.6, bby + br * 0.8, br * 1.2, br * 0.45, 2, "#5d3aa6", OUT, lw * 0.7);
                }
            }
        }
        void star(double x, double y, double s, string col)
        {
            ctx.BeginPath();
            for (int i = 0; i < 10; i++) { double a = -Math.PI / 2 + i * Math.PI / 5, rad = (i % 2 == 1) ? s * 0.45 : s; ctx.LineTo(x + Math.Cos(a) * rad, y + Math.Sin(a) * rad); }
            ctx.ClosePath(); ctx.FillStyle = col; ctx.Fill();
        }
        void drawCart(double x, double y, double s)
        {
            ellipse(x, y + s * 0.75, s * 0.9, s * 0.22, "rgba(30,20,40,.25)");
            ctx.StrokeStyle = OUT; ctx.LineWidth = 2.5; ctx.BeginPath(); ctx.MoveTo(x - s * 0.7, y - s * 0.1); ctx.LineTo(x - s * 1.05, y - s * 0.55); ctx.Stroke();
            rr(x - s * 0.7, y - s * 0.35, s * 1.4, s * 0.8, s * 0.15, "#b07a45", OUT, 2.5);
            ctx.StrokeStyle = "rgba(60,35,15,.4)"; ctx.LineWidth = 1.5;
            for (int i = 1; i < 4; i++) { ctx.BeginPath(); ctx.MoveTo(x - s * 0.7 + i * s * 0.35, y - s * 0.33); ctx.LineTo(x - s * 0.7 + i * s * 0.35, y + s * 0.43); ctx.Stroke(); }
            foreach (var (cc, a) in new[] { ("#e0513f", -0.35), ("#f39c33", 0.0), ("#7fbf4d", 0.35) }) circle(x + a * s, y - s * 0.42, s * 0.22, cc, OUT, 1.5);
            circle(x - s * 0.45, y + s * 0.5, s * 0.2, "#4a4458", OUT, 2); circle(x + s * 0.45, y + s * 0.5, s * 0.2, "#4a4458", OUT, 2);
        }
        /// <summary>Boceto: gato atigrado gris, dibujado mirando a la derecha (se voltea con face = -1).</summary>
        void drawCat(double x, double y, CatO o = null)
        {
            o ??= new CatO(); double s = o.s, f = o.face, t = S.t + o.seed * 17;
            bool sit = o.sit && !o.moving; double step = o.moving ? Math.Sin(t * 0.5) : 0;
            double bob = o.moving ? Math.Abs(step) * 1.3 * s : (o.purr ? Math.Sin(t * 0.7) * 0.7 * s : 0), lw = Math.Max(1.6, 2.3 * s);
            ellipse(x, y + 8 * s, (sit ? 11 : 15) * s, 4.2 * s, "rgba(30,20,40,.28)");
            ctx.Save(); ctx.Translate(x, y - bob); if (f < 0) ctx.Scale(-1, 1);
            ctx.LineJoin = "round"; ctx.LineCap = "round";
            double sw = Math.Sin(t * (o.happy || o.purr ? 0.16 : 0.07));
            ctx.BeginPath();
            if (sit) { ctx.MoveTo(-6 * s, 5 * s); ctx.QuadraticCurveTo(-17 * s, 6 * s, -14 * s + sw * 2.5 * s, -8 * s); }
            else { ctx.MoveTo(-11 * s, -1 * s); ctx.QuadraticCurveTo(-21 * s, -3 * s, -19 * s + sw * 4 * s, -14 * s); }
            ctx.StrokeStyle = OUT; ctx.LineWidth = 6.2 * s; ctx.Stroke(); ctx.StrokeStyle = CAT.body; ctx.LineWidth = 3.4 * s; ctx.Stroke();
            if (!sit) foreach (var (lx, ph) in new[] { (-8.0, step), (-4.0, -step), (5.0, -step), (9.0, step) }) ellipse(lx * s, (6.2 - Math.Max(0, ph) * 1.6) * s, 2.5 * s, 3 * s, CAT.dark, OUT, lw * 0.6);
            if (sit) { ellipse(0, 0, 8.6 * s, 9.2 * s, CAT.body); ellipse(3 * s, 2 * s, 4.2 * s, 5.8 * s, CAT.light); }
            else { ellipse(-1 * s, 0, 12.5 * s, 7.6 * s, CAT.body); ellipse(2 * s, 3.2 * s, 7.5 * s, 3 * s, CAT.light); }
            ctx.StrokeStyle = CAT.dark; ctx.LineWidth = 1.8 * s;
            foreach (var ab in sit ? new[] { new[] { -6.0, -4 }, new[] { -7.0, 0 }, new[] { -6.5, 4 } } : new[] { new[] { -8.0, -7 }, new[] { -4.0, -7.4 }, new[] { 0.0, -7.4 } })
            { double a = ab[0], b = ab[1]; ctx.BeginPath(); ctx.MoveTo(a * s, b * s); ctx.LineTo((a + (sit ? 2.6 : 0.8)) * s, (b + (sit ? 0.4 : 3)) * s); ctx.Stroke(); }
            if (sit) { ellipse(0, 0, 8.6 * s, 9.2 * s, null, OUT, lw); ellipse(3.5 * s, 8 * s, 2.8 * s, 2 * s, CAT.light, OUT, lw * 0.6); ellipse(7.5 * s, 8 * s, 2.8 * s, 2 * s, CAT.light, OUT, lw * 0.6); }
            else ellipse(-1 * s, 0, 12.5 * s, 7.6 * s, null, OUT, lw);
            double hx = sit ? 3.5 * s : 10 * s, hy = sit ? -11 * s : -6.5 * s, eh = o.sad ? 0.55 : 1;
            foreach (var tri in new[] { new[] { new[] { -5.6, -3 }, new[] { -6, -3 - 8.5 * eh }, new[] { -0.8, -6.2 } }, new[] { new[] { 1.4, -6.6 }, new[] { 6.2, -3 - 8.5 * eh }, new[] { 6.4, -2.4 } } })
            {
                ctx.BeginPath();
                for (int j = 0; j < tri.Length; j++) { double px = hx + tri[j][0] * s, py = hy + tri[j][1] * s; if (j > 0) ctx.LineTo(px, py); else ctx.MoveTo(px, py); }
                ctx.ClosePath(); ctx.FillStyle = CAT.body; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = lw * 0.8; ctx.Stroke();
            }
            circle(hx, hy, 7.4 * s, CAT.body, OUT, lw);
            ctx.FillStyle = CAT.ear;
            foreach (var ab in new[] { new[] { -4.2, -5.4 }, new[] { 3.8, -5.6 } }) { ctx.BeginPath(); ctx.Arc(hx + ab[0] * s, hy + ab[1] * s - 1.4 * s * eh, 1.4 * s, 0, Math.PI * 2); ctx.Fill(); }
            ellipse(hx + 1.8 * s, hy + 3.2 * s, 4.2 * s, 2.8 * s, CAT.light);
            ctx.StrokeStyle = CAT.dark; ctx.LineWidth = 1.4 * s; ctx.BeginPath(); ctx.MoveTo(hx - 1.5 * s, hy - 6.8 * s); ctx.LineTo(hx - 1 * s, hy - 4.2 * s); ctx.MoveTo(hx + 1.5 * s, hy - 7 * s); ctx.LineTo(hx + 1.6 * s, hy - 4.4 * s); ctx.Stroke();
            bool blink = ((t + 31) % 210) < 6;
            foreach (double a in new[] { -1.8, 3.8 })
            {
                double ex = hx + a * s, ey = hy - 0.6 * s;
                if (o.happy || blink) { ctx.BeginPath(); ctx.Arc(ex, ey + 0.8 * s, 1.6 * s, Math.PI * 1.1, Math.PI * 1.9); ctx.StrokeStyle = OUT; ctx.LineWidth = 1.3 * s; ctx.Stroke(); }
                else if (o.sad) { ctx.BeginPath(); ctx.MoveTo(ex - 1.6 * s, ey + 0.4 * s); ctx.LineTo(ex + 1.6 * s, ey + 0.9 * s); ctx.StrokeStyle = OUT; ctx.LineWidth = 1.3 * s; ctx.Stroke(); }
                else { ellipse(ex, ey, 1.7 * s, 2.1 * s, CAT.eye, OUT, Math.Max(0.8, 0.9 * s)); ellipse(ex + 0.3 * s, ey, 0.55 * s, 1.5 * s, OUT); circle(ex - 0.4 * s, ey - 0.8 * s, 0.45 * s, COL.white); }
            }
            ctx.BeginPath(); ctx.MoveTo(hx + 1.2 * s, hy + 1.9 * s); ctx.LineTo(hx + 3 * s, hy + 1.9 * s); ctx.LineTo(hx + 2.1 * s, hy + 3 * s); ctx.ClosePath(); ctx.FillStyle = CAT.nose; ctx.Fill();
            if (o.meow) ellipse(hx + 2.1 * s, hy + 4.6 * s, 1.2 * s, 1.1 * s, OUT);
            ctx.StrokeStyle = "rgba(42,34,56,.55)"; ctx.LineWidth = Math.Max(0.8, 0.7 * s);
            foreach (int kk in new[] { -1, 0, 1 }) { ctx.BeginPath(); ctx.MoveTo(hx + 4.6 * s, hy + (2.8 + kk * 0.9) * s); ctx.LineTo(hx + 9.2 * s, hy + (2.4 + kk * 1.8) * s); ctx.Stroke(); }
            ctx.Restore();
        }
        void drawPaw(double x, double y, double r, string col)
        {
            ellipse(x, y + r * 0.35, r * 0.62, r * 0.5, col);
            foreach (var ab in new[] { new[] { -0.62, -0.32 }, new[] { -0.22, -0.7 }, new[] { 0.22, -0.7 }, new[] { 0.62, -0.32 } }) circle(x + ab[0] * r, y + ab[1] * r, r * 0.24, col);
        }

        // ---------------- Escenografía ----------------
        Texture2D GRASS, COBBLE, BGZ1;
        Texture2D grassCanvas() => GRASS;
        Texture2D cobble() => COBBLE;
        void drawGrassOffscreen()
        {
            ctx.FillStyle = "#6fae52"; ctx.FillRect(0, 0, W, H); var Rr = rng(7);
            for (int i = 0; i < 26; i++) ellipse(Rr() * W, Rr() * H, 40 + Rr() * 70, 20 + Rr() * 30, "rgba(70,130,50,.22)");
            for (int i = 0; i < 950; i++) { double x = Rr() * W, y = Rr() * H; ctx.StrokeStyle = Rr() < 0.5 ? "#5c9a44" : "#88c466"; ctx.LineWidth = 2; ctx.BeginPath(); ctx.MoveTo(x - 3, y + 3); ctx.LineTo(x, y - 2); ctx.LineTo(x + 3, y + 3); ctx.Stroke(); }
            for (int i = 0; i < 70; i++) { double x = Rr() * W, y = Rr() * H; string col = new[] { "#fff6e0", "#ffd166", "#ff9ec4", "#b8e1ff" }[(int)Math.Floor(Rr() * 4)]; for (int p = 0; p < 4; p++) circle(x + Math.Cos(p * Math.PI / 2) * 3, y + Math.Sin(p * Math.PI / 2) * 3, 2.4, col); circle(x, y, 1.8, "#f39c33"); }
        }
        void drawCobbleOffscreen()
        {
            ctx.FillStyle = "#a8957a"; ctx.FillRect(0, 0, 64, 64); var Rr = rng(3);
            for (int row = 0; row < 4; row++)
            {
                int off = row % 2 == 1 ? 16 : 0;
                for (int i = -1; i < 3; i++) { double x = i * 32 + off + 2, y = row * 16 + 2; rr(x, y, 28, 12, 5, new[] { "#d6c6a7", "#cbb998", "#dfcfb1", "#c4b292" }[(int)Math.Floor(Rr() * 4)], "rgba(80,60,40,.35)", 1.5); ctx.FillStyle = "rgba(255,255,255,.18)"; ctx.FillRect(x + 4, y + 2, 14, 2); }
            }
        }
        void cobbleFill(double x, double y, double w, double h) { ctx.Save(); ctx.FillStyle = ctx.CreatePattern(cobble()); ctx.FillRect(x, y, w, h); ctx.Restore(); }
        void curb(double x, double y, double w, double h) { rr(x, y, w, h, 2, "#ddd3c0"); ctx.FillStyle = "rgba(42,34,56,.3)"; ctx.FillRect(x, y + h - 2, w, 2); }
        void tree(double x, double y, double s) { ellipse(x + 4, y + s * 0.85, s * 0.95, s * 0.3, "rgba(20,30,20,.28)"); rr(x - s * 0.16, y + s * 0.1, s * 0.32, s * 0.75, 3, "#7a4b2e", OUT, 2.5); circle(x, y - s * 0.2, s * 0.85, "#2f6b3a", OUT, 3); circle(x - s * 0.32, y - s * 0.42, s * 0.48, "#3f8a46"); circle(x + s * 0.28, y - s * 0.5, s * 0.38, "#4f9d50"); circle(x - s * 0.12, y - s * 0.72, s * 0.2, "#6cbf62"); }
        void bush(double x, double y, double s) { ellipse(x + 2, y + s * 0.6, s * 1.2, s * 0.35, "rgba(20,30,20,.25)"); circle(x - s * 0.6, y, s * 0.7, "#3a7d42", OUT, 2); circle(x + s * 0.6, y, s * 0.7, "#3a7d42", OUT, 2); circle(x, y - s * 0.3, s * 0.85, "#468f4c", OUT, 2); circle(x - s * 0.2, y - s * 0.6, s * 0.25, "#6cbf62"); }
        void hedgeRow(double x, double y, double w, double h) { rr(x, y, w, h, 8, "#3a7d42", OUT, 2.5); for (double i = x + 10; i < x + w - 6; i += 18) circle(i, y + 4, 9, "#468f4c"); ctx.StrokeStyle = OUT; ctx.LineWidth = 2.5; ctx.BeginPath(); ctx.MoveTo(x + 4, y + h); ctx.LineTo(x + w - 4, y + h); ctx.Stroke(); }
        void nookPatch(double x, double y, double w, double h) { rr(x, y, w, h, 10, "#c49a66"); rr(x + 4, y + 4, w - 8, h - 8, 8, "#d1aa76"); var Rr = rng((int)x); for (int i = 0; i < 6; i++) circle(x + 8 + Rr() * (w - 16), y + 8 + Rr() * (h - 16), 2, "#a87e4e"); }
        void house3q(double x, double y, double w, double h, string roof, string wall = null, bool chimney = false)
        {
            double rh = Math.Floor(h * 0.5 + 0.5), wy = y + rh - 4, wh = h - rh + 4;
            ellipse(x + w / 2, y + h, w * 0.52, 8, "rgba(20,15,30,.22)");
            rr(x, wy, w, wh, 4, wall ?? "#ecdcbc", OUT, 3);
            ctx.StrokeStyle = "rgba(120,85,50,.35)"; ctx.LineWidth = 2; ctx.BeginPath(); ctx.MoveTo(x + 3, wy + 12); ctx.LineTo(x + w - 3, wy + 12); ctx.Stroke();
            if (w > 110)
            {
                foreach (double wx in new[] { x + 16, x + w - 44 })
                {
                    rr(wx, wy + 20, 28, 22, 3, "#8cc3dd", OUT, 2); ctx.StrokeStyle = OUT; ctx.LineWidth = 1.5; ctx.BeginPath(); ctx.MoveTo(wx + 14, wy + 20); ctx.LineTo(wx + 14, wy + 42); ctx.MoveTo(wx, wy + 31); ctx.LineTo(wx + 28, wy + 31); ctx.Stroke(); rr(wx - 3, wy + 42, 34, 5, 2, "#9b6a43", OUT, 1.5);
                }
            }
            rr(x + w / 2 - 13, y + h - 34, 26, 34, new double[] { 11, 11, 2, 2 }, "#8a5a3b", OUT, 2.5); circle(x + w / 2 + 7, y + h - 17, 2, "#f3c24f");
            ctx.BeginPath(); ctx.MoveTo(x - 8, y + rh); ctx.LineTo(x + 8, y); ctx.LineTo(x + w - 8, y); ctx.LineTo(x + w + 8, y + rh); ctx.ClosePath(); ctx.FillStyle = roof; ctx.Fill();
            ctx.Save(); ctx.Clip(); ctx.StrokeStyle = "rgba(0,0,0,.17)"; ctx.LineWidth = 2;
            for (double ly = y + 9; ly < y + rh; ly += 9) { ctx.BeginPath(); ctx.MoveTo(x - 10, ly); ctx.LineTo(x + w + 10, ly); ctx.Stroke(); }
            ctx.FillStyle = "rgba(255,255,255,.16)"; ctx.FillRect(x - 10, y, w + 20, 6); ctx.Restore();
            ctx.BeginPath(); ctx.MoveTo(x - 8, y + rh); ctx.LineTo(x + 8, y); ctx.LineTo(x + w - 8, y); ctx.LineTo(x + w + 8, y + rh); ctx.ClosePath(); ctx.StrokeStyle = OUT; ctx.LineWidth = 3; ctx.Stroke();
            ctx.FillStyle = "rgba(0,0,0,.2)"; ctx.FillRect(x - 4, y + rh - 1, w + 8, 5);
            if (chimney) { rr(x + w - 36, y - 10, 14, 22, 2, "#9b6a43", OUT, 2); }
        }
        void marketStall(double x, double y, double w, double h)
        {
            ellipse(x + w / 2, y + h, w * 0.5, 8, "rgba(20,15,30,.22)");
            rr(x, y + 24, w, h - 24, 6, "#dcc69c", OUT, 3);
            rr(x + 8, y + h - 44, w - 16, 42, 5, "#9b6a43", OUT, 2.5);
            var fruits = new[] { "#e0513f", "#f39c33", "#7fbf4d", "#f3c24f", "#c94f8a" };
            for (int i = 0; i < 5; i++) { double cx = x + 24 + i * ((w - 48) / 4); rr(cx - 15, y + h - 58, 30, 16, 3, "#b98d5c", OUT, 2); for (int j = 0; j < 3; j++) circle(cx - 8 + j * 8, y + h - 61, 5, fruits[(i + j) % 5], OUT, 1.2); }
            int n = 8; double sw = (w + 12) / n;
            for (int i = 0; i < n; i++) { ctx.FillStyle = i % 2 == 1 ? "#f6efe0" : "#d8483a"; ctx.FillRect(x - 6 + i * sw, y + 6, sw + 0.5, 30); ctx.BeginPath(); ctx.Arc(x - 6 + i * sw + sw / 2, y + 36, sw / 2, 0, Math.PI); ctx.Fill(); }
            ctx.StrokeStyle = OUT; ctx.LineWidth = 2.5; ctx.StrokeRect(x - 6, y + 6, w + 12, 30);
            rr(x + w / 2 - 54, y - 16, 108, 26, 6, "#f6efe0", OUT, 2.5);
        }

        // ---------------- Mundo ----------------
        class Nook { public double x, y, w, h; }
        static readonly Nook NOOK1 = new() { x = 414, y = 140, w = 62, h = 66 }, NOOK2 = new() { x = 684, y = 140, w = 62, h = 66 }, NOOK3 = new() { x = 590, y = 414, w = 62, h = 62 };
        Texture2D bgZ1() => BGZ1;
        void drawBgZ1Offscreen()
        {
            ctx.DrawImage(grassCanvas(), 0, 0);
            cobbleFill(0, 214, W, 192); curb(0, 206, W, 10); curb(0, 404, W, 10);
            house3q(18, 30, 160, 176, "#c8553d", null, true);
            tree(208, 150, 24); bush(208, 198, 11);
            house3q(238, 40, 168, 166, "#3f8f8a");
            nookPatch(NOOK1.x, NOOK1.y, NOOK1.w, NOOK1.h); rr(NOOK1.x + 6, NOOK1.y + 8, 18, 14, 2, "#b07a45", OUT, 1.5);
            house3q(486, 46, 98, 160, "#5c6f8c");
            tree(632, 118, 28); bush(612, 194, 11); bush(656, 196, 9);
            nookPatch(NOOK2.x, NOOK2.y, NOOK2.w, NOOK2.h); circle(NOOK2.x + 50, NOOK2.y + 14, 6, "#8a8c84", OUT, 1.5);
            marketStall(756, 62, 192, 144);
            hedgeRow(0, 416, 590, 26); hedgeRow(652, 416, 308, 26);
            nookPatch(NOOK3.x, NOOK3.y, NOOK3.w, NOOK3.h);
            foreach (var a in new[] { new double[] { 60, 522, 30 }, new double[] { 150, 570, 26 }, new double[] { 262, 514, 28 }, new double[] { 360, 568, 24 }, new double[] { 470, 522, 30 }, new double[] { 548, 576, 22 }, new double[] { 722, 530, 30 }, new double[] { 814, 572, 26 }, new double[] { 902, 518, 28 } }) tree(a[0], a[1], a[2]);
            bush(410, 490, 12); bush(860, 486, 12); bush(120, 476, 10);
        }
        void stoneBlock(double x, double y, double s, string state)
        {
            ellipse(x + s / 2 + 3, y + s - 1, s * 0.5, s * 0.13, "rgba(20,15,30,.28)");
            rr(x, y, s, s, s * 0.16, "#7a66b0", OUT, 3);
            ctx.FillStyle = "rgba(255,255,255,.18)"; ctx.BeginPath(); ctx.MoveTo(x + 4, y + 4); ctx.LineTo(x + s - 4, y + 4); ctx.LineTo(x + s - 11, y + 11); ctx.LineTo(x + 11, y + 11); ctx.LineTo(x + 11, y + s - 11); ctx.LineTo(x + 4, y + s - 4); ctx.ClosePath(); ctx.Fill();
            ctx.FillStyle = "rgba(0,0,0,.2)"; ctx.BeginPath(); ctx.MoveTo(x + s - 4, y + 4); ctx.LineTo(x + s - 4, y + s - 4); ctx.LineTo(x + 4, y + s - 4); ctx.LineTo(x + 11, y + s - 11); ctx.LineTo(x + s - 11, y + s - 11); ctx.LineTo(x + s - 11, y + 11); ctx.ClosePath(); ctx.Fill();
            double cx = x + s / 2, cy = y + s / 2, e = s * 0.26; ctx.StrokeStyle = "#cbb9f2"; ctx.LineWidth = Math.Max(2, s * 0.045); ctx.BeginPath(); ctx.MoveTo(cx - e, cy); ctx.QuadraticCurveTo(cx, cy - e * 0.95, cx + e, cy); ctx.QuadraticCurveTo(cx, cy + e * 0.95, cx - e, cy); ctx.Stroke(); circle(cx, cy, e * 0.32, "#cbb9f2");
            if (state == "ok") { circle(x + s - 7, y + 7, 10, COL.evid, COL.white, 2); txt("✓", x + s - 7, y + 7.5, "700 12px " + FONT, COL.white); }
            if (state == "warn") { double a = 0.5 + 0.5 * Math.Sin(S.t * 0.25); rr(x - 4, y - 4, s + 8, s + 8, s * 0.2, null, $"rgba(255,90,74,{Inv(0.5 + a * 0.5)})", 4); }
        }
        static string Inv(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        void laneOverlay(double x, double y, double w, double h)
        {
            ctx.Save(); rr(x, y, w, h, 12, "rgba(70,190,120,.22)", "rgba(42,138,104,.75)", 2); ctx.SetLineDash(null); ctx.FillStyle = "rgba(42,138,104,.55)";
            for (double i = x + 30; i < x + w - 20; i += 48) { ctx.BeginPath(); ctx.MoveTo(i, y + h * 0.3); ctx.LineTo(i + 10, y + h / 2); ctx.LineTo(i, y + h * 0.7); ctx.LineTo(i + 5, y + h / 2); ctx.ClosePath(); ctx.Fill(); }
            ctx.Restore();
        }
        void hatch(double x, double y, double w, double h)
        {
            ctx.Save(); rr(x, y, w, h, 6, "rgba(243,156,51,.25)", "#d2483a", 2.5); ctx.Clip(); ctx.StrokeStyle = "rgba(210,72,58,.55)"; ctx.LineWidth = 3;
            for (double i = -h; i < w; i += 12) { ctx.BeginPath(); ctx.MoveTo(x + i, y + h); ctx.LineTo(x + i + h, y); ctx.Stroke(); }
            ctx.Restore();
        }
        void dashed(double x, double y, double w, double h, string col = null) { ctx.Save(); ctx.SetLineDash(new double[] { 7, 5 }); rr(x, y, w, h, 10, null, col ?? "#1f6e52", 3); ctx.Restore(); }
        void drawZ1()
        {
            ctx.DrawImage(bgZ1(), 0, 0);
            txt("MERCADO", 852, 59, "700 15px " + FONT_D, OUT);
            if (S.zone == 1)
            {
                if (S.f["i_rosa"]) { laneOverlay(8, 290, W - 16, 40); if (!S.f["wall"]) tag("Paso plano · Rosa", 300, 310, COL.evid); }
                if (S.f["i_mario"]) { hatch(770, 216, 166, 34); if (!S.f["wall"]) tag("No tapar · Mario", 852, 262, COL.warn); }
                if (S.f["i_tito"] && !S.f["wall"])
                {
                    foreach (var n in new[] { NOOK1, NOOK2, NOOK3 }) dashed(n.x + 2, n.y + 2, n.w - 4, n.h - 4);
                    tag("Rincón libre", NOOK1.x + 31, NOOK1.y - 10, COL.evid); tag("Rincón libre", NOOK2.x + 31, NOOK2.y - 10, COL.evid); tag("Rincón libre", NOOK3.x + 31, NOOK3.y + NOOK3.h + 12, COL.evid);
                }
            }
            if (!S.f["wall"])
            {
                rr(522, 180, 70, 22, 6, COL.adiv, OUT, 2); txt("ENTRADA →", 557, 191, "700 11px " + FONT, COL.white);
                for (int i = 0; i < 3; i++) stoneBlock(589, 214 + i * 64, 64, "");
            }
            else
            {
                stoneBlock(NOOK1.x + 6, NOOK1.y + 8, 50, ""); stoneBlock(NOOK2.x + 6, NOOK2.y + 8, 50, ""); stoneBlock(NOOK3.x + 6, NOOK3.y + 4, 50, "");
                rr(888, 286, 64, 24, 6, "#9b6a43", OUT, 2); ctx.FillStyle = "#7a4b2e"; ctx.FillRect(916, 310, 6, 24); txt("Estación →", 920, 298, "700 11px " + FONT, COL.cream);
            }
        }

        // ---------------- Escenas 2 ----------------
        void drawZ2()
        {
            ctx.DrawImage(grassCanvas(), 0, 0);
            cobbleFill(0, 214, W, 192); curb(0, 206, W, 10); curb(0, 404, W, 10);
            rr(400, 26, 160, 166, 12, COL.adiv, OUT, 3); ctx.FillStyle = "rgba(255,255,255,.14)"; ctx.FillRect(404, 30, 152, 10); drawEye(480, 92, 34); txt("Mirador del Adivino", 480, 160, "700 15px " + FONT_D, COL.white);
            ctx.Save(); ctx.Translate(520, 24); ctx.Rotate(-0.4); rr(-4, -7, 42, 14, 4, "#c9a24a", OUT, 2); ctx.Restore();
            var mt = METRICS[S.mstage];
            ctx.FillStyle = "#6e4a2e"; ctx.FillRect(610, 120, 8, 46); ctx.FillRect(722, 120, 8, 46);
            rr(592, 40, 156, 84, 10, "#231c2b", OUT, 3); txt("TABLERO DE LA ESTACIÓN", 670, 56, "700 10px " + FONT, "#cbb9f2");
            txt(mt.ok + " de 100", 670, 84, "700 26px " + FONT_D, mt.ok >= 80 ? "#7fd8b2" : (mt.ok >= 50 ? "#ffcf70" : "#ff8a73"));
            txt("llegan a su destino", 670, 108, "700 11px " + FONT, COL.cream);
            rr(760, 110, 170, 86, 10, COL.evid, OUT, 3); txt("Andén Valle", 845, 152, "700 16px " + FONT_D, COL.white);
            rr(760, 430, 170, 74, 10, "#3d7c96", OUT, 3); txt("Andén Bosque", 845, 467, "700 16px " + FONT_D, COL.white);
            house3q(100, 418, 170, 150, "#b0703c"); txt("TAQUILLA", 185, 430, "700 13px " + FONT_D, OUT);
            tree(60, 120, 26); tree(200, 90, 30); tree(320, 140, 24); tree(640, 110, 26); tree(700, 560, 26); tree(380, 540, 28);
            if (!S.f["tiles"]) { drawArrow(700, 262, "L", COL.adiv, 30); drawArrow(480, 224, "U", COL.adiv, 30); drawArrow(420, 380, "R", COL.adiv, 30); drawArrow(220, 300, "R", COL.adiv, 30); }
            else { rr(888, 286, 64, 24, 6, "#9b6a43", OUT, 2); ctx.FillStyle = "#7a4b2e"; ctx.FillRect(916, 310, 6, 24); txt("Plaza →", 920, 298, "700 11px " + FONT, COL.cream); }
        }
        static readonly (string, double, double)[] CROWD = { ("rosa", 90, 470), ("tito", 150, 505), ("mario", 220, 470), ("ernesto", 300, 505), ("taq", 660, 470), ("c1", 740, 505), ("c2", 810, 470), ("c3", 880, 505) };
        void drawZ3()
        {
            ctx.DrawImage(grassCanvas(), 0, 0);
            cobbleFill(0, 214, W, 192); curb(0, 206, W, 10); curb(0, 404, W, 10);
            rr(340, 40, 280, 150, 14, COL.adivDark, OUT, 3); rr(352, 52, 256, 30, 8, COL.adiv, OUT, 2); txt("Plaza del pueblo", 480, 68, "700 16px " + FONT_D, COL.white);
            tree(120, 110, 30); tree(820, 110, 30);
            for (int i = 0; i < CROWD.Length; i++) drawCreature(CROWD[i].Item1, CROWD[i].Item2, CROWD[i].Item3, 14, new CO { seed = i + 10 });
        }
        void drawEnt(Ent e, bool near)
        {
            double x = e.x, y = e.y;
            if (near) ellipse(x, y + e.r * 0.9, e.r * 1.5, e.r * 0.5, null, "#ffcf70", 2.5);
            if (e.kind == "obj")
            {
                if (e.id == "tablero") { rr(x - 26, y - 20, 52, 40, 6, "#5b4a3a", OUT, 2.5); rr(x - 22, y - 16, 44, 32, 4, COL.paper); drawArrow(x - 8, y, "R", COL.adiv, 16); drawArrow(x + 10, y, "U", COL.evid, 16); }
                if (!string.IsNullOrEmpty(e.label)) label(e.label, x, y + e.r + 14);
            }
            else
            {
                bool moving = e.circle != null || (e.bump && !S.f["wall"]);
                if (e.cart) drawCart(x + e.r * 1.75, y + 2, e.r);
                double dx = S.p.x - x, dy = S.p.y - y, dl = hypot(dx, dy); if (dl == 0) dl = 1;
                drawCreature(e.ck, x, y, e.r, new CO { seed = e.seed, moving = moving, look = new[] { dx / dl, dy / dl } });
                label(e.label, x, y + e.r + 16);
            }
            var m = e.mark != null ? e.mark() : "";
            if (!string.IsNullOrEmpty(m)) drawMark(x, y - e.r - (e.ck == "adivino" ? 44 : 22), m);
        }
        void drawMark(double x, double y, string m)
        {
            double b = Math.Sin(S.t * 0.12) * 3; double yy = y + b;
            rr(x - 12, yy - 12, 24, 22, 8, m == "?" ? "#ffcf5a" : COL.evid, OUT, 2.5);
            ctx.BeginPath(); ctx.MoveTo(x - 5, yy + 9); ctx.LineTo(x + 3, yy + 9); ctx.LineTo(x - 2, yy + 15); ctx.ClosePath(); ctx.FillStyle = OUT; ctx.Fill();
            txt(m, x, yy, "700 15px " + FONT, m == "?" ? OUT : COL.white);
        }
        void drawPlayer() { var p = S.p; drawCreature("player", p.x, p.y, p.r, new CO { seed = 0, moving = p.moving, look = new[] { p.face, 0 } }); label("Tú", p.x, p.y + p.r + 16); }
        void drawPrompt(Ent e)
        {
            string s = e.verb ?? ("Hablar con " + e.label);
            ctx.Font = "700 13px " + FONT; double tw = ctx.MeasureText(s), w = tw + 44;
            double y = e.y - e.r - (e.ck == "adivino" ? 78 : 56); double x = Math.Max(8, Math.Min(W - w - 8, e.x - w / 2));
            if (y < 70) y = e.y + e.r + 28;
            rr(x, y, w, 28, 14, "rgba(42,34,56,.94)", OUT, 2); rr(x + 5, y + 4, 20, 20, 5, COL.cream, OUT, 1.5); txt("E", x + 15, y + 14.5, "700 12px " + FONT, OUT); txt(s, x + 32 + tw / 2, y + 14.5, "700 13px " + FONT, COL.cream);
        }
        void drawWorld()
        {
            if (S.zone == 1) drawZ1(); else if (S.zone == 2) drawZ2(); else drawZ3();
            var near = (S.mode == "world" && !D.on && S.modal == null) ? nearest() : null;
            drawCatTrail();
            var list = S.ents.Where(visible).Select(e => (e: e, p: false, cat: false, y: e.y)).ToList();
            list.Add((null, true, false, S.p.y)); list.Add((null, false, true, S.cat.y));
            foreach (var o in list.OrderBy(o => o.y)) { if (o.p) drawPlayer(); else if (o.cat) drawCatAt(); else drawEnt(o.e, o.e == near); }
            drawCatOverlay();
            if (near != null) drawPrompt(near);
            else if (catCanPet()) drawCatPrompt();
            if (S.p.tx != null && S.mode == "world") { ctx.Save(); ctx.SetLineDash(new double[] { 3, 4 }); circle(S.p.tx.Value, S.p.ty.Value, 9, null, "#ffcf70", 2.5); ctx.Restore(); }
        }
        void title(string a, string b)
        {
            rr(210, 22, 540, b != null ? 100 : 56, 14, "rgba(42,34,56,.9)", OUT, 2.5); txt(a, W / 2, 46, "700 24px " + FONT_D, "#ffcf70");
            if (b != null) { var ls = wrapLines(b, "16px " + FONT, 500); for (int i = 0; i < ls.Count; i++) txt(ls[i], W / 2, 78 + i * 20, "16px " + FONT, COL.cream); }
        }

        /// <summary>El retrato del diálogo (canvas de 120x120 en game.js).</summary>
        void drawPortrait()
        {
            string key = D.spk ?? "piensa"; var sp = SPK.ContainsKey(key) ? SPK[key] : SPK["piensa"];
            ctx.ResetTransform();
            var c = CK.ContainsKey(sp.k) ? CK[sp.k] : CK["c1"];
            var g = ctx.CreateRadialGradient(60, 50, 10, 60, 60, 70); g.AddColorStop(0, c.body + "88"); g.AddColorStop(1, "#3b3152");
            // En la web el borde redondeado del <canvas> recorta el fondo; aquí se rellena directamente el rectángulo redondeado.
            rr(0, 0, 120, 120, 12 * 120 / 104.0, g);
            if (sp.k == "cat") drawCat(60, 84, new CatO { s = 3, sit = true, face = 1, meow = D.typing, purr = D.full.ToLower().Contains("prr"), happy = D.full.ToLower().Contains("prr") });
            else if (sp.k == "crowd") { drawCreature("c1", 36, 80, 18, new CO { seed = 1 }); drawCreature("c3", 84, 82, 18, new CO { seed = 2 }); drawCreature("c2", 60, 64, 17, new CO { seed = 3 }); }
            else { bool isA = sp.k == "adivino"; drawCreature(sp.k, 60, isA ? 80 : 66, isA ? 28 : 34, new CO { seed = 0, talking = D.typing && !sp.think, big = true }); }
            if (sp.think) { circle(100, 20, 8, COL.white, OUT, 2); circle(88, 34, 4.5, COL.white, OUT, 1.5); }
        }

        /// <summary>draw() de game.js: elige qué pantalla dibujar.</summary>
        void draw()
        {
            ctx.ResetTransform();
            if (S.mode == "soko" && S.soko != null) drawSoko();
            else if (S.mode == "tiles" && S.tiles != null) drawTiles();
            else if ((S.mode == "plaza" || S.mode == "end") && S.pz != null && S.zone == 3) drawPlaza();
            else if (S.zone > 0) drawWorld();
            else { drawZ1(); drawCreature("adivino", 560, 250, 24, new CO { seed = 4 }); drawCreature("player", 380, 320, 18, new CO { seed = 0, look = new double[] { 1, 0 } }); drawCreature("rosa", 470, 350, 15, new CO { seed = 1 }); drawCart(496, 352, 15); drawCat(342, 334, new CatO { sit = true, face = 1, s = 1.1 }); }
            if (S.fade > 0) { ctx.FillStyle = $"rgba(27,26,43,{Inv(S.fade)})"; ctx.FillRect(0, 0, W, H); }
        }
    }
}
