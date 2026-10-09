using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantallas de victoria y derrota, mas la barra de vida del jefe.
/// Toda la UI se construye por codigo en tiempo de ejecucion, asi que no hace
/// falta tocar ninguna escena y el sistema sobrevive a los cambios de nivel.
/// </summary>
public class FinDeJuego : MonoBehaviour
{
    private static FinDeJuego instancia;

    public static FinDeJuego Instancia
    {
        get
        {
            if (instancia == null)
            {
                GameObject go = new GameObject("FinDeJuego");
                instancia = go.AddComponent<FinDeJuego>();
            }

            return instancia;
        }
    }

    private Canvas canvas;
    private GameObject panelFinal;
    private TMP_Text titulo;
    private TMP_Text detalle;
    private Button botonPrincipal;
    private TMP_Text textoBotonPrincipal;
    private Button botonSecundario;
    private TMP_Text textoBotonSecundario;

    private bool esVictoria;
    private Image arteVictoria;
    private GameObject panelBarra;
    private RectTransform rellenoBarra;
    private TMP_Text nombreJefe;
    private TMP_Text vidaJefe;
    private Image imagenRelleno;
    private Coroutine parpadeo;
    private Coroutine victoriaPendiente;

    private readonly Color colorPanel = new Color(0.031f, 0.035f, 0.039f, 0.92f);
    private readonly Color colorBoton = new Color(0.72f, 0.53f, 0.26f, 1f);

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        CrearCanvas();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        OcultarPaneles();
    }

    // ------------------------- construccion de la UI -------------------------

    private void CrearCanvas()
    {
        GameObject canvasGO = new GameObject("CanvasFinDeJuego", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;   // el fundido de escena usa 999 y debe quedar arriba

        CanvasScaler escalador = canvasGO.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        CrearPanelFinal();
        CrearBarraJefe();
    }

    private void CrearPanelFinal()
    {
        // Los paneles deben ser hijos del Canvas; antes estaban como hermanos y Unity no los dibujaba.
        panelFinal = CrearImagen("PanelFinal", canvas.transform, colorPanel);
        Estirar(panelFinal);

        // Ilustracion final entregada en los recursos oficiales de ONIET26.
        GameObject arte = CrearImagen("ArteVictoria", panelFinal.transform, Color.white);
        Estirar(arte);
        arteVictoria = arte.GetComponent<Image>();
        arteVictoria.sprite = Resources.Load<Sprite>("ONIET26/Victoria");
        arteVictoria.preserveAspect = false;
        arteVictoria.raycastTarget = false;
        arte.SetActive(false);

        titulo = CrearTexto("Titulo", panelFinal.transform, "", 110f, TextAlignmentOptions.Center);
        RectTransform rtTitulo = titulo.rectTransform;
        rtTitulo.anchorMin = new Vector2(0.5f, 0.5f);
        rtTitulo.anchorMax = new Vector2(0.5f, 0.5f);
        rtTitulo.sizeDelta = new Vector2(1400f, 160f);
        rtTitulo.anchoredPosition = new Vector2(0f, 170f);

        detalle = CrearTexto("Detalle", panelFinal.transform, "", 42f, TextAlignmentOptions.Center);
        RectTransform rtDetalle = detalle.rectTransform;
        rtDetalle.anchorMin = new Vector2(0.5f, 0.5f);
        rtDetalle.anchorMax = new Vector2(0.5f, 0.5f);
        rtDetalle.sizeDelta = new Vector2(1200f, 120f);
        rtDetalle.anchoredPosition = new Vector2(0f, 40f);

        botonPrincipal = CrearBoton("BotonPrincipal", panelFinal.transform, "Reintentar", new Vector2(0f, -110f), AlPulsarPrincipal);
        textoBotonPrincipal = botonPrincipal.GetComponentInChildren<TMP_Text>();

        botonSecundario = CrearBoton("BotonSecundario", panelFinal.transform, "Volver al menu", new Vector2(0f, -215f), AlPulsarSecundario);
        textoBotonSecundario = botonSecundario.GetComponentInChildren<TMP_Text>();

        panelFinal.SetActive(false);
    }

    private void CrearBarraJefe()
    {
        panelBarra = CrearImagen("BarraJefe", canvas.transform, new Color(0f, 0f, 0f, 0.65f));
        RectTransform rtBarra = panelBarra.GetComponent<RectTransform>();
        rtBarra.anchorMin = new Vector2(0.5f, 1f);
        rtBarra.anchorMax = new Vector2(0.5f, 1f);
        rtBarra.pivot = new Vector2(0.5f, 1f);
        rtBarra.sizeDelta = new Vector2(1100f, 62f);
        rtBarra.anchoredPosition = new Vector2(0f, -34f);

        GameObject relleno = CrearImagen("Relleno", panelBarra.transform, new Color(0.72f, 0.19f, 0.14f, 1f));
        rellenoBarra = relleno.GetComponent<RectTransform>();
        imagenRelleno = relleno.GetComponent<Image>();
        rellenoBarra.anchorMin = new Vector2(0f, 0f);
        rellenoBarra.anchorMax = new Vector2(1f, 1f);
        rellenoBarra.offsetMin = new Vector2(4f, 4f);
        rellenoBarra.offsetMax = new Vector2(-4f, -4f);

        nombreJefe = CrearTexto("NombreJefe", panelBarra.transform, "EL TIRANO", 34f, TextAlignmentOptions.Center);
        nombreJefe.color = new Color(1f, 0.95f, 0.85f, 1f);
        Estirar(nombreJefe.gameObject);

        // Vida total y la que le va quedando, en numeros
        vidaJefe = CrearTexto("VidaJefe", panelBarra.transform, "", 34f, TextAlignmentOptions.Center);
        vidaJefe.color = Color.white;

        RectTransform rtVida = vidaJefe.rectTransform;
        rtVida.anchorMin = new Vector2(0.5f, 0f);
        rtVida.anchorMax = new Vector2(0.5f, 0f);
        rtVida.pivot = new Vector2(0.5f, 1f);
        rtVida.sizeDelta = new Vector2(500f, 46f);
        rtVida.anchoredPosition = new Vector2(0f, -6f);

        panelBarra.SetActive(false);
    }

    private GameObject CrearImagen(string nombre, Transform padre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        Image imagen = go.AddComponent<Image>();
        imagen.color = color;

        return go;
    }

    private TMP_Text CrearTexto(string nombre, Transform padre, string contenido, float tamano, TextAlignmentOptions alineacion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        TextMeshProUGUI texto = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) texto.font = TMP_Settings.defaultFontAsset;

        texto.text = contenido;
        texto.fontSize = tamano;
        texto.alignment = alineacion;
        texto.color = Color.white;
        texto.raycastTarget = false;

        return texto;
    }

    private Button CrearBoton(string nombre, Transform padre, string etiqueta, Vector2 posicion, UnityEngine.Events.UnityAction accion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 84f);
        rt.anchoredPosition = posicion;

        Image fondo = go.AddComponent<Image>();
        fondo.color = colorBoton;

        Button boton = go.AddComponent<Button>();
        boton.targetGraphic = fondo;
        boton.onClick.AddListener(accion);

        TMP_Text etiquetaTexto = CrearTexto("Etiqueta", go.transform, etiqueta, 40f, TextAlignmentOptions.Center);
        etiquetaTexto.color = new Color(0.06f, 0.06f, 0.07f, 1f);
        Estirar(etiquetaTexto.gameObject);

        return boton;
    }

    private static void Estirar(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ------------------------- API publica -------------------------

    public void ProgramarVictoria(float retraso)
    {
        // Esta corrutina vive en FinDeJuego (DontDestroyOnLoad), no en el jefe:
        // Orco.cs destruye al jefe a los 1,2 s y cortaba la rutina de victoria de 2,2 s.
        if (victoriaPendiente != null) StopCoroutine(victoriaPendiente);
        victoriaPendiente = StartCoroutine(VictoriaConRetraso(retraso));
    }

    private IEnumerator VictoriaConRetraso(float retraso)
    {
        yield return new WaitForSecondsRealtime(retraso);
        victoriaPendiente = null;
        MostrarVictoria();
    }

    public void MostrarVictoria()
    {
        OcultarBarraJefe();

        esVictoria = true;
        bool tieneArte = arteVictoria != null && arteVictoria.sprite != null;
        arteVictoria.gameObject.SetActive(tieneArte);
        titulo.gameObject.SetActive(!tieneArte);
        detalle.gameObject.SetActive(!tieneArte);
        titulo.text = "VICTORIA";
        titulo.color = new Color(0.98f, 0.85f, 0.45f, 1f);
        detalle.text = "El tirano ha caido y el camino del guerrero queda completo.";
        textoBotonPrincipal.text = "Ver el final";
        textoBotonSecundario.text = "Volver al menu";

        panelFinal.SetActive(true);
        Time.timeScale = 0f;
    }

    public void MostrarDerrota()
    {
        OcultarBarraJefe();

        esVictoria = false;
        if (arteVictoria != null) arteVictoria.gameObject.SetActive(false);
        titulo.gameObject.SetActive(true);
        detalle.gameObject.SetActive(true);
        titulo.text = "DERROTA";
        titulo.color = new Color(0.75f, 0.2f, 0.16f, 1f);
        detalle.text = "Has caido en combate. El camino no termina aca.";
        textoBotonPrincipal.text = "Reintentar nivel";
        textoBotonSecundario.text = "Volver al menu";

        panelFinal.SetActive(true);
        Time.timeScale = 0f;
    }

    public void MostrarBarraJefe(int vida, int vidaMaxima)
    {
        if (panelBarra == null) return;

        panelBarra.SetActive(true);
        ActualizarBarraJefe(vida, vidaMaxima);
    }

    public void ActualizarBarraJefe(int vida, int vidaMaxima)
    {
        if (rellenoBarra == null) return;

        float normalizada = vidaMaxima > 0 ? Mathf.Clamp01((float)vida / vidaMaxima) : 0f;
        rellenoBarra.anchorMax = new Vector2(normalizada, 1f);

        if (vidaJefe != null) vidaJefe.text = vida + " / " + vidaMaxima;

        // El relleno destella al recibir un golpe: se nota que le estas pegando
        if (imagenRelleno != null)
        {
            if (parpadeo != null) StopCoroutine(parpadeo);
            parpadeo = StartCoroutine(ParpadeoDeDano());
        }
    }

    private IEnumerator ParpadeoDeDano()
    {
        Color normal = new Color(0.72f, 0.19f, 0.14f, 1f);
        imagenRelleno.color = new Color(1f, 0.86f, 0.55f, 1f);

        yield return new WaitForSecondsRealtime(0.14f);

        if (imagenRelleno != null) imagenRelleno.color = normal;
        parpadeo = null;
    }

    public void OcultarBarraJefe()
    {
        if (panelBarra != null) panelBarra.SetActive(false);
    }

    public void OcultarPaneles()
    {
        if (panelFinal != null) panelFinal.SetActive(false);
        OcultarBarraJefe();
    }

    // ------------------------- botones -------------------------

    private void AlPulsarPrincipal()
    {
        if (esVictoria)
        {
            // Cierre: la cinematica final ya existente
            IrA("SampleScene3");
            return;
        }

        // Derrota: se reintenta el nivel conservando monedas e inventario
        DatosJugador.vida = DatosJugador.VIDA_MAXIMA;

        Time.timeScale = 1f;
        IrA(SceneManager.GetActiveScene().name);
    }

    private void AlPulsarSecundario()
    {
        IrA("MenuPrincipal");
    }

    private void IrA(string escena)
    {
        Time.timeScale = 1f;
        OcultarPaneles();
        TransicionEscena.CargarEscena(escena);
    }
}
