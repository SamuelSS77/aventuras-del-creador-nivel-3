using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventurasNivel3
{
    public class Duel { public double crack, shatter, shake; public bool vision, notebook, mirror; public int invCount; public int?[] persona = { null, null, null }; public HashSet<string> bad = new(); }
    public class Inv { public string type; public double x, y, warn, fixT; public bool fixed_; }
    public class Orb { public double x, y, vx, vy; public bool dead; }
    public class PzFx { public string txt, c; public double x, y, life = 1.1, t; public bool bubble; }
    public class Chip { public double x, y, w, h; public Action fn; }
    public class Banner { public string txt; public double t0; }
    public class Helper { public string k, type; public double x, y; public double[] home; public Inv target; public double work; public bool moving; }
    public class Calm { public double t; public string phase; }
    public class Pz
    {
        public string phase; public double stress, t, spawnT, orbT, cool, stun, waveT, teamT, endT, catCd; public double? energy;
        public List<Inv> items = new(); public List<Orb> orbs = new(); public List<PzFx> fx = new(); public List<Chip> chips = new();
        public bool paused, everSel, failed, won, dilemma, boost, endDone; public Inv sel, _near; public Banner banner;
        public int wave, spawned2, fixedN; public List<string> queue = new(); public List<Helper> helpers = new(); public Calm calm;
    }
    public class PersonaOpt { public string t, src; public bool ok; public PersonaOpt(string t, string src, bool ok) { this.t = t; this.src = src; this.ok = ok; } }
    public class FixDef { public int key; public string n, c, q; }

    public partial class Game
    {
        /* ---------- Tiempo 3: la Plaza ---------- */
        static readonly (string f, PersonaOpt[] opts)[] PERSONA =
        {
            ("Qué quiere lograr", new[] {
                new PersonaOpt("Admirar monumentos grandiosos.", "la bola del Adivino", false),
                new PersonaOpt("Llegar al mercado y a los buses sin dar vueltas.", "Doña Rosa, Don Mario y el tablero de la Estación", true),
                new PersonaOpt("Visitar el mirador del Adivino.", "la bola del Adivino", false) }),
            ("Qué le molesta", new[] {
                new PersonaOpt("Los obstáculos que la obligan a dar vueltas y las flechas que confunden.", "Doña Rosa, Don Ernesto y las pruebas de rutas", true),
                new PersonaOpt("Que el pueblo no sea lo bastante espectacular.", "la bola del Adivino", false),
                new PersonaOpt("Que haya pocas flechas brillantes.", "la bola del Adivino", false) }),
            ("Qué necesita", new[] {
                new PersonaOpt("Un mirador más alto para ver mejor.", "la bola del Adivino", false),
                new PersonaOpt("Más letreros con la cara del Adivino.", "la bola del Adivino", false),
                new PersonaOpt("Caminos planos y directos hasta donde va.", "Doña Rosa, Don Mario y las pruebas de rutas", true) })
        };
        void startDuel()
        {
            if (S.mode != "plaza" || S.duel == null || S.pz == null)
            {
                S.mode = "plaza"; S.duel = new Duel();
                S.pz = new Pz { phase = "intro", stress = 0, t = 0, paused = true };
                S.p.x = 480; S.p.y = 532; S.p.tx = null; S.p.ty = null; S.p.goal = null;
            }
            setHud("Tiempo 3 de 3 · La Plaza", null); hideMgbar();
            talk(new List<DItem>
            {
                L("creador", "Adivino, en mi recorrido noté algo: nunca le preguntaste a nadie ni probaste tus inventos. Por eso fallaban."),
                L("adivino", "¿Fallaban? ¡Mis inventos son perfectos! ¿Y para quién, según tú, debí diseñarlos?"),
                Fn(() => { S.duel.vision = true; }),
                L("adivino", "Mi bola lo sabe: el usuario de este pueblo ama los monumentos, los miradores y todo lo grandioso."),
                L("creador", "Yo no lo voy a adivinar. Te lo muestro con lo que investigué: una User Persona.")
            }, () => renderPersona(false));
        }
        void pzIntro(string ph)
        {
            // Solo existe la introducción de la huelga.
            string t = "La huelga";
            var steps = new[] { "El Adivino llena la plaza de inventos absurdos sin preguntar, y el pueblo entra en huelga.", "<b>Acércate a un invento</b> (con clic o WASD) para ver su queja y usa los <b>botones de abajo (o las teclas 1, 2 y 3)</b> para darle el arreglo correcto.", "Esquiva las visiones moradas del Adivino y no dejes que el estrés del pueblo llegue al máximo.", $"Las visiones y los inventos acumulados te suben <b>tu estrés</b>: con mucho estrés te mueves más lento. Si {CAT_NAME} te ve muy estresado, irá a ayudarte." };
            var m = NewModal();
            m.Eyebrow("Tiempo 3 · La Plaza"); m.H2(t); m.Steps(steps);
            m.Row(m.Btn("Empezar", true, () => { closeModal(); pzStart(ph); }));
            m.Show();
        }
        void chipRow(double cx, double y, (string label, string color, Action fn)[] opts)
        {
            ctx.Font = "700 13px " + FONT;
            var ws = opts.Select(o => ctx.MeasureText(o.label) + 26).ToArray(); double gap = 8, total = ws.Sum() + gap * (opts.Length - 1);
            double x = Math.Max(10, Math.Min(W - 10 - total, cx - total / 2));
            for (int i = 0; i < opts.Length; i++) { double w = ws[i]; rr(x, y, w, 32, 16, opts[i].color, OUT, 2.5); txt(opts[i].label, x + w / 2, y + 16.5, "700 13px " + FONT, COL.white); S.pz.chips.Add(new Chip { x = x, y = y, w = w, h = 32, fn = opts[i].fn }); x += w + gap; }
        }
        Inv nearestInv()
        {
            var z = S.pz; if (z == null || z.items == null) return null;
            Inv best = null; double bd = 75;
            foreach (var i in z.items) { if (i.fixed_ || i.warn > 0) continue; double d = hypot(i.x - S.p.x, i.y - S.p.y); if (d < bd) { bd = d; best = i; } }
            return best;
        }
        void pzFix(Inv i, string type)
        {
            var z = S.pz; if (z == null || z.paused || z.stun > 0 || i.fixed_ || z.cool > 0) return;
            if (i.type == type) { i.fixed_ = true; z.fixedN++; z.stress = Math.Max(0, z.stress - 6); pzEnergy(3); z.sel = null; pzFx("¡Arreglado!", i.x, i.y - 34, "#7fd8b2"); }
            else { z.cool = 0.5; z.stress = Math.Min(100, z.stress + 5); pzEnergy(-6); S.duel.shake = 6; pzFx("Eso no responde a la queja", i.x, i.y - 34, "#ff8a73"); }
        }
        void pzEnergy(double n) { var z = S.pz; if (z != null) z.energy = Math.Max(0, Math.Min(100, (z.energy ?? 100) + n)); }
        void plazaClick(double mx, double my)
        {
            var z = S.pz; if (z == null || z.paused || S.modal != null || D.on) return;
            var c = z.chips.FirstOrDefault(c => mx >= c.x && mx <= c.x + c.w && my >= c.y && my <= c.y + c.h);
            if (c != null) { c.fn(); return; }
            if (z.phase == "strike" || z.phase == "team")
            {
                if (z.stun > 0) return;
                var it = z.items.FirstOrDefault(i => !i.fixed_ && i.warn <= 0 && hypot(mx - i.x, my - (i.y - 8)) < 42);
                if (it != null)
                {
                    z.sel = it; z.everSel = true;
                    double a = Math.Atan2(S.p.y - it.y, S.p.x - it.x);
                    S.p.tx = Math.Max(24, Math.Min(W - 24, it.x + Math.Cos(a) * 35)); S.p.ty = Math.Max(228, Math.Min(556, it.y + Math.Sin(a) * 35));
                }
                else { z.sel = null; S.p.tx = mx; S.p.ty = Math.Max(228, Math.Min(556, my)); }
                return;
            }
            if (z.phase == "endA") { z.items.Add(new Inv { type = new[] { "paso", "letrero", "altura" }[(int)Math.Floor(rnd() * 3)], x = mx, y = Math.Max(240, Math.Min(540, my)), warn = 0, fixed_ = false, fixT = 0 }); S.duel.invCount++; pzFx("A mi manera…", mx, my - 30, "#cbb9f2"); }
        }

        static readonly string[] TRAITS_OK = { "Va con carrito", "Quiere llegar sin dar vueltas", "No puede subir escaleras", "Necesita letreros claros", "Camina despacio", "Va al mercado y al bus" };
        static readonly string[] TRAITS_BAD = { "Ama los monumentos", "Quiere ver el mirador", "Le encantan los ojos brillantes", "Prefiere rutas largas y bonitas", "Es igualito al Adivino" };
        static readonly Dictionary<string, FixDef> FIX = new()
        {
            ["paso"] = new FixDef { key = 1, n = "Despejar el paso", c = "#3fae7c", q = "¡No puedo pasar!" },
            ["letrero"] = new FixDef { key = 2, n = "Letrero claro", c = "#4a90d9", q = "¿Qué dice eso?" },
            ["altura"] = new FixDef { key = 3, n = "Ajustar altura", c = "#f08a3c", q = "¡Está muy alto!" }
        };
        static readonly string[] FIX_ORDER = { "paso", "letrero", "altura" };
        // Franja superior de protesta, a los lados de la tarima: la plaza (y 245..555) queda libre para jugar.
        static readonly (string k, double x, double y, string type)[] PZ_CROWD = { ("rosa", 45, 206, "paso"), ("mario", 105, 214, "letrero"), ("c1", 160, 200, null), ("ernesto", 215, 210, null), ("tito", 745, 210, "altura"), ("taq", 830, 200, null), ("c2", 915, 212, null) };
        static readonly string[] PZ_STRIKE = { "¡Nadie nos preguntó!", "¡Huelga!", "¡Así no se puede!", "Nada para nosotros sin nosotros", "¡Pregúntennos!", "¡Huelga!", "¡Otra vez sin preguntar!" };
        void pzFx(string t, double x, double y, string c, double life = 1.1, double t0 = 0, bool bub = false) => S.pz.fx.Add(new PzFx { txt = t, x = x, y = y, c = c, life = life, t = t0, bubble = bub });
        void pzStart(string ph, int wave = 0)
        {
            var z = S.pz;
            z.phase = ph; z.t = 0; z.items = new(); z.orbs = new(); z.chips = new(); z.sel = null; z.everSel = false; z.spawnT = 0; z.orbT = 2; z.cool = 0; z.stun = 0; z.paused = false; z.failed = false; z.won = false; z.banner = null;
            if (ph == "team") z.stress = Math.Min(z.stress, 70); else z.stress = 0;
            z.energy = ph == "team" ? Math.Max(z.energy ?? 100, 80) : 100; z.catCd = 0; z.calm = null;
            if (S.cat.mode != "follow") S.cat.mode = "follow";
            if (ph == "strike")
            {
                z.wave = wave != 0 ? wave : 1; z.spawned2 = 0; z.fixedN = 0; z.queue = z.wave == 1 ? new List<string> { "paso", "letrero", "altura" } : new List<string>(); z.dilemma = false; S.p.y = 420;
                z.banner = new Banner { txt = z.wave == 1 ? "Oleada 1 · Escucha la queja y arregla" : "Oleada 2 · El Adivino acelera", t0 = 0 }; setHud("Tiempo 3 · La huelga", null);
            }
            if (ph == "team")
            {
                z.teamT = 0; z.fixedN = 0; z.boost = true;
                z.helpers = PZ_CROWD.Where(c => c.type != null).Select(c => new Helper { k = c.k, x = c.x, y = c.y, home = new[] { c.x, c.y }, type = c.type, target = null, work = 0, moving = false }).ToList();
                z.banner = new Banner { txt = "En equipo · Aguanten 18 segundos", t0 = 0 }; setHud("Tiempo 3 · En equipo", null);
            }
            pzMgbar();
        }
        void plazaAction(int n)
        {
            var z = S.pz; if (z == null || z.paused || S.modal != null || D.on || z.stun > 0) return;
            if (z.phase == "endA") { z.items.Add(new Inv { type = n >= 1 && n <= 3 ? FIX_ORDER[n - 1] : "paso", x = S.p.x, y = Math.Max(240, S.p.y - 30), warn = 0, fixed_ = false, fixT = 0 }); S.duel.invCount++; pzFx("A mi manera…", S.p.x, S.p.y - 40, "#cbb9f2"); return; }
            if (z.phase != "strike" && z.phase != "team") return;
            if (z.cool > 0) return;
            Inv best = null; double bd = 75;
            foreach (var i in z.items) { if (i.fixed_ || i.warn > 0) continue; double d = hypot(i.x - S.p.x, i.y - S.p.y); if (d < bd) { bd = d; best = i; } }
            if (best == null) { pzFx("Acércate a un invento", S.p.x, S.p.y - 40, "#ffcf70"); return; }
            if (FIX[best.type].key == n) { best.fixed_ = true; z.fixedN++; z.stress = Math.Max(0, z.stress - 6); pzEnergy(3); pzFx("¡Arreglado!", best.x, best.y - 34, "#7fd8b2"); }
            else { z.cool = 0.5; z.stress = Math.Min(100, z.stress + 5); pzEnergy(-6); S.duel.shake = 6; pzFx("Eso no responde a la queja", best.x, best.y - 34, "#ff8a73"); }
        }
        void pzMove(double dt)
        {
            var p = S.p; var z = S.pz; p.moving = false; if (z.stun > 0) return;
            bool free = z.phase == "strike" || z.phase == "team" || z.phase == "endA";
            double vx = 0, vy = 0; if (K["left"]) vx -= 1; if (K["right"]) vx += 1; if (free) { if (K["up"]) vy -= 1; if (K["down"]) vy += 1; }
            if (vx != 0 || vy != 0) p.tx = null;
            else if (p.tx != null) { double dx = p.tx.Value - p.x, dy = free ? (p.ty.Value - p.y) : 0, d = hypot(dx, dy); if (d < 5) p.tx = null; else { vx = dx / d; vy = dy / d; } }
            double l = hypot(vx, vy); if (l == 0) return;
            double tired = z.phase == "strike" && z.energy != null ? 0.55 + 0.45 * z.energy.Value / 100 : 1;
            double sp = 4.4 * tired * dt; p.x = Math.Max(24, Math.Min(W - 24, p.x + vx / l * sp));
            if (free) p.y = Math.Max(228, Math.Min(556, p.y + vy / l * sp));
            if (vx != 0) p.face = sign(vx); p.moving = true;
        }
        void updPlaza(double dt, double ms)
        {
            var z = S.pz; if (z == null) return; double s = ms / 1000;
            foreach (var f in z.fx) f.t += s; z.fx = z.fx.Where(f => f.t < f.life).ToList();
            if (z.banner != null) z.banner.t0 += s;
            if (z.paused) return;
            z.t += s; pzMove(dt);
            if (z.cool > 0) z.cool -= s; if (z.stun > 0) z.stun -= s;
            updStrike(s);
            if (z.stress >= 100 && !z.failed && z.phase != "endA") pzFail();
        }
        void pzSpawnInv(string type = null)
        {
            var z = S.pz; double x, y; int tries = 0;
            do { x = 110 + rnd() * 740; y = 250 + rnd() * 270; tries++; } while (tries < 12 && (hypot(x - S.p.x, y - S.p.y) < 70 || z.items.Any(i => !i.fixed_ && hypot(i.x - x, i.y - y) < 70)));
            z.items.Add(new Inv { type = type ?? FIX_ORDER[(int)Math.Floor(rnd() * 3)], x = x, y = y, warn = 0.9, fixed_ = false, fixT = 0 });
            S.duel.invCount++;
        }
        void pzOrb() { var z = S.pz; double sx = 585, sy = 100, dx = S.p.x - sx, dy = S.p.y - sy, d = hypot(dx, dy); if (d == 0) d = 1; double sp = z.phase == "team" ? 180 : 230; z.orbs.Add(new Orb { x = sx, y = sy, vx = dx / d * sp, vy = dy / d * sp }); }
        void updStrike(double s)
        {
            var z = S.pz;
            if (z.sel != null && (z.sel.fixed_ || !z.items.Contains(z.sel))) z.sel = null;
            if (z.phase == "strike")
            {
                z.waveT = z.waveT + s;
                if (z.wave == 1)
                {
                    if (z.queue.Count > 0 && !z.items.Any(i => !i.fixed_)) { var q0 = z.queue[0]; z.queue.RemoveAt(0); pzSpawnInv(q0); }
                    if (z.queue.Count == 0 && z.fixedN >= 3) { z.wave = 2; z.waveT = 0; z.spawnT = 1; z.spawned2 = 0; z.banner = new Banner { txt = "Oleada 2 · El Adivino acelera", t0 = 0 }; }
                }
                else if (z.wave == 2)
                {
                    z.spawnT -= s; if (z.spawnT <= 0 && z.spawned2 < 8) { z.spawnT = 2.5; pzSpawnInv(); z.spawned2++; }
                    z.orbT -= s; if (z.orbT <= 0) { z.orbT = 3.2; pzOrb(); }
                    if (z.spawned2 >= 8 && !z.items.Any(i => !i.fixed_)) { z.wave = 3; z.waveT = 0; z.spawnT = 0.3; z.banner = new Banner { txt = "«¡Tú arreglas uno y yo hago diez!»", t0 = 0 }; }
                }
                else
                {
                    z.spawnT -= s; if (z.spawnT <= 0) { z.spawnT = 0.6; pzSpawnInv(); }
                    z.orbT -= s; if (z.orbT <= 0) { z.orbT = 1.6; pzOrb(); }
                    if ((z.stress >= 82 || z.waveT > 16) && !z.dilemma) { z.dilemma = true; pzCalm(); return; }
                }
                // Estrés del Creador (z.energy guarda la calma: estrés = 100 − energy). Sube con los inventos acumulados (y más en la oleada 3).
                int pend = z.items.Count(i => !i.fixed_ && i.warn <= 0);
                pzEnergy(-((pend > 1 ? (pend - 1) * 1.1 : 0) + (z.wave == 3 ? 2.4 : 0)) * s);
                if (z.catCd > 0) z.catCd -= s;
                if (z.wave < 3 && z.energy < 35 && !(z.catCd > 0) && S.cat.mode == "follow") { z.catCd = 15; S.cat.mode = "assist"; catSay("¡Miau!", 1.2); }
            }
            else if (z.phase == "team")
            {
                z.teamT += s; pzEnergy(5 * s);
                if (z.teamT < 18) { z.spawnT -= s; if (z.spawnT <= 0) { z.spawnT = 0.75; pzSpawnInv(); } z.orbT -= s; if (z.orbT <= 0) { z.orbT = 2.4; pzOrb(); } }
                updHelpers(s);
                if (z.teamT >= 18 && !z.items.Any(i => !i.fixed_)) pzWin();
            }
            else if (z.phase == "endA")
            {
                z.spawnT -= s; if (z.spawnT <= 0) { z.spawnT = 0.45; pzSpawnInv(); }
                z.endT = z.endT + s; z.stress = Math.Min(100, z.stress + 10 * s);
                if (z.endT > 4 && !z.endDone) { z.endDone = true; pzEndA(); }
            }
            foreach (var i in z.items) { if (i.warn > 0) { i.warn -= s; if (i.warn <= 0) S.duel.shake = Math.Max(S.duel.shake, 5); } if (i.fixed_) i.fixT += s; }
            z.items = z.items.Where(i => !(i.fixed_ && i.fixT > 1.2)).ToList();
            int active = z.items.Count(i => !i.fixed_ && i.warn <= 0);
            if (z.phase != "endA") z.stress = Math.Max(0, Math.Min(100, z.stress + active * (z.phase == "team" ? 0.85 : 2.6) * s - (active == 0 ? 4 * s : 0)));
            foreach (var o in z.orbs)
            {
                o.x += o.vx * s; o.y += o.vy * s;
                if (z.stun <= 0 && hypot(o.x - S.p.x, o.y - S.p.y) < 22) { o.dead = true; z.stun = 0.9; z.stress = Math.Min(100, z.stress + 6); if (z.phase == "strike") pzEnergy(-14); S.duel.shake = 10; pzFx("¡Una visión del Adivino te mareó!", S.p.x, S.p.y - 40, "#cbb9f2"); }
            }
            z.orbs = z.orbs.Where(o => !o.dead && o.y < 620 && o.x > -20 && o.x < W + 20).ToList();
        }
        void updHelpers(double s)
        {
            var z = S.pz; double spd = (z.boost ? 150 : 110) * s;
            foreach (var h in z.helpers)
            {
                if (h.target != null && (h.target.fixed_ || !z.items.Contains(h.target))) h.target = null;
                if (h.target == null)
                {
                    Inv best = null; double bd = 1e9;
                    foreach (var i in z.items) { if (i.fixed_ || i.warn > 0 || i.type != h.type || z.helpers.Any(o => o != h && o.target == i)) continue; double d0 = hypot(i.x - h.x, i.y - h.y); if (d0 < bd) { bd = d0; best = i; } }
                    h.target = best; h.work = 0;
                }
                double tx = h.target != null ? h.target.x : h.home[0], ty = h.target != null ? h.target.y + 20 : h.home[1], dx = tx - h.x, dy = ty - h.y, d = hypot(dx, dy);
                if (d > 8) { h.x += dx / d * Math.Min(spd, d); h.y += dy / d * Math.Min(spd, d); h.moving = true; }
                else
                {
                    h.moving = false;
                    if (h.target != null) { h.work += s; if (h.work > 0.5) { h.target.fixed_ = true; z.fixedN++; z.stress = Math.Max(0, z.stress - 5); pzFx("¡" + SPK[h.k].n + " lo arregló!", h.target.x, h.target.y - 34, "#7fd8b2"); h.target = null; } }
                }
            }
        }
        void pzWin()
        {
            var z = S.pz; if (z.won) return; z.won = true; z.paused = true;
            after(650, () => { if (z.phase == "team") pzEndB(); });
        }
        void pzFail()
        {
            var z = S.pz; z.failed = true; z.paused = true;
            string[] m = z.phase == "team" ? new[] { "La huelga estalló", "Aun en equipo se acumularon demasiados inventos." } : new[] { "La huelga estalló", "Se acumularon demasiados inventos sin arreglar." };
            string ph = z.phase; int wave = z.wave;
            var md = NewModal();
            md.Eyebrow("Tiempo 3 · La Plaza"); md.H2(m[0]); md.P(m[1] + " El estrés del pueblo llegó al máximo.");
            md.Row(md.Btn("Reintentar", true, () => { closeModal(); pzStart(ph, ph == "strike" && wave >= 2 ? 2 : 1); }));
            md.Show();
        }
        void pzDilemma()
        {
            var z = S.pz; z.paused = true;
            var m = NewModal("dilema");
            m.Eyebrow("Tiempo 3 · La Plaza"); m.H2("¿Vale la pena?");
            m.P("Arreglas uno y él hace diez. El pueblo sigue en huelga y ya no das abasto. Preguntar, observar, medir, probar… ¿vale la pena esforzarse tanto por entender a los demás?");
            m.Dilemma(("No vale la pena", "Lo hago rápido, a mi manera.", () => { closeModal(); pzChooseA(); }), ("Sí vale la pena…", "pero no tengo que hacerlo solo.", () => { closeModal(); pzChooseB(); }));
            m.Show();
        }
        // Antes del dilema: la plaza se detiene, Boceto se acerca y al Creador le baja el estrés para decidir con calma.
        void pzCalm()
        {
            var z = S.pz; z.paused = true; z.orbs = new(); z.sel = null; z.stun = 0; z.calm = new Calm { t = 0, phase = "approach" };
            K["left"] = K["right"] = K["up"] = K["down"] = false; S.p.tx = null; S.p.moving = false;
            S.cat.mode = "calm"; S.cat.bub = null; hideMgbar();
            setHud("Tiempo 3 · Una pausa", null);
        }
        void pzCalmTalk()
        {
            var z = S.pz;
            talk(new List<DItem>
            {
                L("piensa", "Arreglo uno y él hace diez. Ya no doy más… me tiemblan las manos."),
                L("boceto", "Miau."),
                L("piensa", CAT_NAME + "… Se me sube a las piernas, como cuando me quedo hasta tarde terminando un proyecto."),
                L("boceto", "Prrrr… prrrr…"),
                L("piensa", "Respiro con él. Inhalo… exhalo. El ruido de la plaza se oye más lejos."),
                Fn(() => { z.energy = Math.Max(z.energy ?? 0, 85); }),
                L("piensa", CAT_NAME + " no arregla nada de la plaza. Pero estando aquí, ya no me siento solo."),
                L("piensa", "Con la cabeza más tranquila, ahora sí puedo pensarlo bien.")
            }, () =>
            {
                z.energy = 100; if (z.calm != null) z.calm.phase = "done"; toast("Tu estrés bajó");
                after(600, () => { if (S.pz == z && z.calm != null) pzDilemma(); });
            });
        }
        void pzChooseA()
        {
            var z = S.pz; z.calm = null; S.cat.mode = "away"; S.cat.rub = 0; z.paused = false; z.phase = "endA"; z.endT = 0; z.endDone = false; z.spawnT = 0; S.duel.mirror = true; z.banner = new Banner { txt = "A tu manera…", t0 = 0 }; pzMgbar(); setHud("Tiempo 3 · A tu manera", null);
        }
        void pzChooseB()
        {
            var z = S.pz; z.calm = null; S.cat.mode = "follow"; S.cat.rub = 1.5; catHearts(3);
            talk(new List<DItem> { L("creador", "Sí vale la pena. Pero no tengo que hacerlo solo. ¡Pueblo! Ustedes saben lo que necesitan: ¿lo arreglamos juntos?") }, () =>
            {
                var keep = z.items.Where(i => !i.fixed_).ToList(); pzStart("team"); z.items = keep;
                var lines = new[] { ("rosa", "¡Yo despejo los pasos!"), ("mario", "¡Yo hago los letreros!"), ("tito", "¡Yo ajusto las alturas!"), ("taq", "¡Yo llevo la cuenta para que nadie se agobie!"), ("ernesto", "Yo cronometro. Rápido.") };
                for (int i = 0; i < lines.Length; i++)
                {
                    var c = PZ_CROWD.First(cc => cc.k == lines[i].Item1);
                    pzFx(lines[i].Item2, c.x < 480 ? c.x + 70 : (c.x > 480 ? c.x - 70 : c.x), c.y + 44, "#ffffff", 2.8, -i * 0.7, true);
                }
            });
        }
        void pzEndA()
        {
            S.pz.paused = true;
            talk(new List<DItem> { L("adivino", "¡Por fin lo entiendes! ¡Bienvenido a mi bando, colega!"), L("rosa", "Ay, joven… usted también."), L("piensa", "Hasta " + CAT_NAME + " se fue a sentar lejos de mí."), L("piensa", "Miré la bola… y ya no aparecía el Adivino. Aparecía yo.") }, showEndA);
        }
        void pzEndB()
        {
            hideMgbar();
            talk(new List<DItem>
            {
                L("taq", $"¡Listo! Las cuentas: el Adivino hizo {S.duel.invCount} inventos sin preguntarle a nadie. Entre todos los arreglamos."),
                L("piensa", "Cada uno aportó lo que sabe. Como en un equipo de Creación Digital: nadie hace todo solo."), Fn(() => addLes("H")),
                L("adivino", "¿Tan rápido…? Yo trabajé solo toda la vida."),
                Fn(() => { S.duel.shatter = 1; S.duel.shake = 18; }),
                L("creador", "Tu bola nunca vio a la gente del pueblo. Te mostraba a ti. Tú no eres el usuario."), Fn(() => addLes("G")),
                L("adivino", "¿Y cómo se hace… sin bola?"), Fn(() => { S.duel.notebook = true; }),
                L("creador", "Se pregunta. Y se diseña con ellos, en equipo. Toma, te regalo mi libreta."),
                L("adivino", "¿Puedo… ayudar?"),
                Fn(() => { S.cat.mode = "visit"; S.cat.guide = new CatGuideT { x = 470, y = 236 }; }),
                L("boceto", "Miau."),
                Fn(() => { S.cat.rub = 4; catHearts(4); }),
                L("piensa", CAT_NAME + " fue hasta la tarima y se le frotó contra la túnica. Creo que eso es un sí."),
                L("creador", "Claro. Todo lo que creamos es para alguien. Y nadie lo crea solo.")
            }, showEnd);
        }
        void retryDilemma()
        {
            closeModal(); S.mode = "plaza"; S.duel.mirror = false;
            var z = S.pz; z.phase = "strike"; z.wave = 3; z.stress = 82; z.endDone = false; z.dilemma = true; z.energy = 100; z.calm = null; S.cat.mode = "follow";
            setHud("Tiempo 3 · La huelga", null); pzMgbar(); pzDilemma();
        }
        void showEndA()
        {
            S.mode = "end"; hideMgbar(); setHud("Final 1 de 2", null);
            var m = NewModal("end");
            m.Eyebrow("Final 1 de 2"); m.H2("El segundo Adivino");
            m.P("Dejaste de preguntar para ir más rápido. Los inventos se acumularon, nadie los usó y la huelga siguió.");
            m.Quote("Cuando dejas de escuchar, empiezas a adivinar.");
            m.P("Este no es el único final.", "small");
            m.Row(m.Btn("Jugar de nuevo", false, restart), m.Btn("Volver a elegir", true, retryDilemma));
            m.Show();
        }
        void showEnd()
        {
            S.mode = "end"; hideMgbar();
            setHud("Final 2 de 2 · Nivel 3 completado", null);
            var m = NewModal("end");
            m.Eyebrow("Final 2 de 2 · Fin del Nivel 3"); m.H2("Diseñar con la gente");
            m.P("Última página de tu Bitácora: lo que aprendí en este pueblo", "small");
            m.LesList(LESSON_ORDER.Select(l => LESSONS[l]), true);
            m.P("Versión beta.", "small");
            m.Row(m.Btn("Jugar de nuevo", true, restart));
            m.Show();
        }

        // ---------------- User Persona ----------------
        void renderPersona(bool keep)
        {
            var st = S.duel.persona; var bad = S.duel.bad; bool all = st.All(v => v != null);
            var fields = new List<PersonaField>();
            for (int fi = 0; fi < PERSONA.Length; fi++)
            {
                var F = PERSONA[fi]; int fic = fi;
                var pf = new PersonaField { legend = F.f };
                if (st[fi] != null) { var o = F.opts[st[fi].Value]; pf.okText = o.t; pf.okSrc = "Fuente: " + o.src + " ✓"; }
                else
                {
                    pf.opts = F.opts.Select((o, oi) => (o.t, bad.Contains(fic + "-" + oi))).ToList();
                    pf.pick = oi => { if (PERSONA[fic].opts[oi].ok) st[fic] = oi; else bad.Add(fic + "-" + oi); renderPersona(true); };
                }
                fields.Add(pf);
            }
            var m = NewModal("duel");
            m.Eyebrow("La Plaza · Para quién debiste diseñar"); m.H2("Arma la User Persona");
            m.P("En cada parte, elige lo que de verdad descubriste investigando. Lo que salga de la bola no sirve.");
            m.Persona("Carmen, 63 años", "Nombre de ejemplo. No es una persona real: resume a varias personas del pueblo.", fields);
            if (all) m.Row(m.Btn("Presentar a Carmen al pueblo", true, () => { closeModal(); presentPersona(); }));
            else m.Row(m.Small("Completa las tres partes."));
            m.Show(keep);
        }
        void presentPersona()
        {
            S.duel.vision = false;
            talk(new List<DItem>
            {
                L("creador", "Ella es Carmen. Quiere llegar al mercado y a los buses sin dar vueltas. Le molestan los obstáculos y las rutas que confunden. Necesita caminos planos y directos."),
                L("rosa", "¡Esa soy yo! …Bueno, se parece mucho."),
                L("mario", "¡Así son mis clientes!"),
                L("ernesto", "Lo de las rutas que confunden. Sí."),
                L("creador", "No es una persona real: es un retrato de muchas. Para ella debiste diseñar."),
                Fn(() => { addLes("F"); S.duel.crack = 1; S.duel.shake = 10; }),
                L("adivino", "¡Pero mi bola decía que amaban los monumentos! ¡Mi opinión vale más que tus datos!"),
                L("creador", "Una opinión, por segura que suene, pierde contra lo que dicen y hacen los usuarios."),
                Fn(() => { addLes("E"); S.duel.crack = 2; S.duel.shake = 14; }),
                L("adivino", "¡Bah! ¡Te demostraré que puedo diseñar esta plaza yo solo, sin preguntarle a nadie!")
            }, () => pzIntro("strike"));
        }

        // ---------------- Dibujo de la Plaza ----------------
        static readonly double[][][] CRACKS =
        {
            new[] { new[] { -.15, -.95 }, new[] { -.02, -.5 }, new[] { -.25, -.15 }, new[] { 0, .15 } },
            new[] { new[] { .9, -.25 }, new[] { .45, -.12 }, new[] { .32, .22 }, new[] { .05, .3 } },
            new[] { new[] { -.85, .4 }, new[] { -.42, .3 }, new[] { -.22, .62 }, new[] { .08, .92 } }
        };
        void drawBall(double bx, double by, double r, Duel d)
        {
            ctx.FillStyle = COL.adivDark; ctx.BeginPath(); ctx.MoveTo(bx - r * 0.6, by + r + 16); ctx.LineTo(bx + r * 0.6, by + r + 16); ctx.LineTo(bx + r * 0.38, by + r - 6); ctx.LineTo(bx - r * 0.38, by + r - 6); ctx.ClosePath(); ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = 3; ctx.Stroke();
            if (d.shatter == 0)
            {
                var gr = ctx.CreateRadialGradient(bx - r * .35, by - r * .35, r * .1, bx, by, r); gr.AddColorStop(0, "#f7f2ff"); gr.AddColorStop(1, "#b59ce4");
                circle(bx, by, r * 1.25, "rgba(190,160,255,.15)"); circle(bx, by, r, gr, OUT, 3);
                if (d.mirror) drawCreature("player", bx, by + 8, r * 0.34, new CO { seed = 9, still = true });
                else if (d.vision) drawCreature("adivino", bx, by + 10, r * 0.3, new CO { seed = 9, big = true, still = true });
                else circle(bx - r * .35, by - r * .38, r * .16, "rgba(255,255,255,.75)");
                ctx.StrokeStyle = OUT; ctx.LineWidth = 2.5;
                for (int i = 0; i < Math.Min(d.crack, 3); i++) { var c = CRACKS[i]; ctx.BeginPath(); for (int j = 0; j < c.Length; j++) { if (j > 0) ctx.LineTo(bx + c[j][0] * r, by + c[j][1] * r); else ctx.MoveTo(bx + c[j][0] * r, by + c[j][1] * r); } ctx.Stroke(); }
            }
            else
            {
                double p = Math.Min(1, d.shatter / 30);
                ctx.GlobalAlpha = 0.6; circle(bx, by, r * 0.6, COL.adivSoft); drawCreature("adivino", bx, by + 8, r * 0.3, new CO { seed = 9, big = true, still = true }); ctx.GlobalAlpha = 1;
                for (int i = 0; i < 9; i++) { double a = i / 9.0 * Math.PI * 2 + 0.3, dist = r * (0.7 + p * 0.7), sx = bx + Math.Cos(a) * dist, sy = by + Math.Sin(a) * dist; ctx.FillStyle = "#c8b6ec"; ctx.BeginPath(); ctx.MoveTo(sx, sy - 8); ctx.LineTo(sx + 8, sy + 5); ctx.LineTo(sx - 7, sy + 5); ctx.ClosePath(); ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = 1.5; ctx.Stroke(); }
            }
        }
        void drawInvSmall(Inv i)
        {
            double x = i.x, y = i.y;
            if (i.warn > 0) { double k = 1 - i.warn / 0.9; ctx.Save(); ctx.SetLineDash(new double[] { 6, 5 }); circle(x, y, 18 + k * 14, "rgba(122,79,201,.18)", COL.adiv, 2.5); ctx.Restore(); txt("!", x, y, "700 20px " + FONT_D, COL.adiv); return; }
            bool ok = i.fixed_;
            if (!ok && S.pz._near == i) { double pr = Math.Sin(S.t * 0.25) * 2; ellipse(x, y + 16, 42 + pr, 14 + pr * 0.4, "rgba(255,207,112,.2)", "#ffcf70", 3); }
            ctx.Save(); if (i.fixed_) ctx.GlobalAlpha = Math.Max(0, 1 - i.fixT / 1.2);
            if (i.type == "paso") { ellipse(x, y + 16, 34, 8, "rgba(20,15,30,.25)"); ellipse(x, y + 6, 32, 14, ok ? "#b9b1a2" : "#7a66b0", OUT, 2.5); ellipse(x, y + 3, 24, 9, "#7fc4e8", OUT, 1.5); if (!ok) { rr(x - 5, y - 26, 10, 28, 3, "#7a66b0", OUT, 2); drawEye(x, y - 28, 8); } }
            else if (i.type == "letrero") { ctx.FillStyle = "#6e4a2e"; ctx.FillRect(x - 3, y - 6, 6, 26); rr(x - 30, y - 32, 60, 28, 6, ok ? COL.cream : "#7a66b0", OUT, 2.5); if (ok) txt("Mercado →", x, y - 18, "700 11px " + FONT, OUT); else { drawEye(x - 14, y - 18, 7); drawEye(x + 14, y - 18, 7); } }
            else { if (!ok) { rr(x - 20, y - 34, 4, 52, 2, "#7a66b0", OUT, 1.5); rr(x + 16, y - 34, 4, 52, 2, "#7a66b0", OUT, 1.5); rr(x - 26, y - 42, 52, 9, 3, "#7a66b0", OUT, 2); } else { rr(x - 24, y + 2, 48, 9, 3, "#9b6a43", OUT, 2); rr(x - 20, y + 11, 5, 8, 1, "#7a4b2e"); rr(x + 15, y + 11, 5, 8, 1, "#7a4b2e"); } }
            if (ok) { circle(x + 26, y - 30, 9, COL.evid, COL.white, 2); txt("✓", x + 26, y - 29.5, "700 11px " + FONT, COL.white); }
            ctx.Restore();
            if (!ok)
            {
                var f = FIX[i.type]; double by = y - 56; string lab = f.q;
                ctx.Font = "700 12px " + FONT; double w = ctx.MeasureText(lab) + 18;
                rr(x - w / 2, by - 11, w, 22, 11, COL.cream, f.c, 3); txt(lab, x, by + 0.5, "700 12px " + FONT, OUT);
            }
        }
        string pzObjective()
        {
            var z = S.pz;
            if (z.phase == "strike") return z.wave == 1 ? "Escucha la queja y arregla" : (z.wave == 2 ? $"Oleada 2 · Arreglados: {z.fixedN}" : "¡Diez más!");
            if (z.phase == "team") return z.teamT < 18 ? $"En equipo · Aguanten {Math.Ceiling(18 - z.teamT)} s" : "¡Arreglen lo que queda!";
            if (z.phase == "endA") return "A tu manera…";
            return "La Plaza";
        }
        void drawPlaza()
        {
            var z = S.pz; var d = S.duel;
            z.chips = new(); z._near = (z.phase == "strike" || z.phase == "team") ? nearestInv() : null;
            ctx.Save();
            if (d.shake > 0) ctx.Translate((rnd() - .5) * 10, (rnd() - .5) * 10);
            ctx.DrawImage(grassCanvas(), 0, 0); cobbleFill(0, 204, W, 396); curb(0, 198, W, 8);
            rr(250, 4, 460, 160, 18, COL.adivDark, OUT, 3);
            drawCreature("adivino", 385, 112, 34, new CO { seed = 4, big = true, talking = D.on && D.spk == "adivino" && D.typing });
            if (d.notebook) { rr(412, 128, 28, 20, 3, COL.paper, OUT, 2); }
            drawBall(585, 84, 40, d);
            rr(150, 170, 660, 26, 10, "rgba(42,34,56,.94)", OUT, 2);
            txt(pzObjective(), 320, 183, "700 13px " + FONT, COL.cream);
            txt("Estrés del pueblo", 548, 183, "700 11px " + FONT, "#cbb9f2");
            rr(612, 177, 186, 12, 6, "#3b3152"); double st = Math.Max(0, Math.Min(100, z.stress));
            if (st > 0) rr(612, 177, 186 * st / 100, 12, 6, st > 75 ? "#d2483a" : (st > 45 ? "#f39c33" : "#4cc38a"));
            if (z.phase == "strike" || z.phase == "team" || z.phase == "endA")
            {
                foreach (var it in z.items.OrderBy(a => a.y).ToList()) drawInvSmall(it);
                foreach (var o in z.orbs) { circle(o.x, o.y, 14, "rgba(190,160,255,.3)"); circle(o.x, o.y, 9, "#b59ce4", OUT, 2); drawEye(o.x, o.y, 6); }
            }
            bool strikeOn = z.phase == "strike" || z.phase == "endA";
            for (int i = 0; i < PZ_CROWD.Length; i++)
            {
                var (k, x, y, type) = PZ_CROWD[i];
                if (z.phase == "team" && type != null) { var h = z.helpers.First(hh => hh.k == k); drawCreature(k, h.x, h.y, 15, new CO { seed = i + 20, moving = h.moving }); continue; }
                drawCreature(k, x, y, 15, new CO { seed = i + 20, talking = D.on && D.spk == k && D.typing });
                if (strikeOn) { double bob = Math.Sin(S.t * 0.2 + i) * 3; ctx.FillStyle = "#6e4a2e"; ctx.FillRect(x + 15, y - 34 + bob, 3, 26); rr(x + 2, y - 50 + bob, 30, 18, 3, COL.cream, OUT, 1.5); txt("¡NO!", x + 17, y - 41 + bob, "700 10px " + FONT, COL.warn); }
            }
            // Cántico de huelga como banner ambiental (no es una pista ni un NPC con quien hablar).
            if (strikeOn && z.stress > 20)
            {
                int idx = (int)(Math.Floor(S.t / 100) % PZ_STRIKE.Length); string chant = "«¡Huelga! " + PZ_STRIKE[idx] + "»";
                ctx.Font = "700 12px " + FONT; double cw = ctx.MeasureText(chant) + 24;
                rr(W / 2 - cw / 2, 201, cw, 22, 11, "rgba(42,34,56,0.88)", "#d2483a", 2); txt(chant, W / 2, 212.5, "700 12px " + FONT, "#ff9c86");
            }
            var p = S.p; bool catBehind = S.cat.y <= p.y;
            if (z.calm != null) { double a = Math.Min(1, z.calm.t * 1.2) * 0.62; var g = ctx.CreateRadialGradient(p.x, p.y - 6, 50, p.x, p.y - 6, 520); g.AddColorStop(0, "rgba(27,26,43,0)"); g.AddColorStop(1, $"rgba(27,26,43,{Inv2(a)})"); ctx.FillStyle = g; ctx.FillRect(0, 0, W, H); }
            if (catBehind) drawCatAt();
            drawCreature("player", p.x, p.y, 17, new CO { seed = 0, moving = p.moving, look = new[] { p.face, -0.3 }, talking = D.on && D.spk == "creador" && D.typing });
            if (!catBehind) drawCatAt();
            double en = z.energy ?? 100;
            if (z.phase == "strike" && en < 30 && z.calm == null)
            {
                for (int k = 0; k < 2; k++)
                {
                    double ph = (S.t * 0.03 + k * 0.5) % 1; ctx.GlobalAlpha = 1 - ph; ctx.BeginPath(); double dx = p.x + (k > 0 ? 14 : -14), dy = p.y - 22 + ph * 14;
                    ctx.MoveTo(dx, dy - 5); ctx.QuadraticCurveTo(dx + 4, dy + 1, dx, dy + 3); ctx.QuadraticCurveTo(dx - 4, dy + 1, dx, dy - 5); ctx.FillStyle = "#8cc3dd"; ctx.Fill(); ctx.StrokeStyle = OUT; ctx.LineWidth = 1.2; ctx.Stroke(); ctx.GlobalAlpha = 1;
                }
            }
            if (z.phase == "strike" || z.phase == "team" || z.calm != null)
            {
                bool glow = z.calm != null && z.calm.phase != "approach"; double bx = z.calm != null ? W / 2 - 118 : 14, by = z.calm != null ? 232 : 562;
                rr(bx, by, 236, 30, 15, "rgba(42,34,56,.92)", glow ? CAT.eye : OUT, glow ? 3 : 2);
                drawCat(bx + 22, by + 18, new CatO { s = 0.62, sit = true, happy = glow, purr = glow, seed = 3 });
                double ps = 100 - en;
                txt("Tu estrés", bx + 80, by + 15.5, "700 12px " + FONT, COL.cream);
                rr(bx + 124, by + 9, 100, 12, 6, "#3b3152");
                if (ps > 0) rr(bx + 124, by + 9, Math.Max(6, ps), 12, 6, ps < 45 ? "#4cc38a" : (ps < 70 ? "#f39c33" : "#d2483a"));
            }
            if (z.stun > 0 && z.calm == null) { for (int k = 0; k < 3; k++) { double a = S.t * 0.2 + k * 2.1; star(p.x + Math.Cos(a) * 16, p.y - 26 + Math.Sin(a) * 5, 5, "#ffd36b"); } }
            foreach (var f in z.fx)
            {
                if (f.t < 0) continue; double a = Math.Max(0, 1 - f.t / f.life); ctx.GlobalAlpha = a;
                if (f.bubble) bubble(f.txt, f.x, f.y, COL.evid);
                else { ctx.Font = "700 14px " + FONT; ctx.TextAlign = "center"; ctx.TextBaseline = "middle"; ctx.LineWidth = 4; ctx.StrokeStyle = OUT; ctx.StrokeText(f.txt, f.x, f.y - f.t * 30); ctx.FillStyle = f.c; ctx.FillText(f.txt, f.x, f.y - f.t * 30); }
                ctx.GlobalAlpha = 1;
            }
            drawCatOverlay();
            if (z.calm != null && z.calm.phase != "approach" && !D.on) { rr(W / 2 - 150, 300, 300, 40, 14, "rgba(42,34,56,.9)", CAT.eye, 2.5); txt("Respira…", W / 2, 321, "700 18px " + FONT_D, COL.cream); }
            if ((z.phase == "strike" || z.phase == "team") && !z.paused)
            {
                if (z._near != null)
                {
                    var i = z._near; z.everSel = true;
                    chipRow(i.x, Math.Min(556, i.y + 28), new (string, string, Action)[] { (FIX["paso"].n, FIX["paso"].c, () => pzFix(i, "paso")), (FIX["letrero"].n, FIX["letrero"].c, () => pzFix(i, "letrero")), (FIX["altura"].n, FIX["altura"].c, () => pzFix(i, "altura")) });
                }
                else if (!z.everSel) { var it = z.items.FirstOrDefault(i => !i.fixed_ && i.warn <= 0); if (it != null) bubble("Acércate al invento", it.x, it.y - 84 + Math.Sin(S.t * 0.15) * 3, "#ffcf70"); }
            }
            if (z.banner != null && z.banner.t0 < 2.4) { double a = Math.Min(1, (2.4 - z.banner.t0) * 2); ctx.GlobalAlpha = a; rr(W / 2 - 230, 300, 460, 46, 14, "rgba(42,34,56,.94)", OUT, 3); txt(z.banner.txt, W / 2, 323, "700 18px " + FONT_D, "#ffcf70"); ctx.GlobalAlpha = 1; }
            ctx.Restore();
        }
        static string Inv2(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
