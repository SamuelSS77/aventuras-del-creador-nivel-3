# Contexto Maestro y Técnico: Las Aventuras del Creador — Nivel 3: El Pueblo del Adivino

## 1. Visión General y Reglas del Proyecto
- **Proyecto:** Videojuego universitario ("Las aventuras del Creador") — Carrera: Creación Digital[cite: 3, 5, 6].
- **Nivel 3:** Basado en la materia **Análisis de Usuario** (Entrevistas, observación, métricas, pruebas de usabilidad, User Persona y co-diseño/trabajo en equipo)[cite: 3, 5, 6].
- **Rol del autor (Samuel):** Dueño de la historia, UX y dirección del nivel[cite: 3, 5]. No programa el código directamente; prueba e itera pidiendo cambios[cite: 3].
- **Restricciones clave de diseño:**
  - La mecánica de "devolver el color" pertenece exclusivamente al Nivel 1 (NO usar aquí)[cite: 3].
  - Flujo pedagógico obligatorio para cada aprendizaje: **1. Vive (el jugador lo experimenta) → 2. Lo dice un personaje → 3. Queda registrado en la Bitácora**[cite: 3].
  - No modificar el Figma del equipo; todos los ajustes se trabajan sobre este prototipo y en texto[cite: 3, 5].

---

## 2. Arquitectura Técnica y Estética (`index.html`)
- **Stack:** Single-file HTML5 (`index.html`) sin dependencias externas de JS[cite: 3, 6]. Todo corre en un `<canvas id="cv" width="960" height="600">` renderizado con primitivas 2D y capas de UI en HTML/CSS superpuestas dentro de `.stage`.
- **Tipografías (Google Fonts):**
  - Títulos / Display (`--f-display`): `'Baloo 2'`[cite: 6].
  - Cuerpo / UI (`--f-body`): `'Atkinson Hyperlegible'` (alta accesibilidad)[cite: 6].
  - Manuscrita / Bitácora (`--f-hand`): `'Kalam'`[cite: 6].
- **Paleta de Colores (CSS Custom Properties & JS `COL`/`CK`):**
  - Fondos oscuros: `--night: #1b1a2b`, `--panel: #2a2238`, contorno `OUT: #2a2238`[cite: 6].
  - Cuaderno / Modales: `--paper: #eef1e8` con cuadrícula `--paper-2: #dde3d6`, tinta `--ink: #1d2a2e`[cite: 6].
  - Semánticos: Adivino (`#7a4fc9` morado), Evidencia/Éxito (`#2a8a68` verde), Creador (`#f08a3c` naranja), Alerta/Error (`#d2483a` rojo), Highlight (`#ffcf70` amarillo)[cite: 6].
- **Sistemas Globales:**
  - **Estado global (`S`):** Controla `mode` (`'title'`, `'world'`, `'soko'`, `'tiles'`, `'plaza'`, `'end'`), `zone` (`1`, `2`, `3`), flags `S.f`, evidencias `S.ev`, lecciones `S.les`[cite: 6].
  - **Accesos directos de depuración:** Botones en pantalla de título para saltar a Tiempo 2 o Tiempo 3, y soporte por URL hash (`#tiempo2`, `#tiempo3`) a través de `jumpTo(n)`[cite: 6].
  - **Controles:** Flechas / WASD o clic en el suelo (point-and-click con pathing básico) para moverse; `E` / `Espacio` / `Enter` para interactuar y avanzar diálogo; `1-3` para elegir opciones de diálogo; `B` para abrir/cerrar la Bitácora[cite: 6].

---

## 3. Catálogo de Evidencias (`EVID`) y Lecciones (`LESSONS`)

### Evidencias recolectables (`S.ev`):
1. `rosa` (Entrevista - Doña Rosa): *«Con el carrito no puedo trepar. Necesito pasar por el centro de la calle, que es plano.»*[cite: 6]
2. `mario` (Entrevista - Don Mario, vendedor): *«Si tapan el frente de mi puesto, nadie me encuentra.»*[cite: 6]
3. `tito` (Entrevista - Tito): *«En los rincones entre las casas no pasa nadie.»*[cite: 6]
4. `ernesto` (Entrevista - Don Ernesto): *«Treinta y ocho minutos. Debería ser ocho. Tres vueltas cada mañana.»*[cite: 6]
5. `metricas` (Datos - Tablero de la Estación): *Solo 23 de cada 100 llegan a su destino. 8 de cada 10 rutas van al Mirador, pero solo 2 de cada 100 pasajeros quieren ir allá.*[cite: 6]

### Lecciones de la Bitácora (`LESSON_ORDER`: A, B, C, M, D, E, F, H, G):
- **A:** Si preguntas «¿verdad que…?», te dicen lo que quieres oír. Pregunta «¿por qué?»[cite: 6].
- **B:** La gente te pide soluciones. Tu trabajo es encontrar el problema que hay detrás[cite: 6].
- **C:** Los números te dicen qué falla y cuánto[cite: 6].
- **M:** Si mucha gente falla en lo mismo, el problema es el diseño, no el usuario[cite: 6].
- **D:** Pruébalo con la gente y mide otra vez: así sabes si tu cambio funcionó[cite: 6].
- **E:** Una opinión, por segura que suene, pierde contra lo que dicen y hacen los usuarios[cite: 6].
- **F:** User Persona: un retrato del usuario hecho con lo que investigaste, no con lo que imaginas[cite: 6].
- **H:** Nadie diseña bien solo: se diseña en equipo y con los usuarios[cite: 6].
- **G:** Tú no eres el usuario[cite: 6].

