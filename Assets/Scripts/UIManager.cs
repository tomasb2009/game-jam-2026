using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private int totalMoendas;
    public int TotalObjetos;
    private int precioObjeto;

    [SerializeField] private TMP_Text textoMonedas;
    [SerializeField] private Image imagenBarraVida;
    [SerializeField] private List<Sprite> spritesBarraVida; // 0 = vacío ... último = lleno
    [SerializeField] private GameObject cajaTexto;
    [SerializeField] private TMP_Text textoDialogo;
    [SerializeField] private GameObject panelEquipo;
    [SerializeField] private GameObject tienda;
    [SerializeField] private TMP_Text textoAviso;   // mensajes de la tienda ("no alcanzan las monedas", etc.)

    private void OnEnable()
    {
        Moneda.sumaMoneda += SumarMonedas;
    }

    private void OnDisable()
    {
        Moneda.sumaMoneda -= SumarMonedas;
    }

    private void Start()
    {
        // Restaura monedas e inventario guardados al cambiar de escena
        // Ninguna escena debe arrancar con el tiempo congelado por la anterior,
        // ni con la tienda abierta (regla del diseno).
        // OJO: en la escena del menu este campo apunta por error al Panel principal,
        // asi que solo se apaga si de verdad es la tienda (si no, se borraba el menu).
        Time.timeScale = 1f;

        if (tienda != null && EsLaTienda(tienda)) tienda.SetActive(false);

        totalMoendas = DatosJugador.monedas;

        // La escena de menu tiene un UIManager sin campos asignados: sin estos
        // controles, Start() lanzaba NullReferenceException al abrir el juego.
        if (textoMonedas != null) textoMonedas.text = totalMoendas.ToString();

        if (panelEquipo != null)
        {
            foreach (string nombre in DatosJugador.inventario)
            {
                GameObject prefab = Resources.Load<GameObject>(nombre);
                if (prefab != null)
                {
                    Instantiate(prefab, Vector3.zero, Quaternion.identity, panelEquipo.transform);
                }
            }
        }
        TotalObjetos = DatosJugador.inventario.Count;
    }

    private void SumarMonedas(int moneda)
    {
        totalMoendas += moneda;
        DatosJugador.monedas = totalMoendas;
        if (textoMonedas != null) textoMonedas.text = totalMoendas.ToString();
    }

    // Mismos nombres y parámetro que antes: los otros scripts siguen funcionando
    public void RestaCorazones(int indice)
    {
        MostrarSprite(indice);
    }

    public void SumaCorazones(int indice)
    {
        MostrarSprite(indice);
    }

    private void MostrarSprite(int indice)
    {
        if (spritesBarraVida == null || spritesBarraVida.Count == 0) return;
        if (imagenBarraVida == null) return;

        indice = Mathf.Clamp(indice, 0, spritesBarraVida.Count - 1);
        imagenBarraVida.sprite = spritesBarraVida[indice];
    }

    public void ActivaDesactivaCajaTextos(bool activado)
    {
        if (cajaTexto != null) cajaTexto.SetActive(activado);
    }

    public void MostrarTextos(string texto)
    {
        if (textoDialogo != null) textoDialogo.text = texto.ToString();
    }

    #region TIENDA

    public void PrecioObjeto(string objeto)
    {
        switch (objeto)
        {
            case "BotonPocionPeque":
                precioObjeto = 1;
                break;
            case "BotonPocionMed":
                precioObjeto = 2;
                break;
            case "BotonPocionVel":
                precioObjeto = 5;
                break;
        }
    }

    public void AdquirirObjeto(string objeto)
    {
        PrecioObjeto(objeto);

        if (TotalObjetos >= 3)
        {
            Avisar("Inventario lleno (maximo 3 objetos)");
            return;
        }

        if (precioObjeto > totalMoendas)
        {
            Avisar("No tenes monedas suficientes");
            return;
        }

        GameObject equipo = Resources.Load<GameObject>(objeto);
        if (equipo == null) return; // el prefab del objeto no existe: no se cobra nada

        TotalObjetos++;
        totalMoendas -= precioObjeto;
        DatosJugador.monedas = totalMoendas;
        DatosJugador.inventario.Add(objeto);
        if (textoMonedas != null) textoMonedas.text = totalMoendas.ToString();

        if (panelEquipo != null)
        {
            Instantiate(equipo, Vector3.zero, Quaternion.identity, panelEquipo.transform);
        }

        Avisar("Compraste " + NombreDelObjeto(objeto));
    }

    private bool EsLaTienda(GameObject objeto)
    {
        return objeto.name.ToLower().Contains("tienda");
    }

    private void Avisar(string mensaje)
    {
        if (textoAviso != null) textoAviso.text = mensaje;
    }

    private string NombreDelObjeto(string objeto)
    {
        switch (objeto)
        {
            case "BotonPocionPeque": return "pocion de salud pequena ($1)";
            case "BotonPocionMed": return "pocion de salud mediana ($2)";
            case "BotonPocionVel": return "pocion de velocidad ($5)";
            default: return objeto;
        }
    }

    public void CerrarTienda()
    {
        if (tienda != null) tienda.SetActive(false);
        Avisar("");
        Time.timeScale = 1f;
    }

    #endregion
}