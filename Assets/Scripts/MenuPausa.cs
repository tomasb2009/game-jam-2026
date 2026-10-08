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

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;   // MUY importante, ver abajo
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}