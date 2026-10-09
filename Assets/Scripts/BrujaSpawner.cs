using System.Collections;
using UnityEngine;

public class BrujaSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject orcoPrefab;
    [SerializeField] private float intervalo = 5f;
    [SerializeField] private float retrasoSpawn = 0.8f; // segundos desde que arranca la animación hasta que salen los orcos
    [SerializeField] private float distancia = 1.5f;
    [SerializeField] private int esbirrosPorInvocacion = 1;
    [SerializeField] private int maximoVivos = 6;   // tope de enemigos invocados a la vez en la escena

    [Header("Animación")]
    [SerializeField] private string triggerInvocar = "Invocar";

    private Animator animator;

    // Cuenta compartida por todas las brujas de la escena
    private static int vivos;

    public static void EsbirroMuerto()
    {
        if (vivos > 0) vivos--;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        vivos = 0;   // cada escena arranca con la cuenta limpia
        StartCoroutine(CicloInvocacion());
    }

    private IEnumerator CicloInvocacion()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalo);   // espera 5 s
            animator.SetTrigger(triggerInvocar);          // arranca la animación
            yield return new WaitForSeconds(retrasoSpawn); // espera al momento del gesto
            SpawnearEsbirros();                           // aparecen los esbirros
        }
    }

    private void SpawnearEsbirros()
    {
        if (orcoPrefab == null) return;

        for (int i = 0; i < esbirrosPorInvocacion; i++)
        {
            // Con tope: si ya hay demasiados enemigos invocados, no sale ninguno mas
            if (vivos >= maximoVivos) return;

            float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 posicion = (Vector2)transform.position + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * distancia;

            GameObject esbirro = Instantiate(orcoPrefab, posicion, Quaternion.identity);

            if (esbirro.GetComponent<Esbirro>() == null) esbirro.AddComponent<Esbirro>();
            vivos++;
        }
    }
}