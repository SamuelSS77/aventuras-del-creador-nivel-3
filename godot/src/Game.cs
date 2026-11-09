using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventurasNivel3
{
    // ---------------- Tipos de datos del juego (equivalen a los objetos sueltos de game.js) ----------------
    public class Spk { public string n, k; public bool think; public Spk(string n, string k, bool think = false) { this.n = n; this.k = k; this.think = think; } }
    public class Ck { public string body, dark; public Ck(string b, string d) { body = b; dark = d; } }
    public class Evid { public string src, kind, who, txt; }
    public class CircleMove { public double cx, cy, rad, spd; }
    public class Ent
    {
        public string id, kind, ck, label = "", verb; public double x, y, r, ang; public bool cart, bump; public int seed;
        public Action act; public Func<bool> hidden; public Func<string> mark; public CircleMove circle;
    }
    public class Player { public double x = 70, y = 310, r = 15, face = 1, idle; public double? tx, ty; public Ent goal; public bool moving; }
    public class HudItem { public string t; public bool done, warn, plain; }
    public class Flags
    {
        readonly Dictionary<string, bool> d = new();
        public bool this[string k] { get => d.TryGetValue(k, out var v) && v; set => d[k] = value; }
        public void Clear() => d.Clear();
    }
    /// <summary>Un elemento de la cola de diálogo: una línea, una pregunta con opciones o una acción.</summary>
    public class DItem { public string s, t; public string[] tag; public List<Choice> choices; public Action fn; }
    public class Choice { public string t; public List<DItem> then; }

    public partial class Game : Control
    {
        public const double W = 960, H = 600;
        const string OUT = "#2a2238";
        static class COL
        {
            public const string adiv = "#7a4fc9", adivDark = "#4b2d8f", adivSoft = "#e3d8f6", evid = "#2a8a68", evidSoft = "#d3ecdf", ink = "#1d2a2e", inkSoft = "#526266",
                player = "#f08a3c", rock = "#8a8c84", stairs = "#cdb790", stairsLine = "#a48e66", warn = "#d2483a", white = "#ffffff", paper = "#eef1e8", cream = "#f6efe0",
                street = "#e7e0cf", house = "#93a6b7", roof = "#6f8497";
        }
        static readonly Dictionary<string, Ck> CK = new()
        {
            ["player"] = new("#f08a3c", "#c96a24"), ["rosa"] = new("#f283a5", "#cf5f82"), ["mario"] = new("#f3c24f", "#cf9a2c"),
            ["tito"] = new("#62bfe8", "#3b97c2"), ["ernesto"] = new("#b08463", "#8a6245"), ["taq"] = new("#69b884", "#4a9466"),
            ["adivino"] = new("#8d62d9", "#653fb0"), ["c1"] = new("#e98fd0", "#c46aac"), ["c2"] = new("#f0a35a", "#c97f3a"), ["c3"] = new("#7fa4e8", "#5a80c4"),
            ["cat"] = new("#8b93a7", "#646b80")
        };
        // Boceto, el gato del Creador: lo acompaña en todo el nivel y lo ayuda cuando hace falta.
        const string CAT_NAME = "Boceto";
        static class CAT { public const string body = "#8b93a7", dark = "#5f667b", light = "#f1f2f6", ear = "#f2a7bd", eye = "#9be15d", nose = "#f283a5"; }
        const string FONT = "\"Atkinson Hyperlegible\", system-ui, sans-serif";
        const string FONT_D = "\"Baloo 2\", \"Trebuchet MS\", sans-serif";
        const bool RM = false;

        static readonly Dictionary<string, Spk> SPK = new()
        {
            ["creador"] = new("Creador", "player"), ["piensa"] = new("Creador (piensa)", "player", true), ["adivino"] = new("El Adivino", "adivino"),
            ["rosa"] = new("Doña Rosa", "rosa"), ["mario"] = new("Don Mario", "mario"), ["tito"] = new("Tito", "tito"), ["ernesto"] = new("Don Ernesto", "ernesto"),
            ["taq"] = new("Jefe de estación", "taq"), ["pueblo"] = new("El pueblo", "crowd"), ["boceto"] = new(CAT_NAME, "cat")
        };
        static readonly Dictionary<string, Evid> EVID = new()
        {
            ["rosa"] = new Evid { src = "Entrevista", kind = "entrevista", who = "Doña Rosa", txt = "«Con el carrito no puedo trepar. Necesito pasar por el centro de la calle, que es plano.»" },
            ["mario"] = new Evid { src = "Entrevista", kind = "entrevista", who = "Don Mario, vendedor", txt = "«Si tapan el frente de mi puesto, nadie me encuentra.»" },
            ["tito"] = new Evid { src = "Entrevista", kind = "entrevista", who = "Tito", txt = "«En los rincones entre las casas no pasa nadie.»" },
            ["ernesto"] = new Evid { src = "Entrevista", kind = "entrevista", who = "Don Ernesto", txt = "«Treinta y ocho minutos. Debería ser ocho. Tres vueltas cada mañana.»" },
            ["metricas"] = new Evid { src = "Datos", kind = "datos", who = "Tablero de la Estación", txt = "Solo 23 de cada 100 llegan a su destino. 8 de cada 10 rutas van al Mirador, pero solo 2 de cada 100 pasajeros quieren ir allá." }
        };
        static readonly Dictionary<string, string> LESSONS = new()
        {
            ["A"] = "Si preguntas «¿verdad que…?», te dicen lo que quieres oír. Pregunta «¿por qué?».",
            ["B"] = "La gente te pide soluciones. Tu trabajo es encontrar el problema que hay detrás.",
            ["C"] = "Los números te dicen qué falla y cuánto.",
            ["M"] = "Si mucha gente falla en lo mismo, el problema es el diseño, no el usuario.",
            ["D"] = "Pruébalo con la gente y mide otra vez: así sabes si tu cambio funcionó.",
            ["E"] = "Una opinión, por segura que suene, pierde contra lo que dicen y hacen los usuarios.",
            ["F"] = "User Persona: un retrato del usuario hecho con lo que investigaste, no con lo que imaginas.",
            ["G"] = "Tú no eres el usuario.",
            ["H"] = "Nadie diseña bien solo: se diseña en equipo y con los usuarios."
        };
        static readonly string[] LESSON_ORDER = { "A", "B", "C", "M", "D", "E", "F", "H", "G" };

        // ---------------- Estado (S en game.js) ----------------
        public class State
        {
            public string mode = "title"; public int zone; public Flags f = new(); public List<string> ev = new(), les = new();
            public double t; public string modal; public double fade; public Player p = new(); public List<Ent> ents = new();
            public Duel duel; public Soko soko; public Tiles tiles; public CatState cat = new(); public Pz pz; public int mstage;
        }
        public readonly State S = new();
        static readonly Random R = new();
        static double rnd() => R.NextDouble();
        static double hypot(double a, double b) => Math.Sqrt(a * a + b * b);
        static int sign(double v) => Math.Sign(v);
        /// <summary>Math.round de JavaScript (redondea .5 hacia arriba).</summary>
        static double jsRound(double v) => Math.Floor(v + 0.5);

        /// <summary>El mismo generador pseudoaleatorio con semilla de game.js (mulberry32).</summary>
        static Func<double> rng(int a)
        {
            return () =>
            {
                unchecked
                {
                    a = a + 0x6D2B79F5;
                    int t = (a ^ (int)((uint)a >> 15)) * (1 | a);
                    t = (t + (t ^ (int)((uint)t >> 7)) * (61 | t)) ^ t;
                    return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296.0;
                }
            };
        }

        Ctx ctx; // el contexto de dibujo activo (como la variable ctx de game.js)

        // ---------------- Bitácora, toast, HUD ----------------
        void updBit() { UiSetBitCount(S.ev.Count + S.les.Count); UiPulseBit(); }
        void addEv(string id) { if (S.ev.Contains(id)) return; S.ev.Add(id); updBit(); toast("Nueva evidencia en tu Bitácora: " + EVID[id].who); }
        void addLes(string id) { if (S.les.Contains(id)) return; S.les.Add(id); updBit(); toast("Nueva lección anotada en tu Bitácora"); }
        void hudSet(string time, List<HudItem> items)
        {
            if (time != null) UiHudTime(time);
            if (items != null)
            {
                bool nowSet = false; var rows = new List<(string, string)>();
                foreach (var i in items)
                {
                    string c = i.warn ? "warn" : (i.done ? "done" : "");
                    if (!i.done && !i.warn && !nowSet && !i.plain) { c = "now"; nowSet = true; }
                    rows.Add((c, i.t));
                }
                UiHudList(rows);
            }
        }
        void setHud(string time, string text) => hudSet(time, text != null ? new List<HudItem> { new HudItem { t = text } } : null);
        int interviews() => (S.f["i_rosa"] ? 1 : 0) + (S.f["i_mario"] ? 1 : 0) + (S.f["i_tito"] ? 1 : 0);
        void updObj()
        {
            if (S.zone == 1) hudSet("Tiempo 1 de 3 · Entrevistar", new List<HudItem> { new() { t = "Entrevista a Doña Rosa", done = S.f["i_rosa"] }, new() { t = "Entrevista a Don Mario", done = S.f["i_mario"] }, new() { t = "Entrevista a Tito", done = S.f["i_tito"] }, new() { t = "Reacomoda el muro", done = S.f["wall"] }, new() { t = "Cruza hacia la Estación →" } });
            else if (S.zone == 2) hudSet("Tiempo 2 de 3 · Medir y probar", new List<HudItem> { new() { t = "Habla con el jefe de estación", done = S.f["metricas"] }, new() { t = "Arregla las rutas en el Tablero de rutas", done = S.f["tiles"] }, new() { t = "Cruza hacia la Plaza →" } });
            else if (S.zone == 3) hudSet("Tiempo 3 de 3 · Sintetizar", new List<HudItem> { new() { t = "Habla con el Adivino" }, new() { t = "Resuelve lo que pasa en la Plaza" } });
        }

        // ---------------- Diálogos ----------------
        public class Dlg { public List<DItem> q = new(); public bool on, wait, typing; public Action done; public DItem cur; public string full = ""; public double shown; public string spk; }
        readonly Dlg D = new();
        static DItem L(string s, string t, string[] tag = null) => new DItem { s = s, t = t, tag = tag };
        static DItem Fn(Action a) => new DItem { fn = a };
        static DItem Q(string t, params Choice[] ch) => new DItem { t = t, choices = ch.ToList() };
        static Choice C(string t, params DItem[] then) => new Choice { t = t, then = then.ToList() };
        static string[] Tag(string kind, string txt) => new[] { kind, txt };

        void talk(List<DItem> lines, Action done = null) { D.q = lines.ToList(); D.done = done; D.on = true; UiDialogShow(true); next(); }
        void talk(params DItem[] lines) => talk(lines.ToList());
        void next()
        {
            while (D.q.Count > 0 && D.q[0].fn != null) { var fn = D.q[0].fn; D.q.RemoveAt(0); fn(); }
            if (D.q.Count == 0)
            {
                D.on = false; D.wait = false; D.cur = null; D.typing = false; UiDialogShow(false);
                var d = D.done; D.done = null; d?.Invoke(); return;
            }
            var it = D.q[0]; D.q.RemoveAt(0);
            var sp = (it.s != null && SPK.ContainsKey(it.s)) ? SPK[it.s] : SPK["piensa"];
            D.spk = it.s ?? "piensa";
            var ck = CK.ContainsKey(sp.k) ? CK[sp.k] : CK["c1"];
            UiDialogLine(it.choices != null ? "Tu pregunta" : sp.n, it.choices != null ? "#ffcf70" : ck.body, sp.think && it.choices == null, it.tag);
            if (it.choices != null)
            {
                D.typing = false; UiDialogText(it.t ?? "¿Qué preguntas?");
                UiDialogHint("Elige con clic o con las teclas 1 y 2", false);
                D.wait = false; D.cur = it;
                UiDialogChoices(it.choices.Select(c => c.t).ToList());
            }
            else
            {
                D.full = it.t; D.shown = RM ? it.t.Length : 0; D.typing = !RM; UiDialogText(RM ? it.t : "");
                UiDialogHint("E / Espacio / clic", true); D.wait = true; D.cur = null;
                UiDialogChoices(null);
            }
        }
        void pick(int i)
        {
            var it = D.cur; if (it == null) return;
            if (i < 0 || i >= it.choices.Count) return;
            var c = it.choices[i]; D.cur = null; D.q.InsertRange(0, c.then ?? new List<DItem>()); next();
        }
        void adv()
        {
            if (!D.on || !D.wait) return;
            if (D.typing) { D.typing = false; D.shown = D.full.Length; UiDialogText(D.full); return; }
            D.wait = false; next();
        }
        void updTyping(double ms)
        {
            if (!D.typing) return;
            D.shown = Math.Min(D.full.Length, D.shown + ms * 0.1);
            UiDialogText(D.full.Substring(0, (int)Math.Floor(D.shown)));
            if (D.shown >= D.full.Length) D.typing = false;
        }

        // ---------------- Arranque ----------------
        public override void _Ready()
        {
            Fonts.Load();
            BuildUi();
            BuildOffscreens();
            var args = OS.GetCmdlineUserArgs();
            if (args.Contains("--tiempo3")) jumpTo(3);
            else if (args.Contains("--tiempo2")) jumpTo(2);
            else showTitle();
        }

        // ---------------- Bucle (requestAnimationFrame) ----------------
        public override void _Process(double delta)
        {
            double ms = Math.Max(0, Math.Min(60, delta * 1000)); double dt = ms / 16.667;
            S.t += dt;
            if (S.fade > 0) S.fade = Math.Max(0, S.fade - ms / 450);
            updTyping(ms); updEnts(dt); updWorld(dt); updCat(dt, ms);
            if (S.mode == "tiles" && S.tiles != null && S.tiles.sim != null) updSim(ms);
            if (S.mode == "plaza" && S.pz != null && S.modal == null && !D.on) updPlaza(dt, ms);
            else if (S.pz != null && S.pz.fx != null) { double s = ms / 1000; foreach (var f in S.pz.fx) if (f.t < 0) f.t += s; }
            if (S.soko != null)
            {
                if (S.soko.tw < 1) S.soko.tw = Math.Min(1, S.soko.tw + ms / 120);
                if (S.soko.done) { S.soko.cut += ms / 2600; if (S.soko.cut >= 1.35 && !S.soko.won) { S.soko.won = true; sokoWin(); } }
                if (S.soko != null && !S.soko.hintOn && !S.soko.done && S.modal == null && (S.soko.resets >= 3 || S.t - S.soko.t0 > 5400)) { S.soko.hintOn = true; UiShowSokoHint(); catSay("¡Miau! Tengo una pista.", 4); }
            }
            if (S.duel != null) { if (S.duel.shake > 0) S.duel.shake -= dt; if (S.duel.shatter > 0 && S.duel.shatter < 40) S.duel.shatter += dt; }
            bool compact = S.mode == "tiles" || S.mode == "duel" || S.mode == "end" || S.mode == "plaza";
            UiHudCompact(compact);
            UiHudVisible(S.mode != "title");
            UiFrame(ms);
            stageView.QueueRedraw();
            if (D.on) portraitView.QueueRedraw();
        }

        void updEnts(double dt)
        {
            foreach (var e in S.ents)
            {
                if (e.circle != null)
                {
                    e.ang = e.ang + e.circle.spd * dt; e.x = e.circle.cx + Math.Cos(e.ang) * e.circle.rad; e.y = e.circle.cy + Math.Sin(e.ang) * e.circle.rad * 0.6;
                }
                if (e.bump && S.zone == 1 && !S.f["wall"]) e.x = 528 + Math.Abs(Math.Sin(S.t * 0.03)) * 14;
            }
        }
        void updWorld(double dt)
        {
            var p = S.p; p.moving = false; p.idle += dt / 60;
            if (S.mode != "world" || D.on || S.modal != null) return;
            double vx = 0, vy = 0;
            if (K["left"]) vx -= 1; if (K["right"]) vx += 1; if (K["up"]) vy -= 1; if (K["down"]) vy += 1;
            if (vx != 0 || vy != 0) { p.tx = null; p.goal = null; }
            else if (p.tx != null)
            {
                if (p.goal != null) { p.tx = p.goal.x; p.ty = p.goal.y; }
                double dx = p.tx.Value - p.x, dy = p.ty.Value - p.y, d = hypot(dx, dy);
                if (d < 4) p.tx = null; else { vx = dx / d; vy = dy / d; }
            }
            double l = hypot(vx, vy);
            if (l != 0)
            {
                double sp = (S.zone == 1 || S.zone == 2 ? 4.2 : 3.4) * dt, ox = p.x, oy = p.y;
                double nx = Math.Max(14, Math.Min(W - 14, p.x + vx / l * sp)), ny = Math.Max(222, Math.Min(398, p.y + vy / l * sp));
                if (S.zone == 1 && !S.f["wall"] && nx + p.r > 589 && nx - p.r < 653) nx = p.x;
                p.x = nx; p.y = ny; if (vx != 0) p.face = sign(vx); p.moving = hypot(p.x - ox, p.y - oy) > 0.05;
                if (p.moving) p.idle = 0;
                if (p.tx != null && !p.moving) p.tx = null;
            }
            if (p.goal != null && distTo(p.goal) < 30) { var e = p.goal; p.goal = null; p.tx = null; e.act(); return; }
            if (S.zone == 1 && S.f["wall"] && p.x > W - 22) zone2();
            else if (S.zone == 2 && S.f["tiles"] && p.x > W - 22) zone3();
        }

        // ---------------- Entrada ----------------
        class Keys { readonly Dictionary<string, bool> d = new(); public bool this[string k] { get => d.TryGetValue(k, out var v) && v; set => d[k] = value; } public void Clear() => d.Clear(); }
        readonly Keys K = new();
        static string KeyMap(Key k) => k switch
        {
            Key.Left or Key.A => "left",
            Key.Right or Key.D => "right",
            Key.Up or Key.W => "up",
            Key.Down or Key.S => "down",
            _ => null
        };
        static readonly Dictionary<string, (int, int)> SOKO_DIR = new() { ["left"] = (-1, 0), ["right"] = (1, 0), ["up"] = (0, -1), ["down"] = (0, 1) };
        ulong lastSoko;

        public override void _Input(InputEvent ev)
        {
            if (ev is not InputEventKey k) return;
            Key c = k.PhysicalKeycode;
            if (!k.Pressed) { var km = KeyMap(c); if (km != null) K[km] = false; return; }
            bool repeat = k.Echo;
            void handled() => GetViewport().SetInputAsHandled();
            if (c == Key.B) { if (!repeat) openBit(); handled(); return; }
            if (S.modal != null)
            {
                if (c == Key.Escape && S.modal == "bit") closeModal();
                if ((c == Key.E || c == Key.Space || c == Key.Enter || c == Key.KpEnter) && !repeat) { if (UiClickPrimary()) handled(); }
                handled();
                return;
            }
            if (D.on)
            {
                if (c == Key.Space || c == Key.Enter || c == Key.E) { handled(); if (!repeat) adv(); }
                else if (c >= Key.Key1 && c <= Key.Key9 && D.cur != null) { handled(); pick((int)(c - Key.Key1)); }
                if (KeyMap(c) != null) handled();
                return;
            }
            var mapped = KeyMap(c);
            if (S.mode == "world")
            {
                if (mapped != null) { K[mapped] = true; handled(); }
                if ((c == Key.E || c == Key.Space || c == Key.Enter) && !repeat) { handled(); var n = nearest(); if (n != null) n.act(); else if (catCanPet()) petCat(); }
            }
            else if (S.mode == "soko")
            {
                if (mapped != null)
                {
                    handled(); var m = SOKO_DIR[mapped]; ulong now = Time.GetTicksMsec();
                    if (repeat && now - lastSoko < 130) return; lastSoko = now; sokoMove(m.Item1, m.Item2);
                }
                else if (c == Key.Z) sokoUndo(); else if (c == Key.R) sokoReset();
            }
            else if (S.mode == "tiles")
            {
                if (c == Key.Enter || c == Key.T) { handled(); testear(); }
                if (mapped != null || c == Key.Space) handled();
            }
            else if (S.mode == "plaza")
            {
                if (mapped != null) { K[mapped] = true; handled(); }
                int n = c switch { Key.Key1 or Key.Kp1 => 1, Key.Key2 or Key.Kp2 => 2, Key.Key3 or Key.Kp3 => 3, _ => 0 };
                if (n > 0 && !repeat) { handled(); plazaAction(n); }
                if (c == Key.Space) handled();
            }
        }
        public override void _Notification(int what)
        {
            if (what == NotificationWMWindowFocusOut || what == NotificationApplicationFocusOut) K.Clear();
        }

        /// <summary>Clic en el escenario (pointerdown sobre el canvas en game.js).</summary>
        void OnStagePointer(double mx, double my, bool right)
        {
            if (D.on) { adv(); return; }
            if (S.modal != null) return;
            if (S.mode == "world")
            {
                var e = entAt(mx, my);
                if (e != null) { if (distTo(e) < 30) e.act(); else { S.p.goal = e; S.p.tx = e.x; S.p.ty = e.y; } }
                else if (hypot(mx - S.cat.x, my - (S.cat.y - 4)) < 22 && hypot(S.cat.x - S.p.x, S.cat.y - S.p.y) < 60 && S.cat.mode == "follow") petCat();
                else { S.p.goal = null; S.p.tx = mx; S.p.ty = Math.Max(222, Math.Min(398, my)); }
            }
            else if (S.mode == "tiles") tilesClick(mx, my, right);
            else if (S.mode == "plaza" && S.pz != null) plazaClick(mx, my);
            else if (S.mode == "soko" && S.soko != null)
            {
                var g = sokoGeom(); double px = g.ox + (S.soko.px + .5) * g.cs, py = g.oy + (S.soko.py + .5) * g.cs, dx = mx - px, dy = my - py;
                if (Math.Abs(dx) > Math.Abs(dy)) sokoMove(sign(dx), 0); else if (dy != 0) sokoMove(0, sign(dy));
            }
        }

        // ---------------- Inicio, accesos directos y reinicio ----------------
        void jumpTo(int n)
        {
            closeModal(); hideMgbar(); UiDialogShow(false); D.on = false; D.q = new(); D.cur = null; D.done = null; D.typing = false;
            S.f = new Flags(); S.ev = new(); S.les = new(); S.duel = null; S.soko = null; S.tiles = null; S.mstage = 0;
            if (n >= 2) { S.f["i_rosa"] = S.f["i_mario"] = S.f["i_tito"] = S.f["wall"] = true; S.ev.AddRange(new[] { "rosa", "mario", "tito" }); S.les.AddRange(new[] { "A", "B" }); }
            if (n >= 3) { S.f["metricas"] = S.f["tiles"] = S.f["ernesto"] = true; S.ev.AddRange(new[] { "ernesto", "metricas" }); S.les.AddRange(new[] { "C", "M", "D" }); S.mstage = METRICS.Length - 1; }
            UiSetBitCount(S.ev.Count + S.les.Count);
            if (n == 2) zone2(); else if (n == 3) zone3(); else { zone1(); introZ1(); }
        }
        void restart()
        {
            closeModal(); hideMgbar(); UiDialogShow(false); D.on = false; D.q = new(); D.cur = null; D.done = null; D.typing = false;
            S.mode = "title"; S.zone = 0; S.f = new Flags(); S.ev = new(); S.mstage = 0; S.les = new(); S.ents = new(); S.duel = null; S.pz = null; S.soko = null; S.tiles = null; resetPlayer(70, 310);
            S.cat.petN = 0; UiSetBitCount(0); showTitle();
        }

        /// <summary>setTimeout de game.js.</summary>
        void after(double ms, Action a) => GetTree().CreateTimer(ms / 1000.0).Timeout += a;
    }
}
