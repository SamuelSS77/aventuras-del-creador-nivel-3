# Las aventuras del Creador · Nivel 3: El Pueblo del Adivino

Nivel 3 del videojuego *Las aventuras del Creador*, proyecto de la carrera de Creación Digital. El nivel está basado en la materia **Análisis de Usuario**.

Es una versión beta (MVP) hecha con HTML, CSS y JavaScript sobre `canvas`, toda en un solo archivo.

## Cómo jugarlo

Abre `index.html` en cualquier navegador (Chrome, Edge o Firefox). No hay que instalar nada.

Para probar una parte sin jugar las anteriores, usa los botones de **acceso directo** de la pantalla de inicio, o abre:

- `index.html#tiempo2`
- `index.html#tiempo3`

## Controles

| Acción | Cómo |
|---|---|
| Moverse | Flechas o WASD, o clic en el suelo |
| Acariciar a Boceto | E o clic, quieto a su lado |
| Hablar y continuar | E, Espacio o clic |
| Abrir la Bitácora | B o el botón «Bitácora» |
| Tiempo 3 (la Plaza) | Todo se juega con clic |

## La historia

El Creador llega a un pueblo diseñado por **el Adivino**, que decide todo según las visiones de su bola de cristal sin preguntarle nada a nadie. No es malvado: es arrogante y cree que lo que le gusta a él es lo que necesitan los demás.

| Tiempo | Lugar | Qué se aprende | Cómo se juega |
|---|---|---|---|
| 1 · Entrevistar | La Barrera Caprichosa | Preguntar bien y encontrar el problema detrás de la solución que pide la gente | Entrevistas con preguntas a elegir y un Sokoban para reacomodar el muro |
| 2 · Medir y probar | La Estación colapsada | Leer las métricas sin culpar al usuario, probar con la gente y volver a medir | Tablero de métricas y puzzle de rutas con el botón «Testear» |
| 3 · La Plaza | Confrontación con el Adivino | User Persona, evidencia contra opiniones y diseñar en equipo con los usuarios | User Persona, refutar supuestos, la huelga en tiempo real y un dilema con dos finales |

## Boceto, el gato del Creador

Boceto acompaña al Creador en todo el nivel y lo ayuda sin resolverle los retos:

- **Te sigue** por el pueblo y puedes acariciarlo (E o clic) cuando te quedas quieto a su lado.
- **Te muestra el camino** si pasan 20 segundos sin avanzar, o si intentas algo antes de tiempo (mover el muro sin las entrevistas, usar el Tablero sin ver los números).
- **Da pistas** en los dos minijuegos: «Pista de Boceto» en el muro y en el Tablero de rutas, donde marca con una huella la casilla clave.
- **Te devuelve las fuerzas** en el Tiempo 3. Durante la huelga tienes un medidor de **fuerzas**: las visiones y los inventos acumulados te cansan y te mueves más lento. Si te ve agotado, corre a ayudarte.
- **La pausa antes de «¿Vale la pena?»:** la plaza se detiene, Boceto se acerca, ronronea y recuperas las fuerzas para decidir con calma.

El nombre está en la constante `CAT_NAME` de `game.js`.

## Lecciones de la Bitácora

1. Si preguntas «¿verdad que…?», te dicen lo que quieres oír. Pregunta «¿por qué?».
2. La gente te pide soluciones. Tu trabajo es encontrar el problema que hay detrás.
3. Los números te dicen qué falla y cuánto.
4. Si mucha gente falla en lo mismo, el problema es el diseño, no el usuario.
5. Pruébalo con la gente y mide otra vez: así sabes si tu cambio funcionó.
6. Una opinión, por segura que suene, pierde contra lo que dicen y hacen los usuarios.
7. User Persona: un retrato del usuario hecho con lo que investigaste, no con lo que imaginas.
8. Nadie diseña bien solo: se diseña en equipo y con los usuarios.
9. Tú no eres el usuario.

## Estructura

- `index.html`: el juego completo (estilos, lógica, diálogos y dibujo en canvas).
- Usa Google Fonts (Baloo 2, Atkinson Hyperlegible y Kalam). Sin conexión, el juego funciona igual con las fuentes del sistema.

## Estado

Beta. El arte está dibujado por código (sin sprites), siguiendo la referencia visual top-down vectorizada del equipo.
