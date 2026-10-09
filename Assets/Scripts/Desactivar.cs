using System.Collections;
using UnityEngine;

public class Desactivar : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject[] objetosADesactivar;

    private void Awake()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (objetosADesactivar == null || objetosADesactivar.Length == 0)
        {
            objetosADesactivar = new GameObject[] { gameObject };
        }
    }

    private void OnEnable()
    {
        StartCoroutine(EsperarFinAnimacion());
    }

    private IEnumerator EsperarFinAnimacion()
    {
        yield return null;

        // Un Animator sin controller (o sin capas) no tiene estado que consultar y
        // GetCurrentAnimatorStateInfo(0) tira excepcion. Antes eso mataba la corrutina
        // y los objetos (la portada de la cinematica) quedaban activos para siempre:
        // la pantalla se quedaba tapada. Ahora hay salidas garantizadas.
        if (anim == null || anim.runtimeAnimatorController == null || anim.layerCount == 0)
        {
            yield return new WaitForSeconds(1f);
            ApagarObjetos();
            yield break;
        }

        float limite = Time.time + 10f;   // red de seguridad: nunca esperar de mas

        while (Time.time < limite)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

            if (!anim.IsInTransition(0) && info.normalizedTime >= 1f) break;

            yield return null;
        }

        ApagarObjetos();
    }

    private void ApagarObjetos()
    {
        foreach (GameObject obj in objetosADesactivar)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}