---

## 4. Estructura Detallada de los 3 Tiempos

### TIEMPO 1: La Barrera Caprichosa (Entrevistar)
- **Narrativa:** El Adivino bloqueó la calle hacia el mercado con un muro de 3 bloques morados porque su bola de cristal dijo que el pueblo quería un "monumento"[cite: 6]. Le hace una *leading question* a Doña Rosa («¿Verdad que ama mi muro?») → Desbloquea **Lección A**[cite: 6].
- **Regla de bloqueo:** El jugador no puede mover el muro hasta completar las 3 entrevistas (`interviews() === 3`)[cite: 6].
- **Entrevistas y Capas Visuales sobre el Mapa:**
  1. **Doña Rosa:** Enseña a evitar preguntas sesgadas y a preguntar «¿Por qué?» cuando pide un puente (el puente tendría escalones; su necesidad real es un paso plano para su carrito) → Da `EVID.rosa`, **Lección B**, y dibuja en el mapa el carril verde central (`Paso plano · Rosa`)[cite: 6].
  2. **Don Mario:** Cuenta que el muro ocultó su puesto de frutas y que el Adivino quiere apilar bloques frente a su local → Da `EVID.mario` y dibuja zona roja achurada frente al puesto (`No tapar · Mario`)[cite: 6].
  3. **Tito:** Experto jugando a las escondidas, revela que los 3 rincones entre las casas/setos no los usa nadie → Da `EVID.tito` y marca los 3 recovecos (`NOOK1`, `NOOK2`, `NOOK3`) con borde punteado (`Rincón libre`)[cite: 6].
- **Minijuego 1: Sokoban (`mode = 'soko'`):**
  - Tablero de 13x5 (`SOKO_MAP`). El jugador empuja los 3 bloques hacia los 3 rincones (`n`)[cite: 6].
  - Los bloques se guardan en los rincones (NO se convierten en bancas)[cite: 3, 6].
  - Si un bloque pisa `STALL` (`[10,1]`, `[11,1]`), sale alerta roja de que está tapando a Don Mario[cite: 6].
  - Incluye botones/teclas de Deshacer (`Z`) y Reiniciar (`R`)[cite: 6].
  - Al guardar los 3 bloques, corre cinemática de Doña Rosa cruzando feliz al mercado y se muestra la **Tarjeta de Cierre 1 (Predicción vs. Realidad)**[cite: 6].

### TIEMPO 2: La Estación Colapsada (Medir y probar)
- **Narrativa:** En la estación, Doña Rosa y Don Ernesto caminan en círculos guiados por las flechas moradas del Adivino hacia su Mirador[cite: 6].
- **Investigación Cuantitativa y Cualitativa:**
  - **Don Ernesto (NPC con prisa):** Solo permite 1 pregunta. Si le haces pregunta cerrada dice «No» y te ignora; si preguntas cuánto tarda, da datos exactos (38 min en vez de 8, 3 vueltas) → Da `EVID.ernesto`[cite: 6].
  - **Jefe de Estación (`taq`):** Abre el modal del **Tablero de la Estación (`showMetrics`)** comparando Rutas (morado) vs. Pasajeros (verde): Mirador (8/10 rutas vs 2/100 pasajeros), Valle (1/10 vs 58/100), Bosque (1/10 vs 40/100)[cite: 6].
  - **Diálogo post-métricas:** El jugador refuta al Adivino eligiendo *«Si 77 de cada 100 se pierden, el problema no es la gente: es el diseño»* → Da `EVID.metricas`, **Lección C** y **Lección M**[cite: 6].
- **Minijuego 2: Tablero de Rutas / Baldosas (`mode = 'tiles'`):**
  - Cuadrícula de 9x5 (`tilesGeom`).
  - **Mecánica:** Clic en casilla vacía/escalera pone flecha verde (`#3fae7c`) y la rota (`R → D → L → U → borrar`). Clic derecho la borra. Clic sobre una **flecha morada del Adivino** la elimina directamente[cite: 3, 6]. Botón **Testear (`Enter`/`T`)** simula el recorrido paso a paso[cite: 6].
  - **Progresión de los 3 Puzzles (`TP`) y el Tablero en Vivo (`METRICS`):**
    1. **Puzzle 1 (Doña Rosa):** Llevarla al Valle (`D`) evitando las escaleras (`S`) y quitando la flecha morada que la manda al Mirador (`T`). Mejora tablero: **23 → 55 de 100** (38 min → 24 min)[cite: 6].
    2. **Puzzle 2 (Don Ernesto):** Llevarlo al Bosque en **máximo 11 pasos** (él sí puede usar escaleras `S`). Mejora tablero: **55 → 80 de 100** (24 min → 14 min)[cite: 6].
    3. **Puzzle 3 (Hora pico):** Una misma red de flechas para que Don Ernesto (origen `1`) y Doña Rosa (origen `2`, sin escaleras) lleguen juntos a los Andenes. Mejora tablero: **80 → 98 de 100** (14 min → 9 min, 0 vueltas)[cite: 6].
  - Si un intento falla o al superar los puzzles, se registra la **Lección D**[cite: 6]. Cierra con **Tarjeta de Cierre 2** (tabla Antes vs. Después)[cite: 6].

