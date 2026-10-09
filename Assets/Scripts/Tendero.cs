using UnityEngine;

public class Tendero : MonoBehaviour
{
    [SerializeField] private GameObject tienda;

    private float momentoDeEntrada;

    private void Start()
    {
        momentoDeEntrada = Time.time;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Regla del diseno: despues de cambiar de escena la tienda NO se abre sola.
        // Se ignora cualquier choque de los primeros segundos (el jugador puede
        // aparecer pegado al tendero) y ademas hay que entrar caminando.
        if (Time.time - momentoDeEntrada < 1.5f) return;

        // Antes cualquier collider (enemigos, monedas, decorado) abria la tienda y
        // congelaba el juego. Ahora solo el jugador, y una sola vez.
        if (tienda == null) return;
        if (tienda.activeSelf) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        tienda.SetActive(true);
        Time.timeScale = 0f;
    }
}
