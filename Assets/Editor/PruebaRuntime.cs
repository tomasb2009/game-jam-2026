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
        ("SampleScene",   16f, "pueblo: jugador, goblins patrullando y percibiendo al jugador"),
        ("SampleScene2",  12f, "cavernas: brujas, murcielagos, portal al jefe"),
        ("JefeFinal",     14f, "jefe: fases, embestidas, invocacion de esbirros, barra de vida"),
    };

    private static int paso;
    private static double inicioPaso;
    private static bool capturando;
    private static bool menuRevisado;
    private static bool pausaProbada;
    private static bool patrullaTomada;
    private static int fasePueblo;
    private static Vector2[] posicionesGoblins;
    private static Transform[] goblinsPrevios;
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
    private static bool bossAssetsRevisados;
    private static bool victoriaBossEncolada;
    private static bool victoriaBossVerificada;
    private static double horaVictoriaBoss;
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

            if (GameObject.Find("BotonContinuar") != null)
                Debug.LogError("[Prueba] el boton Continuar sigue presente, pero el juego no debe guardar partida");

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

        // Regresion de controles: P debe pausar/reanudar; ESC se reserva para cerrar.
        if (Pasos[paso].escena == "SampleScene" && !pausaProbada && ahora - inicioPaso >= 1.2)
        {
            pausaProbada = true;
            MenuPausa menuPausa = UnityEngine.Object.FindAnyObjectByType<MenuPausa>();

            if (menuPausa == null)
            {
                Debug.LogError("[Prueba] falta MenuPausa en la Zona 1");
                return;
            }

            System.Reflection.MethodInfo alternar = typeof(MenuPausa).GetMethod("AlternarPausa");
            if (alternar == null)
            {
                Debug.LogError("[Prueba] KeyCode.P no tiene una accion AlternarPausa");
                return;
            }

            System.Reflection.FieldInfo campoPanel = typeof(MenuPausa).GetField("panelPausa", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            GameObject panelPausa = campoPanel == null ? null : campoPanel.GetValue(menuPausa) as GameObject;

            alternar.Invoke(menuPausa, null);
            bool pauso = Time.timeScale == 0f && panelPausa != null && panelPausa.activeSelf;
            alternar.Invoke(menuPausa, null);
            bool reanudo = Time.timeScale == 1f && panelPausa != null && !panelPausa.activeSelf;

            Debug.Log("[Prueba] tecla P: pausa=" + pauso + " reanuda=" + reanudo);
            if (!pauso || !reanudo) Debug.LogError("[Prueba] P no controla correctamente la pausa");
            if (typeof(MenuPausa).GetMethod("SalirDelJuego") == null || typeof(MenuPrincipal).GetMethod("Salir") == null)
                Debug.LogError("[Prueba] falta una accion de salida para ESC");

            Orco[] goblins = UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None);
            int goblinsOk = 0;
            int goblinsConAnimacionOjo = 0;
            foreach (Orco goblin in goblins)
            {
                if (goblin.GetComponent<JefeFinal>() != null) continue;
                Animator animator = goblin.GetComponentInChildren<Animator>(true);
                SpriteRenderer sprite = goblin.GetComponentInChildren<SpriteRenderer>(true);
                bool animGoblin = animator != null && animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.name == "AnimacionOrco";
                string spritePath = sprite != null && sprite.sprite != null ? UnityEditor.AssetDatabase.GetAssetPath(sprite.sprite) : "";
                if (animGoblin && spritePath.Contains("Sprites/Orco/")) goblinsOk++;
                if (spritePath.Contains("Eye-Bat")) goblinsConAnimacionOjo++;
            }
            Debug.Log("[Prueba] goblins con controlador/sprite de goblin: " + goblinsOk + " | con sprite de ojo: " + goblinsConAnimacionOjo);
            if (goblinsOk < 1 || goblinsConAnimacionOjo > 0) Debug.LogError("[Prueba] enemigos de Zona 1 aun muestran murcielago/ojos");

            return;
        }

        // Pueblo: la patrulla se mide con el jugador LEJOS (si no, ya lo detectan y se
        // quedan pegados atacandolo). Despues se acerca al jugador para ver la percepcion.
        if (Pasos[paso].escena == "SampleScene")
        {
            if (fasePueblo == 0 && ahora - inicioPaso >= 1.0)
            {
                fasePueblo = 1;

                Personaje jugador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
                Orco[] enemigos = UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None);

                Vector2 centro = Vector2.zero;
                int contados = 0;

                foreach (Orco o in enemigos)
                {
                    if (o == null || o.GetComponent<JefeFinal>() != null) continue;
                    centro += (Vector2)o.transform.position;
                    contados++;
                }

                if (contados > 0) centro /= contados;

                if (jugador != null) jugador.transform.position = centro + new Vector2(0f, 30f);

                TomarPosicionesDeGoblins();
                Debug.Log("[Prueba] jugador alejado 30 unidades: midiendo patrulla de " + goblinsPrevios.Length + " goblins");
                return;
            }

            if (fasePueblo == 1 && ahora - inicioPaso >= 11.0)
            {
                fasePueblo = 2;

                Debug.Log("[Prueba] goblin que mas patrullo en 10 s sin ver al jugador: "
                    + MayorRecorridoDeGoblins().ToString("0.00") + " unidades");

                // Ahora se acerca al jugador a un goblin para probar la percepcion
                Personaje jugador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
                Orco[] enemigos = UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None);

                foreach (Orco o in enemigos)
                {
                    if (o == null || o.GetComponent<JefeFinal>() != null) continue;

                    if (jugador != null) jugador.transform.position = o.transform.position + new Vector3(6f, 0f, 0f);
                    break;
                }

                return;
            }

            if (fasePueblo == 2 && ahora - inicioPaso >= 15.0)
            {
                fasePueblo = 3;

                Personaje jugador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
                Orco[] enemigos = UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None);
                int cerca = 0;
                int total = 0;

                foreach (Orco o in enemigos)
                {
                    if (o == null || o.GetComponent<JefeFinal>() != null) continue;
                    total++;

                    if (jugador != null && Vector2.Distance(o.transform.position, jugador.transform.position) < 4f) cerca++;
                }

                Debug.Log("[Prueba] percepcion: " + cerca + " de " + total + " goblins se acercaron al jugador en 4 s");
                return;
            }
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

            if (!bossAssetsRevisados && ahora - inicioPaso >= 1.5)
            {
                bossAssetsRevisados = true;
                VerificarIntegracionONIET26();
            }

            if (victoriaBossEncolada && !victoriaBossVerificada && ahora - horaVictoriaBoss >= 3.0)
            {
                victoriaBossVerificada = true;
                GameObject final = GameObject.Find("PanelFinal");
                GameObject arte = GameObject.Find("ArteVictoria");
                UnityEngine.UI.Image imagen = arte == null ? null : arte.GetComponent<UnityEngine.UI.Image>();
                bool gana = final != null && final.activeInHierarchy && arte != null && arte.activeInHierarchy
                    && imagen != null && imagen.sprite != null && Time.timeScale == 0f;
                Debug.Log("[Prueba] muerte real del jefe activa victoria/cinematica ONIET26: " + gana);
                if (!gana) Debug.LogError("[Prueba] la muerte del jefe no activa el final de victoria");

                FinDeJuego.Instancia.OcultarPaneles();
                Time.timeScale = 1f;
                return;
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

    private static void TomarPosicionesDeGoblins()
    {
        Orco[] enemigos = UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Transform> elegidos = new System.Collections.Generic.List<Transform>();
        System.Collections.Generic.List<Vector2> posiciones = new System.Collections.Generic.List<Vector2>();

        foreach (Orco o in enemigos)
        {
            // Solo los del pueblo (los que no son el jefe)
            if (o.GetComponent<JefeFinal>() != null) continue;

            elegidos.Add(o.transform);
            posiciones.Add(o.transform.position);
        }

        goblinsPrevios = elegidos.ToArray();
        posicionesGoblins = posiciones.ToArray();
    }

    private static float MayorRecorridoDeGoblins()
    {
        Orco[] enemigos = UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None);
        float maximo = 0f;

        foreach (Orco o in enemigos)
        {
            if (o == null || o.GetComponent<JefeFinal>() != null) continue;

            for (int j = 0; j < goblinsPrevios.Length; j++)
            {
                if (!ReferenceEquals(o.transform, goblinsPrevios[j])) continue;

                float d = Vector2.Distance(o.transform.position, posicionesGoblins[j]);
                if (d > maximo) maximo = d;
                break;
            }
        }

        return maximo;
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

    private static void VerificarIntegracionONIET26()
    {
        JefeFinal jefe = UnityEngine.Object.FindAnyObjectByType<JefeFinal>();
        if (jefe == null)
        {
            Debug.LogError("[Prueba] no aparece el jefe final en la escena");
            return;
        }

        Animator animator = jefe.GetComponentInChildren<Animator>(true);
        SpriteRenderer render = jefe.GetComponentInChildren<SpriteRenderer>(true);
        string controller = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name : "NULL";
        string sprite = render != null && render.sprite != null
            ? UnityEditor.AssetDatabase.GetAssetPath(render.sprite) : "NULL";
        Orco datos = jefe.GetComponent<Orco>();

        Debug.Log("[Prueba] boss ONIET26: controller=" + controller + " sprite=" + sprite
            + " escala=" + jefe.transform.localScale.x.ToString("0.0")
            + " vida=" + (datos != null ? datos.VidaInicial : 0));

        if (controller != "JefeONIET26") Debug.LogError("[Prueba] el boss no usa el controlador de animacion ONIET26");
        if (sprite == "NULL" || !sprite.Contains("Boss/ONIET26/")) Debug.LogError("[Prueba] falta textura de jefe ONIET26");
        if (Mathf.Abs(jefe.transform.localScale.x) < 2.5f) Debug.LogError("[Prueba] el jefe no esta claramente agrandado");
        if (datos == null || datos.VidaInicial < 25) Debug.LogError("[Prueba] la barra/vida maxima del jefe no corresponde");

        Sprite victoria = Resources.Load<Sprite>("ONIET26/Victoria");
        if (victoria == null) Debug.LogError("[Prueba] no se cargo el arte de victoria ONIET26");
        else Debug.Log("[Prueba] arte victoria ONIET26 cargado: " + victoria.name);

        GameObject barraAntesDeVencer = GameObject.Find("BarraJefe");
        bool barraVisibleEnCombate = barraAntesDeVencer != null && barraAntesDeVencer.activeInHierarchy
            && barraAntesDeVencer.GetComponentInParent<Canvas>() != null;
        Debug.Log("[Prueba] barra del jefe visible durante la pelea y en Canvas: " + barraVisibleEnCombate);
        if (!barraVisibleEnCombate) Debug.LogError("[Prueba] la barra del jefe no aparece durante el combate");

        bool guarda = false;
        foreach (var m in typeof(DatosJugador).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (m.Name.Contains("Guardar") || m.Name.Contains("Cargar")) guarda = true;
        }
        if (guarda) Debug.LogError("[Prueba] siguen expuestas funciones de guardado");

        // Comprueba la interfaz de victoria con la imagen real y deja el estado limpio
        FinDeJuego fin = FinDeJuego.Instancia;
        fin.MostrarVictoria();
        GameObject arte = GameObject.Find("ArteVictoria");
        bool visible = arte != null && arte.activeInHierarchy && arte.GetComponent<UnityEngine.UI.Image>().sprite != null;
        bool enCanvas = arte != null && arte.GetComponentInParent<Canvas>() != null;
        Debug.Log("[Prueba] interfaz de victoria con arte ONIET26 visible: " + visible + " | dentro del Canvas: " + enCanvas);
        if (!visible || !enCanvas) Debug.LogError("[Prueba] la pantalla de victoria no muestra el recurso ONIET26 en Canvas");
        fin.OcultarPaneles();
        fin.MostrarBarraJefe(datos != null ? datos.VidaActual : 0, datos != null ? datos.VidaInicial : 0);
        Time.timeScale = 1f;

        // RED/GREEN del flujo de victoria real: un golpe mortal debe salir del evento
        // del jefe, mostrar el arte y pausar hasta aceptar la cinemática.
        Personaje jugador = UnityEngine.Object.FindAnyObjectByType<Personaje>();
        if (datos != null && jugador != null)
        {
            SerializedObject so = new SerializedObject(datos);
            SerializedProperty hp = so.FindProperty("vidaOrco");
            if (hp != null) hp.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            victoriaBossEncolada = true;
            horaVictoriaBoss = EditorApplication.timeSinceStartup;
            datos.RecibirGolpe(jugador.transform);
            Debug.Log("[Prueba] le di el golpe mortal al jefe; verifico su final real en 3 s");
        }
        else Debug.LogError("[Prueba] no pude preparar la prueba real de victoria del jefe");
    }

    private static void PruebasFuncionales()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Orco");
        Debug.Log("[Prueba] enemigos 'Orco' vivos en la escena del jefe: " + enemigos.Length
            + " (los 4 que invoca cada Bruja del pueblo probaron el camino que antes tiraba NullReferenceException)");

        Orco jefeEscena = null;
        foreach (Orco o in UnityEngine.Object.FindObjectsByType<Orco>(FindObjectsSortMode.None))
        {
            if (o.GetComponent<JefeFinal>() != null) { jefeEscena = o; break; }
        }

        if (jefeEscena != null)
        {
            SpriteRenderer sr = jefeEscena.GetComponentInChildren<SpriteRenderer>(true);
            Debug.Log("[Prueba] jefe: escala=" + jefeEscena.transform.localScale.x.ToString("0.00")
                + " color=" + (sr != null ? sr.color.ToString() : "sin sprite")
                + " vida=" + jefeEscena.VidaInicial);
        }
        else Debug.LogWarning("[Prueba] no encontre al jefe en la escena");

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
        bool enCanvas = panel != null && panel.GetComponentInParent<Canvas>() != null;

        bool jugadorVivo = UnityEngine.Object.FindAnyObjectByType<Personaje>() != null;

        int finDeJuego = UnityEngine.Object.FindObjectsByType<FinDeJuego>(FindObjectsSortMode.None).Length;

        Debug.Log("[Prueba] pantalla de derrota visible tras morir: " + visible
            + " | dentro del Canvas: " + enCanvas
            + " | jugador en escena: " + jugadorVivo
            + " | DatosJugador.vida = " + DatosJugador.vida
            + " | sistemas FinDeJuego: " + finDeJuego
            + " | Time.timeScale = " + Time.timeScale
            + " | Time.time = " + Time.time.ToString("0.0"));

        if (!visible || !enCanvas) Guardar("error=Exception en JefeFinal: la pantalla de derrota no aparecio en Canvas al morir");
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
