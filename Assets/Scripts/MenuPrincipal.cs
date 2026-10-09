using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private string escenaPrimerMapa = "SampleScene";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Salir();
    }

    // Partida nueva. El juego no guarda progreso, asi que siempre se arranca de cero.
    public void Jugar()
    {
        DatosJugador.Reiniciar();
        DatosJugador.nivel = escenaPrimerMapa;

        SceneManager.LoadScene(escenaPrimerMapa);
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
