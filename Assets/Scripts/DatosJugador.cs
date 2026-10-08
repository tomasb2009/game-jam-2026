using System.Collections.Generic;

public static class DatosJugador
{
    // 5 sprites en la barra = índices 0 a 4, por eso la vida máxima es 4
    public const int VIDA_MAXIMA = 5;

    public static int monedas = 0;
    public static int vida = VIDA_MAXIMA;
    public static List<string> inventario = new List<string>(); // nombres de prefabs en Resources

    public static void Reiniciar()
    {
        monedas = 0;
        vida = VIDA_MAXIMA;
        inventario.Clear();
    }
}