# THE WARRIOR PATH — Diagnóstico y plan de trabajo

> Generado por el agente autónomo de Hermes. Fecha: 2026-10-08.
> Todo lo que figura como «confirmado» está respaldado por lectura directa de archivos
> del proyecto (no por suposiciones). Lo que no se pudo verificar se marca explícitamente.

## 1. Proyecto identificado

| Dato | Valor |
|---|---|
| Ruta | `C:\Users\lauta\OneDrive\Documentos\6to\ONIET\game-jam-2026` |
| Motor | Unity **6000.5.8f1** (`ProjectSettings/ProjectVersion.txt`) |
| Pipeline | URP 2D (`com.unity.render-pipelines.universal 17.6.0`), Cinemachine 3.1.7, Input System 1.20, NavMeshPlus (vendorizado en `Assets/NavMeshPlus-master`) |
| Escenas en build | `MenuPrincipal`, `SampleScene`, `SampleScene2`, `SampleScene3` |
| Scripts propios | 20 archivos `.cs` en `Assets/Scripts` (1.304 líneas) |
| Repositorio | `github.com/tomasb2009/game-jam-2026`, rama `main`, un único commit (`f93bee7 Initial Unity project`) |
| Estado git local | limpio, sin cambios pendientes |

Verificaciones que confirman que **éste** es el proyecto correcto: existe `ProjectSettings/ProjectVersion.txt`,
`Assets/Scenes/*.unity` referenciadas en `EditorBuildSettings.asset`, y los prefabs de enemigos
(`Orco.prefab`, `OrcoFinal.prefab`, `Murcielago Variant.prefab`) contienen `Orco.cs` / `AtaqueOrco.cs`.

## 2. BLOQUEO PRINCIPAL (requiere decisión humana)

**En esta máquina no hay Unity instalado.** Evidencia:

- `%APPDATA%/UnityHub/hubInfo.json` declara `executablePath: C:\Program Files\Unity Hub\Unity Hub.exe`,
  pero **esa carpeta no existe**; tampoco existe `C:\Program Files\Unity*` ni ningún editor en
  `C:\Program Files\Unity\Hub\Editor`.
- La búsqueda de `Unity.exe` en `C:\Users\lauta`, `C:\Program Files`, `C:\Projects` y `C:\Games`
  no devuelve resultados.
- `Assets/…/Library/` no existe (nunca se hizo import), lo que confirma que el proyecto
  **nunca se abrió en un editor en esta PC**; los `AppData` de Unity Hub son de julio 2025.
- Sólo quedan restos de licencias: `%LOCALAPPDATA%/Unity/licenses/` y logs del cliente de licencias.

Consecuencia real y honesta: **no puedo compilar, ni ejecutar, ni construir el juego en esta máquina.**
Todo lo que implemente quedará con estado «escrito y revisado a mano», no «compilado ni validado en Unity».
Instalarlo implica ~10 GB de descarga y el inicio de sesión con la cuenta de Unity del usuario
(licencia personal, gratuita, pero requiere credenciales que no me corresponden).
→ Registrado como **NEEDS_HUMAN_DECISION #1**.

## 3. Diagnóstico del contenido (hallazgos confirmados)

### 3.1 Lo que está bien y conviene conservar

- Arquitectura de escenas coherente: menú → pueblo (`SampleScene`) → cavernas (`SampleScene2`) → cierre (`SampleScene3`),
  con pórticos (`Portal.cs`) que encadenan `SampleScene → SampleScene2 → SampleScene3`.
- Los 20 scripts propios están correctamente referenciados en escenas y prefabs
  (verificado resolviendo los GUID de `m_Script` contra los `.meta`; no hay scripts huérfanos).
- Los parámetros de animación coinciden con los que usan los scripts:
  `AnimacionPersonaje` = Camina/Ataca/Muere; `AnimacionOrco` = Ataca/Muere; `Bruja` = Invocar. Sin desajustes.
