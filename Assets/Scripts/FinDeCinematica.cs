using System.Collections;
using UnityEngine;

/// <summary>
/// Cierra la cinematica final: al terminar el tiempo indicado vuelve al menu
/// principal. Se le agrega a la escena de la cinematica desde el editor.
/// </summary>
public class FinDeCinematica : MonoBehaviour
{
    [SerializeField] private float segundos = 42f;
    [SerializeField] private string escenaDestino = "MenuPrincipal";

    private void Start()
    {
        Time.timeScale = 1f;
        StartCoroutine(EsperarYVolver());
    }

    private IEnumerator EsperarYVolver()
    {
        yield return new WaitForSeconds(segundos);

        TransicionEscena.CargarEscena(escenaDestino);
    }
}
