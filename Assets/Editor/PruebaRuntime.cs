using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prueba de ejecucion: entra en Play mode y recorre el flujo real del juego
/// (menu -> pueblo -> cavernas -> jefe), anotando cualquier excepcion o error de
/// los scripts. Existe porque el juego no se puede probar "a mano" desde la consola.
///
/// Se lanza asi (sin -quit: el editor sale solo al terminar):
///   Unity.exe -batchmode -nographics -projectPath . -logFile log.txt -pruebaRuntime -executeMethod PruebaRuntime.Lanzar
///
/// El estado se guarda en un archivo porque entrar y salir de Play mode recarga
/// el dominio y borra las variables estaticas.
/// </summary>
public static class PruebaRuntime
{
    private static readonly string ArchivoEstado = Path.Combine(Path.GetTempPath(), "warriorpath_prueba_runtime.txt");

    private static readonly (string escena, float segundos, string nota)[] Pasos =
    {
        ("MenuPrincipal", 3f, "menu principal: musica, boton Continuar, UIManager"),
        ("SampleScene",   14f, "pueblo: jugador, monedas, bruja invocando orcos (el bug B1)"),
        ("SampleScene2",  10f, "cavernas: brujas, murcielagos, portal al jefe"),
        ("JefeFinal",     14f, "jefe: fases, embestidas, invocacion de esbirros, barra de vida"),
    };

    private static int paso;
    private static double inicioPaso;
    private static bool capturando;
    private static bool conteoHecho;
    private static bool funcionalEmpezado;
    private static bool funcionalHecho;
    private static bool muerteProvocada;

    [InitializeOnLoadMethod]
    private static void Enganchar()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pruebaRuntime") < 0) return;

        Cargar();

