using UnityEngine;

public class AtaqueOrco : MonoBehaviour
{
    public bool puedeHacerDaño = false;

    private void Start()
    {
        // Se fuerza a false al empezar, por si quedó tildado en el Inspector
        puedeHacerDaño = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && puedeHacerDaño)
        {
            Personaje personaje = collision.GetComponent<Personaje>();
            personaje.CausarHerida();
            puedeHacerDaño = false;
        }
    }
}