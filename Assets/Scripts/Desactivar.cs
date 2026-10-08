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

        while (true)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

            if (!anim.IsInTransition(0) && info.normalizedTime >= 1f) break;

            yield return null;
        }

        foreach (GameObject obj in objetosADesactivar)
        {
            obj.SetActive(false);
        }
    }
}