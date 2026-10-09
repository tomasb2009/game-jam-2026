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

---

## Iteración 4 — 2026-10-08 21:34 (compilación, contenido final y build, todo verificado)

Con la licencia activada por el dueño, corrí el editor en lote y **por primera vez el proyecto se
compiló de verdad**.

### Resultados reales (logs en `%LOCALAPPDATA%/hermes/cache/scratch/`)

| Paso | Comando | Resultado |
|---|---|---|
| Import + compilación | `Unity.exe -batchmode -nographics -quit -logFile wp_import.log` | **206 ensamblados compilados.** 3 errores, los 3 míos en el script de editor (`List<string>` pasado donde se esperaba `string[]`, y `FindFirstObjectByType` obsoleto). **Los 20 scripts del juego y todo lo que agregué no dieron ni un error.** |
| Construcción de contenido | `-executeMethod ConstruirContenidoFinal.Construir` | 0 errores. Log propio: escena del jefe creada desde `SampleScene2`, arena limpia (12 objetos del nivel anterior quitados), jefe colocado en (3,00, -3,20), portal de `SampleScene2` apuntando a `JefeFinal`, botón Continuar agregado al menú, cierre agregado a la cinemática, build settings ordenados. |
| Build de Windows | `-executeMethod ConstruirContenidoFinal.CompilarWindows` | **`[WarriorPath] Build: Succeeded errores=0`** → `Builds/Windows/TheWarriorPath.exe` |
| Verificación de escenas | `-executeMethod VerificarEscenas.Verificar` | **5 escenas, 0 scripts faltantes, 0 problemas.** Avisos esperados en la cinemática final (sin UI ni pórtico, por diseño). |

### Comprobaciones puntuales sobre los archivos que escribió Unity

- `Assets/Scenes/JefeFinal.unity` existe (2,9 MB) y el jefe tiene `vidaOrco = 24`, `rangoAtaque = 1.6`,
  `cooldownAtaque = 1.8` y `personaje` apuntando al jugador.
- `MenuPrincipal.unity`: `botonContinuar` apunta a un GameObject `BotonContinuar`, con etiqueta
  "Continuar" y llamada persistente a `MenuPrincipal.Continuar` (verificado en el YAML:
  `m_MethodName: Continuar`, `m_TargetAssemblyTypeName: MenuPrincipal, Assembly-CSharp`).
- `SampleScene2.unity`: `Portal.escenaDestino = JefeFinal`.
- `SampleScene3.unity`: `CierreDeCinamatica` con `FinDeCinematica` (42 s → menú principal).
- Build settings en orden: MenuPrincipal → SampleScene → SampleScene2 → JefeFinal → SampleScene3.

### Lo que NO está verificado (y por qué)

- **Nadie jugó el juego todavía.** Las pruebas de humo que intenté desde la consola no sirven:
  con `-nographics` el dispositivo gráfico es nulo y el jugador no llega a cargar la escena, y sin
  `-nographics` el proceso no consigue crear el dispositivo (mi consola no tiene sesión interactiva).
  Falta una partida real: menú → pueblo → cavernas → jefe → victoria.
- Balance de dificultad, sensación de los controles y diseño de la arena: requieren jugar.

### Estado del repositorio

5 commits locales (último: `b163634`). El `push` sigue bloqueado por permisos (403, cuenta sin
escritura en el repo del compañero). Se agregó `*.slnx` al `.gitignore` (Unity generó ese archivo).

---

## Iteración 5 — 2026-10-08 21:58 (prueba de ejecución en Play mode: 3 defectos más, encontrados y corregidos)

Como no puedo jugar a mano desde la consola, escribí `Assets/Editor/PruebaRuntime.cs`: abre el editor,
entra en Play mode y recorre el flujo real del juego (**Menú 3 s → pueblo 14 s → cavernas 10 s →
nivel del jefe 14 s**), capturando cualquier excepción o error que vengan de código del juego
(los errores internos del editor, como el indexador de búsqueda, se filtran aparte).
El estado vive en un archivo porque entrar en Play recarga el dominio y borra las variables estáticas.