- Sistemas existentes: combate melee (`Espada.cs`), arco y flecha (`Personaje.cs` + `Flecha.cs`),
  retroceso + flash de daño + sacudida de cámara (`Orco.cs`, `SpriteFlash.shader`, `CinemachineImpulseSource`),
  monedas, tienda, inventario de pociones, diálogos, pausa, transición con fundido (`TransicionEscena.cs`),
  y persistencia entre escenas en memoria (`DatosJugador.cs`, estático).
- Arte propio abundante: 5 hojas de sprites del personaje, 4 del goblin/orco, bruja, murciélago, tendero,
  tilesets de suelo/paletas, 84 imágenes de cinemática y 8 audios.

### 3.2 Defectos confirmados (con la evidencia)

| # | Severidad | Qué | Evidencia |
|---|---|---|---|
| B1 | **Alta** | Los orcos/orcos finales **instanciados en runtime** por `BrujaSpawner` nacen con `personaje = null` y `puntosRuta = [null×4]` (valores por defecto del prefab). `Orco.Update()` hace `personaje.position` sin comprobar → `NullReferenceException` en cada frame. La mecánica «la bruja invoca 4 orcos» queda rota. | `Orco.prefab`: `personaje: {fileID: 0}` y `puntosRuta` con 4 entradas `{fileID: 0}`. Las instancias colocadas a mano en la escena **sí** tienen override (`propertyPath: personaje` / `puntosRuta.Array.data[i]`), pero `BrujaSpawner` usa los valores del prefab. |
| B2 | **Alta** | `UIManager.Start()` escribe en `textoMonedas.text` y luego itera el inventario usando `panelEquipo`. En `MenuPrincipal.unity` ese componente existe pero **todos sus campos están vacíos** (`textoMonedas`, `imagenBarraVida`, `cajaTexto`, `textoDialogo`, `panelEquipo` = `{fileID: 0}`, `spritesBarraVida` = lista vacía) → excepción al entrar al menú. | Auditoría de campos serializados de `UIManager` en `MenuPrincipal.unity`. |
| B3 | Media | `Tendero.cs` abre la tienda y **congela el tiempo ante cualquier colisión** (`OnCollisionEnter2D` sin comprobar tag ni estado). Cualquier cuerpo con collider que lo toque abre la tienda. | `Assets/Scripts/Tendero.cs` (12 líneas, sin filtro). |
| ~~B4~~ | **RETIRADO** | Falso positivo mío: `spriteEspada` y `spriteArco` sí son **sub-sprites distintos** del mismo PNG (`fileID 1529004468571396027` vs `-8733595206056069556`). Al auditar miré la ruta del PNG y no el `fileID` del sub-sprite. El icono de arma está bien. | `SampleScene.unity:5528-5529` |
| ~~B5~~ | **RETIRADO** | Mismo error de método: son tres sub-sprites distintos de `frames.png` (`-3227606743446095257`, `-800886026776649501`, `3203698396550408301`). Los estados del arco sí estaban cableados. | `SampleScene.unity:26685-26687` |
| B6 | **Alta (jugabilidad)** | Morir no tiene salida: `Personaje.Morir()` ejecuta `DatosJugador.Reiniciar()` y `Destroy(gameObject)`. **No hay pantalla de derrota, ni reinicio, ni respawn**: el juego queda sin jugador. | `Personaje.cs` líneas finales. |
| B7 | **Alta (contenido)** | **No existe el enfrentamiento final.** `SampleScene3` contiene sólo `Main Camera`, `EventSystem`, `Cutscene46`, `Canvas`, `Text (TMP)` y una `CinemachineCamera`: es una cinemática y **no tiene pórtico de salida ni jefe**. `OrcoFinal.prefab` existe pero sólo se usa en `SampleScene`. Tampoco hay pantalla de victoria. | Parseo de `SampleScene3.unity` (6 GameObjects) y `EditorBuildSettings`. |
| B8 | Media | No hay música: ningún `AudioSource` del proyecto tiene `m_AudioClip` asignado (`Music` en `MenuPrincipal` y `SampleScene`, y el `AudioSource` del `AudioManager` están vacíos), pese a existir `musica de fondo.wav`. Todos los `AudioClip` de efectos sí están cableados. | Auditoría de `m_AudioClip` en escenas. |
| B9 | Media (legal) | Hay pistas comerciales con copyright dentro de `Assets/Sounds`: `Aitana - SUPERESTRELLA.mp3` y `666herohero-slash-21834.mp3`. Distribuir un juego con música comercial sin licencia es un riesgo legal. | Listado de `Assets/Sounds`. |
| B10 | Baja | Sin guardado de progreso: `DatosJugador` es memoria estática; no hay `PlayerPrefs` ni archivo de partida. Al cerrar, se pierde todo. | `DatosJugador.cs`. |
| B11 | Baja | `Moneda.cs` sólo suma si hay suscriptor del evento (`if (sumaMoneda != null)`); sin `UIManager` la moneda ni se destruye. `AudioManager.Instance` se usa sin comprobar null. | `Moneda.cs`, `Flecha.cs`, `Orco.cs`. |
| B12 | Baja | Tecla de debug: `K` causa daño al jugador en build final (`Personaje.Update`). | `Personaje.cs`. |
| B13 | Baja | `Orco.Update()` compara posiciones con `==` (igualdad exacta de float) para avanzar por la ruta y no valida que `puntosRuta` tenga elementos. | `Orco.cs`. |
| B14 | **Alta (progresión)** | `ReiniciarDatos` está activo en `SampleScene` y `SampleScene2` (`m_IsActive: 1`) y su `Awake()` llamaba a `DatosJugador.Reiniciar()`: **cada cambio de nivel borraba monedas, vida e inventario**, contradiciendo lo que promete el comentario de `UIManager`. | `m_IsActive` de ambos GameObjects + `ReiniciarDatos.cs`. |
| B15 | Baja | El pórtico de `SampleScene2` lleva a `SampleScene3`, que es solo una cinemática sin salida: no hay a dónde ir después. | `Portal.escenaDestino` de `SampleScene2` + contenido de `SampleScene3`. |

