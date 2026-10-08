using System.Collections;
using UnityEngine;

public class BrujaSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject orcoPrefab;
    [SerializeField] private float intervalo = 5f;
    [SerializeField] private float retrasoSpawn = 0.8f; // segundos desde que arranca la animación hasta que salen los orcos
    [SerializeField] private float distancia = 1.5f;

    [Header("Animación")]
    [SerializeField] private string triggerInvocar = "Invocar";

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        StartCoroutine(CicloInvocacion());
    }

    private IEnumerator CicloInvocacion()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalo);   // espera 5 s
            animator.SetTrigger(triggerInvocar);          // arranca la animación
            yield return new WaitForSeconds(retrasoSpawn); // espera al momento del gesto
            SpawnearOrcos();                              // aparecen los 4 orcos
        }
    }

    private void SpawnearOrcos()
    {
        Vector2[] esquinas =
        {
            new Vector2(-1,  1),
            new Vector2( 1,  1),
            new Vector2(-1, -1),
            new Vector2( 1, -1)
        };

        foreach (Vector2 esquina in esquinas)
        {
            Vector2 posicion = (Vector2)transform.position + esquina * distancia;
            Instantiate(orcoPrefab, posicion, Quaternion.identity);
        }
    }
}