### Resultado final (corrida 8)

```
[Prueba] enemigos 'Orco' invocados por las brujas en las cavernas: 17  (el camino que antes tiraba NullReferenceException)
[Prueba] barra de vida del jefe existe: True | visible: True
[Prueba] ANTES de golpear: timeScale = 0 | panel de pausa activo: False
[Prueba] pantalla de derrota visible tras morir: True | jugador en escena: False | DatosJugador.vida = 0 | Time.timeScale = 0
[Prueba] RESULTADO: el juego corrio los 4 niveles sin una sola excepcion ni error.
```

### Defectos que encontró la prueba (los tres corregidos)

| # | Qué pasaba | Causa raíz (medida, no supuesta) | Arreglo |
|---|---|---|---|
| B16 | **Morir no terminaba**: el jugador se quedaba tirado, sin pantalla de derrota y sin poder seguir. La partida quedaba trabada. | `Personaje` usaba `Invoke(nameof(Morir), 1f)`, que corre con **tiempo escalado**. El juego puede estar legítimamente en `Time.timeScale = 0` en ese instante (tienda abierta o pausa) y entonces el `Invoke` **nunca dispara**. Lo confirmé midiendo `timeScale = 0` justo antes de morir, con el panel de pausa inactivo. | `MorirConRetraso()` como corrutina con `WaitForSecondsRealtime`, que no depende del tiempo escalado. Verificado: ahora la derrota aparece y el jugador se destruye. |
| B17 | **Excepción en el menú principal** (`ArgumentOutOfRangeException`) al entrar. | `Desactivar.cs` llamaba a `anim.GetCurrentAnimatorStateInfo(0)` sin comprobar que el Animator tenga controller o capas. | Se corta el bucle si no hay `runtimeAnimatorController` o `layerCount == 0`. |
| B18 | Un nivel podía arrancar con **0 corazones** (jugador "muerto" desde el inicio: no recibía heridas ni disparaba la derrota). | `DatosJugador.vida` quedaba en 0 tras una muerte y `Personaje.Start()` lo copiaba tal cual. | Si `DatosJugador.vida <= 0` al arrancar el nivel, se restaura a la vida máxima. |

Nota honesta: el escenario de la prueba (el jugador quieto, empujado por 17 enemigos hasta el tendero
en las cavernas) es lo que dejó `timeScale` en 0 y destapó B16. No fue un fallo de la prueba sino un
caso real que antes quedaba sin salida.

### Verificado en ejecución (no por lectura de código)

- La Bruja invoca enemigos y **ninguno tira NullReferenceException** (17 vivos en las cavernas): el
  defecto B1 está resuelto de verdad.
- El jefe crea su barra de vida (`BarraJefe` visible) → `FinDeJuego` se instancia solo en la escena nueva.
- Al morir, el jugador se destruye y aparece `PanelFinal` → el arco de derrota funciona.
- Los 4 niveles corren sin excepciones de código de juego.

### Pendiente

- **Jugarlo con manos humanas** (sensación de control, dificultad, música) y diseñar la arena del jefe
  como arena real, no como el nivel de cavernas limpio.
- El `push` sigue bloqueado por permisos (403).

---

## Iteración 6 — 2026-10-08 22:27 (ajustes de diseño pedidos por el dueño del producto)

Pedidos y cómo quedaron, con la medición que lo respalda.

### 1. Zona 1 (pueblo): sin bruja, solo los goblins colocados a mano

- Se **eliminó el GameObject `Bruja`** de `SampleScene` (estaba desactivado, pero con su `BrujaSpawner`
  listo para invocar). Quedan los 6 goblins colocados a mano.
- Log del script: `Zona 1: bruja(s) quitadas = 1`.

### 2. Zona 2 (cavernas): menos invocaciones, con tope

