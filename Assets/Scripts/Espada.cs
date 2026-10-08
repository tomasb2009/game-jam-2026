using UnityEngine;

public class Espada : MonoBehaviour
{
    // Arrastrá acá el transform del personaje (el padre/raíz del jugador).
    // Si lo dejás vacío, intenta usar transform.root como fallback.
    [SerializeField] private Transform personaje;

    private BoxCollider2D colEspada;

    private void Awake()
    {
        colEspada = GetComponent<BoxCollider2D>();

        if (personaje == null)
        {
            personaje = transform.root;
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Orco"))
        {
            Orco orco = otro.GetComponent<Orco>();

            if (orco != null)
            {
                orco.RecibirDaño(personaje);
            }
        }
    }
}
