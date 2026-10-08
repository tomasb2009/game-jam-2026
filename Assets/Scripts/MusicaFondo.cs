using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Musica de fondo continua. Se crea sola al arrancar (no hay que tocar escenas),
/// sobrevive a los cambios de nivel y cambia de tema segun la escena.
/// Los clips se cargan de Assets/Resources/Audio.
/// </summary>
public class MusicaFondo : MonoBehaviour
{
    private static MusicaFondo instancia;

    private const string RUTA_AUDIO = "Audio/";
    private const float VOLUMEN = 0.45f;

    private AudioSource fuente;
    private string temaActual = "";
    private Coroutine fundido;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoArrancar()
    {
        if (instancia != null) return;

        GameObject go = new GameObject("MusicaFondo");
        instancia = go.AddComponent<MusicaFondo>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.volume = 0f;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Start()
    {
        PonerTema(TemaDe(SceneManager.GetActiveScene().name));
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        PonerTema(TemaDe(escena.name));
    }

    private static string TemaDe(string escena)
    {
        switch (escena)
        {
            case "MenuPrincipal": return "musica_menu";
            case "SampleScene": return "musica_pueblo";
            case "SampleScene2": return "musica_caverna";
            case "JefeFinal": return "musica_jefe";
            case "SampleScene3": return "musica_epilogo";
            default: return "musica_pueblo";
        }
    }

    public void PonerTema(string tema)
    {
        if (string.IsNullOrEmpty(tema)) return;
        if (tema == temaActual && fuente.isPlaying) return;

        temaActual = tema;

        if (fundido != null)
        {
            StopCoroutine(fundido);
            fundido = null;
        }

        fuente.Stop();

        AudioClip clip = Resources.Load<AudioClip>(RUTA_AUDIO + tema);

        // Si la pista todavia no existe, el juego sigue sin musica y sin errores
        if (clip == null) return;

        fuente.clip = clip;
        fuente.volume = 0f;
        fuente.Play();

        fundido = StartCoroutine(FundidoEntrada());
    }

    private IEnumerator FundidoEntrada()
    {
        float t = 0f;
        const float duracion = 1.2f;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(0f, VOLUMEN, Mathf.Clamp01(t / duracion));
            yield return null;
        }

        fuente.volume = VOLUMEN;
        fundido = null;
    }
}
