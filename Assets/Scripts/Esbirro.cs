using UnityEngine;

/// <summary>
/// Marca a los enemigos que la bruja invoca en tiempo de ejecucion, para poder
/// llevar la cuenta de cuantos hay vivos en la escena (tope de invocacion).
/// </summary>
public class Esbirro : MonoBehaviour
{
    private void OnDestroy()
    {
        BrujaSpawner.EsbirroMuerto();
    }
}
