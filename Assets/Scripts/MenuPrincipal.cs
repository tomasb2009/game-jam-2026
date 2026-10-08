using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] private string escenaPrimerMapa = "SampleScene";

    public void Jugar()
    {
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