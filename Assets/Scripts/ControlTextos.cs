using UnityEngine;

public class ControlTextos : MonoBehaviour
{
    [SerializeField, TextArea(3, 10)] private string[] arrayTetos;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private Personaje personaje;

    [Header("Sprite de aviso (Presione E)")]
    [SerializeField] private GameObject spriteAvisoE;

    private int indice;
    private bool jugadorCerca;
    private bool dialogoActivo;

    private void Awake()
    {
        personaje = GameObject.FindGameObjectWithTag("Player").GetComponent<Personaje>();
    }

    private void Update()
    {
        float distancia = Vector2.Distance(this.gameObject.transform.position, personaje.transform.position);
        jugadorCerca = distancia <= 2;

        spriteAvisoE.SetActive(jugadorCerca && !dialogoActivo);

        if (Input.GetKeyDown(KeyCode.E))
        {
            // Caso 1: no hay diálogo activo, el jugador está cerca -> ABRIR
            if (!dialogoActivo && jugadorCerca)
            {
                dialogoActivo = true;
                uIManager.ActivaDesactivaCajaTextos(true);
                personaje.ChequearSiHablo(true);
                ActivarCartel();
            }
            // Caso 2: ya hay diálogo activo -> AVANZAR (sin chequear distancia)
            else if (dialogoActivo)
            {
                ActivarCartel();
            }
        }
    }

    void ActivarCartel()
    {
        if (indice < arrayTetos.Length)
        {
            uIManager.MostrarTextos(arrayTetos[indice]);
            indice++;
        }
        else
        {
            indice = 0;
            dialogoActivo = false;
            uIManager.ActivaDesactivaCajaTextos(false);
            personaje.ChequearSiHablo(false);
        }
    }
}