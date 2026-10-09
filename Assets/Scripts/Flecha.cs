using UnityEngine;

public class Flecha : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 3f; // autodestrucción si no pega contra nada

    private Rigidbody2D rig;
    private Transform origenAtaque; // quién la disparó (el Personaje), para pasarlo a RecibirDaño

    private void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
    }

    public void Disparar(Vector2 direccion, float velocidad, Transform origen)
    {
        origenAtaque = origen;

        // Rota el sprite de la flecha para que apunte en la dirección del tiro.
        // Asume que el sprite original mira "hacia la derecha" (eje +X).
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        if (rig != null)
        {
            rig.linearVelocity = direccion * velocidad;
        }

        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Orco") || otro.GetComponent<Bruja>() != null)
        {
            Orco orco = otro.GetComponent<Orco>();
            if (orco != null) orco.RecibirGolpe(origenAtaque);
            else
            {
                Bruja bruja = otro.GetComponent<Bruja>();
                if (bruja != null) bruja.RecibirGolpe(origenAtaque);
            }

            Destroy(gameObject);
        }

        if (otro.CompareTag("Paredes"))
        {
            Destroy(gameObject);
        }

        // Si más adelante tenés un tag para paredes/obstáculos, sumá acá:
        // else if (otro.CompareTag("Obstaculo")) Destroy(gameObject);
    }
}
