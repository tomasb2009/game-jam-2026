using UnityEngine;

public class Tendero : MonoBehaviour
{
    [SerializeField] private GameObject tienda;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Antes cualquier collider (enemigos, monedas, decorado) abria la tienda y
        // congelaba el juego. Ahora solo el jugador, y una sola vez.
        if (tienda == null) return;
        if (tienda.activeSelf) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        tienda.SetActive(true);
        Time.timeScale = 0f;
    }
}
