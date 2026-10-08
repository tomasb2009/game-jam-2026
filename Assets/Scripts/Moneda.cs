using UnityEngine;

public class Moneda : MonoBehaviour
{
    public delegate void SumaMoneda(int moneda);
    public static event SumaMoneda sumaMoneda;

    [SerializeField] private int cantidadMonedas;

    [SerializeField] private AudioClip sonidoMoneda;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (sumaMoneda != null)
            {
                SumarMoneda();
                Destroy(this.gameObject);
                AudioManager.Instance.ReproducirSonido(sonidoMoneda);
            }
        }
    }

    private void SumarMoneda()
    {
        sumaMoneda(cantidadMonedas);
    }
}
