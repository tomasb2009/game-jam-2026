using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class Personaje : MonoBehaviour
{
    private enum TipoArma { Espada, Arco }

    [SerializeField] private float velocidad;
    [SerializeField] private BoxCollider2D colEspada;

    [SerializeField] UIManager uiManager;

    [SerializeField] private AudioClip sonidoAtaque;
    [SerializeField] private AudioClip sonidoMuerte;

    [Header("Arco")]
    [SerializeField] private TipoArma armaActual = TipoArma.Espada;
    [SerializeField] private GameObject flechaPrefab;
    [SerializeField] private Transform puntoFlecha;     // hijo vacío, más o menos a la altura de las manos
    [SerializeField] private float velocidadFlecha = 12f;

    [SerializeField] private SelectorArmaUI selectorArmaUI;

    [Header("Poción de velocidad")]
    [SerializeField] private float multiplicadorVelocidad = 1.5f;
    [SerializeField] private float duracionVelocidad = 15f;

    private Rigidbody2D rig;
    private Animator anim;
    private SpriteRenderer spritePersonaje;

    private float posColX = 0.1f;
    private float posColY = 0f;

    private int vidaPersonaje = DatosJugador.VIDA_MAXIMA;

    private float velocidadBase;
    private Coroutine corrutinaVelocidad;

    private bool estoyHablando;
    private bool estaMuerto;
    private bool arcoCargado; // true desde que apretás click derecho hasta que disparás

    private CinemachineImpulseSource impulso;

    private void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        spritePersonaje = GetComponentInChildren<SpriteRenderer>();

        impulso = GetComponent<CinemachineImpulseSource>();

        velocidadBase = velocidad;

        ActualizarVisualArma();
    }

    private void Start()
    {
        // Recupera la vida guardada al venir de otra escena y actualiza la barra
        vidaPersonaje = DatosJugador.vida;
        uiManager.SumaCorazones(vidaPersonaje);
    }

    private void Update()
    {
        if (estoyHablando || estaMuerto) return;

        DetectarCambioDeArma();

        if (armaActual == TipoArma.Espada)
        {
            if (Input.GetMouseButtonDown(0))
            {
                anim.SetTrigger("Ataca");
                AudioManager.Instance.ReproducirSonido(sonidoAtaque);
            }
        }
        else // TipoArma.Arco
        {
            ApuntarConMouse();

            if (Input.GetMouseButtonDown(1))
            {
                CargarArco();
            }

            if (Input.GetMouseButtonDown(0) && arcoCargado)
            {
                Disparar();
            }
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            CausarHerida();
        }
    }

    private void FixedUpdate()
    {
        Movimiento();
    }

    [Header("Visual del arco")]
    [SerializeField] private Transform arcoVisual;          // el pivote "ArcoVisual"
    [SerializeField] private SpriteRenderer arcoRenderer;   // el SpriteRenderer de "SpriteArco"
    [SerializeField] private Sprite spriteArcoReposo;
    [SerializeField] private Sprite spriteArcoTensado;
    [SerializeField] private Sprite spriteArcoSoltado;
    [SerializeField] private float duracionSoltado = 0.12f;

    private void SacudirCamara(float fuerza)
    {
        if (impulso == null) return;

        // Dirección al azar, pero evitamos que sea casi vertical u horizontal pura
        // para que el golpe no se sienta siempre igual
        float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 direccion = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f);

        impulso.GenerateImpulse(direccion * fuerza);
    }

    public void ChequearSiHablo(bool hablando)
    {
        estoyHablando = hablando;
    }

    private void Movimiento()
    {
        if (estaMuerto)
        {
            rig.linearVelocity = Vector2.zero;
            anim.SetFloat("Camina", 0);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (estoyHablando == false)
        {
            rig.linearVelocity = new Vector2(horizontal, vertical) * velocidad;
            anim.SetFloat("Camina", Mathf.Abs(rig.linearVelocity.magnitude));
        }

        if (estoyHablando == true)
        {
            rig.linearVelocity = Vector2.zero;
            anim.SetFloat("Camina", 0);
        }

        // Con la espada equipada, el flip y el offset del collider los maneja
        // el movimiento. Con el arco equipado, los maneja ApuntarConMouse().
        if (armaActual == TipoArma.Espada)
        {
            if (horizontal > 0)
            {
                colEspada.offset = new Vector2(posColX, posColY);
                spritePersonaje.flipX = false;
            }
            else if (horizontal < 0)
            {
                colEspada.offset = new Vector2(-posColX, posColY);
                spritePersonaje.flipX = true;
            }
        }
    }

    // --- Velocidad temporal ---

    public void AumentarVelocidad()
    {
        if (estaMuerto) return;

        // Si ya había un efecto activo, se reinicia el contador de 15 seg
        // (no se acumula el multiplicador).
        if (corrutinaVelocidad != null)
        {
            StopCoroutine(corrutinaVelocidad);
        }

        corrutinaVelocidad = StartCoroutine(RutinaVelocidad());
    }

    private IEnumerator RutinaVelocidad()
    {
        velocidad = velocidadBase * multiplicadorVelocidad;

        yield return new WaitForSeconds(duracionVelocidad);

        velocidad = velocidadBase;
        corrutinaVelocidad = null;
    }

    // --- Cambio de arma con la ruedita del mouse ---

    private void DetectarCambioDeArma()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            armaActual = armaActual == TipoArma.Espada ? TipoArma.Arco : TipoArma.Espada;
            arcoCargado = false; // si cambiás de arma, se cancela cualquier carga pendiente
            ActualizarVisualArma();
        }
    }

    private void ActualizarVisualArma()
    {
        bool tieneArco = armaActual == TipoArma.Arco;

        if (colEspada != null) colEspada.gameObject.SetActive(!tieneArco);

        if (arcoVisual != null) arcoVisual.gameObject.SetActive(tieneArco);
        if (tieneArco && arcoRenderer != null) arcoRenderer.sprite = spriteArcoReposo;

        if (selectorArmaUI != null) selectorArmaUI.ActualizarArma(tieneArco);
    }

    // --- Arco ---

    private void CargarArco()
    {
        arcoCargado = true;
        if (arcoRenderer != null) arcoRenderer.sprite = spriteArcoTensado;
        // TODO: sonido de carga
    }

    private void Disparar()
    {
        arcoCargado = false;
        StartCoroutine(RutinaSoltarArco());

        if (flechaPrefab == null || puntoFlecha == null) return;

        Vector2 direccion = ObtenerDireccionMouse();

        GameObject flechaObj = Instantiate(flechaPrefab, puntoFlecha.position, Quaternion.identity);
        Flecha flecha = flechaObj.GetComponent<Flecha>();
        if (flecha != null)
        {
            flecha.Disparar(direccion, velocidadFlecha, transform);
        }
    }

    private IEnumerator RutinaSoltarArco()
    {
        if (arcoRenderer != null) arcoRenderer.sprite = spriteArcoSoltado;
        yield return new WaitForSeconds(duracionSoltado);
        if (arcoRenderer != null && armaActual == TipoArma.Arco)
            arcoRenderer.sprite = spriteArcoReposo;
    }

    private void ApuntarConMouse()
    {
        Vector3 mouseWorld = ObtenerPosicionMouseEnMundo();

        if (mouseWorld.x > transform.position.x) spritePersonaje.flipX = false;
        else if (mouseWorld.x < transform.position.x) spritePersonaje.flipX = true;

        if (arcoVisual != null)
        {
            Vector2 dir = (Vector2)mouseWorld - (Vector2)arcoVisual.position;
            float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arcoVisual.rotation = Quaternion.Euler(0f, 0f, angulo);

            // Al apuntar hacia la izquierda el sprite quedaría cabeza abajo, lo espejamos
            if (arcoRenderer != null) arcoRenderer.flipY = dir.x < 0;
        }
    }

    private Vector2 ObtenerDireccionMouse()
    {
        Vector3 mouseWorld = ObtenerPosicionMouseEnMundo();
        Vector2 origen = puntoFlecha != null ? (Vector2)puntoFlecha.position : (Vector2)transform.position;
        Vector2 direccion = ((Vector2)mouseWorld - origen).normalized;
        return direccion == Vector2.zero ? Vector2.right : direccion;
    }

    private Vector3 ObtenerPosicionMouseEnMundo()
    {
        Vector3 mouseScreen = Input.mousePosition;
        // Distancia de la cámara al plano z = 0 (cámara ortográfica típica
        // de un juego 2D, suele estar en z = -10).
        mouseScreen.z = -Camera.main.transform.position.z;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;
        return mouseWorld;
    }

    // --- Vida ---

    public void CausarHerida()
    {
        if (vidaPersonaje > 0 && estaMuerto == false)
        {
            vidaPersonaje--;
            DatosJugador.vida = vidaPersonaje;

            uiManager.RestaCorazones(vidaPersonaje);

            if (vidaPersonaje == 0)
            {
                estaMuerto = true;

                SacudirCamara(0.8f);

                rig.linearVelocity = Vector2.zero;

                anim.SetTrigger("Muere");

                AudioManager.Instance.ReproducirSonido(sonidoMuerte);

                Invoke(nameof(Morir), 1f);
            }

            else
            {
                SacudirCamara(0.2f);   // golpe normal
            }
        }
    }

    public void SumaVida()
    {
        if (estaMuerto) return;

        if (vidaPersonaje < DatosJugador.VIDA_MAXIMA)
        {
            vidaPersonaje++;
            DatosJugador.vida = vidaPersonaje;
            uiManager.SumaCorazones(vidaPersonaje);
        }
    }

    private void Morir()
    {
        DatosJugador.Reiniciar();
        Destroy(this.gameObject);
    }
}