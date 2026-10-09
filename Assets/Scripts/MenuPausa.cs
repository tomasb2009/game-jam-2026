using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelGradiante;
    [SerializeField] private string nombreEscenaMenu = "MenuPrincipal";

    private bool estaPausado = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (estaPausado) Reanudar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        panelPausa.SetActive(true);
        panelGradiante.SetActive(true);
        Time.timeScale = 0f;   // congela el juego
        estaPausado = true;
    }

    public void Reanudar()
    {
        panelPausa.SetActive(false);
        panelGradiante.SetActive(false);
        Time.timeScale = 1f;   // el juego vuelve a velocidad normal
        estaPausado = false;
    }

    // Reinicia el nivel actual conservando monedas, inventario y ultimo nivel guardado
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;

        DatosJugador.vida = DatosJugador.VIDA_MAXIMA;
        DatosJugador.Guardar();

        TransicionEscena.CargarEscena(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;   // MUY importante, ver abajo
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}