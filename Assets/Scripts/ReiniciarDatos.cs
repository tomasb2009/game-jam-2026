using UnityEngine;

public class ReiniciarDatos : MonoBehaviour
{
    // OJO: este componente estaba activo en SampleScene y SampleScene2 y su Awake()
    // llamaba a DatosJugador.Reiniciar(), asi que cada cambio de nivel borraba las
    // monedas, la vida y el inventario. Ahora solo reinicia si se tilda a mano.
    [SerializeField] private bool reiniciarAlEntrar = false;

    private void Awake()
    {
        if (reiniciarAlEntrar)
        {
            DatosJugador.Reiniciar();
        }
    }
}
