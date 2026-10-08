using System.Collections.Generic;
using UnityEngine;

public static class DatosJugador
{
    // 5 sprites en la barra = indices 0 a 4, por eso la vida maxima es 4
    public const int VIDA_MAXIMA = 5;

    private const string CLAVE_EXISTE = "wp_partida";
    private const string CLAVE_MONEDAS = "wp_monedas";
    private const string CLAVE_VIDA = "wp_vida";
    private const string CLAVE_NIVEL = "wp_nivel";
    private const string CLAVE_INVENTARIO = "wp_inventario";

    public static int monedas = 0;
    public static int vida = VIDA_MAXIMA;
    public static List<string> inventario = new List<string>(); // nombres de prefabs en Resources
    public static string nivel = "SampleScene";                 // ultimo nivel alcanzado

    public static void Reiniciar()
    {
        monedas = 0;
        vida = VIDA_MAXIMA;
        inventario.Clear();
        nivel = "SampleScene";
    }

    public static bool TienePartida()
    {
        return PlayerPrefs.GetInt(CLAVE_EXISTE, 0) == 1;
    }

    public static void Guardar()
    {
        PlayerPrefs.SetInt(CLAVE_EXISTE, 1);
        PlayerPrefs.SetInt(CLAVE_MONEDAS, monedas);
        PlayerPrefs.SetInt(CLAVE_VIDA, vida);
        PlayerPrefs.SetString(CLAVE_NIVEL, nivel);

        // Unity no guarda listas: el inventario se une con ';'
        PlayerPrefs.SetString(CLAVE_INVENTARIO, string.Join(";", inventario.ToArray()));
        PlayerPrefs.Save();
    }

    public static void BorrarPartida()
    {
        PlayerPrefs.DeleteKey(CLAVE_EXISTE);
        PlayerPrefs.DeleteKey(CLAVE_MONEDAS);
        PlayerPrefs.DeleteKey(CLAVE_VIDA);
        PlayerPrefs.DeleteKey(CLAVE_NIVEL);
        PlayerPrefs.DeleteKey(CLAVE_INVENTARIO);
        PlayerPrefs.Save();
    }

    // Devuelve true si habia partida guardada y la carga en memoria
    public static bool Cargar()
    {
        if (!TienePartida()) return false;

        monedas = PlayerPrefs.GetInt(CLAVE_MONEDAS, 0);
        vida = Mathf.Clamp(PlayerPrefs.GetInt(CLAVE_VIDA, VIDA_MAXIMA), 1, VIDA_MAXIMA);
        nivel = PlayerPrefs.GetString(CLAVE_NIVEL, "SampleScene");

        inventario.Clear();
        string guardado = PlayerPrefs.GetString(CLAVE_INVENTARIO, "");
        if (!string.IsNullOrEmpty(guardado))
        {
            foreach (string nombre in guardado.Split(';'))
            {
                if (!string.IsNullOrEmpty(nombre)) inventario.Add(nombre);
            }
        }

        return true;
    }
}
