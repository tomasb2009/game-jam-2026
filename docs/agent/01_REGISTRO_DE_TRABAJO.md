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
