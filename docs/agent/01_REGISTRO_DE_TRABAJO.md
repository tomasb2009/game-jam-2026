# Registro de trabajo — THE WARRIOR PATH

Formato por iteración: qué se hizo → evidencia → qué estado tiene (implementado / verificado / pendiente).
Regla de honestidad: **«implementado» no es «compilado» ni «probado en Unity»**. Sin editor instalado
(ver bloqueo en `00_DIAGNOSTICO_Y_PLAN.md`) nada de esto se pudo ejecutar.

---

## Iteración 1 — 2026-10-08

### Hecho

1. **Localización y verificación del proyecto** (sin suposiciones):
   `C:\Users\lauta\OneDrive\Documentos\6to\ONIET\game-jam-2026`, Unity 6000.5.8f1, URP 2D,
   4 escenas en build settings, 20 scripts propios / 1.304 líneas, 53 `.cs` contando NavMeshPlus.
2. **Auditoría estática completa**: parseo del YAML de las 4 escenas y los 5 prefabs con PyYAML
   (resolviendo GUIDs contra los `.meta`), inventario de componentes, campos serializados,
   instancias de prefab y sus overrides, parámetros de animator, y uso de audios.
3. **Punto de recuperación**: tag git `pre-agente-2026-10-08` sobre `f93bee7`.
4. **Correcciones de lógica aplicadas** (5 archivos):
   - `Orco.cs`: resuelve el jugador por tag si `personaje` viene vacío (caso de los orcos que crea la
     Bruja en runtime), tolera `puntosRuta` vacío o con huecos `null`, avanza la ruta por radio de
     llegada en vez de igualdad exacta de float, no lanza excepción si `origenAtaque` es null, y no usa
     `AudioManager.Instance` sin comprobarlo. → ataca **B1 y B13**.
   - `UIManager.cs`: controles de null en `Start`, `SumarMonedas`, `MostrarSprite`,
     `ActivaDesactivaCajaTextos`, `MostrarTextos`, `AdquirirObjeto` y `CerrarTienda`; además no cobra
     si el prefab del objeto no existe. → ataca **B2**.
   - `Tendero.cs`: la tienda sólo se abre si colisiona el jugador, si hay panel asignado y si ya no
     estaba abierta. → ataca **B3**.
   - `Moneda.cs`: la moneda se recoge siempre (suma al HUD si existe, si no al contador global) y no
     asume que exista `AudioManager`. → ataca **B11**.
   - `Personaje.cs`: controles de null en `uiManager` y `AudioManager`; la tecla `K` de debug queda
     dentro de `#if UNITY_EDITOR`; y **morir ya no deja la partida sin jugador**: reinicia datos y
     recarga el nivel en curso con el fundido de `TransicionEscena`. → ataca **B6 y B12**.

### Verificación real ejecutada

- **Sintaxis C#**: parseo con tree-sitter (gramática C#) de los 53 `.cs` del proyecto.
  Resultado: los 20 scripts propios **sin nodos ERROR**; los 2 únicos marcados son código de terceros
  (`NavMeshPlus`, construcciones `#else`), no del equipo.
  Nota importante: los archivos del proyecto están en **ISO-8859-1 con CRLF**, no UTF-8.
  El chequeo se hizo decodificando explícitamente así, y las ediciones se escribieron de vuelta en el
  mismo encoding y con los mismos finales de línea (verificado byte a byte tras cada escritura).
- Balance de llaves y paréntesis idéntico antes/después en cada archivo editado.
- `git diff --stat` como control de alcance de los cambios.

### NO verificado (no afirmable)