- `BrujaSpawner` ahora tiene `esbirrosPorInvocacion` (1) y un **tope de vivos** (`maximoVivos` = 6),
  con una cuenta compartida entre brujas que se descuenta cuando muere un esbirro (componente `Esbirro`).
- Las 4 brujas quedaron en 1 esbirro cada 5 s, tope 6.
- Medido en ejecución: **5 enemigos vivos contra los 17 de antes**.

### 3. La bruja se mueve y tiene su propio ataque

- Nuevo `Bruja.cs`: huye del jugador cuando se le acerca (y lo espera si está lejos), esquiva paredes
  con un chequeo de colisión lateral, y le lanza un proyectil propio.
- Nuevo `ProyectilBruja.cs` + prefab `Assets/Prefabs/ProyectilBruja.prefab` (creado desde `Flecha.prefab`;
  `Flecha.cs` no servía porque solo lastima a los de tag `Orco`).
  El Animator de la bruja ya tenía `BrujaCamina` como estado por defecto, así que camina sola al moverse.
- Medido en ejecución: al acercarle el jugador a 2 unidades, la bruja **recorrió 4,16 unidades** y
  terminó **a 9,86 unidades** (mantiene distancia), se **vio un proyectil suyo en vuelo** y el jugador
  **perdió 2 corazones** a su lado. Y ningún error en el log.

### 4. Interfaces de pausa y tienda (ajuste concreto, sin rediseño)

- Se borró el **texto de prueba** que había quedado en la escena (`dasdasdasdasddsadasdasd`, en el objeto
  `Content`) en las tres escenas de juego.
- Etiquetas de la tienda coherentes: `Pocion Salud Pequeña $1`, `Pocion Salud Mediana $2`,
  `Pocion Velocidad $5` (solo caracteres que la fuente bitmap ya dibujaba).
- `UIManager`: nuevo campo `textoAviso` (creado en `PanelTienda`) con mensajes al comprar,
  al no llegar las monedas ("No tenes monedas suficientes") y con el inventario lleno.
- `MenuPausa.ReiniciarNivel()` + botón **"Reiniciar nivel"** clonado del de Reanudar y cableado
  (reinicia el nivel conservando monedas, inventario y el último nivel guardado).

### 5. La tienda no se abre sola al cambiar de escena

- `Tendero`: ignora cualquier choque durante los primeros 1,5 s de la escena (el jugador puede aparecer
  pegado al tendero) y solo abre con el jugador.
- `UIManager.Start()` fuerza `Time.timeScale = 1`, para que ninguna escena herede el tiempo congelado.
- Evidencia indirecta de que funciona: en la corrida de prueba anterior el tiempo llegaba congelado a la
  arena (`timeScale = 0`), y en la última llega en `1`.

### Verificación de esta iteración

| Prueba | Resultado |
|---|---|
| Compilación | 0 errores |
| Ajustes aplicados | Proyectil creado; Zona 1: 1 bruja quitada; Zona 2: 4 brujas ajustadas; en cada escena 1 texto de prueba limpiado, 3 etiquetas y botón reiniciar |
| Integridad de escenas | 5 escenas, 0 scripts faltantes, 0 problemas |
| Ejecución (Play mode, 4 niveles) | Sin una sola excepción; 5 enemigos vivos; bruja huyendo y disparando; derrota funcionando |
| Build de Windows | `Build: Succeeded errores=0` |

### Pendiente

- Jugarlo con manos humanas: cuánto molesta que la bruja huya, si el tope de 6 es el correcto y si el
  proyectil se siente justo.
- La arena del jefe sigue siendo la geometría de las cavernas limpia.
- El `push` sigue bloqueado por permisos (403).

---

## Iteración 7 — 2026-10-08 22:44 (bugs reportados al jugar)

Lo que reportó el dueño y lo que se encontró al revisarlo.

### 1. El goblin se veía como ojo (murciélago)

