# THE WARRIOR PATH

Aventura de acción 2D en Unity (Game Jam «Nivela»). Un guerrero derrotado por el tirano cae desde la
arena en la cima de la montaña y debe atravesar el pueblo, las cavernas y volver a la cima para
tomar revancha: gana experiencia, monedas, pociones y armas hasta volverse capaz de vencerlo.

## Cómo jugar

| Acción | Tecla |
|---|---|
| Moverse | `W` `A` `S` `D` |
| Atacar (espada) | Click izquierdo |
| Cambiar de arma | Rueda del mouse |
| Cargar el arco | Click derecho |
| Disparar la flecha | Click izquierdo (con el arco cargado) |
| Hablar / avanzar diálogo | `E` |
| Pausa | `Esc` |
| Tomar poción del inventario | Click sobre la poción |

## Cómo abrirlo

1. Unity **6000.5.8f1** (la versión exacta del proyecto). Unity Hub → *Add project from disk*.
2. Abrir la escena `Assets/Scenes/MenuPrincipal.unity` y darle Play.

## Cómo compilar

- Desde el editor: menú **The Warrior Path → Compilar para Windows**.
- Por línea de comandos:
  `Unity.exe -batchmode -quit -projectPath . -executeMethod ConstruirContenidoFinal.CompilarWindows`

## Estructura

| Carpeta | Qué hay |
|---|---|
| `Assets/Scenes` | `MenuPrincipal`, `SampleScene` (pueblo), `SampleScene2` (cavernas), `JefeFinal`, `SampleScene3` (cinemática final) |
| `Assets/Scripts` | Scripts del juego (jugador, enemigos, jefe, UI, guardado, música) |
| `Assets/Editor` | Herramientas de editor (construcción de contenido final y build) |
| `Assets/Resources/Audio` | Música y efectos originales generados con `tools/generar_audio.py` |
| `Assets/Resources` | Prefabs de los botones de pociones del inventario |
| `Assets/Sprites`, `Assets/Animaciones`, `Assets/Palettes` | Arte, animaciones y paletas de tiles |
| `tools/generar_audio.py` | Sintetizador que genera la banda sonora (sin dependencias ni servicios externos) |
| `docs/agent` | Diagnóstico del proyecto, plan y registro de trabajo |

El progreso se guarda solo (monedas, vida, inventario y último nivel) al cruzar cada pórtico; el menú
principal ofrece **Continuar** cuando hay partida guardada.

## Créditos y licencias

- Música y efectos de `Assets/Resources/Audio`: originales, generados por el propio proyecto, sin
  restricciones.
- Efectos en `Assets/Sounds`: descargas de Freesound/pixabay. **Revisar la licencia de cada uno antes
  de distribuir el juego.** Se eliminó una pista comercial (`Aitana - SUPERESTRELLA.mp3`) que estaba
  en el proyecto sin licencia.
