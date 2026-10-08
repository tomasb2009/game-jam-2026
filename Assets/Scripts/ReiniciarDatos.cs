using UnityEngine;

public class ReiniciarDatos : MonoBehaviour
{
    private void Awake()
    {
        DatosJugador.Reiniciar();
    }
}