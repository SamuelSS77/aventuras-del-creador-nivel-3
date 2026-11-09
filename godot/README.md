# El Pueblo del Adivino · versión Godot (C#)

El mismo Nivel 3 de *Las aventuras del Creador*, convertido de JavaScript a C# para **Godot 4.7 (.NET)**. El juego, los textos, las reglas y el dibujo son los mismos de la versión web (`../index.html` y `../game.js`); solo cambió el lenguaje.

## Cómo abrirlo

1. Necesitas **Godot 4.7 .NET** y el **SDK de .NET 8** (o más nuevo).
2. En Godot: *Importar* → elige `godot/project.godot`.
3. Pulsa **Compilar** (el martillo) y luego **Ejecutar** (F5).

Para probar una parte directamente, ejecuta con `-- --tiempo2` o `-- --tiempo3` (equivale a `index.html#tiempo2` / `#tiempo3`), o usa los botones de acceso directo de la pantalla de inicio.

## Cómo está hecho

| Archivo | Qué contiene |
|---|---|
| `src/Canvas2D.cs` | Una imitación del canvas 2D del navegador (caminos, curvas, recortes, degradados, patrones, texto) sobre el dibujo de Godot. Gracias a ella el código de dibujo se tradujo casi línea por línea. |
| `src/Game.cs` | Estado, datos (personajes, evidencias, lecciones), diálogos, bucle principal y controles. |
| `src/Game.Draw.cs` | Personajes, Boceto, escenarios y la elección de pantalla (`draw`). |
| `src/Game.Cat.cs` | Comportamiento de Boceto, el gato del Creador. |
| `src/Game.World.cs` | Zonas, Tiempo 1, el muro (Sokoban), tarjetas de cierre y Tiempo 2. |
| `src/Game.Tiles.cs` | Minijuego del Tablero de rutas. |
| `src/Game.Plaza.cs` | Tiempo 3: User Persona, la huelga, la pausa de Boceto y los dos finales. |
| `src/Game.Ui.cs`, `src/Ui.cs`, `src/ClickPanel.cs` | La interfaz que en la web era HTML/CSS: HUD, Bitácora, diálogo, avisos, ventanas y barra de minijuegos. |
| `fonts/` | Baloo 2, Atkinson Hyperlegible y Kalam (las mismas de la web). Los símbolos que no traen (→ ✓ ▼ ♪) salen de una fuente del sistema, igual que en el navegador. |

Los nombres de funciones y variables son los mismos de `game.js`, así que un cambio en la versión web se puede encontrar fácilmente aquí.