### 3.3 No verificado (no puedo afirmarlo)

- Que el juego compile hoy, que las escenas abran sin errores, tiempos de carga, rendimiento real,
  comportamiento en NavMesh 2D, y que la tienda/diálogos funcionen en ejecución.
  Nada de eso es comprobable sin el editor.

## 4. Plan priorizado

1. **P0 — Desbloquear verificación**: decidir si se instala Unity 6000.5.8f1 (necesita al humano).
2. **P1 — Corregir defectos de lógica ya confirmados**: B1, B2, B3, B6, B11, B12, B13 (seguros, sin arte nuevo).
3. **P2 — Cerrar el arco de juego**: escena de jefe final + pantalla de victoria + pantalla de derrota con reintento (B6, B7).
4. **P3 — Audio**: música de fondo y sonido de carga de arco (B8), resolviendo el problema legal (B9).
5. **P4 — Guardado de progreso** (B10, implementado) y pulido visual/UX.
6. **P5 — Balance de dificultad** y pase final de prueba en el editor.

## 4bis. Permisos de repositorio (hallado al intentar subir)

`git push origin main` **falla con 403**: el repo es de `tomasb2009` y la cuenta `AllaDios` (Lauti) no
tiene permiso de escritura. Los commits quedan locales. Para subir hace falta: (a) que el dueño dé
permisos de colaborador, (b) un fork + pull request, o (c) que el dueño haga el push.

## 5. Registro de trabajo

Ver `docs/agent/01_REGISTRO_DE_TRABAJO.md` (se actualiza en cada iteración).
