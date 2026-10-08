using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Orco : MonoBehaviour
{
    public Transform personaje;
    public Transform[] puntosRuta;
    public float rangoAtaque = 1.2f;

    [Header("Vida y feedback de daño")]
    [SerializeField] private int vidaOrco = 2;
    [SerializeField] private float fuerzaRetroceso = 4f;
    [SerializeField] private float duracionRetroceso = 0.2f;
    [SerializeField] private float duracionFlash = 0.12f;
    [SerializeField] private float tiempoInvulnerable = 0.35f;
    [SerializeField] private AudioClip sonidoGolpeOrco;
    [SerializeField] private AudioClip sonidoMuerteOrco;
    [SerializeField] private float tiempoHastaDestruir = 1.2f;

    private int indiceRuta;
    private NavMeshAgent agente;
    private bool objetivoDetectado;
    private SpriteRenderer sprite;
    private Transform objetivo;
    private Animator anim;
    private bool puedeAtacar = true;
    private AtaqueOrco ataqueOrco;

    private MaterialPropertyBlock mpb;
    private bool estaMuerto;
    private bool recibiendoDaño;
    private bool enRetroceso;

    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        agente = GetComponent<NavMeshAgent>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        ataqueOrco = GetComponentInChildren<AtaqueOrco>();
        mpb = new MaterialPropertyBlock();

        // Los orcos que crea la Bruja en tiempo de ejecucion usan los valores del
        // prefab, donde 'personaje' y 'puntosRuta' estan vacios. Sin esto, Update()
        // tira NullReferenceException todos los frames.
        if (puntosRuta == null) puntosRuta = new Transform[0];
        BuscarJugador();
    }

    private void BuscarJugador()
    {
        if (personaje != null) return;

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null) personaje = jugador.transform;
    }

    private bool TieneRuta()
    {
        if (puntosRuta == null || puntosRuta.Length == 0) return false;

        for (int i = 0; i < puntosRuta.Length; i++)
        {
            if (puntosRuta[i] != null) return true;
        }

        return false;
    }

    private Vector3 PosicionRutaActual()
    {
        if (puntosRuta == null || puntosRuta.Length == 0) return transform.position;

        if (indiceRuta < 0 || indiceRuta >= puntosRuta.Length) indiceRuta = 0;

        if (puntosRuta[indiceRuta] == null)
        {
            // Busca el primer punto valido (los puntos borrados dejan huecos null)
            for (int i = 0; i < puntosRuta.Length; i++)
            {
                if (puntosRuta[i] != null) { indiceRuta = i; break; }
            }
        }

        return puntosRuta[indiceRuta] != null ? puntosRuta[indiceRuta].position : transform.position;
    }

    private void Start()
    {
        agente.updateRotation = false;
        agente.updateUpAxis = false;
    }

    private void Update()
    {
        if (estaMuerto) return;

        transform.position = new Vector3(transform.position.x, transform.position.y, 0);

        if (personaje == null) BuscarJugador();
        if (personaje == null) return; // todavia no existe el jugador en la escena

        float distancia = Vector3.Distance(personaje.position, transform.position);

        if (TieneRuta())
        {
            // Antes era 'transform.position == punto.position' (igualdad exacta de float,
            // casi nunca verdadera). Ahora usamos un radio de llegada.
            Vector3 posicionRuta = PosicionRutaActual();

            if ((transform.position - posicionRuta).sqrMagnitude < 0.04f)
            {
                if (indiceRuta < puntosRuta.Length - 1) indiceRuta++;
                else indiceRuta = 0;
            }
        }

        if(distancia > 3)objetivoDetectado = true;

        if (distancia <= rangoAtaque && puedeAtacar && !recibiendoDaño)
        {
            Atacar();
        }

        if (!enRetroceso)
        {
            MovimientoOrco(objetivoDetectado);
        }

        RotarOrco();
    }

    void Atacar()
    {
        puedeAtacar = false;
        ataqueOrco.puedeHacerDaño = true;
        anim.SetTrigger("Ataca");
        StartCoroutine(CooldownAtaque());
    }

    void MovimientoOrco(bool esDetectado)
    {
        if (esDetectado)
        {
            agente.isStopped = false;   // por si quedo detenido tras un retroceso
            agente.SetDestination(personaje.position);
            objetivo = personaje;
        }
        else if (TieneRuta())
        {
            agente.isStopped = false;
            agente.SetDestination(PosicionRutaActual());
            objetivo = puntosRuta[indiceRuta];
        }
        else
        {
            agente.isStopped = true;   // sin ruta asignada, se queda quieto
            objetivo = personaje;
        }
    }

    void RotarOrco()
    {
        if (objetivo == null) return;

        if (transform.position.x > objetivo.position.x)
            transform.localScale = new Vector2(-1, 1);
        else
            transform.localScale = new Vector2(1, 1);
    }

    IEnumerator CooldownAtaque()
    {
        yield return new WaitForSeconds(2f);
        puedeAtacar = true;
    }
    public void RecibirDaño(Transform origenAtaque)
    {
        if (estaMuerto || recibiendoDaño) return;

        vidaOrco--;
        recibiendoDaño = true;

        objetivoDetectado = true;

        Vector2 direccionRetroceso = Vector2.right;

        if (origenAtaque != null)
        {
            Vector2 calculada = ((Vector2)transform.position - (Vector2)origenAtaque.position).normalized;
            if (calculada != Vector2.zero) direccionRetroceso = calculada;
        }

        StartCoroutine(Flash());

        if (vidaOrco <= 0)
        {
            Morir(direccionRetroceso);
        }
        else
        {
            if (sonidoGolpeOrco != null && AudioManager.Instance != null) AudioManager.Instance.ReproducirSonido(sonidoGolpeOrco);

            StartCoroutine(Retroceso(direccionRetroceso, tiempoInvulnerable));
        }
    }

    private void Morir(Vector2 direccionRetroceso)
    {
        estaMuerto = true;

        agente.isStopped = true;
        agente.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (ataqueOrco != null) ataqueOrco.puedeHacerDaño = false;

        anim.SetTrigger("Muere");

        if (sonidoMuerteOrco != null && AudioManager.Instance != null) AudioManager.Instance.ReproducirSonido(sonidoMuerteOrco);

        StartCoroutine(RetrocesoSinReactivar(direccionRetroceso));

        Destroy(gameObject, tiempoHastaDestruir);
    }

    private IEnumerator Retroceso(Vector2 direccion, float tiempoInvulnerabilidad)
    {
        enRetroceso = true;
        agente.isStopped = true;

        float t = 0f;
        while (t < duracionRetroceso)
        {
            transform.position += (Vector3)direccion * fuerzaRetroceso * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }

        enRetroceso = false;

        // Solo vuelve a caminar si tiene a donde ir (ruta o jugador detectado)
        agente.isStopped = !(objetivoDetectado || TieneRuta());

        float restante = tiempoInvulnerabilidad - duracionRetroceso;
        if (restante > 0) yield return new WaitForSeconds(restante);

        recibiendoDaño = false;
    }

    private IEnumerator RetrocesoSinReactivar(Vector2 direccion)
    {
        float t = 0f;
        while (t < duracionRetroceso)
        {
            transform.position += (Vector3)direccion * fuerzaRetroceso * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Flash()
    {

        sprite.GetPropertyBlock(mpb);
        mpb.SetFloat(FlashAmountID, 1f);
        sprite.SetPropertyBlock(mpb);

        yield return new WaitForSeconds(duracionFlash);

        sprite.GetPropertyBlock(mpb);
        mpb.SetFloat(FlashAmountID, 0f);
        sprite.SetPropertyBlock(mpb);
    }
}
