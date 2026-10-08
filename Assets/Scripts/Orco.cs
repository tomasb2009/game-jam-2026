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

        float distancia = Vector3.Distance(personaje.position, transform.position);

        if (transform.position == puntosRuta[indiceRuta].position)
        {
            if (indiceRuta < puntosRuta.Length - 1) indiceRuta++;
            else indiceRuta = 0;
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
            agente.SetDestination(personaje.position);
            objetivo = personaje;
        }
        else
        {
            agente.SetDestination(puntosRuta[indiceRuta].position);
            objetivo = puntosRuta[indiceRuta];
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

        Vector2 direccionRetroceso = (transform.position - origenAtaque.position).normalized;
        if (direccionRetroceso == Vector2.zero) direccionRetroceso = Vector2.right;

        StartCoroutine(Flash());

        if (vidaOrco <= 0)
        {
            Morir(direccionRetroceso);
        }
        else
        {
            if (sonidoGolpeOrco != null) AudioManager.Instance.ReproducirSonido(sonidoGolpeOrco);

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

        if (sonidoMuerteOrco != null) AudioManager.Instance.ReproducirSonido(sonidoMuerteOrco);

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
        agente.isStopped = false;

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
