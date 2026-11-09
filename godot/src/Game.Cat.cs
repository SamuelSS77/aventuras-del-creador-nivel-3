using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventurasNivel3
{
    public class CatBub { public string txt; public double t, dur; }
    public class CatFx { public double x, y, vx, t, life; public string kind; }
    public class CatTrail { public double x, y, t, f; public int n; }
    public class CatGuideT { public double x, y; public Ent ent; }
    public class CatState
    {
        public double x = 44, y = 320, face = 1, sitT = 1, helpT, rub, guideT; public bool moving; public string mode = "follow", progKey = "";
        public CatBub bub; public List<CatFx> fx = new(); public List<CatTrail> trail = new(); public CatGuideT guide; public int petN;
    }

    public partial class Game
    {
        double distTo(Ent e) => hypot(e.x - S.p.x, e.y - S.p.y) - e.r - S.p.r;
        static bool visible(Ent e) => !(e.hidden != null && e.hidden());
        Ent nearest() { Ent best = null; double bd = 34; foreach (var e in S.ents) { if (!visible(e) || e.act == null) continue; double d = distTo(e); if (d < bd) { bd = d; best = e; } } return best; }
        Ent entAt(double mx, double my) => S.ents.FirstOrDefault(e => visible(e) && e.act != null && hypot(mx - e.x, my - e.y) < e.r + 16);
        void resetPlayer(double x, double y) { S.p.x = x; S.p.y = y; S.p.tx = null; S.p.ty = null; S.p.goal = null; S.p.face = 1; S.p.idle = 0; catReset(x - 28, y + 10); }

        /* ---------- Boceto, el gato del Creador ----------
           Modos: follow (te sigue), guide (te muestra el siguiente paso), assist (corre a bajarte el estrés),
           calm (escena de calma antes del dilema), away (se aleja triste en el final 1), perch (mira los minijuegos). */
        const double CAT_IDLE_HELP = 20; // segundos sin avanzar antes de que Boceto te muestre el camino
        void catReset(double x, double y)
        {
            var c = S.cat; c.x = x; c.y = y; c.face = 1; c.moving = false; c.mode = "follow"; c.sitT = 1; c.bub = null; c.guide = null; c.helpT = 0; c.rub = 0;
            c.trail = new(); c.fx = new();
        }
        void catSay(string t, double dur = 0) { S.cat.bub = new CatBub { txt = t, t = 0, dur = dur != 0 ? dur : 2.2 }; }
        void catHearts(int n = 3, string col = null)
        {
            var c = S.cat; if (n == 0) n = 3;
            for (int i = 0; i < n; i++) c.fx.Add(new CatFx { x = c.x + (rnd() - 0.5) * 18, y = c.y - 18, vx = (rnd() - 0.5) * 14, t = -i * 0.22, life = 1.4, kind = col == "note" ? "note" : "heart" });
        }
        static string B(bool v) => v ? "true" : "false";
        string catProgress() { var f = S.f; return string.Join(",", S.zone, B(f["i_rosa"]), B(f["i_mario"]), B(f["i_tito"]), B(f["wall"]), B(f["metricas"]), B(f["tiles"])); }
        CatGuideT catStand(Ent e) { int side = S.p.x < e.x ? -1 : 1; return new CatGuideT { x = Math.Max(20, Math.Min(W - 20, e.x + side * (e.r + 22))), y = Math.Max(226, Math.Min(396, e.y + 12)), ent = e }; }
        CatGuideT catTarget()
        {
            Ent ent(string id) => S.ents.FirstOrDefault(e => e.id == id);
            if (S.zone == 1)
            {
                if (interviews() < 3)
                {
                    var left = new[] { "rosa", "mario", "tito" }.Where(id => !S.f["i_" + id]).Select(ent).Where(e => e != null).ToList();
                    left = left.OrderBy(a => hypot(a.x - S.p.x, a.y - S.p.y)).ToList();
                    return left.Count > 0 ? catStand(left[0]) : null;
                }
                if (!S.f["wall"]) return new CatGuideT { x = 556, y = 312, ent = ent("muro") };
                return new CatGuideT { x = 900, y = 318 };
            }
            if (S.zone == 2)
            {
                if (!S.f["metricas"]) return catStand(ent("taq"));
                if (!S.f["tiles"]) return catStand(ent("tablero"));
                return new CatGuideT { x = 900, y = 318 };
            }
            return null;
        }
        void catGuide(CatGuideT tg, string say = null)
        {
            tg ??= catTarget(); if (tg == null) return;
            var c = S.cat; c.mode = "guide"; c.guide = tg; c.guideT = 0; c.trail = new(); c.helpT = 0;
            catSay(say ?? "¡Miau! Por aquí.", 2.4);
        }
        bool catCanPet()
        {
            var c = S.cat; var p = S.p;
            return S.mode == "world" && !D.on && S.modal == null && c.mode == "follow" && p.idle > 2.5 && hypot(c.x - p.x, c.y - p.y) < 52;
        }
        void petCat()
        {
            var c = S.cat; c.petN++; c.rub = 1.6; c.face = S.p.x > c.x ? 1 : -1; catHearts(4); catSay("Prrr…", 1.8); c.helpT = 0;
            if (c.petN == 1) talk(L("boceto", "Prrr… prrr…"), L("piensa", "Boceto siempre sabe cuándo necesito una pausa."));
        }
        (double x, double y, double face) catPerch() => S.mode == "soko" ? (46, 420, 1) : (132, 478, 1);
        void updCat(double dt, double ms)
        {
            var c = S.cat; var p = S.p; double s = ms / 1000;
            foreach (var f in c.fx) f.t += s; c.fx = c.fx.Where(f => f.t < f.life).ToList();
            foreach (var q in c.trail) q.t += s; c.trail = c.trail.Where(q => q.t < 5).ToList();
            if (c.bub != null) { c.bub.t += s; if (c.bub.t > c.bub.dur) c.bub = null; }
            if (c.rub > 0) c.rub -= s;
            if (S.mode == "title") return;
            if (S.mode == "soko" || S.mode == "tiles") { var q = catPerch(); c.x = q.x; c.y = q.y; c.face = q.face; c.moving = false; c.sitT += s; if (c.mode != "perch") { c.mode = "perch"; c.bub = null; } return; }
            if (c.mode == "perch") { c.mode = "follow"; c.x = p.x - 28; c.y = p.y + 10; }
            bool plaza = S.mode == "plaza" || (S.mode == "end" && S.zone == 3);
            double yMin = plaza ? 230 : 224, yMax = plaza ? 560 : 400;
            // ayuda cuando te quedas sin rumbo (solo en el mundo abierto)
            if (S.mode == "world")
            {
                string key = catProgress();
                if (key != c.progKey) { c.progKey = key; c.helpT = 0; if (c.mode == "guide") c.mode = "follow"; }
                if (D.on || S.modal != null) c.helpT = 0;
                else if (c.mode == "follow") { c.helpT += s; if (c.helpT > CAT_IDLE_HELP) { catGuide(null); if (S.cat.mode == "guide") toast(CAT_NAME + " te muestra por dónde seguir"); } }
            }
            else if (c.mode == "guide") c.mode = "follow";
            var z = S.pz;
            double tx, ty, stopAt = 4; double? spd;
            if ((c.mode == "guide" || c.mode == "visit") && c.guide != null) { tx = c.guide.x; ty = c.guide.y; spd = 3.4; }
            else if (c.mode == "assist" || c.mode == "calm") { tx = p.x + p.face * 20; ty = p.y + 8; spd = c.mode == "assist" ? 5.2 : 2.4; }
            else if (c.mode == "away") { tx = Math.Max(40, Math.Min(W - 40, p.x + (p.x < 480 ? 130 : -130))); ty = Math.Min(yMax, p.y + 6); spd = 1.8; }
            else { tx = p.x - p.face * 28; ty = p.y + 10; spd = null; stopAt = 14; }
            if (S.zone == 1 && !S.f["wall"] && !plaza) tx = Math.Min(tx, 572);
            ty = Math.Max(yMin, Math.Min(yMax, ty));
            double dx = tx - c.x, dy = ty - c.y, d = hypot(dx, dy);
            double ox = c.x;
            if (d > stopAt)
            {
                double v = (spd ?? Math.Min(5.6, 0.8 + d * 0.06)) * dt, k = Math.Min(1, v / d);
                c.x += dx * k; c.y += dy * k; c.moving = true; c.sitT = 0;
                if (Math.Abs(c.x - ox) > 0.15) c.face = sign(c.x - ox);
                if (c.mode == "guide") { var lt = c.trail.LastOrDefault(); if (lt == null || hypot(lt.x - c.x, lt.y - c.y) > 22) c.trail.Add(new CatTrail { x = c.x, y = c.y + 6, t = 0, f = c.face, n = c.trail.Count }); }
            }
            else
            {
                c.moving = false; c.sitT += s;
                if (c.mode == "follow" || c.mode == "calm") c.face = p.x >= c.x ? 1 : -1;
                if (c.mode == "away") c.face = p.x >= c.x ? -1 : 1;
                if (c.mode == "visit") c.face = 385 >= c.x ? 1 : -1;
            }
            if (c.mode == "guide")
            {
                c.guideT += s;
                if (!c.moving && Math.Floor(c.guideT / 2.6) != Math.Floor((c.guideT - s) / 2.6)) catSay("¡Miau!", 1.4);
                if ((d <= stopAt && c.guideT > 0.6 && hypot(p.x - c.x, p.y - c.y) < 64) || c.guideT > 30) { c.mode = "follow"; c.guide = null; c.helpT = 0; }
            }
            if (c.mode == "assist")
            {
                if (z == null || z.paused || (z.phase != "strike" && z.phase != "team")) c.mode = "follow";
                else if (d < 18) { z.energy = Math.Min(100, (z.energy ?? 0) + 35); c.mode = "follow"; c.rub = 1.2; catHearts(3); catSay("Prrr…", 1.4); pzFx(CAT_NAME + " ronronea · baja tu estrés", p.x, p.y - 46, CAT.eye); }
            }
            if (c.mode == "calm" && z != null && z.calm != null)
            {
                var m = z.calm; m.t += s;
                if (p.y > 400) { p.y += (400 - p.y) * Math.Min(1, 0.06 * dt); p.moving = true; }
                if (m.phase == "approach" && (d < 6 || m.t > 3.5)) { m.phase = "talk"; c.rub = 99; pzCalmTalk(); }
                if (m.phase != "approach") { z.energy = Math.Min(100, (z.energy ?? 0) + 14 * s); if (Math.Floor(m.t / 0.9) != Math.Floor((m.t - s) / 0.9)) catHearts(1, rnd() < 0.5 ? "note" : null); }
            }
        }
        void drawCatAt()
        {
            var c = S.cat; bool purr = c.rub > 0;
            bool sitting = !c.moving && c.sitT > 0.35;
            drawCat(c.x + (purr && !c.moving ? Math.Sin(S.t * 0.12) * 3 : 0), c.y, new CatO { s = S.mode == "soko" || S.mode == "tiles" ? 1.1 : 1.15, face = c.face, moving = c.moving, sit = sitting, happy = purr, purr = purr, sad = c.mode == "away", meow = (c.bub != null && c.bub.t < 0.5) || (D.on && D.spk == "boceto" && D.typing) });
            if (c.mode == "guide") label(CAT_NAME, c.x, c.y + 22);
        }
        void drawCatTrail()
        {
            foreach (var q in S.cat.trail) { double a = Math.Max(0, 1 - q.t / 5) * 0.55; ctx.GlobalAlpha = a; drawPaw(q.x + (q.n % 2 == 1 ? 4 : -4), q.y, 5, "#3b3152"); ctx.GlobalAlpha = 1; }
        }
        void drawCatOverlay()
        {
            var c = S.cat;
            foreach (var f in c.fx)
            {
                if (f.t < 0) continue; double a = Math.Max(0, 1 - f.t / f.life), fx = f.x + f.vx * f.t, fy = f.y - f.t * 34;
                ctx.GlobalAlpha = a;
                if (f.kind == "note") txt("♪", fx, fy, "700 18px " + FONT_D, CAT.light);
                else { ctx.Save(); ctx.Translate(fx, fy); ctx.BeginPath(); ctx.MoveTo(0, 4); ctx.BezierCurveTo(-8, -2, -5, -9, 0, -4); ctx.BezierCurveTo(5, -9, 8, -2, 0, 4); ctx.FillStyle = "#ff7fa3"; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = 1.5; ctx.Stroke(); ctx.Restore(); }
                ctx.GlobalAlpha = 1;
            }
            if (c.bub != null) { double a = Math.Min(1, (c.bub.dur - c.bub.t) * 3); ctx.GlobalAlpha = Math.Max(0, a); bubble(c.bub.txt, c.x, c.y - 26, CAT.dark); ctx.GlobalAlpha = 1; }
            if (c.mode == "guide" && c.guide != null && c.guide.ent != null && !c.moving) { var e = c.guide.ent; double b = Math.Sin(S.t * 0.15) * 3; circle(e.x, e.y - e.r - 30 + b, 13, "rgba(42,34,56,.88)", CAT.light, 2); drawPaw(e.x, e.y - e.r - 29 + b, 7, CAT.light); }
        }
        void drawCatPrompt()
        {
            var c = S.cat; string s = "Acariciar a " + CAT_NAME;
            ctx.Font = "700 13px " + FONT; double tw = ctx.MeasureText(s), w = tw + 44, x = Math.Max(8, Math.Min(W - w - 8, c.x - w / 2)), y = c.y - 52;
            rr(x, y, w, 28, 14, "rgba(42,34,56,.94)", OUT, 2); rr(x + 5, y + 4, 20, 20, 5, COL.cream, OUT, 1.5); txt("E", x + 15, y + 14.5, "700 12px " + FONT, OUT); txt(s, x + 32 + tw / 2, y + 14.5, "700 13px " + FONT, COL.cream);
        }
    }
}
