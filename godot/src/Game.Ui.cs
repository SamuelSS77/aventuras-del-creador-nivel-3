using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static AventurasNivel3.Ui;

namespace AventurasNivel3
{
    public class PersonaField { public string legend, okText, okSrc; public List<(string t, bool bad)> opts; public Action<int> pick; }

    public partial class Game
    {
        // ---- Nodos de la interfaz (equivalen a los elementos de index.html) ----
        CanvasView stageView, portraitView;
        PanelContainer hudPanel, dlgPanel, toastPanel, mgbar, dlgNamePill, dlgTagPill;
        Label hudTime, dlgName, dlgText, dlgHint, dlgTag, toastLbl, bitCountLbl;
        VBoxContainer hudList, dlgChoices;
        ClickPanel btnBit;
        ColorRect modalOverlay;
        PanelContainer modalCard;
        ScrollContainer modalScroll;
        VBoxContainer modalContent;
        Action modalPrimary;
        Button bHint, bTilesHint;
        RichTextLabel mgStatus;
        bool hudCompact = false, hudShown = true, hintBlink;
        double blinkT;
        int toastToken;
        string bitPanelKind;

        // ---------------- Construcción ----------------
        void BuildUi()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture; CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var theme = new Theme { DefaultFont = Fonts.Body, DefaultFontSize = 15 };
            Theme = theme;
            MouseFilter = MouseFilterEnum.Ignore;

            // #app: columna centrada (padding 16 a los lados, 14 arriba)
            var app = VBox(10);
            app.Position = new Vector2(17, 14); app.Size = new Vector2(966, 0); app.MouseFilter = MouseFilterEnum.Ignore;
            AddChild(app);

            // .stage: 960x600 con borde de 3 px, esquinas redondeadas y anillo exterior
            var stageHolder = new Control { CustomMinimumSize = new Vector2(966, 606), MouseFilter = MouseFilterEnum.Ignore };
            app.AddChild(stageHolder);
            var ring = new Panel { Position = new Vector2(-3, -3), Size = new Vector2(972, 612), MouseFilter = MouseFilterEnum.Ignore };
            var ringBox = Box(night3, null, 0, 19); ringBox.ShadowColor = new Color(0, 0, 0, 0.35f); ringBox.ShadowSize = 30; ringBox.ShadowOffset = new Vector2(0, 10);
            ring.AddThemeStyleboxOverride("panel", ringBox);
            stageHolder.AddChild(ring);
            var stage = new Panel { Position = Vector2.Zero, Size = new Vector2(966, 606), MouseFilter = MouseFilterEnum.Ignore, ClipChildren = ClipChildrenMode.AndDraw };
            stage.AddThemeStyleboxOverride("panel", Box(Ui.C("#6fae52"), outline, 3, 16));
            stageHolder.AddChild(stage);
            var inner = new Control { Position = new Vector2(3, 3), Size = new Vector2(960, 600), MouseFilter = MouseFilterEnum.Ignore, ClipContents = true };
            stage.AddChild(inner);

            stageView = new CanvasView { Size = new Vector2(960, 600), MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.PointingHand };
            stageView.OnDraw = c => { ctx = c; draw(); };
            stageView.GuiInput += ev =>
            {
                if (ev is InputEventMouseButton mb && mb.Pressed && (mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right))
                    OnStagePointer(mb.Position.X, mb.Position.Y, mb.ButtonIndex == MouseButton.Right);
            };
            inner.AddChild(stageView);

            BuildHud(inner);
            BuildBitButton(inner);
            BuildDialog(inner);
            BuildToast(inner);

            // .mgbar
            mgbar = Pnl(Box(night2, night3, 2, 14, 12, 10));
            mgbar.Visible = false;
            app.AddChild(mgbar);

            // .controls
            app.AddChild(Lbl("Moverse: flechas o WASD (o clic en el suelo) · Hablar y continuar: E o Espacio · Bitácora: B", Fonts.Body, 13, muted));

            BuildModal();
        }

        void BuildOffscreens()
        {
            Texture2D Off(Action fn, int w, int h)
            {
                var vp = new SubViewport { Size = new Vector2I(w, h), TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Always, Msaa2D = Viewport.Msaa.Msaa4X, Disable3D = true };
                var cv = new CanvasView { Size = new Vector2(w, h) };
                cv.OnDraw = c => { var save = ctx; ctx = c; fn(); ctx = save; };
                vp.AddChild(cv); AddChild(vp);
                // Se dibujan unos cuadros y luego se congelan, como un canvas fuera de pantalla.
                GetTree().CreateTimer(0.5).Timeout += () => vp.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
                return vp.GetTexture();
            }
            GRASS = Off(drawGrassOffscreen, 960, 600);
            COBBLE = Off(drawCobbleOffscreen, 64, 64);
            BGZ1 = Off(drawBgZ1Offscreen, 960, 600);
        }