- **Causa real**: el clip `OrcoCamina.anim` (que es el estado por defecto del goblin) animaba el
  `m_Sprite` con los **4 sprites de `Eye-Bat.png`**. La animación de caminar del goblin había quedado
  con las imágenes del murciélago.
- **Arreglo**: se reescribieron las curvas de sprite (`AnimationUtility.SetObjectReferenceCurve`) con
  los sprites de `Golblin-S1-Walk-Sheet.png`. Verificado: `OrcoCamina.anim` ahora referencia 10 veces
  el sheet del goblin y 0 veces el del ojo.
- Además `OrcoAtaca.anim` y `OrcoMuere.anim` **no tenían ninguna curva de sprite** (por eso el golpe no
  cambiaba la imagen): se les creó la curva con `Goblin-S1-Attack-Sheet` (4 sprites) y
  `Golblin-S1-Damage-Sheet` (3 sprites).
- El murciélago quedaba roto por este arreglo (compartía controlador), así que se le dio el suyo:
  `Assets/Animaciones/OrcoFinal (2).controller`, que ya existía sin usar, más los triggers `Ataca` y
  `Muere` que dispara `Orco.cs`.

### 2. Las brujas no recibían daño

- Tenían collider (CapsuleCollider2D) pero **tag `Untagged`**, y `Espada`/`Flecha` solo dañaban a los de
  tag `Orco`, así que eran inmortales.
- **Arreglo**: `Bruja.cs` ahora tiene vida (3), recibe golpes (`RecibirGolpe`, mismo nombre ASCII que se
  le agregó a `Orco.cs`), parpadea, retrocede y muere; `Espada`/`Flecha` buscan `Orco` **o** `Bruja`.
- Verificado en ejecución: 4 golpes espaciados → "SI: la bruja desapareció al morir".

### 3. El proyectil de la bruja era una flecha

- Se generó un sprite propio de **bola de magia verde** (`Assets/Sprites/ProyectilBruja.png`, 24x24 con
  alfa, hecho con un codificador PNG propio) y se asignó al prefab (con su collider ajustado a 0,5x0,5).

### 4. La tienda aparecía al final del nivel 3

- Estado real: **`PanelTienda` estaba activo en `SampleScene2` y en `JefeFinal`** (venía así en los
  archivos de escena). En el nivel del jefe, además, no debería existir.
- **Arreglo**: en el pueblo y las cavernas la tienda queda cerrada; en el nivel del jefe se **borró**
  la tienda y el tendero. `UIManager.Start()` además fuerza `tienda.SetActive(false)` y `timeScale = 1`.

### 5. Botones del menú principal desalineados

- Estaban **superpuestos**: los dos botones miden 157 px de alto y el clon de "Continuar" estaba solo
  90 px debajo del de EMPEZAR.
- **Arreglo**: mismo ancho, misma x, y separación de alto + 20 px (y=182 y y=5).

### 6. "No puedo probar la pelea del jefe"

- La tienda activa en la escena del jefe tapaba la pantalla; ya está resuelta (punto 4).
- Dato para el balance que salió de la prueba: el jugador **llega a la arena con 2 de 5 corazones** si
  no cura, y **muere a los 4 s quieto**. Puede que la dificultad esté alta.

### Verificación

| Prueba | Resultado |
|---|---|
| Compilación | 0 errores |
| Integridad de escenas | 5 escenas, 0 scripts faltantes, 0 problemas |
| Ejecución (Play mode) | Sin excepciones; bruja eliminable; tienda fuera del jefe; 5 enemigos en cavernas |
| Build de Windows | `Build: Succeeded errores=0` |

### Pendiente de aclarar con el dueño

- **"Doble sonido"**: no pude reproducirlo. Las `AudioSource` de las escenas están **sin clip**
  asignado, así que no hay música duplicada por ese lado; la música la pone un único `MusicaFondo`.
  Falta saber qué sonido exactamente se escucha dos veces.
