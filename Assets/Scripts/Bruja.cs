using System.Collections;
using UnityEngine;

/// <summary>
/// La bruja de las cavernas: se mueve huyendo del jugador mientras invoca
/// esbirros (eso lo sigue haciendo BrujaSpawner) y ademas le lanza su propio
/// ataque (un proyectil).
///
/// El Animator de la bruja tiene 'BrujaCamina' como estado por defecto, asi que
/// al moverse camina sola, sin tocar el controlador.
/// </summary>
[RequireComponent(typeof(BrujaSpawner))]
public class Bruja : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidadFuga = 2.2f;    // cuando el jugador esta cerca
    [SerializeField] private float velocidadVagar = 0.7f;   // cuando esta lejos
    [SerializeField] private float distanciaSegura = 4.5f;  // mas cerca que esto: huye
    [SerializeField] private float distanciaMaxima = 8f;    // mas lejos que esto: se queda
    [SerializeField] private float radioPared = 0.45f;
    [SerializeField] private float tiempoEntreVagares = 2.5f;

    [Header("Ataque propio")]
    [SerializeField] private GameObject proyectilPrefab;
    [SerializeField] private float alcanceAtaque = 7f;
    [SerializeField] private float intervaloAtaque = 2.6f;
    [SerializeField] private float velocidadProyectil = 7f;
    [SerializeField] private float retrasoLanzamiento = 0.35f;

    private Transform jugador;
    private SpriteRenderer sprite;
    private Vector2 direccionVagar;
    private float proximoVagar;
    private float proximoAtaque;
    private bool moviendo;

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        direccionVagar = Random.insideUnitCircle.normalized;
    }

    private void Start()
    {
        BuscarJugador();
        proximoAtaque = Time.time + intervaloAtaque;
    }

    private void BuscarJugador()
    {
        if (jugador != null) return;

        GameObject encontrado = GameObject.FindGameObjectWithTag("Player");
        if (encontrado != null) jugador = encontrado.transform;
    }

    private void Update()
    {
        if (jugador == null) BuscarJugador();
        if (jugador == null) return;

        Vector2 posicion = transform.position;
        Vector2 haciaJugador = (Vector2)jugador.position - posicion;
        float distancia = haciaJugador.magnitude;

        Mover(haciaJugador, distancia);
        Girar(haciaJugador.x);

        if (distancia <= alcanceAtaque && Time.time >= proximoAtaque)
        {
            proximoAtaque = Time.time + intervaloAtaque;
            StartCoroutine(LanzarProyectil());
        }
    }

    // ------------------------- movimiento -------------------------

    private void Mover(Vector2 haciaJugador, float distancia)
    {
        Vector2 direccion = Vector2.zero;

        if (distancia < distanciaSegura)
        {
            // Huye: se aleja del jugador, esquivando paredes
            direccion = -haciaJugador.normalized * velocidadFuga;
        }
        else if (distancia > distanciaMaxima)
        {
            // Lo espera: se queda quieta
            moviendo = false;
            return;
        }
        else
        {
            // Vaga despacio mientras sigue invocando
            if (Time.time >= proximoVagar)
            {
                proximoVagar = Time.time + tiempoEntreVagares;
                direccionVagar = Random.insideUnitCircle.normalized;
            }

            direccion = direccionVagar * velocidadVagar;
        }

        Vector2 destino = (Vector2)transform.position + direccion * Time.deltaTime;

        if (HayPared(destino))
        {
            // Busca un costado libre en vez de empujar contra la pared
            Vector2 costado = new Vector2(-direccion.y, direccion.x);
            Vector2 alternativa = (Vector2)transform.position + costado * Time.deltaTime;

            if (!HayPared(alternativa)) destino = alternativa;
            else destino = transform.position;
        }

        transform.position = destino;
        moviendo = direccion.sqrMagnitude > 0.001f;
    }

    private bool HayPared(Vector2 posicion)
    {
        Collider2D[] choques = Physics2D.OverlapCircleAll(posicion, radioPared);

        for (int i = 0; i < choques.Length; i++)
        {
            if (choques[i] != null && choques[i].CompareTag("Paredes")) return true;
        }

        return false;
    }

    private void Girar(float direccionX)
    {
        if (sprite == null) return;

        // La animacion mira a la derecha: si va hacia la izquierda se espeja
        if (direccionX > 0.01f) sprite.flipX = false;
        else if (direccionX < -0.01f) sprite.flipX = true;
    }

    // ------------------------- ataque -------------------------

    private IEnumerator LanzarProyectil()
    {
        yield return new WaitForSeconds(retrasoLanzamiento);

        if (proyectilPrefab == null || jugador == null) yield break;
        if (Vector2.Distance(transform.position, jugador.position) > alcanceAtaque + 1f) yield break;

        Vector2 direccion = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        if (direccion == Vector2.zero) direccion = Vector2.right;

        GameObject proyectil = Instantiate(proyectilPrefab, transform.position, Quaternion.identity);

        ProyectilBruja componente = proyectil.GetComponent<ProyectilBruja>();
        if (componente != null) componente.Lanzar(direccion, velocidadProyectil);
    }
}
