using Godot;
using System;

namespace AventurasNivel3
{
    /// <summary>Un panel que se comporta como botón (las tarjetas clicables de la web: opciones, dilema, respuestas).</summary>
    public partial class ClickPanel : PanelContainer
    {
        public Action OnPressed;
        StyleBox normal, hover, disabled;
        bool dis, over;
        public bool Disabled { get => dis; set { dis = value; MouseDefaultCursorShape = dis ? CursorShape.Forbidden : CursorShape.PointingHand; Refresh(); } }
        public ClickPanel() : this(null, null) { }
        public ClickPanel(StyleBox normal, StyleBox hover, StyleBox disabled = null)
        {
            this.normal = normal; this.hover = hover; this.disabled = disabled ?? normal;
            MouseFilter = MouseFilterEnum.Stop; MouseDefaultCursorShape = CursorShape.PointingHand; FocusMode = FocusModeEnum.None;
            MouseEntered += () => { over = true; Refresh(); };
            MouseExited += () => { over = false; Refresh(); };
            Refresh();
        }
        void Refresh() => AddThemeStyleboxOverride("panel", dis ? disabled : (over ? hover : normal));
        public override void _Ready() => Ui.IgnoreMouse(this);
        public override void _GuiInput(InputEvent ev)
        {
            if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left && !dis) { AcceptEvent(); OnPressed?.Invoke(); }
        }
    }
}
