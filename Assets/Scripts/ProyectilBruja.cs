using UnityEngine;

/// <summary>
/// Proyectil de la bruja: viaja en linea recta y le hace dano al jugador.
/// (Flecha.cs no sirve para esto porque solo lastima a los que tienen tag "Orco".)
/// </summary>
public class ProyectilBruja : MonoBehaviour
{
    [SerializeField] private float tiempoVida = 4f;

    private Rigidbody2D rig;

    private void Awake()
    {
        rig = GetComponent<Rigidbody2D>();
    }

    public void Lanzar(Vector2 direccion, float velocidad)
    {
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);

        if (rig != null) rig.linearVelocity = direccion * velocidad;

        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Player"))
        {
            Personaje personaje = otro.GetComponent<Personaje>();
            if (personaje != null) personaje.CausarHerida();

            Destroy(gameObject);
            return;
        }

        if (otro.CompareTag("Paredes")) Destroy(gameObject);
    }
}
