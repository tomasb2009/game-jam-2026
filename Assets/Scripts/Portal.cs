using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string escenaDestino;

    // Las usan los scripts de editor para reordenar los niveles y verificarlos
    public void PonerDestino(string destino)
    {
        escenaDestino = destino;
    }

    public string Destino()
    {
        return escenaDestino;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        DatosJugador.nivel = escenaDestino;   // para saber en que nivel esta (sin guardar nada)

        TransicionEscena.CargarEscena(escenaDestino);
    }
}