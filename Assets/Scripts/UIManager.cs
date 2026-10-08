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
        totalMoendas = DatosJugador.monedas;
        textoMonedas.text = totalMoendas.ToString();

        foreach (string nombre in DatosJugador.inventario)
        {
            GameObject prefab = Resources.Load<GameObject>(nombre);
            if (prefab != null)
            {
                Instantiate(prefab, Vector3.zero, Quaternion.identity, panelEquipo.transform);
            }
        }
        TotalObjetos = DatosJugador.inventario.Count;
    }

    private void SumarMonedas(int moneda)
    {
        totalMoendas += moneda;
        DatosJugador.monedas = totalMoendas;
        textoMonedas.text = totalMoendas.ToString();
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

        indice = Mathf.Clamp(indice, 0, spritesBarraVida.Count - 1);
        imagenBarraVida.sprite = spritesBarraVida[indice];
    }

    public void ActivaDesactivaCajaTextos(bool activado)
    {
        cajaTexto.SetActive(activado);
    }

    public void MostrarTextos(string texto)
    {
        textoDialogo.text = texto.ToString();
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

        if (precioObjeto <= totalMoendas && TotalObjetos < 3)
        {
            TotalObjetos++;
            totalMoendas -= precioObjeto;
            DatosJugador.monedas = totalMoendas;
            DatosJugador.inventario.Add(objeto);
            textoMonedas.text = totalMoendas.ToString();

            GameObject equipo = Resources.Load<GameObject>(objeto);
            Instantiate(equipo, Vector3.zero, Quaternion.identity, panelEquipo.transform);
        }
    }

    public void CerrarTienda()
    {
        tienda.SetActive(false);
        Time.timeScale = 1f;
    }

    #endregion
}