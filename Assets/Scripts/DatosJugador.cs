using System.Collections.Generic;

public static class DatosJugador
{
    // 5 sprites en la barra = indices 0 a 4, por eso la vida maxima es 4
    public const int VIDA_MAXIMA = 5;

    // El juego NO guarda partida: estos datos solo viven mientras el juego esta abierto
    // y sirven para pasar de un nivel a otro.
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
}
