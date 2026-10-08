using UnityEngine;
using UnityEngine.UI;

public class SelectorArmaUI : MonoBehaviour
{
    [SerializeField] private Image icono;
    [SerializeField] private Sprite spriteEspada;
    [SerializeField] private Sprite spriteArco;

    private void Start()
    {
        ActualizarArma(false); // arranca mostrando la espada
    }

    // Llamado desde Personaje.cs cada vez que cambiás de arma con la rueda del mouse.
    public void ActualizarArma(bool tieneArco)
    {
        if (icono == null) return;
        icono.sprite = tieneArco ? spriteArco : spriteEspada;
    }
}