### TIEMPO 3: La Plaza (Sintetizar y Co-diseñar — Minijuego Activo `mode = 'plaza'`)
*(Nota técnica: En el código aún existen las funciones viejas `drawDuel`, `askPred` y `startFrenzy` de una versión anterior, pero el flujo activo conectado a `startDuel()` usa el sistema dinámico `S.pz` y `drawPlaza()`)*[cite: 6].
1. **Fase 1 — Armar la User Persona (`renderPersona` → `presentPersona`):**
   - El Adivino muestra en su bola que "el usuario" es idéntico a él[cite: 6].
   - El jugador abre el constructor de **User Persona ("Carmen, 63 años")** y debe elegir la opción respaldada por evidencia real en 3 categorías (*Qué quiere lograr*, *Qué le molesta*, *Qué necesita*). Las opciones falsas dicen *"Fuente: la bola del Adivino"* y se bloquean al fallar[cite: 6].
   - Al presentar a Carmen al pueblo, la bola sufre su primera grieta (`crack = 1`) y se anota la **Lección F**[cite: 6].
2. **Fase 2 — Refutar los Supuestos (`pzStart('confront')`):**
   - Del Adivino caen burbujas con supuestos (`CLAIMS`) hacia una línea punteada roja inferior[cite: 6].
   - El jugador hace clic en cada supuesto y aparece una fila de 3 chips para refutarlo según su naturaleza: **Lo que dijo la gente** (verde, `ent`), **Los números** (azul, `dat`) o **Lo que pasó al probar** (naranja, `pru`)[cite: 6].
   - Si cruzan la línea roja o se elige mal, sube la barra de **Estrés del pueblo**. Al refutar 6 supuestos, la bola se agrieta más y se anota la **Lección E**[cite: 6].
3. **Fase 3 — La Huelga (`pzStart('strike')`):**
   - El Adivino conjura inventos absurdos en la plaza (fuentes que tapan el paso, letreros llenos de ojos, bancas altísimas) y lanza orbes morados que marean al jugador (`stun`)[cite: 6]. El pueblo entra en huelga con pancartas de *«¡NO!»* y gritos (*«Nada para nosotros sin nosotros»*)[cite: 6].
   - **Mecánica:** El jugador camina hasta el invento (clic o WASD), lee la queja del pueblo (`¡No puedo pasar!`, `¿Qué dice eso?`, `¡Está muy alto!`) y elige el chip correcto (`Despejar el paso`, `Letrero claro`, `Ajustar altura`)[cite: 6].
   - **Oleadas:**
     - *Oleada 1:* 3 inventos secuenciales (uno de cada tipo)[cite: 6].
     - *Oleada 2:* El Adivino acelera (8 inventos + orbes morados)[cite: 6].
     - *Oleada 3 (Desborde):* El Adivino spawnea inventos cada 0.6s («¡Tú arreglas uno y yo hago diez!»). Cuando el estrés llega a 82% (o pasan 16s), salta el **Dilema Final (`pzDilemma`)**[cite: 6].
4. **Fase 4 — Dilema Final (`¿Vale la pena?`) y Dos Finales:**
   - **Opción A ("No vale la pena. Lo hago rápido, a mi manera") → `pzChooseA` / `showEndA` (Final 1 de 2: "El segundo Adivino"):**
     - Tus clics ahora crean más inventos egoístas ("A mi manera…"), la bola de cristal refleja la cara del Creador (`d.mirror = true`) y la huelga continúa. Permite **"Volver a elegir"** (`retryDilemma`)[cite: 6].
   - **Opción B ("Sí vale la pena… pero no tengo que hacerlo solo") → `pzChooseB` / `pzStart('team')` (Final 2 de 2: "Diseñar con la gente"):**
     - Se activa la fase **En Equipo** durante 18 segundos: los NPCs entran a la plaza a arreglar inventos automáticamente junto al jugador (Doña Rosa despeja pasos, Don Mario arregla letreros, Tito ajusta alturas, el Jefe de estación lleva la cuenta y Ernesto cronometra)[cite: 6].
     - Al terminar los 18s: se anota la **Lección H** (trabajo en equipo en Creación Digital), **la bola de cristal se rompe en pedazos (`shatter = 1`)** revelando que solo reflejaba al Adivino, se anota la **Lección G («Tú no eres el usuario»)**, y el Creador le regala su libreta al Adivino para que aprenda a preguntar y trabajar en equipo[cite: 6].