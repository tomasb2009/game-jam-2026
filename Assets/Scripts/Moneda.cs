using UnityEngine;

public class Moneda : MonoBehaviour
{
    public delegate void SumaMoneda(int moneda);
    public static event SumaMoneda sumaMoneda;

    [SerializeField] private int cantidadMonedas;

    [SerializeField] private AudioClip sonidoMoneda;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        SumarMoneda();
        Destroy(this.gameObject);

        if (AudioManager.Instance != null) AudioManager.Instance.ReproducirSonido(sonidoMoneda);
    }

    private void SumarMoneda()
    {
        if (sumaMoneda != null) sumaMoneda(cantidadMonedas);
        else DatosJugador.monedas += cantidadMonedas; // sin HUD en escena la moneda igual cuenta
    }
}
