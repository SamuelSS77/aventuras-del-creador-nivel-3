using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventurasNivel3
{
    public class Soko
    {
        public HashSet<string> walls; public List<(int x, int y)> tg; public List<(int x, int y)> bl; public int px, py;
        public List<SokoSnap> hist = new(); public SokoSnap init; public bool done, warned, won, hintOn; public SokoSnap prev; public double tw = 1, cut, face = 1, t0; public int resets;
    }
    public class SokoSnap { public int px, py; public List<(int x, int y)> bl; }
    public class Metric { public int ok, min, v; }

    public partial class Game
    {
        // ---------------- Zonas ----------------
        void zone1()
        {
            S.zone = 1; resetPlayer(70, 310); S.fade = 1;
            S.ents = new List<Ent>
            {
                new Ent { id = "rosa", kind = "npc", ck = "rosa", x = 532, y = 338, r = 15, label = "Doña Rosa", cart = true, bump = true, seed = 1, act = talkRosa, mark = () => S.f["i_rosa"] ? "" : "?" },
                new Ent { id = "mario", kind = "npc", ck = "mario", x = 330, y = 252, r = 16, label = "Don Mario", seed = 2, act = talkMario, mark = () => S.f["i_mario"] ? "" : "?" },
                new Ent { id = "tito", kind = "npc", ck = "tito", x = 452, y = 388, r = 12, label = "Tito", seed = 3, act = talkTito, mark = () => S.f["i_tito"] ? "" : "?" },
                new Ent { id = "adivino", kind = "npc", ck = "adivino", x = 488, y = 248, r = 19, label = "El Adivino", seed = 4, act = talkAdivZ1, mark = () => "" },
                new Ent { id = "muro", kind = "obj", x = 621, y = 310, r = 52, label = "", verb = "Reacomodar el muro", act = useWall, hidden = () => S.f["wall"], mark = () => interviews() >= 3 ? "!" : "" }
            };
        }
        void zone1After()
        {
            foreach (var e in S.ents)
            {
                if (e.id == "rosa") { e.bump = false; e.x = 830; e.y = 300; }
                if (e.id == "mario") { e.x = 852; e.y = 232; }
                if (e.id == "tito") { e.x = 445; e.y = 232; }
            }
        }
        void zone2()
        {
            S.zone = 2; resetPlayer(40, 310); S.mode = "world"; S.fade = 1; S.mstage = 0;
            S.ents = new List<Ent>
            {
                new Ent { id = "adivino", kind = "npc", ck = "adivino", x = 690, y = 238, r = 19, label = "El Adivino", seed = 4, act = talkAdivZ2, mark = () => "" },
                new Ent { id = "taq", kind = "npc", ck = "taq", x = 180, y = 396, r = 15, label = "Jefe de estación", verb = "Hablar con el jefe de estación", seed = 5, act = talkTaq, mark = () => S.f["metricas"] ? "" : "?" },
                new Ent { id = "tablero", kind = "obj", x = 250, y = 240, r = 24, label = "Tablero de rutas", verb = "Usar el Tablero de rutas", act = useTablero, mark = () => S.f["tiles"] ? "" : (S.f["metricas"] ? "!" : "") },
                new Ent { id = "rosa", kind = "npc", ck = "rosa", x = 440, y = 322, r = 15, label = "Doña Rosa", cart = true, seed = 1, circle = new CircleMove { cx = 440, cy = 322, rad = 95, spd = .016 }, act = talkRosaZ2, mark = () => "" },
                new Ent { id = "ernesto", kind = "npc", ck = "ernesto", x = 500, y = 312, r = 15, label = "Don Ernesto", seed = 6, circle = new CircleMove { cx = 500, cy = 312, rad = 140, spd = .026 }, act = talkErnestoZ2, mark = () => S.f["ernesto"] ? "" : "?" }
            };
            updObj();
            talk(new List<DItem>
            {
                L("taq", "¡Creador! La Estación colapsó: los buses del Valle y del Bosque van repletos y los del Mirador salen vacíos."),
                L("adivino", "¿Colapsada? ¡Tonterías! Mi Estación es perfecta: todas las rutas llevan a mi mirador, la mejor vista del pueblo. ¿Quién no querría verla?"),
                L("piensa", "El jefe de estación dice que lleva la cuenta de cada viaje. Antes de opinar, quiero ver esos números.")
            }, updObj);
        }
        void zone3()
        {
            S.zone = 3; S.mode = "plaza"; S.fade = 1; S.ents = new();
            S.duel = new Duel();
            S.pz = new Pz { phase = "intro", stress = 0, t = 0, paused = true };
            resetPlayer(480, 532); S.p.face = 1;
            updObj();
            talk(new List<DItem> { L("piensa", "Ahí está el Adivino, frente a todo el pueblo. Es momento de decirle lo que noté en mi recorrido.") }, startDuel);
        }

        // ---------------- Tiempo 1: entrevistas ----------------
        void introZ1()
        {
            S.mode = "world";
            talk(new List<DItem>
            {
                L("piensa", "Llegamos, " + CAT_NAME + ". Pueblo nuevo, proyecto nuevo."),
                L("boceto", "Miau."),
                L("adivino", "¡Bienvenido, Creador! Soy el Adivino y diseñé este pueblo sin preguntarle a nadie. ¡Como este muro monumental! Señora, ¿verdad que ama mi muro?"),
                L("rosa", "Eh… sí, claro. Muy bonito."),
                L("piensa", "Esa pregunta solo busca un sí."),
                Fn(() => addLes("A")),
                L("adivino", "Te reto: despeja la calle. ¡Mi bola dice que fracasarás!"),
                L("piensa", "No voy a adivinar. Primero hablo con los que tienen «?».")
            }, () => { updObj(); toast(CAT_NAME + " te acompaña. Si no sabes qué hacer, te mostrará el camino."); });
        }
        void talkRosa()
        {
            if (S.f["wall"]) { talk(L("rosa", "¡Derechito y sin piedras hasta el mercado! Gracias por preguntar antes de mover nada.")); return; }
            if (S.f["i_rosa"]) { talk(L("rosa", "Si el centro de la calle queda libre, yo paso con mi carrito sin problema.")); return; }
            Func<DItem> q = null;
            q = () => Q("¿Qué le preguntas a Doña Rosa?",
                C("«¿Verdad que el muro es bonito?»", L("rosa", "Pues… sí, supongo.", Tag("bad", "Pregunta que sugiere la respuesta")), L("piensa", "Así solo oigo un sí. Mejor una pregunta abierta."), Fn(() => D.q.Insert(0, q()))),
                C("«¿Por qué un puente?»",
                    L("rosa", "Tito se trepa el muro, pero yo con el carrito no puedo, y por el bosque se atasca. Antes iba derechito por el centro de la calle, que es plano.", Tag("good", "Preguntar «por qué» revela el problema")),
                    L("piensa", "Pidió un puente, pero lo que necesita es un paso plano por el centro. Lo marco en el mapa."),
                    Fn(() => { S.f["i_rosa"] = true; addEv("rosa"); addLes("B"); updObj(); })),
                C("«Listo, le construyo un puente.»", L("piensa", "Un puente tendría escalones. Primero debo entender por qué lo pide.", Tag("bad", "Aceptar la solución sin entender el problema")), Fn(() => D.q.Insert(0, q())))
            );
            talk(L("rosa", "Ay, joven. Llevo toda la mañana intentando llegar al mercado con mi carrito. ¿No puede poner un puente bonito por encima del muro?"), q());
        }
        void talkMario()
        {
            if (S.f["wall"]) { talk(L("mario", "¡Mi puesto se ve desde toda la calle! Ya me están llegando clientes.")); return; }
            if (S.f["i_mario"]) { talk(L("mario", "Usted mueva lo que quiera, pero no me tape el frente del puesto, ¿eh?")); return; }
            Func<DItem> q = null;
            q = () => Q("¿Qué le preguntas a Don Mario?",
                C("«¿Qué cambió para usted desde que pusieron el muro?»",
                    L("mario", "Mi puesto quedó del otro lado y nadie sabe que sigue abierto. Y el Adivino quiere apilar bloques frente a mi puesto: ¡si lo tapan, nadie me encuentra!", Tag("good", "Pregunta abierta sobre su experiencia")),
                    Fn(() => { S.f["i_mario"] = true; addEv("mario"); updObj(); }),
                    L("piensa", "Lo marco en el mapa: su puesto no se puede tapar.")),
                C("«¿No cree que el pueblo necesitaba un monumento?»", L("mario", "Si usted lo dice…", Tag("bad", "Pregunta que sugiere la respuesta")), L("piensa", "Le metí la respuesta en la pregunta."), Fn(() => D.q.Insert(0, q())))
            );
            talk(L("mario", "¡Frutas frescas…! Bueno, frescas eran hace tres días. Desde el muro casi nadie llega."), q());
        }
        void talkTito()
        {
            if (S.f["wall"]) { talk(L("tito", "¡Ese bloque en el rincón es ahora el mejor escondite del pueblo!")); return; }
            if (S.f["i_tito"]) { talk(L("tito", "En los rincones entre las casas no pasa nadie. ¡Palabra de experto en escondidas!")); return; }
            Func<DItem> q = null;
            q = () => Q("¿Qué le preguntas a Tito?",
                C("«¿Verdad que trepar el muro es divertido?»", L("tito", "¡Sí, es lo máximo!", Tag("bad", "Pregunta cerrada: ya sabías la respuesta")), L("piensa", "Eso ya lo sabía. No aprendí nada."), Fn(() => D.q.Insert(0, q()))),
                C("«Tú que juegas por todo el pueblo, ¿hay lugares que nadie use?»",
                    L("tito", "¡Claro! Los rincones entre las casas. Ahí no pasa nadie, solo nosotros cuando jugamos a las escondidas.", Tag("good", "Pregunta abierta sobre cómo se usa el lugar")),
                    L("piensa", "Quien lo vive a diario lo sabe: ahí guardo los bloques sin estorbar. Lo marco en el mapa."),
                    Fn(() => { S.f["i_tito"] = true; addEv("tito"); updObj(); }))
            );
            talk(L("tito", "¡Hola! ¿Viste? Yo sí paso el muro: me trepo como gato."), L("boceto", "¿Miau?"), L("tito", "¡Jaja! Bueno, casi como tu gato."), q());
        }
        void talkAdivZ1()
        {
            if (S.f["wall"]) { talk(L("adivino", "Hmph. Nos vemos en la Estación, Creador.")); return; }
            talk(L("adivino", interviews() < 3 ? "¿Hablando con la gente? Qué pérdida de tiempo. Mi bola ya lo sabe todo." : "¿Y ahora qué? Mi bola dice que vas a tapar algo importante."));
        }
        void useWall()
        {
            int n = interviews();
            if (n < 3) { talk(new List<DItem> { L("piensa", $"Si muevo bloques ahora, estaría adivinando. Llevo {n} de 3 entrevistas.") }, () => catGuide(null, "¡Miau! Falta hablar con alguien.")); return; }
            startSoko();
        }

        // ---------------- Minijuego 1: Sokoban ----------------
        static readonly string[] SOKO_MAP = { "####n####n###", "#.....B.....#", "#....PB.....#", "#.....B.....#", "######n######" };
        static readonly (int x, int y)[] STALL = { (10, 1), (11, 1) };
        (double cs, int cols, int rows, double ox, double oy) sokoGeom() { double cs = 60; return (cs, 13, 5, (W - 13 * cs) / 2, 206); }
        void sokoHelp(bool first)
        {
            var m = NewModal();
            m.Eyebrow("Minijuego 1 · Tiempo 1"); m.H2("Reacomoda el muro");
            m.P("Despeja la calle <b>sin crear otro problema</b>. Lo que te contaron está marcado en el tablero.");
            m.P("<b>Cómo se juega:</b> muévete con las flechas o WASD y empuja los 3 bloques hasta los rincones. Solo puedes empujar, no jalar. Si un bloque se atasca, <b>Z</b> deshace y <b>R</b> reinicia.", "how");
            m.Row(m.Btn(first ? "Empezar" : "Volver al tablero", true, closeModal));
            m.Show();
        }
        void startSoko()
        {
            var walls = new HashSet<string>(); var tg = new List<(int, int)>(); var bl = new List<(int, int)>(); int px = 0, py = 0;
            for (int y = 0; y < SOKO_MAP.Length; y++) for (int x = 0; x < SOKO_MAP[y].Length; x++)
                {
                    char ch = SOKO_MAP[y][x];
                    if (ch == '#') walls.Add(x + "," + y); else if (ch == 'B') bl.Add((x, y)); else if (ch == 'P') { px = x; py = y; } else if (ch == 'n') tg.Add((x, y));
                }
            S.soko = new Soko { walls = walls, tg = tg, bl = bl, px = px, py = py, init = new SokoSnap { bl = bl.ToList(), px = px, py = py }, done = false, prev = null, tw = 1, cut = 0, face = 1, resets = 0, t0 = S.t, hintOn = false };
            S.mode = "soko"; showMgbar("soko"); sokoHud(); sokoHelp(true);
        }
        bool sokoWall(Soko k, int x, int y) => x < 0 || y < 0 || x >= 13 || y >= 5 || k.walls.Contains(x + "," + y);
        bool stallBlocked(Soko k) => k.bl.Any(b => STALL.Any(s => b.x == s.x && b.y == s.y));
        int stored(Soko k) => k.bl.Count(b => k.tg.Any(t => t.x == b.x && t.y == b.y));
        void sokoHud()
        {
            var k = S.soko; if (k == null) return; int n = stored(k); bool lane = !k.bl.Any(b => b.y == 2), sb = stallBlocked(k);
            hudSet("Minijuego 1 · Reacomoda el muro", new List<HudItem> { new() { t = $"Guarda los bloques en los rincones ({n}/3)", done = n == 3 }, new() { t = "Deja libre el centro de la calle", done = lane, plain = !lane }, new() { t = sb ? "¡Estás tapando el puesto de Don Mario!" : "No tapes el puesto de Don Mario", warn = sb, plain = true } });
        }
        void sokoMove(int dx, int dy)
        {
            var k = S.soko; if (k == null || k.done || S.modal != null || D.on) return;
            int nx = k.px + dx, ny = k.py + dy; if (dx != 0) k.face = dx;
            if (sokoWall(k, nx, ny)) return;
            int bi = k.bl.FindIndex(b => b.x == nx && b.y == ny);
            var snap = new SokoSnap { px = k.px, py = k.py, bl = k.bl.ToList() };
            if (bi >= 0)
            {
                int bx = nx + dx, by = ny + dy;
                if (sokoWall(k, bx, by) || k.bl.Any(b => b.x == bx && b.y == by)) return;
                k.bl[bi] = (bx, by);
            }
            k.hist.Add(snap); k.prev = snap; k.tw = 0; k.px = nx; k.py = ny;
            bool wasBlocked = k.warned; k.warned = stallBlocked(k);
            if (k.warned && !wasBlocked) toast("Así tapas el puesto de Don Mario. Eso era justo lo que no quería.");
            sokoHud();
            if (stored(k) == 3) { k.done = true; k.cut = 0; toast("¡Calle despejada! Mira a Doña Rosa."); S.cat.rub = 2; catHearts(4); }
        }
        void sokoUndo() { var k = S.soko; if (k == null || k.done || k.hist.Count == 0) return; var h = k.hist[^1]; k.hist.RemoveAt(k.hist.Count - 1); k.px = h.px; k.py = h.py; k.bl = h.bl; k.prev = null; k.tw = 1; k.warned = stallBlocked(k); sokoHud(); }
        void sokoReset() { var k = S.soko; if (k == null || k.done) return; k.resets++; k.bl = k.init.bl.ToList(); k.px = k.init.px; k.py = k.init.py; k.hist = new(); k.prev = null; k.tw = 1; k.warned = false; sokoHud(); }
        void sokoHint()
        {
            var k = S.soko; if (k == null || k.done) return; bool has(int x, int y) => k.bl.Any(b => b.x == x && b.y == y);
            catSay("¡Miau!", 1.6); catHearts(1, "note"); toast(CAT_NAME + ": " + (has(6, 2) && has(6, 3) ? "mueve el bloque del medio a un lado para llegar al de abajo." : "ponte junto a un bloque y empújalo hacia un rincón punteado."));
        }
        void sokoWin()
        {
            hideMgbar(); S.f["wall"] = true; S.soko = null; S.mode = "world"; zone1After(); updObj(); S.fade = 1;
            talk(new List<DItem>
            {
                L("piensa", "Bloques guardados, calle libre y el puesto de Don Mario a la vista."),
                L("rosa", "¡Derechito y sin piedras! Gracias por preguntar antes de mover nada."),
                L("adivino", "Hmph. Suerte de principiante. Mi bola dice que en la Estación vas a fracasar.")
            }, () => card(1));
        }
        void drawSoko()
        {
            var k = S.soko; var g = sokoGeom(); double cs = g.cs, ox = g.ox, oy = g.oy;
            ctx.DrawImage(grassCanvas(), 0, 0);
            cobbleFill(ox, oy + cs, 13 * cs, 3 * cs);
            curb(ox, oy + cs - 6, 13 * cs, 8); curb(ox, oy + 4 * cs - 2, 13 * cs, 8);
            foreach (var t in k.tg) nookPatch(ox + t.x * cs + 3, oy + t.y * cs + 3, cs - 6, cs - 6);
            house3q(ox - 6, oy - 112, 4 * cs + 2, 112 + cs, "#c8553d", null, true);
            house3q(ox + 5 * cs + 4, oy - 104, 4 * cs - 8, 104 + cs, "#5c6f8c");
            marketStall(ox + 10 * cs + 2, oy - 96, 3 * cs + 6, 96 + cs);
            txt("DON MARIO", ox + 11.5 * cs + 5, oy - 99, "700 13px " + FONT_D, OUT);
            hedgeRow(ox - 6, oy + 4 * cs + 8, 6 * cs + 4, cs - 16); hedgeRow(ox + 7 * cs + 2, oy + 4 * cs + 8, 6 * cs + 4, cs - 16);
            foreach (var a in new[] { new[] { ox + 40, oy + 5 * cs + 50, 26 }, new[] { ox + 210, oy + 5 * cs + 60, 22 }, new[] { ox + 600, oy + 5 * cs + 52, 26 }, new[] { ox + 760, oy + 5 * cs + 62, 22 } }) tree(a[0], a[1], a[2]);
            // laterales: barriles a la izquierda, entrada al mercado a la derecha
            foreach (var (bx0, by0) in new[] { (0, 1), (0, 3) })
            {
                double X = ox + bx0 * cs + cs / 2, Y = oy + by0 * cs + cs / 2;
                ellipse(X, Y + 18, 18, 6, "rgba(20,15,30,.25)"); rr(X - 16, Y - 20, 32, 38, 8, "#9b6a43", OUT, 2.5); ctx.FillStyle = "#6e4a2e"; ctx.FillRect(X - 16, Y - 8, 32, 4); ctx.FillRect(X - 16, Y + 6, 32, 4);
            }
            rr(ox + 12 * cs + 4, oy + cs, cs - 8, 3 * cs, 10, "#dcc69c", OUT, 3); rr(ox + 12 * cs + 12, oy + cs + 30, cs - 24, 3 * cs - 60, new double[] { 18, 18, 4, 4 }, "#8a5a3b", OUT, 2.5);
            // capas de conocimiento
            laneOverlay(ox + cs + 4, oy + 2 * cs + 8, 11 * cs - 8, cs - 16);
            foreach (var st in STALL) hatch(ox + st.x * cs + 3, oy + st.y * cs + 3, cs - 6, cs - 6);
            foreach (var t in k.tg) dashed(ox + t.x * cs + 5, oy + t.y * cs + 5, cs - 10, cs - 10);
            tag("Rincón libre · Tito", ox + 4.5 * cs, oy - 12, COL.evid); tag("Rincón libre · Tito", ox + 9.5 * cs, oy - 12, COL.evid); tag("Rincón libre · Tito", ox + 6.5 * cs, oy + 5 * cs + 18, COL.evid);
            tag("No tapar · Mario", ox + 11 * cs, oy + cs * 1.5, COL.warn);
            tag("Paso plano · Rosa", ox + 3 * cs, oy + 2.5 * cs, COL.evid);
            // bloques y jugador con animación
            double ease(double x) => 1 - Math.Pow(1 - x, 3); double p = ease(Math.Min(1, k.tw));
            for (int i = 0; i < k.bl.Count; i++)
            {
                var b = k.bl[i]; var pb = k.prev != null && i < k.prev.bl.Count ? k.prev.bl[i] : b;
                double X = ox + (pb.x + (b.x - pb.x) * p) * cs, Y = oy + (pb.y + (b.y - pb.y) * p) * cs;
                bool onT = k.tg.Any(t => t.x == b.x && t.y == b.y), onS = STALL.Any(s => b.x == s.x && b.y == s.y);
                stoneBlock(X + 4, Y + 4, cs - 8, onT ? "ok" : (onS ? "warn" : ""));
            }
            var pp = k.prev != null ? (x: k.prev.px, y: k.prev.py) : (x: k.px, y: k.py);
            double PX = ox + (pp.x + (k.px - pp.x) * p + 0.5) * cs, PY = oy + (pp.y + (k.py - pp.y) * p + 0.5) * cs;
            drawCreature("mario", ox + 11 * cs, oy - 12, 13, new CO { seed = 2, look = new double[] { -1, 1 } });
            if (k.done)
            {
                double c = Math.Min(1, k.cut), rx = ox + 0.5 * cs + c * 11.6 * cs, ry = oy + 2.5 * cs;
                drawCart(rx + 24, ry + 2, 14); drawCreature("rosa", rx, ry, 15, new CO { seed = 1, moving = c < 1, look = new double[] { 1, 0 } });
                if (c >= 1) bubble("¡Llegué al mercado!", rx, ry - 26, COL.evid);
            }
            else { drawCart(ox + 0.5 * cs + 24, oy + 2.5 * cs + 2, 14); drawCreature("rosa", ox + 0.5 * cs, oy + 2.5 * cs, 15, new CO { seed = 1, look = new double[] { 1, 0 } }); }
            drawCreature("player", PX, PY, 17, new CO { seed = 0, moving = k.tw < 1, look = new[] { k.face, 0 } });
            drawCatAt(); drawCatOverlay();
            if (k.warned && !k.done) { rr(W / 2 - 230, oy - 64, 460, 34, 10, "rgba(210,72,58,.95)", OUT, 2.5); txt("Ese bloque tapa el puesto de Don Mario. Muévelo a un rincón.", W / 2, oy - 47, "700 14px " + FONT, COL.white); }
        }

        // ---------------- Tarjetas de cierre ----------------
        void card(int n)
        {
            var m = NewModal();
            if (n == 1)
            {
                m.Eyebrow("Fin del Tiempo 1 · Entrevistar"); m.H2("Predicción contra realidad");
                m.Pred(false, "La bola del Adivino dijo", "El pueblo quería un monumento grandioso.");
                m.Pred(true, "Lo que descubriste", "Doña Rosa necesitaba un paso plano, Don Mario que su puesto se viera y Tito sabía dónde guardar los bloques. Lo supiste preguntando.");
                m.H3("Anotado en tu Bitácora"); m.LesList(new[] { "A", "B" }.Select(l => LESSONS[l]), false);
                m.Row(m.Btn("Seguir hacia la Estación", true, () => { closeModal(); updObj(); }));
            }
            else
            {
                m.Eyebrow("Fin del Tiempo 2 · Medir y probar"); m.H2("Predicción contra realidad");
                m.Pred(false, "La bola del Adivino dijo", "Mi Estación es perfecta. Si la gente se pierde, es porque no sabe leer mis flechas.");
                m.Pred(true, "Lo que descubriste", "8 de cada 10 rutas iban a un lugar al que casi nadie quería ir. Al cambiar las rutas y medir otra vez, los números lo confirmaron:");
                m.BeforeAfter(new[] { ("Llegan a su destino", "23 de 100", "98 de 100"), ("Tiempo del viaje", "38 min", "9 min"), ("Vueltas antes de llegar", "3", "0") });
                m.H3("Anotado en tu Bitácora"); m.LesList(new[] { "C", "M", "D" }.Select(l => LESSONS[l]), false);
                m.Row(m.Btn("Seguir hacia la Plaza", true, () => { closeModal(); zone3(); }));
            }
            m.Show();
        }

        // ---------------- Tiempo 2: diálogos ----------------
        static readonly Metric[] METRICS = { new() { ok = 23, min = 38, v = 3 }, new() { ok = 62, min = 19, v = 1 }, new() { ok = 98, min = 9, v = 0 } };
        void showMetrics(int stage, Action nextA)
        {
            var mt = METRICS[stage];
            var m = NewModal();
            m.Eyebrow("Tablero de la Estación"); m.H2("Así funciona la Estación hoy");
            m.Metrics(new[]
            {
                ("Llegan a su destino", $"{mt.ok} de cada 100", (string)null, mt.ok / 100.0),
                ("Tiempo promedio del viaje", $"{mt.min} min", "(debería ser 8)", jsRound(mt.min / 40.0 * 100) / 100.0),
                ("Vueltas antes de llegar", $"{mt.v} por persona", null, jsRound(mt.v / 3.0 * 100) / 100.0)
            });
            m.H3("Rutas contra pasajeros");
            m.P("<span class=\"purple\">Morado:</span> cuántas rutas van a cada lugar. <span class=\"green\">Verde:</span> cuántos pasajeros quieren ir ahí.", "small");
            m.Dist(new[] { ("Mirador", 80, 2), ("Valle", 10, 58), ("Bosque", 10, 40) });
            m.Row(m.Btn("Entendido", true, () => { closeModal(); nextA?.Invoke(); }));
            m.Show();
        }
        void afterMetrics()
        {
            Func<DItem> q = null;
            q = () => Q("¿Qué dicen realmente estos números?",
                C("«La gente no sabe usar la Estación.»", L("adivino", "¡Exacto! ¡Eso mismo digo yo!", Tag("bad", "Culpar al usuario")), L("piensa", "Espera… eso es justo lo que dice el Adivino. Si 77 de cada 100 personas fallan en lo mismo, no puede ser culpa de todas ellas."), Fn(() => D.q.Insert(0, q()))),
                C("«Si 77 de cada 100 se pierden, el problema no es la gente: es el diseño.»",
                    L("creador", "Si 77 de cada 100 se pierden, el problema no es la gente: es el diseño.", Tag("good", "Leer los datos sin culpar al usuario")),
                    L("creador", "Y mire esto: 8 de cada 10 rutas van al mirador, pero solo 2 de cada 100 pasajeros quieren ir allá. Las rutas están donde nadie las necesita."),
                    L("taq", "¡Eso le he dicho mil veces!"),
                    Fn(() => { S.f["metricas"] = true; addEv("metricas"); addLes("C"); addLes("M"); updObj(); }),
                    L("adivino", "Hmph. Mi bola dice otra cosa. Demuéstralo: cambia las rutas, a ver si tus numeritos mejoran."),
                    L("piensa", "Eso haré. Cambio las rutas, las pruebo con la gente y vuelvo a medir. Si el tablero mejora, sabré que funcionó."))
            );
            talk(new List<DItem>
            {
                L("taq", "De cada 100 personas, solo 23 llegan a su destino. Las demás dan vueltas hasta perder el bus."),
                L("adivino", "¡Esos números están mal! Mi bola dice que todos aman mi mirador. Si se pierden, es porque no saben leer mis flechas."),
                q()
            }, updObj);
        }
        void talkTaq()
        {
            if (S.f["tiles"]) { talk(L("taq", $"El tablero no miente: {METRICS[2].ok} de cada 100 llegan a su destino. ¡Gracias, Creador!")); return; }
            if (S.f["metricas"]) { talk(L("taq", "Ya vio los números. Ahora hay que arreglar esas rutas en el Tablero de rutas.")); return; }
            talk(new List<DItem>
            {
                L("taq", "Usted es quien despejó la calle, ¿verdad? Necesito ayuda: la Estación colapsó."),
                L("taq", "Llevo la cuenta de cada viaje en mi tablero. Mire.")
            }, () => showMetrics(0, afterMetrics));
        }
        void talkRosaZ2()
        {
            if (S.f["tiles"]) { talk(L("rosa", "¡Por fin voy en camino a ver a mi nieta!")); return; }
            talk(L("rosa", "Ay, joven, otra vez usted. Voy al bus del Valle a visitar a mi nieta, pero las flechas me mandan hacia el mirador… por unas escaleras. Con el carrito no puedo."));
        }
        void talkErnestoZ2()
        {
            if (S.f["tiles"]) { talk(L("ernesto", "Llegué en nueve minutos. …Gracias.")); return; }
            if (S.f["ernesto"]) { talk(L("ernesto", "Ya le respondí. Voy tarde.")); return; }
            Func<DItem> q = () => Q("Don Ernesto no se detiene. Tienes una sola pregunta:",
                C("«¿Verdad que le encanta el mirador?»", L("ernesto", "No.", Tag("bad", "Pregunta cerrada a alguien con prisa")), L("piensa", "Una palabra y se fue. Con alguien que no tiene tiempo, cada pregunta cuenta."), Fn(() => { S.f["ernesto"] = true; })),
                C("«¿Cuánto se demora en llegar a su bus?»",
                    L("ernesto", "Treinta y ocho minutos. Debería ser ocho. Tres vueltas cada mañana. ¿Algo más? No. Adiós.", Tag("good", "Pregunta concreta sobre su experiencia")),
                    Fn(() => { S.f["ernesto"] = true; addEv("ernesto"); }),
                    L("piensa", "Frío, pero me dio datos: 38 minutos y 3 vueltas. Quiero saber si a los demás les pasa lo mismo."))
            );
            talk(L("ernesto", "No tengo tiempo."), q());
        }
        void talkAdivZ2()
        {
            if (S.f["tiles"]) { talk(L("adivino", "…Entonces el problema eran mis rutas, no la gente.")); return; }
            if (S.f["metricas"]) { talk(L("adivino", "Mi bola dice que tus numeritos no van a cambiar.")); return; }
            talk(L("adivino", "Mis flechas son perfectas. Si la gente se pierde, que aprenda a leer."));
        }
        void useTablero()
        {
            if (S.f["tiles"]) { talk(L("piensa", "Las rutas ya funcionan, y el tablero lo confirma.")); return; }
            if (!S.f["metricas"]) { talk(new List<DItem> { L("piensa", "Antes de cambiar rutas quiero entender qué está fallando y cuánto. El jefe de estación tiene los números.") }, () => catGuide(null, "¡Miau! Él tiene los números.")); return; }
            startTiles();
        }
    }
}
