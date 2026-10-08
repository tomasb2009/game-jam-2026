using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe final. Reutiliza la IA de Orco.cs (persecucion, ataque cuerpo a cuerpo,
/// retroceso al recibir dano) y le suma fases de dificultad, embestidas e
/// invocacion de esbirros. Al morir muestra la pantalla de victoria.
/// </summary>
[RequireComponent(typeof(Orco))]
public class JefeFinal : MonoBehaviour
{
    [Header("Fases (fraccion de vida restante)")]
    [SerializeField] private float umbralFase2 = 0.66f;
    [SerializeField] private float umbralFase3 = 0.33f;
    [SerializeField] private float velocidadExtraPorFase = 0.7f;
    [SerializeField] private float rangoExtraPorFase = 0.15f;
    [SerializeField] private float cooldownMenosPorFase = 0.35f;

    [Header("Embestida")]
    [SerializeField] private float intervaloEmbestida = 7f;
    [SerializeField] private float velocidadEmbestida = 13f;
    [SerializeField] private float duracionEmbestida = 0.55f;
    [SerializeField] private float radioEmbestida = 1.1f;

    [Header("Esbirros")]
    [SerializeField] private GameObject esbirroPrefab;
    [SerializeField] private int esbirrosPorInvocacion = 3;
    [SerializeField] private float intervaloInvocacion = 13f;
    [SerializeField] private float radioInvocacion = 2.5f;

    [Header("Sonido (Assets/Resources/Audio)")]
    [SerializeField] private string recursoSonidoEmbestida = "Audio/sfx_golpe_jefe";

    [Header("Cierre")]
    [SerializeField] private float retrasoVictoria = 2.2f;
    [SerializeField] private bool mostrarBarraDeVida = true;

    private Orco orco;
    private Transform jugador;
    private float velocidadBase;
    private float rangoBase;
    private float cooldownBase;
    private int fase = 1;
    private bool embistiendo;
    private bool terminado;
    private AudioClip sonidoEmbestida;

    private void Awake()
    {
        orco = GetComponent<Orco>();
    }

    private void Start()
    {
        BuscarJugador();

        sonidoEmbestida = Resources.Load<AudioClip>(recursoSonidoEmbestida);

        velocidadBase = orco.VelocidadAgente;
        if (velocidadBase <= 0.01f) velocidadBase = 3.5f;

        rangoBase = orco.rangoAtaque;
        cooldownBase = 2f;

        orco.AlRecibirDano += AlRecibirDano;
        orco.AlMorir += AlMorir;

        if (mostrarBarraDeVida && orco.VidaInicial > 0)
        {
            FinDeJuego.Instancia.MostrarBarraJefe(orco.VidaActual, orco.VidaInicial);
        }

        StartCoroutine(CicloEmbestida());
        StartCoroutine(CicloInvocacion());
    }

    private void BuscarJugador()
    {
        jugador = orco.personaje;

        if (jugador == null)
        {
            GameObject encontrado = GameObject.FindGameObjectWithTag("Player");
            if (encontrado != null) jugador = encontrado.transform;
        }
    }

    // ------------------------- fases -------------------------

    private void AlRecibirDano(Orco quien)
    {
        if (mostrarBarraDeVida && orco.VidaInicial > 0)
        {
            FinDeJuego.Instancia.ActualizarBarraJefe(orco.VidaActual, orco.VidaInicial);
        }

        float restante = orco.VidaInicial > 0 ? (float)orco.VidaActual / orco.VidaInicial : 0f;

        if (fase == 1 && restante <= umbralFase2) SubirFase(2);
        else if (fase == 2 && restante <= umbralFase3) SubirFase(3);
    }

    private void SubirFase(int nuevaFase)
    {
        fase = nuevaFase;

        orco.PonerVelocidad(velocidadBase + velocidadExtraPorFase * (fase - 1));
        orco.PonerRangoAtaque(rangoBase + rangoExtraPorFase * (fase - 1));
        orco.PonerCooldownAtaque(Mathf.Max(0.4f, cooldownBase - cooldownMenosPorFase * (fase - 1)));

        // Acelera los ciclos de embestida e invocacion en cada fase
        StopAllCoroutines();
        StartCoroutine(CicloEmbestida());
        StartCoroutine(CicloInvocacion());
    }

    private void AlMorir(Orco quien)
    {
        if (terminado) return;
        terminado = true;

        FinDeJuego.Instancia.OcultarBarraJefe();
        DatosJugador.vida = DatosJugador.VIDA_MAXIMA;
        DatosJugador.Guardar();

        StartCoroutine(VictoriaConRetraso());
    }

    private IEnumerator VictoriaConRetraso()
    {
        yield return new WaitForSeconds(retrasoVictoria);

        FinDeJuego.Instancia.MostrarVictoria();
    }

    // ------------------------- embestida -------------------------

    private IEnumerator CicloEmbestida()
    {
        while (!terminado)
        {
            float espera = intervaloEmbestida / fase;
            yield return new WaitForSeconds(espera);

            if (terminado || orco.EstaMuerto) yield break;
            if (embistiendo) continue;

            yield return StartCoroutine(Embestir());
        }
    }

    private IEnumerator Embestir()
    {
        if (jugador == null) BuscarJugador();
        if (jugador == null) yield break;

        embistiendo = true;

        Vector2 direccion = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        if (direccion == Vector2.zero) direccion = Vector2.right;

        // Se apaga la IA normal mientras dura la embestida para que no pelee
        // por el control del movimiento (el agente queda detenido a proposito).
        orco.PonerDetenido(true);
        bool iaActiva = orco.enabled;
        orco.enabled = false;

        float t = 0f;
        bool golpeo = false;

        while (t < duracionEmbestida)
        {
            transform.position += (Vector3)(direccion * velocidadEmbestida * Time.deltaTime);
            t += Time.deltaTime;

            if (!golpeo && jugador != null)
            {
                if (Vector2.Distance(transform.position, jugador.position) <= radioEmbestida)
                {
                    Personaje personaje = jugador.GetComponent<Personaje>();
                    if (personaje != null) personaje.CausarHerida();

                    if (AudioManager.Instance != null) AudioManager.Instance.ReproducirSonido(sonidoEmbestida);

                    golpeo = true;
                }
            }

            yield return null;
        }

        orco.enabled = iaActiva;
        orco.PonerDetenido(false);

        embistiendo = false;
    }

    // ------------------------- invocacion -------------------------

    private IEnumerator CicloInvocacion()
    {
        while (!terminado)
        {
            float espera = intervaloInvocacion / fase;
            yield return new WaitForSeconds(espera);

            if (terminado || orco.EstaMuerto) yield break;

            Invocar();
        }
    }

    private void Invocar()
    {
        if (esbirroPrefab == null) return;

        for (int i = 0; i < esbirrosPorInvocacion; i++)
        {
            float angulo = (360f / Mathf.Max(1, esbirrosPorInvocacion)) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f) * radioInvocacion;

            Instantiate(esbirroPrefab, transform.position + offset, Quaternion.identity);
        }
    }
}
