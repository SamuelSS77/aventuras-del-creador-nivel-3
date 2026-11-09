using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventurasNivel3
{
    public class WalkerDef { public string o, dir, name, ck; public bool noStairs; public int max; }
    public class TilePuzzle { public string title, story, need, dest; public string[] grid; public Dictionary<string, string> adiv; public WalkerDef[] walkers; public int maxArrows; }
    public class TArrow { public string d; public bool adiv; }
    public class Walker
    {
        public WalkerDef def; public string dir, state = "walk", msg = ""; public int x, y, fx, fy, steps; public HashSet<string> seen = new();
        public string name => def.name; public string ck => def.ck;
    }
    public class TSim { public double acc; public bool ending; public HashSet<string> used = new(); public List<Walker> walkers; }
    public class TResultW { public int x, y; public string state, msg, name, ck; }
    public class Tiles
    {
        public int i, fails, hintN; public TilePuzzle P; public TSim sim; public List<TResultW> result; public char[][] cells;
        public Dictionary<string, (int x, int y)> origins = new(); public Dictionary<string, TArrow> arrows = new(); public HashSet<string> lastUsed;
    }
    public class TileHint { public string c, d, t; }

    public partial class Game
    {
        /* ---------- Minijuego 2: Baldosas (2 Puzzles con Carga Cognitiva / Límite de Flechas) ---------- */
        static readonly Dictionary<string, (int, int)> DIRS = new() { ["R"] = (1, 0), ["D"] = (0, 1), ["L"] = (-1, 0), ["U"] = (0, -1) };
        static readonly string[] CYCLE = { "R", "D", "L", "U" };
        const double STEP = 260;
        static readonly TilePuzzle[] TP =
        {
            new TilePuzzle
            {
                title = "Doña Rosa · Paso accesible",
                story = "«Voy al bus del Valle con mi carrito. No puedo subir escaleras, y si pones demasiadas flechas me confundo y no sé cuál seguir.»",
                need = "Lleva a Doña Rosa al Valle (D) sin pisar escaleras y usando MÁXIMO 3 flechas.",
                grid = new[] { ".........", ".....T.#.", "1..S.S.#D", "#..S...#.", ".....#..." },
                adiv = new() { ["2,2"] = "D", ["5,0"] = "D", ["7,0"] = "D" },
                walkers = new[] { new WalkerDef { o = "1", dir = "R", name = "Doña Rosa", ck = "rosa", noStairs = true } }, dest = "Valle", maxArrows = 3
            },
            new TilePuzzle
            {
                title = "Hora pico · Dos usuarios, un flujo",
                story = "«Salimos al mismo tiempo: yo voy con mi carrito (sin escaleras) y Don Ernesto va apurado (máximo 10 pasos). Guía a ambos sin saturar la estación de señales.»",
                need = "Lleva a Doña Rosa (sin escaleras) y a Don Ernesto (máx. 10 pasos) a los Andenes (D) con MÁXIMO 4 flechas.",
                grid = new[] { "1..#....T", "...S.#...", ".#.S....D", "...#..S#.", "2....#..." },
                adiv = new() { ["2,0"] = "D", ["3,2"] = "R", ["6,0"] = "R", ["4,4"] = "U" },
                walkers = new[] { new WalkerDef { o = "1", dir = "R", name = "Don Ernesto", ck = "ernesto", max = 10 }, new WalkerDef { o = "2", dir = "R", name = "Doña Rosa", ck = "rosa", noStairs = true } }, dest = "Andenes", maxArrows = 4
            }
        };
        // Pistas de Boceto para cada puzzle: casilla, flecha sugerida y explicación (comprobadas con las soluciones).
        static readonly TileHint[][] TILE_HINTS =
        {
            new[] { new TileHint { c = "2,2", d = "U", t = "La fila de arriba no tiene escaleras. Quita la flecha morada de esta casilla y pon una que suba." },
                    new TileHint { c = "8,0", d = "D", t = "Al final de la fila de arriba, una flecha hacia abajo lleva directo al Valle. Quita las moradas que estorben en esa fila." } },
            new[] { new TileHint { c = "4,2", d = "R", t = "Aquí se cruzan los dos caminos: una sola flecha a la derecha guía a los dos hasta los Andenes." },
                    new TileHint { c = "2,2", d = "R", t = "Don Ernesto baja por la flecha morada de arriba (esa sí servía) y aquí gira a la derecha. No todas las moradas estaban mal: pruébalas antes de quitarlas." } }
        };
        void tilesHint()
        {
            var t = S.tiles; if (t == null || t.sim != null) return;
            t.hintN = Math.Min(t.hintN + 1, TILE_HINTS[t.i].Length);
            var h = TILE_HINTS[t.i][t.hintN - 1];
            catSay("¡Miau!", 1.6); catHearts(1, "note");
            setStatus($"<b>{CAT_NAME} señala una casilla:</b> {h.t}");
            if (t.hintN >= TILE_HINTS[t.i].Length) UiTilesHintVisible(false);
        }
        (double cs, int cols, int rows, double ox, double oy) tilesGeom() { double cs = 68; return (cs, 9, 5, (W - 9 * cs) / 2, 152); }
        int usedArrows(Tiles t = null) => (t ?? S.tiles).arrows.Values.Count(a => !a.adiv);
        void startTiles() { S.mode = "tiles"; S.tiles = new Tiles { i = 0, fails = 0 }; showMgbar("tiles"); loadPuzzle(0); }
        void tilesHelp(bool first)
        {
            var t = S.tiles; var P = t.P;
            var m = NewModal();
            m.Eyebrow($"Minijuego 2 · Puzzle {t.i + 1} de {TP.Length}"); m.H2(P.title); m.Quote(P.story);
            m.P($"<b>Objetivo:</b> {P.need}");
            m.P($"<b>Regla de Análisis de Usuario (Carga Cognitiva):</b> Solo tienes <b>{P.maxArrows} flechas</b> disponibles. Cada señal extra es una decisión más que cansa o confunde al usuario: guía con el mínimo de señales en los puntos clave.<br><br><b>Controles:</b> Clic en una casilla para poner o girar una flecha <span class=\"green\">verde</span> (clic derecho la borra). Haz clic sobre las flechas <span class=\"purple\">moradas</span> del Adivino para quitarlas (quitarlas no gasta tus flechas). Presiona <b>Testear</b> para probar.", "how");
            m.Row(m.Btn(first ? "Diseñar la ruta" : "Volver al tablero", true, closeModal));
            m.Show();
        }
        void loadPuzzle(int i)
        {
            var t = S.tiles; var P = TP[i]; t.i = i; t.P = P; t.sim = null; t.result = null; t.hintN = 0; t.fails = 0;
            UiTilesHintVisible(false);
            t.cells = P.grid.Select(r => r.ToCharArray()).ToArray();
            t.origins = new();
            for (int y = 0; y < t.cells.Length; y++) for (int x = 0; x < t.cells[y].Length; x++) { char ch = t.cells[y][x]; if (ch == '1' || ch == '2') t.origins[ch.ToString()] = (x, y); }
            resetArrows();
            setHud($"Minijuego 2 · {i + 1} de {TP.Length}", P.need);
            setStatus($"Tienes <b>{P.maxArrows} flechas</b> disponibles. Quita los desvíos morados y presiona <b>Testear</b>.");
            tilesHelp(true);
        }
        void resetArrows() { var t = S.tiles; t.arrows = new(); foreach (var kv in t.P.adiv) t.arrows[kv.Key] = new TArrow { d = kv.Value, adiv = true }; t.result = null; }
        void tilesClick(double mx, double my, bool right)
        {
            var t = S.tiles; if (t == null || t.sim != null || S.modal != null || D.on) return;
            var g = tilesGeom(); int x = (int)Math.Floor((mx - g.ox) / g.cs), y = (int)Math.Floor((my - g.oy) / g.cs);
            if (x < 0 || y < 0 || x >= g.cols || y >= g.rows) return;
            char ch = t.cells[y][x]; if (!".S12".Contains(ch)) return;
            string k = x + "," + y; t.arrows.TryGetValue(k, out var a);
            t.result = null;
            if (right) { t.arrows.Remove(k); return; }
            if (a == null)
            {
                int u = usedArrows(t);
                if (u >= t.P.maxArrows)
                {
                    toast($"¡Límite de {t.P.maxArrows} flechas! Más señales saturan al usuario. Gira o borra una con clic derecho.");
                    setStatus($"<b>Límite alcanzado ({u}/{t.P.maxArrows}):</b> Demasiadas señales abruman al usuario. Borra una flecha con clic derecho para ponerla en otro punto.");
                    return;
                }
                t.arrows[k] = new TArrow { d = "R" };
            }
            else if (a.adiv) t.arrows.Remove(k);
            else { int n = Array.IndexOf(CYCLE, a.d) + 1; if (n >= CYCLE.Length) t.arrows.Remove(k); else a.d = CYCLE[n]; }
        }
        void testear()
        {
            var t = S.tiles; if (t == null || t.sim != null || S.modal != null || D.on) return;
            t.result = null;
            t.sim = new TSim { acc = 0, ending = false, walkers = t.P.walkers.Select(w => { var o = t.origins[w.o]; return new Walker { def = w, dir = w.dir, x = o.x, y = o.y, fx = o.x, fy = o.y, steps = 0, state = "walk", msg = "" }; }).ToList() };
            setStatus("Probando la ruta con la gente…");
        }
        void simStep()
        {
            var t = S.tiles; var s = t.sim; var g = tilesGeom();
            foreach (var w in s.walkers)
            {
                w.fx = w.x; w.fy = w.y;
                if (w.state != "walk") continue;
                string k = w.x + "," + w.y; if (t.arrows.TryGetValue(k, out var a)) { w.dir = a.d; s.used.Add(k); }
                string sk = k + "," + w.dir; if (w.seen.Contains(sk)) { w.state = "fail"; w.msg = "Estoy dando vueltas en círculo."; continue; }
                w.seen.Add(sk);
                int nx = w.x + DIRS[w.dir].Item1, ny = w.y + DIRS[w.dir].Item2;
                if (nx < 0 || ny < 0 || nx >= g.cols || ny >= g.rows) { w.state = "fail"; w.msg = "Se acabó el camino. Me perdí justo aquí."; continue; }
                char ch = t.cells[ny][nx];
                if (ch == '#') { w.state = "fail"; w.msg = "Aquí hay una roca. Me perdí justo aquí."; continue; }
                if (ch == 'S' && w.def.noStairs) { w.state = "fail"; w.msg = "Con el carrito no puedo subir escaleras."; continue; }
                w.x = nx; w.y = ny; w.steps++;
                if (ch == 'D') { w.state = "ok"; w.msg = "¡Llegué!"; continue; }
                if (ch == 'T') { w.state = "fail"; w.msg = "¡Otra vez frente al mirador del Adivino!"; continue; }
                if (w.def.max > 0 && w.steps > w.def.max) { w.state = "fail"; w.msg = "¡Llegué tarde! Di demasiadas vueltas."; continue; }
                if (w.steps > 60) { w.state = "fail"; w.msg = "Sigo perdido."; }
            }
            if (!s.walkers.Any(w => w.state == "walk")) finishSim();
        }
        void updSim(double ms) { var s = S.tiles.sim; if (s == null) return; s.acc += ms; while (s.acc >= STEP && !s.ending) { s.acc -= STEP; simStep(); } }
        void finishSim()
        {
            var t = S.tiles; var s = t.sim; if (s.ending) return; s.ending = true;
            after(STEP + 250, () =>
            {
                if (S.tiles == null || S.tiles.sim != s) return;
                bool ok = s.walkers.All(w => w.state == "ok");
                t.result = s.walkers.Select(w => new TResultW { x = w.x, y = w.y, state = w.state, msg = w.msg, name = w.name, ck = w.ck }).ToList();
                t.lastUsed = s.used; t.sim = null;
                if (ok) puzzleOk(); else puzzleFail();
            });
        }
        void puzzleFail()
        {
            var t = S.tiles; t.fails++;
            var f = t.result.First(w => w.state != "ok");
            setStatus($"<b>{f.name}:</b> «{f.msg}» Ajusta tus flechas y vuelve a testear.");
            if (t.fails >= 2 && t.hintN < TILE_HINTS[t.i].Length) { if (!UiTilesHintIsVisible()) { UiTilesHintVisible(true); catSay("¡Miau! ¿Te ayudo?", 3.5); } }
            if (!S.les.Contains("D")) talk(L("piensa", "No llegó… pero al observar la prueba vi exactamente en qué casilla falló mi diseño."), L("piensa", "Así funciona: pruebo con la gente, veo dónde falla, lo ajusto y vuelvo a medir."), Fn(() => addLes("D")));
        }
        void puzzleOk()
        {
            var t = S.tiles;
            var before = METRICS[S.mstage]; S.mstage = Math.Min(2, t.i + 1); var m = METRICS[S.mstage];
            setStatus($"<b>¡Funcionó!</b> Tablero de la Estación: {before.ok} → {m.ok} de cada 100 llegan a su destino.");
            toast($"Tablero: {before.ok} → {m.ok} de cada 100 llegan a su destino");
            S.cat.rub = 2; catHearts(3);
            var Lst = new List<DItem>();
            if (t.i == 0) Lst.Add(L("rosa", "¡Llegué al andén del Valle sin escaleras y con solo tres señales claras! Gracias, joven."));
            if (t.i == 1) Lst.AddRange(new[] {
                L("rosa", "¡Llegamos los dos sin perdernos!"),
                L("ernesto", "…Ocho pasos. Sin rodeos. Nada mal."),
                L("piensa", "Pocas señales, puestas solo donde el usuario debe tomar una decisión. Menos ruido visual significa menos confusión.") });
            Lst.Add(L("taq", $"¡El tablero mejoró! Antes llegaban {before.ok} de cada 100; ahora llegan {m.ok}."));
            if (!S.les.Contains("D")) Lst.AddRange(new[] { L("piensa", "Cambié el flujo, lo probé con la gente y medí otra vez. Ahora sé que funcionó con datos."), Fn(() => addLes("D")) });
            talk(Lst, () => { if (t.i < TP.Length - 1) loadPuzzle(t.i + 1); else tilesWin(); });
        }
        void tilesWin()
        {
            hideMgbar(); S.f["tiles"] = true; S.tiles = null; S.mode = "world"; S.fade = 1;
            foreach (var e in S.ents) { if (e.id == "rosa") { e.circle = null; e.x = 820; e.y = 232; } if (e.id == "ernesto") { e.circle = null; e.x = 845; e.y = 396; } }
            updObj();
            talk(new List<DItem>
            {
                L("adivino", "Pero… mis rutas eran perfectas. Mi bola lo dijo."),
                L("creador", "Antes de cambiarlas, llegaban 23 de cada 100. Después, 98. No es una opinión: lo medimos."),
                L("adivino", "…Entonces el problema eran mis rutas, no la gente. No… mi bola nunca falla… ¿verdad que no?"),
                L("piensa", "Le hizo a su bola la misma pregunta que le hizo a Doña Rosa sobre el muro: una que solo acepta un sí."),
                L("adivino", "Muy bien, Creador. Mañana, en la Plaza, frente a todo el pueblo: mis visiones contra tus datos.")
            }, () => card(2));
        }

        void drawTiles()
        {
            var t = S.tiles; var P = t.P; var g = tilesGeom();
            ctx.DrawImage(grassCanvas(), 0, 0);
            title($"Tablero de rutas · {P.title}", P.need);
            rr(g.ox - 10, g.oy - 10, g.cols * g.cs + 20, g.rows * g.cs + 20, 14, "#9b6a43", OUT, 3);
            for (int y = 0; y < g.rows; y++) for (int x = 0; x < g.cols; x++)
                {
                    char ch = t.cells[y][x]; double X = g.ox + x * g.cs, Y = g.oy + y * g.cs, cx = X + g.cs / 2, cy = Y + g.cs / 2;
                    ctx.FillStyle = (x + y) % 2 == 1 ? "#efe6d2" : "#e6dbc3"; ctx.FillRect(X, Y, g.cs, g.cs);
                    if (ch == 'S') { ctx.FillStyle = COL.stairs; ctx.FillRect(X + 3, Y + 3, g.cs - 6, g.cs - 6); ctx.StrokeStyle = COL.stairsLine; ctx.LineWidth = 3; for (int i = 1; i < 5; i++) { ctx.BeginPath(); ctx.MoveTo(X + 6, Y + i * g.cs / 5); ctx.LineTo(X + g.cs - 6, Y + i * g.cs / 5); ctx.Stroke(); } }
                    else if (ch == '#') { circle(cx, cy + 3, g.cs * 0.34, "#7d7f78", OUT, 2.5); circle(cx - 6, cy - 4, g.cs * 0.12, "rgba(255,255,255,.25)"); }
                    else if (ch == 'T') { rr(X + 4, Y + 4, g.cs - 8, g.cs - 8, 10, COL.adiv, OUT, 2.5); drawEye(cx, cy - 7, 14); txt("Mirador", cx, Y + g.cs - 14, "700 12px " + FONT, COL.white); }
                    else if (ch == 'D') { rr(X + 4, Y + 4, g.cs - 8, g.cs - 8, 10, COL.evid, OUT, 2.5); ctx.FillStyle = COL.white; ctx.FillRect(cx - 2, Y + 12, 3, 26); ctx.BeginPath(); ctx.MoveTo(cx + 1, Y + 12); ctx.LineTo(cx + 17, Y + 19); ctx.LineTo(cx + 1, Y + 26); ctx.ClosePath(); ctx.Fill(); txt(P.dest, cx, Y + g.cs - 14, "700 12px " + FONT, COL.white); }
                    else if (ch == '1' || ch == '2') { ctx.Save(); ctx.SetLineDash(new double[] { 4, 4 }); circle(cx, cy, 26, null, COL.inkSoft, 2); ctx.Restore(); }
                }
            ctx.StrokeStyle = "rgba(0,0,0,.08)"; ctx.LineWidth = 1;
            for (int x = 0; x <= g.cols; x++) { ctx.BeginPath(); ctx.MoveTo(g.ox + x * g.cs, g.oy); ctx.LineTo(g.ox + x * g.cs, g.oy + g.rows * g.cs); ctx.Stroke(); }
            for (int y = 0; y <= g.rows; y++) { ctx.BeginPath(); ctx.MoveTo(g.ox, g.oy + y * g.cs); ctx.LineTo(g.ox + g.cols * g.cs, g.oy + y * g.cs); ctx.Stroke(); }
            foreach (var kv in t.arrows)
            {
                var a = kv.Value; var xy = kv.Key.Split(','); int x = int.Parse(xy[0]), y = int.Parse(xy[1]); double cx = g.ox + (x + .5) * g.cs, cy = g.oy + (y + .5) * g.cs;
                if (a.adiv) { rr(cx - 27, cy - 27, 54, 54, 8, "rgba(122,79,201,.18)"); drawArrow(cx, cy, a.d, COL.adiv, 44); circle(cx + 19, cy - 19, 6, COL.white, COL.adiv, 2); }
                else drawArrow(cx, cy, a.d, "#3fae7c", 44);
            }
            foreach (var h in TILE_HINTS[t.i].Take(t.hintN))
            {
                var xy = h.c.Split(','); int x = int.Parse(xy[0]), y = int.Parse(xy[1]); double cx = g.ox + (x + .5) * g.cs, cy = g.oy + (y + .5) * g.cs;
                bool mine = t.arrows.TryGetValue(h.c, out var ma) && !ma.adiv && ma.d == h.d;
                double pr = Math.Sin(S.t * 0.18) * 2;
                rr(cx - 31 - pr, cy - 31 - pr, 62 + pr * 2, 62 + pr * 2, 10, null, mine ? COL.evid : CAT.dark, 3);
                if (!mine) { ctx.GlobalAlpha = 0.4; drawArrow(cx, cy, h.d, "#3fae7c", 40); ctx.GlobalAlpha = 1; }
                circle(cx + 22, cy + 22, 11, CAT.light, OUT, 2); drawPaw(cx + 22, cy + 23, 6, CAT.dark);
            }
            var ws = new List<(string ck, double dx, double dy)>();
            if (t.sim != null) { double p = Math.Min(1, t.sim.acc / STEP); foreach (var w in t.sim.walkers) ws.Add((w.ck, w.fx + (w.x - w.fx) * p, w.fy + (w.y - w.fy) * p)); }
            else if (t.result != null) foreach (var w in t.result) ws.Add((w.ck, w.x, w.y));
            else foreach (var w in P.walkers) ws.Add((w.ck, t.origins[w.o].x, t.origins[w.o].y));
            for (int i = 0; i < ws.Count; i++)
            {
                double off = ws.Count > 1 ? (i > 0 ? 9 : -9) : 0, cx = g.ox + (ws[i].dx + .5) * g.cs + off, cy = g.oy + (ws[i].dy + .5) * g.cs + off * 0.4;
                drawCreature(ws[i].ck, cx, cy, 15, new CO { seed = i + 1, moving = t.sim != null });
            }
            if (t.result != null) foreach (var w in t.result) { double X = g.ox + w.x * g.cs, Y = g.oy + w.y * g.cs; bool ok = w.state == "ok"; rr(X + 2, Y + 2, g.cs - 4, g.cs - 4, 8, null, ok ? COL.evid : COL.warn, 4); bubble(w.name + ": " + w.msg, X + g.cs / 2, Y - 8, ok ? COL.evid : COL.warn); }
            var mw = P.walkers.FirstOrDefault(w => w.max > 0);
            if (mw != null)
            {
                var sw = t.sim?.walkers.FirstOrDefault(w => w.def.max > 0);
                string s = sw != null ? $"Pasos de {mw.name}: {sw.steps} de {mw.max}" : $"Máximo de pasos para {mw.name}: {mw.max}";
                rr(W / 2 - 150, g.oy + g.rows * g.cs + 22, 300, 28, 10, "rgba(42,34,56,.9)", OUT, 2); txt(s, W / 2, g.oy + g.rows * g.cs + 36, "700 15px " + FONT, COL.cream);
            }
            int u = usedArrows(t);
            bool atLimit = u >= P.maxArrows;
            string badgeTxt = $"Tus flechas: {u} de {P.maxArrows} (Menos flechas = menos confusión)";
            ctx.Font = "700 13px " + FONT;
            double bw = ctx.MeasureText(badgeTxt) + 28;
            double badgeY = mw != null ? g.oy + g.rows * g.cs + 58 : g.oy + g.rows * g.cs + 24;
            rr(W / 2 - bw / 2, badgeY - 14, bw, 28, 10, "rgba(42,34,56,.92)", atLimit ? "#f39c33" : "#3fae7c", 2);
            txt(badgeTxt, W / 2, badgeY, "700 13px " + FONT, atLimit ? "#ffcf70" : COL.cream);
            drawCatAt(); drawCatOverlay();
        }
    }
}