        // ---------------- HUD ----------------
        void BuildHud(Control inner)
        {
            hudPanel = Pnl(Box(new Color(42 / 255f, 34 / 255f, 56 / 255f, 0.9f), outline, 2, 12, 12, 7, new Color(0, 0, 0, 0.25f), 3).Pad(12, 7, 12, 8));
            hudPanel.Position = new Vector2(10, 10); hudPanel.MouseFilter = MouseFilterEnum.Ignore;
            hudTime = Lbl("", Fonts.Display700, 15, highlight);
            hudList = VBox(2); hudList.MouseFilter = MouseFilterEnum.Ignore;
            var col = VBox(4, hudTime, hudList); col.MouseFilter = MouseFilterEnum.Ignore;
            hudPanel.AddChild(col);
            hudPanel.Visible = false;
            inner.AddChild(hudPanel);
        }
        List<(string cls, string t)> hudRows = new();
        void UiHudTime(string t) { hudTime.Text = t; FitHud(); }
        void UiHudList(List<(string, string)> rows)
        {
            hudRows = rows;
            foreach (var c in hudList.GetChildren()) c.QueueFree();
            foreach (var (cls, t) in rows)
            {
                bool done = cls == "done", now = cls == "now", warnC = cls == "warn";
                var box = new Panel { CustomMinimumSize = new Vector2(10.5f, 10.5f), MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkBegin };
                Color bd = warnC ? Ui.C("#ff9c86") : (done ? Ui.C("#7fd8b2") : (now ? highlight : cream));
                Color bg = warnC ? Ui.C("#ff9c86") : (done ? evid : new Color(0, 0, 0, 0));
                box.AddThemeStyleboxOverride("panel", Box(bg, bd, 2, 3));
                var boxWrap = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore }; boxWrap.AddThemeConstantOverride("margin_top", 5); boxWrap.AddChild(box);
                var txtL = Rich(done ? $"[s]{t.Replace("[", "[lb]")}[/s]" : t.Replace("[", "[lb]"), 14, warnC ? Ui.C("#ff9c86") : cream);
                txtL.Text = done ? $"[s]{t.Replace("[", "[lb]")}[/s]" : (now || warnC ? $"[b]{t.Replace("[", "[lb]")}[/b]" : t.Replace("[", "[lb]"));
                var row = HBox(7, boxWrap, txtL); row.MouseFilter = MouseFilterEnum.Ignore;
                row.Modulate = new Color(1, 1, 1, done ? 0.5f : ((now || warnC) ? 1f : 0.72f));
                hudList.AddChild(row);
            }
            FitHud();
        }
        /// <summary>max-width: min(46%, 320px) y ancho según el contenido.</summary>
        void FitHud()
        {
            float w = Fonts.Display700.GetStringSize(hudTime.Text, HorizontalAlignment.Left, -1, 15).X;
            if (!hudCompact) foreach (var (cls, t) in hudRows) w = Math.Max(w, 17.5f + (cls == "now" || cls == "warn" ? Fonts.BodyBold : Fonts.Body).GetStringSize(t, HorizontalAlignment.Left, -1, 14).X);
            w = Math.Min(320 - 24 - 4, w + 1);
            hudPanel.CustomMinimumSize = new Vector2(w + 24 + 4, 0);
            hudTime.CustomMinimumSize = new Vector2(w, 0);
            hudPanel.CallDeferred(Control.MethodName.ResetSize);
        }
        void UiHudCompact(bool c) { if (hudCompact == c) return; hudCompact = c; hudList.Visible = !c; FitHud(); }
        void UiHudVisible(bool v) { if (hudShown == v && hudPanel.Visible == v) return; hudShown = v; hudPanel.Visible = v; btnBit.Visible = v; }

        // ---------------- Botón Bitácora ----------------
        void BuildBitButton(Control inner)
        {
            var n = Box(cream, outline, 2, 12, 10, 4, new Color(0, 0, 0, 0.25f), 3);
            btnBit = new ClickPanel(n, n);
            var icon = new CanvasView { CustomMinimumSize = new Vector2(19.5f, 19.5f) };
            icon.OnDraw = c => { var save = ctx; ctx = c; ctx.Scale(19.5 / 24, 19.5 / 24); rr(4, 3, 15, 18, 2, "#7a4fc9", "#2a2238", 2); ctx.FillStyle = "#2a2238"; ctx.FillRect(7, 3, 2, 18); ctx.StrokeStyle = "#f6efe0"; ctx.LineWidth = 2; ctx.LineCap = "round"; foreach (var (y, x2) in new[] { (8.0, 16.0), (12.0, 16.0), (16.0, 14.0) }) { ctx.BeginPath(); ctx.MoveTo(11, y); ctx.LineTo(x2, y); ctx.Stroke(); } ctx.LineCap = "butt"; ctx = save; };
            var lbl = Lbl("Bitácora", Fonts.Display700, 15, outline);
            bitCountLbl = Lbl("0", Fonts.BodyBold, 12, white); bitCountLbl.HorizontalAlignment = HorizontalAlignment.Center; bitCountLbl.CustomMinimumSize = new Vector2(10, 0);
            var pill = Pnl(Box(evid, null, 0, 999, 6, 0), bitCountLbl); pill.CustomMinimumSize = new Vector2(22, 0); pill.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            var row = HBox(7, icon, lbl, pill); row.Alignment = BoxContainer.AlignmentMode.Center;
            btnBit.AddChild(row);
            btnBit.OnPressed = openBit;
            btnBit.Visible = false;
            inner.AddChild(btnBit);
            // Anclado arriba a la derecha (right: 10px; top: 10px)
            btnBit.AnchorLeft = 1; btnBit.AnchorRight = 1; btnBit.OffsetRight = -10; btnBit.OffsetTop = 10; btnBit.GrowHorizontal = GrowDirection.Begin;
        }
        void UiSetBitCount(int n) => bitCountLbl.Text = n.ToString();
        void UiPulseBit()
        {
            btnBit.PivotOffset = btnBit.Size / 2;
            var tw = CreateTween();
            btnBit.Scale = Vector2.One;
            tw.TweenProperty(btnBit, "scale", new Vector2(1.14f, 1.14f), 0.245).SetEase(Tween.EaseType.Out);
            tw.TweenProperty(btnBit, "scale", Vector2.One, 0.455).SetEase(Tween.EaseType.Out);
        }

        // ---------------- Diálogo ----------------
        void BuildDialog(Control inner)
        {
            var outer = Box(panel2, outline, 3, 14, 2, 2, new Color(0, 0, 0, 0.3f), 4);
            dlgPanel = Pnl(outer);
            var innerBox = Pnl(Box(panel, null, 0, 12).Pad(12, 8, 12, 6));
            dlgPanel.AddChild(innerBox);
            dlgPanel.MouseFilter = MouseFilterEnum.Stop;
            dlgPanel.GuiInput += ev => { if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left) adv(); };

            var pFrame = Pnl(Box(panel2, Ui.C("#4a3f63"), 2, 12));
            pFrame.CustomMinimumSize = new Vector2(104, 104); pFrame.SizeFlagsVertical = SizeFlags.ShrinkBegin; pFrame.MouseFilter = MouseFilterEnum.Ignore;
            portraitView = new CanvasView { CustomMinimumSize = new Vector2(100, 100) };
            portraitView.Ctx.Base = new Transform2D(100f / 120f, 0, 0, 100f / 120f, 0, 0);
            portraitView.OnDraw = c => { var save = ctx; ctx = c; drawPortrait(); ctx = save; };
            pFrame.AddChild(portraitView);

            dlgName = Lbl("", Fonts.Display700, 14, outline);
            dlgNamePill = Pnl(Box(cream, outline, 2, 999, 10, 0), dlgName); dlgNamePill.MouseFilter = MouseFilterEnum.Ignore;
            dlgTag = Lbl("", Fonts.BodyBold, 12, white);
            dlgTagPill = Pnl(Box(evid, null, 0, 999, 10, 1), dlgTag); dlgTagPill.MouseFilter = MouseFilterEnum.Ignore; dlgTagPill.Visible = false;
            dlgText = Lbl("", Fonts.Body, 19, cream, true); dlgText.CustomMinimumSize = new Vector2(1, 50);
            dlgChoices = VBox(6);
            dlgHint = Lbl("", Fonts.Body, 12, Ui.C("#b9aecf")); dlgHint.HorizontalAlignment = HorizontalAlignment.Right; dlgHint.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var nameRow = HBox(0, dlgNamePill); nameRow.MouseFilter = MouseFilterEnum.Ignore;
            var tagRow = HBox(0, dlgTagPill); tagRow.MouseFilter = MouseFilterEnum.Ignore;
            var col = VBox(5, nameRow, tagRow, dlgText, dlgChoices, dlgHint); col.SizeFlagsHorizontal = SizeFlags.ExpandFill; col.MouseFilter = MouseFilterEnum.Ignore;
            var row = HBox(14, pFrame, col); row.MouseFilter = MouseFilterEnum.Ignore;
            innerBox.AddChild(row); innerBox.MouseFilter = MouseFilterEnum.Ignore;

            dlgPanel.AnchorLeft = 0; dlgPanel.AnchorRight = 1; dlgPanel.AnchorTop = 1; dlgPanel.AnchorBottom = 1;
            dlgPanel.OffsetLeft = 10; dlgPanel.OffsetRight = -10; dlgPanel.OffsetBottom = -10; dlgPanel.OffsetTop = -10;
            dlgPanel.GrowVertical = GrowDirection.Begin;
            dlgPanel.Visible = false;
            inner.AddChild(dlgPanel);
        }
        void UiDialogShow(bool v) { dlgPanel.Visible = v; }
        void UiDialogLine(string name, string nameBg, bool think, string[] tag)
        {
            dlgName.Text = name;
            dlgNamePill.AddThemeStyleboxOverride("panel", Box(Ui.C(nameBg), outline, 2, 999, 10, 0));
            dlgText.AddThemeFontOverride("font", think ? Fonts.BodyItalic : Fonts.Body);
            dlgText.AddThemeColorOverride("font_color", think ? Ui.C("#ddd3f0") : cream);
            dlgTagPill.Visible = tag != null;
            if (tag != null)
            {
                dlgTag.Text = (tag[0] == "good" ? "✓ " : "✗ ") + tag[1];
                dlgTagPill.AddThemeStyleboxOverride("panel", Box(tag[0] == "good" ? evid : adivino, null, 0, 999, 10, 1));
            }
        }
        void UiDialogText(string t) => dlgText.Text = t;
        string hintBase = "";
        void UiDialogHint(string t, bool blink) { hintBase = t; hintBlink = blink; blinkT = 0; dlgHint.Text = blink ? t + " ▼" : t; }
        void UiDialogChoices(List<string> choices)
        {
            foreach (var c in dlgChoices.GetChildren()) c.QueueFree();
            dlgChoices.Visible = choices != null && choices.Count > 0;
            if (choices == null) return;
            for (int i = 0; i < choices.Count; i++)
            {
                int idx = i;
                var n = Box(panel2, Ui.C("#5a4d74"), 2, 10, 12, 7); var h = Box(Ui.C("#463b61"), highlight, 2, 10, 12, 7);
                var cp = new ClickPanel(n, h) { OnPressed = () => pick(idx) };
                var k = Lbl((i + 1).ToString(), Fonts.Display700, 17, outline); k.HorizontalAlignment = HorizontalAlignment.Center;
                var kp = Pnl(Box(highlight, null, 0, 6, 0, 0), k); kp.CustomMinimumSize = new Vector2(24, 0); kp.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                var tl = Lbl(choices[i], Fonts.Body, 17, cream, true);
                cp.AddChild(HBox(10, kp, tl));
                dlgChoices.AddChild(cp);
            }
        }

        // ---------------- Toast ----------------
        void BuildToast(Control inner)
        {
            toastLbl = Lbl("", Fonts.BodyBold, 14, cream);
            toastLbl.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; toastLbl.ClipText = true;
            toastPanel = Pnl(Box(panel, Ui.C("#7fd8b2"), 2, 999, 14, 5, new Color(0, 0, 0, 0.3f), 3), toastLbl);
            toastPanel.MouseFilter = MouseFilterEnum.Ignore; toastPanel.Visible = false;
            inner.AddChild(toastPanel);
        }
        void toast(string m)
        {
            toastLbl.Text = m;
            float w = Math.Min(Fonts.BodyBold.GetStringSize(m, HorizontalAlignment.Left, -1, 14).X + 2, 960 - 24 - 32);
            toastLbl.CustomMinimumSize = new Vector2(w, 0);
            toastPanel.ResetSize();
            toastPanel.Visible = true;
            Callable.From(PlaceToast).CallDeferred();
            int tok = ++toastToken;
            after(3400, () => { if (tok == toastToken) toastPanel.Visible = false; });
        }
        void PlaceToast() { toastPanel.ResetSize(); toastPanel.Position = new Vector2((960 - toastPanel.Size.X) / 2, 12); }

        void UiFrame(double ms)
        {
            if (hintBlink) { blinkT += ms; dlgHint.Text = hintBase + ((int)(blinkT / 500) % 2 == 0 ? " ▼" : "   "); }
        }

        // ---------------- Barra de minijuegos (.mgbar) ----------------
        void setStatus(string h) { if (mgStatus != null && IsInstanceValid(mgStatus)) mgStatus.Text = Bb(h, "#ffcf70"); }
        void hideMgbar() { mgbar.Visible = false; ClearMgbar(); }
        void ClearMgbar() { foreach (var c in mgbar.GetChildren()) { mgbar.RemoveChild(c); c.QueueFree(); } mgStatus = null; bHint = null; bTilesHint = null; }
        RichTextLabel StatusLabel() => Rich("", 15, cream, "#ffcf70");
        Control Swatch(string kind)
        {
            var v = new CanvasView { CustomMinimumSize = new Vector2(22, 16) };
            v.OnDraw = c =>
            {
                var save = ctx; ctx = c;
                if (kind == "lane") rr(1, 1, 20, 14, 5, "rgba(70,190,120,.55)", "#2a8a68", 2);
                else if (kind == "nook") { rr(1, 1, 20, 14, 5, "#c49a66"); ctx.Save(); ctx.SetLineDash(new double[] { 3, 2 }); rr(1, 1, 20, 14, 5, null, "#1f6e52", 2); ctx.Restore(); }
                else { ctx.Save(); rr(1, 1, 20, 14, 5, "#fff3d6"); ctx.Clip(); ctx.StrokeStyle = "#f39c33"; ctx.LineWidth = 2.8; for (double i = -16; i < 24; i += 5.6) { ctx.BeginPath(); ctx.MoveTo(i, 16); ctx.LineTo(i + 16, 0); ctx.Stroke(); } ctx.Restore(); rr(1, 1, 20, 14, 5, null, "#b5482f", 2); }
                ctx = save;
            };
            return v;
        }
        void showMgbar(string kind)
        {
            ClearMgbar(); mgbar.Visible = true;
            var col = VBox(10);
            if (kind == "soko")
            {
                var legend = VBox(5,
                    HBox(8, Swatch("lane"), Lbl("Rosa: el centro de la calle, libre", Fonts.Body, 13, cream)),
                    HBox(8, Swatch("nook"), Lbl("Tito: rincones donde guardar bloques", Fonts.Body, 13, cream)),
                    HBox(8, Swatch("stall"), Lbl("Mario: no tapar su puesto", Fonts.Body, 13, cream)));
                legend.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                var pad = new GridContainer { Columns = 3 }; pad.AddThemeConstantOverride("h_separation", 4); pad.AddThemeConstantOverride("v_separation", 4);
                Button P(string t, int dx, int dy)
                {
                    var b = Btn(t, false, () => sokoMove(dx, dy), true); b.CustomMinimumSize = new Vector2(44, 40);
                    var s = Box(panel2, Ui.C("#5a4d74"), 2, 8, 0, 0); b.AddThemeStyleboxOverride("normal", s); b.AddThemeStyleboxOverride("hover", s); b.AddThemeStyleboxOverride("pressed", s);
                    b.AddThemeFontSizeOverride("font_size", 20); return b;
                }
                pad.AddChild(HSpacer(44)); pad.AddChild(P("↑", 0, -1)); pad.AddChild(HSpacer(44));
                pad.AddChild(P("←", -1, 0)); pad.AddChild(P("↓", 0, 1)); pad.AddChild(P("→", 1, 0));
                col.AddChild(HBox(16, legend, HSpacer(0, true), pad));
                bHint = Btn("Pista de " + CAT_NAME, false, sokoHint, true); bHint.Visible = false;
                col.AddChild(HBox(8, Btn("Deshacer (Z)", false, sokoUndo, true), Btn("Reiniciar (R)", false, sokoReset, true), bHint, Btn("Cómo se juega", false, () => sokoHelp(false), true)));
            }
            else
            {
                var legend = new HFlowContainer(); legend.AddThemeConstantOverride("h_separation", 14); legend.AddThemeConstantOverride("v_separation", 4);
                foreach (var (c, t) in new[] { ("#3fae7c", "Tus flechas"), (COL.adiv, "Mirador y rutas del Adivino"), (COL.evid, "Destino"), (COL.stairs, "Escaleras"), (COL.rock, "Roca") })
                {
                    var sw = Pnl(Box(Ui.C(c), null, 0, 3)); sw.CustomMinimumSize = new Vector2(14, 14); sw.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                    legend.AddChild(HBox(5, sw, Lbl(t, Fonts.Body, 13, cream)));
                }
                col.AddChild(legend);
                bTilesHint = Btn("Pista de " + CAT_NAME, false, tilesHint, true); bTilesHint.Visible = false;
                col.AddChild(HBox(8, Btn("Testear (Enter)", true, testear, true), Btn("Reiniciar puzzle", false, () => { if (S.tiles != null && S.tiles.sim == null) { resetArrows(); setStatus("Puzzle reiniciado. Diseña la ruta y presiona <b>Testear</b>."); } }, true), bTilesHint, Btn("Cómo se juega", false, () => tilesHelp(false), true)));
                mgStatus = StatusLabel(); col.AddChild(mgStatus);
            }
            mgbar.AddChild(col);
        }
        void UiShowSokoHint() { if (bHint != null) bHint.Visible = true; }
        void UiTilesHintVisible(bool v) { if (bTilesHint != null) bTilesHint.Visible = v; }
        bool UiTilesHintIsVisible() => bTilesHint != null && bTilesHint.Visible;

        void pzMgbar()
        {
            var z = S.pz;
            if (z.phase == "strike" || z.phase == "team")
            {
                ClearMgbar(); mgbar.Visible = true;
                string extra = z.phase == "team" ? "¡En equipo! Rosa despeja pasos, Mario hace letreros y Tito ajusta alturas. " : "";
                var col = VBox(10);
                col.AddChild(Rich(extra + "Acércate a un invento y usa los botones (o teclas 1, 2, 3) para responder a su queja.", 15, cream, "#ffcf70"));
                var tools = new HFlowContainer(); tools.AddThemeConstantOverride("h_separation", 8); tools.AddThemeConstantOverride("v_separation", 8);
                foreach (var key in FIX_ORDER)
                {
                    var f = FIX[key]; int k = f.key; var tc = Ui.C(f.c);
                    var cp = new ClickPanel(Box(panel2, tc, 2, 10, 12, 7), Box(Ui.C("#463b61"), tc, 2, 10, 12, 7)) { OnPressed = () => plazaAction(k), CustomMinimumSize = new Vector2(0, 42) };
                    var row = HBox(8, Kbd(k.ToString(), tc, white, outline), Lbl(f.n, Fonts.BodyBold, 14, cream)); row.Alignment = BoxContainer.AlignmentMode.Center;
                    cp.AddChild(row);
                    tools.AddChild(cp);
                }
                col.AddChild(tools);
                mgbar.AddChild(col);
                return;
            }
            string help = z.phase == "endA" ? "Ahora haces las cosas a tu manera…" : null;
            if (help == null) { hideMgbar(); return; }
            ClearMgbar(); mgbar.Visible = true; mgbar.AddChild(Rich(help, 15, cream, "#ffcf70"));
        }

        // ---------------- Ventanas (modal) ----------------
        void BuildModal()
        {
            modalOverlay = new ColorRect { Color = new Color(15 / 255f, 14 / 255f, 26 / 255f, 0.76f), MouseFilter = MouseFilterEnum.Stop, Visible = false };
            modalOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(modalOverlay);
            var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore }; center.SetAnchorsPreset(LayoutPreset.FullRect);
            modalOverlay.AddChild(center);
            var cardBox = Box(paper, outline, 3, 16, 0, 0, new Color(0, 0, 0, 0.35f), 6);
            modalCard = Pnl(cardBox); modalCard.CustomMinimumSize = new Vector2(660, 0); modalCard.ClipChildren = ClipChildrenMode.AndDraw;
            center.AddChild(modalCard);
            var grid = new CanvasView();
            grid.OnDraw = c =>
            {
                var sz = grid.Size; var col = paper2;
                for (float x = 24; x < sz.X; x += 24) grid.DrawLine(new Vector2(x, 0), new Vector2(x, sz.Y), col, 1);
                for (float y = 24; y < sz.Y; y += 24) grid.DrawLine(new Vector2(0, y), new Vector2(sz.X, y), col, 1);
            };
            grid.Resized += () => grid.QueueRedraw();
            modalCard.AddChild(grid);
            var margin = new MarginContainer();
            foreach (var (k, v) in new[] { ("margin_left", 24), ("margin_right", 24), ("margin_top", 22), ("margin_bottom", 22) }) margin.AddThemeConstantOverride(k, v);
            modalCard.AddChild(margin);
            modalScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = false };
            margin.AddChild(modalScroll);
        }
        ModalB NewModal(string kind = "card") => new ModalB(this, kind);
        void ShowModalContent(VBoxContainer content, string kind, Action primary, bool keep)
        {
            int keepScroll = keep ? modalScroll.ScrollVertical : 0;
            foreach (var c in modalScroll.GetChildren()) { modalScroll.RemoveChild(c); c.QueueFree(); }
            content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            content.CustomMinimumSize = new Vector2(660 - 48 - 6, 0);
            modalScroll.AddChild(content);
            modalContent = content; modalPrimary = primary;
            modalOverlay.Visible = true; S.modal = kind;
            void Fit()
            {
                if (!IsInstanceValid(content) || modalContent != content) return;
                float maxH = GetViewportRect().Size.Y - 32 - 44 - 6;
                modalScroll.CustomMinimumSize = new Vector2(0, Math.Min(content.GetCombinedMinimumSize().Y, maxH));
                modalCard.ResetSize();
            }
            content.MinimumSizeChanged += Fit;
            Fit();
            GetTree().ProcessFrame += FitOnce;
            void FitOnce() { GetTree().ProcessFrame -= FitOnce; Fit(); modalScroll.ScrollVertical = keepScroll; }
        }
        void closeModal()
        {
            modalOverlay.Visible = false; S.modal = null; modalPrimary = null;
            foreach (var c in modalScroll.GetChildren()) { modalScroll.RemoveChild(c); c.QueueFree(); }
            modalContent = null;
        }
        bool UiClickPrimary() { if (modalPrimary == null) return false; modalPrimary(); return true; }

        void openBit()
        {
            if (S.modal == "bit") { closeModal(); return; }
            if (S.modal != null || S.mode == "title" || S.mode == "end") return;
            var m = NewModal("bit");
            m.Eyebrow("Tu Bitácora"); m.H2("Lo que sabes de este pueblo");
            m.BitGrid(S.ev.Select(id => EVID[id]).ToList(), S.les.Select(id => LESSONS[id]).ToList());
            m.Row(m.Btn("Cerrar (B)", true, closeModal));
            m.Show();
        }
        void showTitle()
        {
            S.mode = "title";
            var m = NewModal("title");
            m.Eyebrow("Las aventuras del Creador · Nivel 3"); m.H1("El Pueblo del Adivino"); m.Sub("Materia: Análisis de Usuario");
            m.P("Un pueblo entero diseñado a partir de las visiones de un adivino que nunca le preguntó nada a nadie. Para cruzarlo tendrás que hacer lo que él nunca hizo: entender a la gente.");
            m.Steps3(new[] { ("Tiempo 1", "Entrevistar"), ("Tiempo 2", "Medir y probar"), ("Tiempo 3", "Sintetizar") });
            m.Keys();
            m.P("Versión beta.", "small");
            m.Row(m.Small("Acceso directo:"), m.Btn("Tiempo 2", false, () => jumpTo(2)), m.Btn("Tiempo 3", false, () => jumpTo(3)), m.Btn("Empezar", true, () => { closeModal(); zone1(); introZ1(); }));
            m.Show();
        }

        /// <summary>Constructor de las ventanas: cada método equivale a un bloque del HTML de game.js.</summary>
        public class ModalB
        {
            readonly Game g; readonly string kind; readonly VBoxContainer v = VBox(0); Action primary; float lastBottom = -1;
            public ModalB(Game g, string kind) { this.g = g; this.kind = kind; v.MouseFilter = MouseFilterEnum.Ignore; }
            void Add(Control c, float mt, float mb)
            {
                float gap = lastBottom < 0 ? mt : Math.Max(lastBottom, mt);
                if (gap > 0) v.AddChild(Spacer(gap));
                v.AddChild(c); lastBottom = mb;
            }
            public void Eyebrow(string t)
            {
                var f = new FontVariation { BaseFont = Fonts.BodyBold, SpacingGlyph = 1 };
                Add(Lbl(t.ToUpper(), f, 12, inkSoft), 0, 0);
            }
            public void H1(string t) => Add(Lbl(t, Fonts.Display700, 48, ink, true), 6, 2);
            public void H2(string t) => Add(Lbl(t, Fonts.Display700, 30, ink, true), 2, 10);
            public void H3(string t) => Add(Lbl(t, Fonts.Display700, 17, ink, true), 16, 6);
            public void Sub(string t) => Add(Lbl(t, Fonts.Display600, 19, adivino, true), 0, 10);
            public void P(string html, string cls = null)
            {
                if (cls == "small") { Add(Rich(html, 13, inkSoft), 0, 10); return; }
                if (cls == "how")
                {
                    var r = Rich(html, 15, ink);
                    var p = Pnl(Box(white, null, 0, 10, 12, 10), r);
                    var dash = new CanvasView(); dash.OnDraw = c => { var save = g.ctx; g.ctx = c; c.Save(); c.SetLineDash(new double[] { 6, 4 }); g.rr(1, 1, dash.Size.X - 2, dash.Size.Y - 2, 10, null, "#526266", 2); c.Restore(); g.ctx = save; };
                    dash.Resized += () => dash.QueueRedraw();
                    var stack = new PanelContainer(); stack.AddThemeStyleboxOverride("panel", new StyleBoxEmpty()); stack.AddChild(p); stack.AddChild(dash);
                    Add(stack, 0, 10); return;
                }
                Add(Rich(html, 15, ink), 0, 10);
            }
            public Control Small(string t) { var l = Lbl(t, Fonts.Body, 13, inkSoft); l.SizeFlagsVertical = SizeFlags.ShrinkCenter; return l; }
            public void Quote(string t) => Add(Pnl(Box(white, null, 0, 10, 14, 10), Lbl(t, Fonts.Body, 17, ink, true)), 6, 10);
            public Button Btn(string t, bool primary, Action a) { var b = Ui.Btn(t, primary, a); if (primary) this.primary = a; return b; }
            public void Row(params Control[] kids)
            {
                var h = HBox(10); h.Alignment = BoxContainer.AlignmentMode.End;
                foreach (var k in kids) h.AddChild(k);
                Add(h, 16, 0);
            }
            public void Pred(bool good, string small, string text)
            {
                var mark = Lbl(good ? "✓" : "✗", Fonts.BodyBold, 15, white); mark.HorizontalAlignment = HorizontalAlignment.Center; mark.VerticalAlignment = VerticalAlignment.Center;
                var mp = Pnl(Box(good ? evid : adivino, null, 0, 15), mark); mp.CustomMinimumSize = new Vector2(30, 30); mp.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                var sm = Lbl(small.ToUpper(), new FontVariation { BaseFont = Fonts.BodyBold, SpacingGlyph = 1 }, 12, inkSoft);
                var col = VBox(2, sm, Lbl(text, Fonts.Body, 15, ink, true)); col.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                Add(Pnl(Box(good ? evidSoft : adivinoSoft, null, 0, 12, 12, 10), HBox(12, mp, col)), 8, 8);
            }
            public void BeforeAfter((string label, string before, string after)[] rows)
            {
                var col = VBox(0);
                void R(string a, string b, string c, bool head)
                {
                    Label L(string t, Color color, bool bold, float ratio) { var l = Lbl(t, bold ? Fonts.BodyBold : Fonts.Body, 15, color, true); l.SizeFlagsStretchRatio = ratio; return l; }
                    var r = HBox(20, L(a, ink, head, 1.6f), L(b, head ? ink : warn, true, 1), L(c, head ? ink : evid, true, 1));
                    col.AddChild(Pnl(Box(white, null, 0, 0, 10, 7), r));
                    var line = new ColorRect { Color = paper2, CustomMinimumSize = new Vector2(0, 1) }; col.AddChild(line);
                }
                R("", "Antes", "Después", true);
                foreach (var (l, b, a) in rows) R(l, b, a, false);
                Add(Pnl(Box(white, ink, 2, 0), col), 6, 6);
            }
            public void LesList(IEnumerable<string> items, bool ordered)
            {
                var col = VBox(10); int n = 0;
                foreach (var t in items)
                {
                    n++;
                    var bullet = Lbl(ordered ? n + "." : "•", Fonts.Hand, 19, ink); bullet.CustomMinimumSize = new Vector2(24, 0); bullet.HorizontalAlignment = HorizontalAlignment.Right;
                    var l = Lbl(t, Fonts.Hand, 19, ink, true);
                    col.AddChild(HBox(6, bullet, l));
                }
                Add(col, 9, 9);
            }
            public void Steps(string[] htmls)
            {
                var col = VBox(8); int n = 0;
                foreach (var h in htmls) { n++; var b = Lbl(n + ".", Fonts.Body, 16, ink); b.CustomMinimumSize = new Vector2(22, 0); b.HorizontalAlignment = HorizontalAlignment.Right; col.AddChild(HBox(6, b, Rich(h, 16, ink))); }
                Add(col, 6, 6);
            }
            public void Steps3((string b, string s)[] items)
            {
                var h = HBox(8);
                foreach (var (b, s) in items)
                {
                    var p = Pnl(Box(white, ink, 2, 10, 10, 7), VBox(0, Lbl(b, Fonts.Display700, 15, ink), Lbl(s, Fonts.Body, 13, inkSoft)));
                    p.SizeFlagsHorizontal = SizeFlags.ExpandFill; h.AddChild(p);
                }
                Add(h, 12, 12);
            }
            public void Keys()
            {
                var flow = new HFlowContainer(); flow.AddThemeConstantOverride("h_separation", 18); flow.AddThemeConstantOverride("v_separation", 6);
                Control Item(params object[] parts)
                {
                    var h = HBox(2);
                    foreach (var p in parts) h.AddChild(p is string s ? Lbl(s, Fonts.Body, 14, ink) : (Control)p);
                    return h;
                }
                flow.AddChild(Item(Kbd("←"), Kbd("↑"), Kbd("↓"), Kbd("→"), " o ", Kbd("WASD"), " moverte (o clic en el suelo)"));
                flow.AddChild(Item(Kbd("E"), " o ", Kbd("Espacio"), " hablar y continuar"));
                flow.AddChild(Item(Kbd("B"), " abrir tu Bitácora"));
                Add(flow, 8, 8);
            }
            static Control Bar(double pct, Color fill)
            {
                var c = new Control { CustomMinimumSize = new Vector2(0, 10), SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
                c.Draw += () =>
                {
                    var bg = Box(paper2, null, 0, 5); c.DrawStyleBox(bg, new Rect2(0, 0, c.Size.X, 10));
                    float w = (float)(c.Size.X * Math.Clamp(pct, 0, 1)); if (w > 0) c.DrawStyleBox(Box(fill, null, 0, 5), new Rect2(0, 0, w, 10));
                };
                c.Resized += () => c.QueueRedraw();
                return c;
            }
            public void Metrics((string label, string value, string small, double pct)[] ms)
            {
                var col = VBox(8);
                foreach (var (label, value, small, pct) in ms)
                {
                    var val = HBox(4, Lbl(value, Fonts.Display700, 18, ink)); val.Alignment = BoxContainer.AlignmentMode.End;
                    if (small != null) { var sl = Lbl(small, Fonts.Body, 12, inkSoft); sl.SizeFlagsVertical = SizeFlags.ShrinkCenter; val.AddChild(sl); }
                    var top = HBox(12, Lbl(label, Fonts.Body, 15, ink, true), val);
                    col.AddChild(Pnl(Box(white, ink, 2, 10, 12, 8), VBox(4, top, Bar(pct, warn))));
                }
                Add(col, 8, 8);
            }
            public void Dist((string name, int r, int p)[] rows)
            {
                var col = VBox(8);
                foreach (var (name, r, p) in rows)
                {
                    var nl = Lbl(name, Fonts.Display700, 15, ink); nl.CustomMinimumSize = new Vector2(70, 0); nl.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                    Control Line(double pct, Color c, string t) { var sl = Lbl(t, Fonts.Body, 12, inkSoft); return HBox(10, Bar(pct, c), sl); }
                    var bars = VBox(4, Line(r / 100.0, adivino, $"{r / 10} de cada 10 rutas"), Line(p / 100.0, evid, $"{p} de cada 100 pasajeros")); bars.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                    col.AddChild(Pnl(Box(white, ink, 2, 10, 12, 8), HBox(10, nl, bars)));
                }
                Add(col, 0, 0);
            }
            public void Persona(string name, string small, List<PersonaField> fields)
            {
                var avatar = new Control { CustomMinimumSize = new Vector2(48, 48), MouseFilter = MouseFilterEnum.Ignore };
                avatar.Draw += () => { avatar.DrawCircle(new Vector2(24, 24), 24, evid); var pts = new List<Vector2>(); for (int i = 0; i <= 24; i++) { float a = Mathf.Pi * i / 24f; pts.Add(new Vector2(24 + Mathf.Cos(a) * 24, 24 + Mathf.Sin(a) * 24)); } var clipY = 34f; var poly = pts.Where(p => p.Y >= clipY).ToList(); if (poly.Count > 2) { float half = Mathf.Sqrt(24 * 24 - 10 * 10); poly.Insert(0, new Vector2(24 + half, clipY)); poly.Add(new Vector2(24 - half, clipY)); avatar.DrawColoredPolygon(poly.ToArray(), new Color(0, 0, 0, 0.15f)); } };
                var head = HBox(12, avatar, VBox(0, Lbl(name, Fonts.Display700, 19, ink), Lbl(small, Fonts.Body, 12, inkSoft, true))); head.Alignment = BoxContainer.AlignmentMode.Center;
                head.GetChild<VBoxContainer>(1).SizeFlagsHorizontal = SizeFlags.ExpandFill;
                var col = VBox(12, head);
                foreach (var f in fields)
                {
                    var fs = VBox(6, Lbl(f.legend, Fonts.Display700, 16, ink));
                    if (f.okText != null)
                    {
                        fs.AddChild(Pnl(Box(evidSoft, evid, 2, 10, 10, 7), VBox(2, Lbl(f.okText, Fonts.Body, 15, ink, true), Lbl(f.okSrc, Fonts.BodyBold, 12, inkSoft, true))));
                    }
                    else
                    {
                        for (int oi = 0; oi < f.opts.Count; oi++)
                        {
                            int idx = oi; var (t, bad) = f.opts[oi];
                            var cp = new ClickPanel(Box(paper, ink, 2, 10, 10, 7), Box(evidSoft, ink, 2, 10, 10, 7), Box(adivinoSoft, adivino, 2, 10, 10, 7));
                            var inner = VBox(2, Lbl(t, Fonts.Body, 15, bad ? inkSoft : ink, true));
                            if (bad) inner.AddChild(Lbl("Fuente: la bola del Adivino. Esto no lo investigaste.", Fonts.BodyBold, 12, inkSoft, true));
                            cp.AddChild(inner);
                            cp.Disabled = bad;
                            var pickA = f.pick; cp.OnPressed = () => pickA(idx);
                            fs.AddChild(cp);
                        }
                    }
                    col.AddChild(fs);
                }
                Add(Pnl(Box(white, ink, 2, 14, 12, 12), col), 0, 0);
            }
            public void Dilemma((string b, string s, Action a) left, (string b, string s, Action a) right)
            {
                var h = HBox(10);
                foreach (var (opt, first) in new[] { (left, true), (right, false) })
                {
                    var cp = new ClickPanel(Box(white, ink, 3, 12, 14, 14), Box(first ? adivinoSoft : evidSoft, ink, 3, 12, 14, 14)) { OnPressed = opt.a, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                    cp.AddChild(VBox(4, Lbl(opt.b, Fonts.Display700, 20, ink, true), Lbl(opt.s, Fonts.Body, 15, ink, true)));
                    h.AddChild(cp);
                }
                Add(h, 12, 0);
            }
            public void BitGrid(List<Evid> evs, List<string> les)
            {
                var left = VBox(0, Lbl("Evidencias", Fonts.Display700, 17, ink), Spacer(6));
                var evList = VBox(8);
                if (evs.Count == 0) evList.AddChild(Lbl("Todavía no tienes evidencias. Se consiguen entrevistando, observando y revisando datos.", Fonts.BodyItalic, 15, inkSoft, true));
                foreach (var e in evs)
                {
                    var chipL = Lbl(e.src.ToUpper(), new FontVariation { BaseFont = Fonts.BodyBold, SpacingGlyph = 1 }, 11, ink);
                    var chip = Pnl(Box(e.kind == "entrevista" ? chipInt : (e.kind == "datos" ? chipDat : chipObs), null, 0, 999, 8, 1), chipL);
                    var chipRow = HBox(0, chip);
                    evList.AddChild(Pnl(Box(white, paper2, 1, 10, 10, 8), VBox(2, chipRow, Lbl(e.who, Fonts.BodyBold, 15, ink, true), Lbl(e.txt, Fonts.Body, 15, ink, true))));
                }
                left.AddChild(evList);
                var right = VBox(0, Lbl("Lo que aprendí", Fonts.Display700, 17, ink), Spacer(6));
                if (les.Count == 0) right.AddChild(Lbl("Aquí se anotan las lecciones a medida que las descubras.", Fonts.BodyItalic, 15, inkSoft, true));
                else
                {
                    var lc = VBox(10);
                    foreach (var t in les) { var bl = Lbl("•", Fonts.Hand, 19, ink); bl.CustomMinimumSize = new Vector2(24, 0); bl.HorizontalAlignment = HorizontalAlignment.Right; lc.AddChild(HBox(6, bl, Lbl(t, Fonts.Hand, 19, ink, true))); }
                    right.AddChild(lc);
                }
                left.SizeFlagsHorizontal = SizeFlags.ExpandFill; right.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                left.SizeFlagsVertical = SizeFlags.ShrinkBegin; right.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                var h = HBox(18, left, right);
                Add(h, 16, 0);
            }
            public void Show(bool keep = false)
            {
                if (lastBottom > 0) v.AddChild(Spacer(lastBottom));
                g.ShowModalContent(v, kind, primary, keep);
            }
        }
    }
}
