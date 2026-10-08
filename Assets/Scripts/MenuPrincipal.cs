using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private string escenaPrimerMapa = "SampleScene";

    [Header("Guardado")]
    [SerializeField] private GameObject botonContinuar;   // se oculta si no hay partida guardada

    private void Start()
    {
        if (botonContinuar != null)
        {
            botonContinuar.SetActive(DatosJugador.TienePartida());
        }
    }

    // Partida nueva: borra el progreso anterior y arranca desde el primer mapa
    public void Jugar()
    {
        DatosJugador.Reiniciar();
        DatosJugador.nivel = escenaPrimerMapa;
        DatosJugador.Guardar();

        SceneManager.LoadScene(escenaPrimerMapa);
    }

    // Continua la partida guardada, en el ultimo nivel alcanzado
    public void Continuar()
    {
        if (!DatosJugador.Cargar())
        {
            Jugar();
            return;
        }

        string destino = string.IsNullOrEmpty(DatosJugador.nivel) ? escenaPrimerMapa : DatosJugador.nivel;
        SceneManager.LoadScene(destino);
    }

    public void BorrarPartidaGuardada()
    {
        DatosJugador.Reiniciar();
        DatosJugador.BorrarPartida();

        if (botonContinuar != null) botonContinuar.SetActive(false);
    }

    public void Salir()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}