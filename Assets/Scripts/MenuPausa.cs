using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelGradiante;
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    private bool estaPausado = false;

    private void Awake()
    {
        estaPausado = panelPausa != null && panelPausa.activeSelf;
        if (estaPausado) Time.timeScale = 0f;
    }

    private void Update()
    {
        // ESC siempre cierra el juego; P alterna la pausa.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SalirDelJuego();
            return;
        }

        if (Input.GetKeyDown(KeyCode.P)) AlternarPausa();
    }

    public void AlternarPausa()
    {
        if (estaPausado) Reanudar();
        else Pausar();
    }

    public void Pausar()
    {
        if (panelPausa != null) panelPausa.SetActive(true);
        if (panelGradiante != null) panelGradiante.SetActive(true);
        Time.timeScale = 0f;
        estaPausado = true;
    }

    public void Reanudar()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelGradiante != null) panelGradiante.SetActive(false);
        Time.timeScale = 1f;
        estaPausado = false;
    }

    public void SalirDelJuego()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Reinicia el nivel actual sin sistema de guardado de partida.
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;

        DatosJugador.vida = DatosJugador.VIDA_MAXIMA;

        TransicionEscena.CargarEscena(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;   // MUY importante, ver abajo
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}