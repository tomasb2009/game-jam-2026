using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string escenaDestino;

    // La usa el script de editor para reordenar los niveles
    public void PonerDestino(string destino)
    {
        escenaDestino = destino;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Autoguardado: al cruzar el portal el progreso queda en disco
        DatosJugador.nivel = escenaDestino;
        DatosJugador.Guardar();

        TransicionEscena.CargarEscena(escenaDestino);
    }
}