using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AventurasNivel3
{
    /// <summary>Colores y piezas de interfaz que reproducen styles.css.</summary>
    public static class Ui
    {
        // :root de styles.css
        public static readonly Color night = C("#1b1a2b"), night2 = C("#262539"), night3 = C("#35334e"), muted = C("#aaa8c2"), highlight = C("#ffcf70"),
            panel = C("#2a2238"), panel2 = C("#3b3152"), cream = C("#f6efe0"), outline = C("#2a2238"), paper = C("#eef1e8"), paper2 = C("#dde3d6"), white = C("#ffffff"),
            ink = C("#1d2a2e"), inkSoft = C("#526266"), adivino = C("#7a4fc9"), adivinoSoft = C("#e5dbf7"), evid = C("#2a8a68"), evidSoft = C("#d3ecdf"),
            creador = C("#f08a3c"), warn = C("#d2483a"), chipInt = C("#d3ecdf"), chipObs = C("#f3e3c4"), chipDat = C("#d6e3f3");

        public static Color C(string css) => Css.Parse(css);

        public static StyleBoxFlat Box(Color bg, Color? border = null, int bw = 0, int radius = 0, float padX = 0, float padY = 0, Color? shadow = null, float shadowY = 0, bool drawCenter = true)
        {
            var s = new StyleBoxFlat { BgColor = bg, DrawCenter = drawCenter, AntiAliasing = true, CornerDetail = 10 };
            if (border.HasValue && bw > 0) { s.BorderColor = border.Value; s.SetBorderWidthAll(bw); }
            s.SetCornerRadiusAll(radius);
            s.ContentMarginLeft = s.ContentMarginRight = padX; s.ContentMarginTop = s.ContentMarginBottom = padY;
            if (shadow.HasValue) { s.ShadowColor = shadow.Value; s.ShadowSize = 1; s.ShadowOffset = new Vector2(0, shadowY); }
            return s;
        }
        public static StyleBoxFlat Pad(this StyleBoxFlat s, float l, float t, float r, float b) { s.ContentMarginLeft = l; s.ContentMarginTop = t; s.ContentMarginRight = r; s.ContentMarginBottom = b; return s; }

        public static Label Lbl(string text, Font font, int size, Color color, bool wrap = false)
        {
            var l = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
            l.AddThemeFontOverride("font", font); l.AddThemeFontSizeOverride("font_size", size); l.AddThemeColorOverride("font_color", color);
            l.AddThemeConstantOverride("line_spacing", Math.Max(1, (int)Math.Round(size * 0.22)));
            if (wrap) { l.AutowrapMode = TextServer.AutowrapMode.WordSmart; l.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; l.CustomMinimumSize = new Vector2(1, 0); }
            return l;
        }

        /// <summary>Pequeño HTML del juego (b, br, span de color) a BBCode de RichTextLabel.</summary>
        public static string Bb(string html, string boldColor = null)
        {
            string s = html.Replace("[", "[lb]");
            s = Regex.Replace(s, @"<br\s*/?>", "\n");
            s = Regex.Replace(s, "<span class=\"(purple|green|orange|blue|orangeT)\">(.*?)</span>", m =>
            {
                string col = m.Groups[1].Value switch { "purple" => "#7a4fc9", "green" => "#2a8a68", "blue" => "#2f6fb3", _ => "#c96a24" };
                return $"[color={col}][b]{m.Groups[2].Value}[/b][/color]";
            });
            s = boldColor != null ? s.Replace("<b>", $"[b][color={boldColor}]").Replace("</b>", "[/color][/b]") : s.Replace("<b>", "[b]").Replace("</b>", "[/b]");
            s = Regex.Replace(s, "<[^>]+>", "");
            return s;
        }
        public static RichTextLabel Rich(string html, int size, Color color, string boldColor = null)
        {
            var r = new RichTextLabel { BbcodeEnabled = true, FitContent = true, ScrollActive = false, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            r.AddThemeFontOverride("normal_font", Fonts.Body); r.AddThemeFontOverride("bold_font", Fonts.BodyBold); r.AddThemeFontOverride("italics_font", Fonts.BodyItalic);
            r.AddThemeFontSizeOverride("normal_font_size", size); r.AddThemeFontSizeOverride("bold_font_size", size); r.AddThemeFontSizeOverride("italics_font_size", size);
            r.AddThemeColorOverride("default_color", color);
            r.AddThemeConstantOverride("line_separation", Math.Max(1, (int)Math.Round(size * 0.22)));
            r.CustomMinimumSize = new Vector2(1, 0);
            r.Text = Bb(html, boldColor);
            return r;
        }

        public static void IgnoreMouse(Node n)
        {
            foreach (var ch in n.GetChildren()) { if (ch is Control c) c.MouseFilter = Control.MouseFilterEnum.Ignore; IgnoreMouse(ch); }
        }

        public static Control Spacer(float h) => new Control { CustomMinimumSize = new Vector2(0, h), MouseFilter = Control.MouseFilterEnum.Ignore };
        public static Control HSpacer(float w = 0, bool expand = false) { var c = new Control { CustomMinimumSize = new Vector2(w, 0), MouseFilter = Control.MouseFilterEnum.Ignore }; if (expand) c.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; return c; }

        public static HBoxContainer HBox(int sep, params Control[] kids) { var h = new HBoxContainer(); h.AddThemeConstantOverride("separation", sep); foreach (var k in kids) h.AddChild(k); return h; }
        public static VBoxContainer VBox(int sep, params Control[] kids) { var v = new VBoxContainer(); v.AddThemeConstantOverride("separation", sep); foreach (var k in kids) v.AddChild(k); return v; }

        /// <summary>Un botón .btn de la web.</summary>
        public static Button Btn(string text, bool primary, Action onClick, bool dark = false)
        {
            var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, MouseDefaultCursorShape = Control.CursorShape.PointingHand, CustomMinimumSize = new Vector2(0, 40) };
            Color bg, fg, bd;
            if (dark) { bg = primary ? highlight : panel2; fg = primary ? outline : cream; bd = primary ? outline : C("#5a4d74"); }
            else { bg = primary ? ink : paper; fg = primary ? paper : ink; bd = ink; }
            var n = Box(bg, bd, 2, 10, 14, 7);
            var h = (StyleBoxFlat)n.Duplicate(); h.BgColor = bg.Lightened(primary || dark ? 0.08f : 0.0f).Darkened(primary || dark ? 0 : 0.04f);
            b.AddThemeStyleboxOverride("normal", n); b.AddThemeStyleboxOverride("hover", h); b.AddThemeStyleboxOverride("pressed", h); b.AddThemeStyleboxOverride("hover_pressed", h);
            b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty()); b.AddThemeStyleboxOverride("disabled", n);
            foreach (var k in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color", "font_disabled_color" }) b.AddThemeColorOverride(k, fg);
            b.AddThemeFontOverride("font", Fonts.BodyBold); b.AddThemeFontSizeOverride("font_size", 15);
            if (onClick != null) b.Pressed += onClick;
            return b;
        }

        /// <summary>Etiqueta de tecla (kbd).</summary>
        public static PanelContainer Kbd(string k, Color? bg = null, Color? fg = null, Color? border = null)
        {
            var s = Box(bg ?? white, border ?? ink, 2, 5, 6, 0); s.BorderWidthBottom = 3;
            var p = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            p.AddThemeStyleboxOverride("panel", s);
            p.AddChild(Lbl(k, Fonts.BodyBold, 12, fg ?? ink));
            return p;
        }

        public static PanelContainer Pnl(StyleBox s, Control child = null)
        {
            var p = new PanelContainer(); p.AddThemeStyleboxOverride("panel", s);
            if (child != null) p.AddChild(child);
            return p;
        }
    }
}