Que compile en Unity, que las escenas abran, que el comportamiento en ejecución sea el esperado.
Requiere el editor (bloqueo #1).

### Pendiente / riesgos

- **B7 (crítico)**: no existe el enfrentamiento final ni pantalla de victoria; `SampleScene3` es sólo
  una cinemática sin salida. Requiere decisión de dirección.
- **B8/B9**: no hay música conectada y hay pistas con copyright dentro del proyecto.
- **B10**: sin guardado de progreso.
- **B4/B5**: iconos de arma y estados del arco apuntan todos al mismo sprite (falta arte propio).
- Si otro integrante del equipo abre el proyecto con una versión distinta de Unity, la recarga de
  escenas/paquetes puede diferir. No se hizo `push`: los cambios son locales a propósito.

### Consumo de recursos

Iteración hecha con herramientas locales (parseo propio, python, git) y sin delegar a subagentes ni
hacer llamadas externas: coste de OpenRouter acotado al modelo de esta sesión.

---

## Iteración 2 — 2026-10-08 (mientras se descarga Unity)

Decisiones tomadas por el dueño del producto en esta iteración: instalar Unity 6000.5.8f1;
generar música/efectos propios y sacar las pistas comerciales; escena nueva de jefe final con
pantallas de victoria y derrota; guardado de progreso con "Continuar"; y commits con push a
`origin/main`.

### Hecho

1. **B14 (progresión) — corregido**: `ReiniciarDatos` ya no borra la partida al entrar a cada nivel.
   Se le agregó un campo `reiniciarAlEntrar` con valor por defecto `false`; la partida nueva la inicia
   el menú. Sin esto, cada cambio de nivel tiraba abajo monedas, vida e inventario.
2. **Guardado de progreso (con PlayerPrefs)** en `DatosJugador`: `Guardar`, `Cargar`, `TienePartida`,
   `BorrarPartida`, más `nivel` (último nivel alcanzado). Autoguardado al cruzar un pórtico,
   al morir/reintentar y al ganar.
3. **Menú**: `Jugar` ahora arranca partida nueva (borra el progreso anterior); `Continuar` carga la
   partida guardada y entra al último nivel; el botón se oculta solo si no hay partida
   (`botonContinuar`, que el script de editor crea y cablea).
4. **Jefe final**: `JefeFinal.cs` reutiliza la IA de `Orco.cs` y le agrega 3 fases (velocidad, rango y
   enfriamiento de ataque), embestidas con daño por contacto e invocación de esbirros, más barra de
   vida en pantalla. `Orco.cs` ahora expone vida actual/inicial, eventos `AlRecibirDano`/`AlMorir` y
   setters; nada de eso cambia el comportamiento anterior.
5. **Robustez de IA**: si una escena no tiene NavMesh horneado, los enemigos ya no quedan plantados:
   persiguen al jugador moviendo el transform. Antes, un NavMesh faltante dejaba al enemigo inmóvil.
6. **Pantallas de victoria y derrota** (`FinDeJuego.cs`): UI construida por código (canvas, títulos y
   botones), así que **no hizo falta tocar ninguna escena**. Derrota ofrece reintentar el nivel
   conservando monedas e inventario, o volver al menú; victoria lleva a la cinemática final.
   `Personaje.Morir()` ahora muestra esa pantalla en vez de dejar la partida sin jugador.
7. **Audio original**: `tools/generar_audio.py` sintetiza 5 pistas (menú, pueblo, caverna, jefe,
   epílogo) y 5 efectos (carga y disparo de arco, victoria, derrota, golpe del jefe) sin usar ningún
   servicio externo. Quedan en `Assets/Resources/Audio`. `MusicaFondo.cs` mantiene la música entre
   escenas y cambia de tema por nivel, creándose sola al arrancar (sin tocar escenas).
   El arco ahora suena (el código tenía un `// TODO: sonido de carga`).
8. **Pista comercial eliminada** del repositorio: `Assets/Sounds/Aitana - SUPERESTRELLA.mp3`.
   Quedan las de Freesound/pixabay (`666herohero-slash`, `edr-video-game-hit-noise`,
   `freesound_community-young-man-being-hurt`): son gratuitas, pero **conviene confirmar la licencia
   de cada una** antes de distribuir; quedaron fuera de esta limpieza para no borrar audios que sí
   están en uso.
9. **Script de editor `Assets/Editor/ConstruirContenidoFinal.cs`**: crea la escena del jefe (copia la
   base para heredar cámara, UI, jugador y NavMesh horneado; quita enemigos y NPCs del nivel anterior;
   coloca el jefe con 24 de vida y sus esbirros), apunta el pórtico de `SampleScene2` a la escena del
   jefe, agrega y cablea el botón "Continuar" del menú, pone el cierre a la cinemática, ordena las
   build settings y compila para Windows. Es idempotente y se puede correr en lote.

### Verificación real ejecutada

- Sintaxis C# de los **58** `.cs` del proyecto con tree-sitter: todos los archivos propios sin nodos
  ERROR (los 2 marcados son código de terceros de NavMeshPlus, no del equipo).
- Los 10 archivos de audio generados se validaron leyéndolos: WAV PCM 16 bits mono 22050 Hz,
  sin recortes (pico 0,75), duraciones de 13 a 28 s las pistas y 0,3 a 2,2 s los efectos,
  y microfade en la unión del loop para que no chasquee al repetir.
- Balance de llaves y paréntesis idéntico antes/después en cada archivo editado; encoding
  ISO-8859-1 y CRLF preservados (el equipo escribe así, no en UTF-8).

### Corrección de mi propio diagnóstico

**B4 y B5 quedan retirados**: los había marcado como defectos porque `spriteEspada`/`spriteArco` y los
tres estados del arco apuntan al mismo PNG, pero al revisar los `fileID` de los sub-sprites son
**distintos** (los assets estaban bien hechos). Error de método: audité la ruta del archivo y no la
referencia al sub-sprite. Queda anotado en el diagnóstico.

### Bloqueos y pendientes

- **Push rechazado (403)**: el repositorio es de `tomasb2009` y la cuenta de Lauti no tiene permiso de
  escritura. Los 2 commits están locales (tag de rescate `pre-agente-2026-10-08`).
- Falta compilar y probar todo esto en Unity: es lo que sigue apenas termine la instalación.
- La arena del jefe es, por ahora, una versión limpia del nivel de cavernas: hay que diseñarla como
  arena real (geometría, obstáculos, ritmo) en una próxima iteración.
- Balance de dificultad sin ajustar (requiere jugar el juego).

### Consumo

Sin subagentes y sin servicios externos. Todo local (Python, tree-sitter instalado en un entorno
temporal fuera del proyecto, git y curl). El gasto de OpenRouter se limita al modelo de esta sesión.

---

## Iteración 3 — 2026-10-08 (Unity instalado, falta la licencia)

- **Unity 6000.5.8f1 descargado y verificado**: `UnitySetup64-6000.5.8f1.exe`, 4.063.082.688 bytes,
  idénticos al tamaño oficial publicado por Unity para la revisión `5cb7df797b7d` (la misma que pide
  el proyecto). Está en `C:\Users\lauta\UnityInstall`.
- **Instalado** por el dueño del producto en `C:\Program Files\Unity 6000.5.8f1\Editor`
  (el instalador exige elevación de administrador: los pedidos de UAC que lanzo desde la consola se
  cancelan solos, así que ese paso no lo puede hacer el agente).
- **Verificado que el editor está completo**: `Unity.exe`, `Data/Managed/UnityEditor.dll`,
  módulos de UnityEngine, `Data/PlaybackEngines/windowsstandalonesupport` (build de Windows) y
  `Data/Resources/unity default resources` presentes.
- **Bloqueo actual**: al ejecutarlo (tanto en lote como en modo ventana) responde
  `No valid Unity Editor license found. Please activate your license.` y termina con código 198.
  Sólo queda el `UnityEntitlementLicense.xml` de julio de 2025, que ya no tiene permisos válidos
  (`Found 0 entitlement groups`). **La activación necesita la cuenta de Unity del dueño**: el agente
  no maneja credenciales y no debe hacerlo.

### Siguiente paso apenas haya licencia (no requiere más intervención)

1. `Unity.exe -batchmode -nographics -quit -projectPath <proyecto> -logFile <log>` para generar
   `Library/` e importar: acá se ve por primera vez si los 58 `.cs` compilan de verdad.
2. Revisar los errores que aparezcan y corregirlos.
3. `-executeMethod ConstruirContenidoFinal.Construir` para crear la escena del jefe, apuntar el
   pórtico, agregar el botón Continuar y ordenar las build settings.
4. `-executeMethod ConstruirContenidoFinal.CompilarWindows` para comprobar que el juego se puede
   distribuir, y reportar con la evidencia de los logs.
