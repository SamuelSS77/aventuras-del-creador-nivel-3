using Godot;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AventurasNivel3
{
    /// <summary>
    /// Las tres familias del juego web: Atkinson Hyperlegible (cuerpo), Baloo 2 (títulos) y Kalam (Bitácora).
    /// Los símbolos que no traen (→ ← ✓ ✗ ▼ ♪) salen de una fuente del sistema, igual que en el navegador.
    /// </summary>
    public static class Fonts
    {
        public static Font Body, BodyBold, BodyItalic, Display500, Display600, Display700, Hand;
        static bool loaded;

        public static void Load()
        {
            if (loaded) return; loaded = true;
            var sys = new SystemFont { FontNames = new[] { "Segoe UI Symbol", "Segoe UI", "Segoe UI Emoji", "Arial Unicode MS", "Arial", "DejaVu Sans", "Noto Sans Symbols", "Noto Sans" } };
            Font Mk(string file)
            {
                var f = GD.Load<FontFile>("res://fonts/" + file);
                f.Fallbacks = new Godot.Collections.Array<Font> { sys };
                return f;
            }
            Body = Mk("AtkinsonHyperlegible-Regular.ttf");
            BodyBold = Mk("AtkinsonHyperlegible-Bold.ttf");
            BodyItalic = Mk("AtkinsonHyperlegible-Italic.ttf");
            Display500 = Mk("Baloo2-Medium.ttf");
            Display600 = Mk("Baloo2-SemiBold.ttf");
            Display700 = Mk("Baloo2-Bold.ttf");
            Hand = Mk("Kalam-Regular.ttf");
        }

        static readonly Dictionary<string, (Font, int)> cache = new();
        static readonly Regex rx = new(@"^\s*(?:(italic)\s+)?(?:(\d{3})\s+)?(\d+(?:\.\d+)?)px\s+(.*)$");

        /// <summary>Convierte un font CSS de canvas ("700 13px "Baloo 2"...") en fuente + tamaño.</summary>
        public static (Font, int) Parse(string css)
        {
            if (cache.TryGetValue(css, out var r)) return r;
            Load();
            var m = rx.Match(css);
            int weight = 400, size = 10; bool italic = false; string fam = "";
            if (m.Success)
            {
                italic = m.Groups[1].Success;
                if (m.Groups[2].Success) weight = int.Parse(m.Groups[2].Value);
                size = (int)System.MathF.Round(float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                fam = m.Groups[4].Value;
            }
            Font f;
            if (fam.Contains("Baloo")) f = weight >= 700 ? Display700 : weight >= 600 ? Display600 : Display500;
            else if (fam.Contains("Kalam")) f = Hand;
            else f = italic ? BodyItalic : (weight >= 600 ? BodyBold : Body);
            r = (f, size);
            cache[css] = r;
            return r;
        }
    }
}
