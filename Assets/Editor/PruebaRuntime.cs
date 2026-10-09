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
        ("SampleScene2",  12f, "cavernas: brujas, murcielagos, portal al jefe"),
        ("JefeFinal",     14f, "jefe: fases, embestidas, invocacion de esbirros, barra de vida"),
    };

    private static int paso;
    private static double inicioPaso;
    private static bool capturando;
    private static bool menuRevisado;
    private static bool conteoHecho;
    private static bool golpesDados;
    private static int golpesLanzados;
    private static float proximoGolpe;
    private static Transform brujaGolpeada;
    private static string resultadoGolpes = "no probado";
    private static bool posicionesTomadas;
    private static int vidaAntesDelAtaque = -1;
    private static int proyectilesVistos;
    private static int cuadros;
    private static float distanciaBrujaJugador;
    private static Transform[] brujasPrevias;
    private static Vector2[] posicionesBrujas;
    private static bool vidaArenaRegistrada;
    private static float muerteEnArena = -1f;
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

        // Menu: compruebo que los textos tengan fuente con glifos (si la fuente esta
        // vacia, TMP no dibuja nada y el menu se ve como una pantalla vacia)
        if (Pasos[paso].escena == "MenuPrincipal" && !menuRevisado && ahora - inicioPaso >= 1.5)
        {
            menuRevisado = true;

            // Ojo: TMP_Text es abstracta y FindObjectsByType no la resuelve; hay que
            // buscar la clase concreta TextMeshProUGUI.
            TMPro.TMP_Text[] textos = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
            int conGlifos = 0;
            string detalle = "";

            foreach (TMPro.TMP_Text tt in textos)
            {
                if (tt == null) continue;

                int glifos = tt.font != null && tt.font.characterTable != null ? tt.font.characterTable.Count : 0;
                if (glifos > 0) conGlifos++;

                detalle += " [" + tt.text + " glifos=" + glifos + "]";
            }

            Debug.Log("[Prueba] menu: textos activos = " + textos.Length + ", con fuente usable = " + conGlifos);
            Debug.Log("[Prueba] detalle del menu:" + detalle);

            // Todos los textos, incluso los de objetos desactivados
            TMPro.TMP_Text[] todos = UnityEngine.Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            string todo = "";
            foreach (TMPro.TMP_Text tt in todos)
            {
                if (tt == null) continue;
                int glifos = tt.font != null && tt.font.characterTable != null ? tt.font.characterTable.Count : 0;
                todo += " [" + tt.text + " activo=" + tt.gameObject.activeInHierarchy + " glifos=" + glifos + "]";
            }
            Debug.Log("[Prueba] TODOS los textos del menu (" + todos.Length + "):" + todo);

            foreach (UnityEngine.GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Debug.Log("[Prueba] raiz del menu: " + raiz.name + " activo=" + raiz.activeInHierarchy);
            }
            return;
        }

        // Cavernas: mido que las brujas se muevan, que disparen y cuantos enemigos hay
        if (Pasos[paso].escena == "SampleScene2")
        {
            cuadros++;

            if (cuadros % 15 == 0)
            {
                int enVuelo = UnityEngine.Object.FindObjectsByType<ProyectilBruja>(FindObjectsSortMode.None).Length;
                if (enVuelo > proyectilesVistos) proyectilesVistos = enVuelo;

                Bruja masCerca = BrujaMasCercana(UnityEngine.Object.FindAnyObjectByType<Personaje>());
                if (masCerca != null)
                {
                    Personaje p = UnityEngine.Object.FindAnyObjectByType<Personaje>();
                    if (p != null) distanciaBrujaJugador = Vector2.Distance(masCerca.transform.position, p.transform.position);
                }
            }

            if (!posicionesTomadas && ahora - inicioPaso >= 2.5)
            {
                posicionesTomadas = true;
                TomarPosicionesDeBrujas();
                Debug.Log("[Prueba] brujas encontradas en las cavernas: " + (posicionesBrujas == null ? 0 : posicionesBrujas.Length));

                // El test deja al jugador quieto y lejos: la bruja por diseno lo espera
                // a mas de 8 unidades. Lo acerco para forzar huida y ataque.
                Personaje jugador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
                Bruja brujaCerca = BrujaMasCercana(jugador);

                if (jugador != null && brujaCerca != null)
                {
                    jugador.transform.position = brujaCerca.transform.position + new Vector3(2f, 0f, 0f);
                    vidaAntesDelAtaque = DatosJugador.vida;
                    Debug.Log("[Prueba] jugador puesto a 2 unidades de una bruja (vida " + vidaAntesDelAtaque + ")");
                }
                return;
            }

            // Un golpe cada 0,4 s (la bruja tiene un momento de invulnerabilidad)
            if (golpesDados && golpesLanzados < 4 && ahora >= proximoGolpe)
            {
                proximoGolpe = (float)ahora + 0.4f;
                golpesLanzados++;

                Personaje quien = UnityEngine.Object.FindAnyObjectByType<Personaje>();

                // Siempre la misma bruja: si se busca la mas cercana cada vez, como ella
                // huye, los golpes se repartian entre brujas distintas y ninguna moria.
                Bruja cercana = brujaGolpeada != null ? brujaGolpeada.GetComponent<Bruja>() : null;

                if (cercana != null && quien != null)
                {
                    cercana.RecibirGolpe(quien.transform);
                    Debug.Log("[Prueba] golpe " + golpesLanzados + " a la misma bruja");
                }
            }

            if (golpesDados && resultadoGolpes == "no probado" && ahora - inicioPaso >= 11.5)
            {
                resultadoGolpes = ReferenceEquals(brujaGolpeada, null) || brujaGolpeada == null
                    ? "SI: la bruja desaparecio al morir" : "NO: la bruja sigue viva";
                Debug.Log("[Prueba] resultado de golpear a la bruja: " + resultadoGolpes);
            }

            if (!conteoHecho && ahora - inicioPaso >= 9.5)
            {
                conteoHecho = true;

                int vivos = GameObject.FindGameObjectsWithTag("Orco").Length;
                int proyectiles = UnityEngine.Object.FindObjectsByType<ProyectilBruja>(FindObjectsSortMode.None).Length;

                float maximoRecorrido = MayorRecorridoDeBrujas();

                Debug.Log("[Prueba] enemigos 'Orco' vivos en las cavernas: " + vivos
                    + " (tope configurado: 6 invocados, 1 por bruja cada 5 s)");
                int dañoRecibido = vidaAntesDelAtaque < 0 ? 0 : Mathf.Max(0, vidaAntesDelAtaque - DatosJugador.vida);

                Debug.Log("[Prueba] bruja que mas se movio en 7 s: " + maximoRecorrido.ToString("0.00")
                    + " unidades | vida que perdio el jugador junto a la bruja: " + dañoRecibido);
                // Ahora pruebo que la bruja SI reciba dano y se pueda eliminar.
                // Los golpes van separados en el tiempo: la bruja tiene un momento
                // de invulnerabilidad corto despues de cada golpe (como los orcos).
                Personaje jugadorGolpeador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
                Bruja objetivo = BrujaMasCercana(jugadorGolpeador);

                if (objetivo != null && jugadorGolpeador != null)
                {
                    brujaGolpeada = objetivo.transform;
                    golpesDados = true;
                    proximoGolpe = (float)ahora;
                    Debug.Log("[Prueba] empiezo a golpear a una bruja (vida 3) para ver si se puede eliminar");
                }
                else
                {
                    resultadoGolpes = "no habia bruja cerca";
                }

                Debug.Log("[Prueba] proyectiles de bruja vistos en vuelo (maximo simultaneo): " + proyectilesVistos
                    + " | distancia bruja-jugador al final: " + distanciaBrujaJugador.ToString("0.00")
                    + " (la bruja mantiene distancia: huye)");
                return;
            }
        }

        // Nivel del jefe: registro con cuanta vida llega y si muere
        if (Pasos[paso].escena == "JefeFinal")
        {
            if (!vidaArenaRegistrada && ahora - inicioPaso >= 0.5)
            {
                vidaArenaRegistrada = true;
                Debug.Log("[Prueba] el jugador entra a la arena con vida " + DatosJugador.vida);
            }

            if (muerteEnArena < 0f && vidaArenaRegistrada && ahora - inicioPaso > 1.0)
            {
                if (UnityEngine.Object.FindAnyObjectByType<Personaje>() == null)
                {
                    muerteEnArena = (float)(ahora - inicioPaso);
                    Debug.Log("[Prueba] el jugador murio en la arena a los " + muerteEnArena.ToString("0.0")
                        + " s (quieto, sin esquivar)");
                }
            }

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

    private static Bruja BrujaMasCercana(Personaje jugador)
    {
        Bruja[] brujas = UnityEngine.Object.FindObjectsByType<Bruja>(FindObjectsSortMode.None);
        Bruja mejor = null;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < brujas.Length; i++)
        {
            float distancia = jugador == null ? 0f : Vector2.Distance(brujas[i].transform.position, jugador.transform.position);
            if (distancia < mejorDistancia) { mejorDistancia = distancia; mejor = brujas[i]; }
        }

        return mejor;
    }

    private static void TomarPosicionesDeBrujas()
    {
        Bruja[] brujas = UnityEngine.Object.FindObjectsByType<Bruja>(FindObjectsSortMode.None);

        brujasPrevias = new Transform[brujas.Length];
        posicionesBrujas = new Vector2[brujas.Length];

        for (int i = 0; i < brujas.Length; i++)
        {
            brujasPrevias[i] = brujas[i].transform;
            posicionesBrujas[i] = brujas[i].transform.position;
        }
    }

    // Compara por identidad de objeto, no por orden: mismo bicho, distinta posicion
    private static float MayorRecorridoDeBrujas()
    {
        Bruja[] brujas = UnityEngine.Object.FindObjectsByType<Bruja>(FindObjectsSortMode.None);
        float maximo = 0f;

        for (int i = 0; i < brujas.Length; i++)
        {
            for (int j = 0; j < brujasPrevias.Length; j++)
            {
                // Se compara la referencia del objeto: misma bruja, otra posicion
                if (!ReferenceEquals(brujas[i].transform, brujasPrevias[j])) continue;

                float distancia = Vector2.Distance(brujas[i].transform.position, posicionesBrujas[j]);
                if (distancia > maximo) maximo = distancia;
                break;
            }
        }

        return maximo;
    }

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

        Debug.Log("[Prueba] bruja golpeada por el jugador -> " + resultadoGolpes);
        Debug.Log("[Prueba] muerte del jugador en la arena: " + (muerteEnArena < 0f ? "sobrevivio los 14 s" : muerteEnArena.ToString("0.0") + " s"));

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
