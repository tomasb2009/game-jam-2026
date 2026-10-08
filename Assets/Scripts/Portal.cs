using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string escenaDestino;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TransicionEscena.CargarEscena(escenaDestino);
        }
    }
}