        Application.logMessageReceived += AlRegistrar;
        EditorApplication.update += Tick;
    }

    public static void Lanzar()
    {
        // Cada corrida empieza limpia: el estado viejo quedaria pegado
        if (File.Exists(ArchivoEstado)) File.Delete(ArchivoEstado);

        Debug.Log("[Prueba] Arrancando prueba de ejecucion.");
    }

    // ---------------------------------------------------------------- estado en disco

    private static void Cargar()
    {
        if (!File.Exists(ArchivoEstado)) return;

        foreach (string linea in File.ReadAllLines(ArchivoEstado))
        {
            if (linea.StartsWith("paso=")) int.TryParse(linea.Substring(5), out paso);
            else if (linea.StartsWith("inicio=")) double.TryParse(linea.Substring(7), out inicioPaso);
        }
    }

    private static void Guardar(string linea)
    {
        File.AppendAllText(ArchivoEstado, linea + Environment.NewLine);
    }

    private static void ReguardarPaso()
    {
        List<string> errores = new List<string>();
        if (File.Exists(ArchivoEstado))
        {
            foreach (string l in File.ReadAllLines(ArchivoEstado))
            {
                if (l.StartsWith("error=")) errores.Add(l);
            }
        }

        List<string> salida = new List<string>();
        salida.Add("paso=" + paso);
        salida.Add("inicio=" + inicioPaso);
        salida.AddRange(errores);
        File.WriteAllLines(ArchivoEstado, salida);
    }

    // ---------------------------------------------------------------- registro de errores

    private static void AlRegistrar(string condicion, string pila, LogType tipo)
    {
        if (!capturando) return;
        if (tipo != LogType.Exception && tipo != LogType.Error && tipo != LogType.Assert) return;

        // El editor tiene sus propios errores internos (indexador de busqueda,
        // por ejemplo). Solo interesan los que vienen de codigo del juego.
        bool esDelJuego = pila != null && (pila.Contains("Assembly-CSharp") || pila.Contains("Assets/"));
        bool esDelEditor = pila != null && pila.Contains("UnityEditor");

        if (!esDelJuego && esDelEditor)
        {
            Debug.Log("[Prueba] ignorado (error interno del editor, no del juego): " + condicion);
            return;
        }

        string escena = SceneManager.GetActiveScene().name;
        string traza = pila == null ? "" : pila.Replace("\n", " <- ").Replace("\r", "");
        if (traza.Length > 320) traza = traza.Substring(0, 320);

        Guardar("error=" + tipo + " en " + escena + ": " + condicion.Replace("\n", " | ") + " | TRAZA: " + traza);
    }

    // ---------------------------------------------------------------- maquina de estados

    private static void Tick()
    {
        double ahora = EditorApplication.timeSinceStartup;

        // Se recaptura en cada frame porque al entrar/salir de Play mode se recarga
        // el dominio y las variables estaticas se pierden.
        capturando = EditorApplication.isPlaying;

        // Estado final: ya salimos de Play, se informa y se cierra el editor.
        if (paso >= Pasos.Length)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            Informar();
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            // Entrar en Play con el primer paso (o retomar el paso pendiente)
            EditorSceneManager.OpenScene("Assets/Scenes/" + Pasos[paso].escena + ".unity", OpenSceneMode.Single);

            inicioPaso = ahora;
            capturando = true;
            ReguardarPaso();

            Debug.Log("[Prueba] >>> Entrando en Play: " + Pasos[paso].escena + " (" + Pasos[paso].nota + ")");
            EditorApplication.isPlaying = true;
            return;
        }

        // En las cavernas hay 3 brujas activas: cuento los enemigos que invocan
        if (Pasos[paso].escena == "SampleScene2" && !conteoHecho && ahora - inicioPaso >= 9.0)
        {
            conteoHecho = true;
            int vivos = GameObject.FindGameObjectsWithTag("Orco").Length;
            Debug.Log("[Prueba] enemigos 'Orco' invocados por las brujas en las cavernas: " + vivos
                + " (este es el camino que antes tiraba NullReferenceException todos los frames)");
            return;
        }

        // Pruebas funcionales dentro del nivel del jefe
        if (Pasos[paso].escena == "JefeFinal")
        {
            if (!funcionalEmpezado && ahora - inicioPaso >= 8.0)
            {
                funcionalEmpezado = true;
                PruebasFuncionales();
                return;
            }

            if (muerteProvocada && !funcionalHecho && ahora - inicioPaso >= 12.5)
            {
                funcionalHecho = true;
                ComprobarDerrota();
                return;
            }
        }

        // Dentro de Play: si se cumplio el tiempo del paso, pasar al siguiente
        if (ahora - inicioPaso >= Pasos[paso].segundos)
        {
            Debug.Log("[Prueba] <<< Fin del paso " + Pasos[paso].escena);
            paso++;
            ReguardarPaso();

            if (paso < Pasos.Length)
            {
                inicioPaso = ahora;
                ReguardarPaso();
                Debug.Log("[Prueba] >>> Cargando escena: " + Pasos[paso].escena + " (" + Pasos[paso].nota + ")");
                SceneManager.LoadScene(Pasos[paso].escena);
            }
        }
    }

    // ------------------------- pruebas funcionales -------------------------

    private static void PruebasFuncionales()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Orco");
        Debug.Log("[Prueba] enemigos 'Orco' vivos en la escena del jefe: " + enemigos.Length
            + " (los 4 que invoca cada Bruja del pueblo probaron el camino que antes tiraba NullReferenceException)");

        GameObject barra = GameObject.Find("BarraJefe");
        Debug.Log("[Prueba] barra de vida del jefe existe: " + (barra != null)
            + " | visible: " + (barra != null && barra.activeInHierarchy));

        Personaje jugador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
        if (jugador == null)
        {
            Debug.LogWarning("[Prueba] no encontre al jugador para la prueba de muerte");
            funcionalHecho = true;
            return;
        }

        // Cinco golpes = muerte. Tiene que aparecer la pantalla de derrota (antes
        // la partida quedaba sin jugador y sin salida).
        Debug.Log("[Prueba] DIAGNOSTICO antes de golpear: DatosJugador.vida = " + DatosJugador.vida
            + ", monedas = " + DatosJugador.monedas + ", inventario = " + DatosJugador.inventario.Count);

        Debug.Log("[Prueba] ANTES de golpear: timeScale = " + Time.timeScale
            + " | panel de pausa activo: " + (GameObject.Find("PanelMenu") != null && GameObject.Find("PanelMenu").activeInHierarchy));

        for (int i = 0; i < 5; i++) jugador.CausarHerida();

        Debug.Log("[Prueba] DESPUES de golpear: timeScale = " + Time.timeScale
            + " | panel de pausa activo: " + (GameObject.Find("PanelMenu") != null && GameObject.Find("PanelMenu").activeInHierarchy)
            + " | jugador en escena: " + (UnityEngine.Object.FindAnyObjectByType<Personaje>() != null));
        Debug.Log("[Prueba] se provocaron 5 heridas al jugador; esperando la pantalla de derrota...");
        muerteProvocada = true;
    }

    private static void ComprobarDerrota()
    {
        GameObject panel = GameObject.Find("PanelFinal");
        bool visible = panel != null && panel.activeInHierarchy;

        bool jugadorVivo = UnityEngine.Object.FindAnyObjectByType<Personaje>() != null;

        int finDeJuego = UnityEngine.Object.FindObjectsByType<FinDeJuego>(FindObjectsSortMode.None).Length;

        Debug.Log("[Prueba] pantalla de derrota visible tras morir: " + visible
            + " | jugador en escena: " + jugadorVivo
            + " | DatosJugador.vida = " + DatosJugador.vida
            + " | sistemas FinDeJuego: " + finDeJuego
            + " | Time.timeScale = " + Time.timeScale
            + " | Time.time = " + Time.time.ToString("0.0"));

        if (!visible) Guardar("error=Exception en JefeFinal: la pantalla de derrota no aparecio al morir el jugador");
    }

    private static void Informar()
    {
        int errores = 0;

        if (File.Exists(ArchivoEstado))
        {
            foreach (string l in File.ReadAllLines(ArchivoEstado))
            {
                if (l.StartsWith("error="))
                {
                    errores++;
                    Debug.LogError("[Prueba] " + l.Substring(6));
                }
            }
        }

        if (errores == 0)
        {
            Debug.Log("[Prueba] RESULTADO: el juego corrio los 4 niveles sin una sola excepcion ni error.");
            File.Delete(ArchivoEstado);
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("[Prueba] RESULTADO: " + errores + " errores/excepciones durante la partida.");
            EditorApplication.Exit(1);
        }
    }
}
