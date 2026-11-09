using Godot;
using System;

namespace AventurasNivel3
{
    /// <summary>Un Control que dibuja con un Ctx. Lo usan el escenario, el retrato, los íconos y los lienzos fuera de pantalla.</summary>
    public partial class CanvasView : Control
    {
        public Ctx Ctx;
        public Action<Ctx> OnDraw;
        public CanvasView() { Ctx = new Ctx(this); MouseFilter = MouseFilterEnum.Ignore; }
        public override void _Draw() { Ctx.BeginFrame(); OnDraw?.Invoke(Ctx); }
    }
}
