using UnityEngine;

public class Objetos : MonoBehaviour
{
    public enum ObjetosEquipo
    {
        SaludPeque,
        SaludMed,
        Velocidad
    };

    [SerializeField] ObjetosEquipo objetosEquipo;

    // Debe coincidir con el nombre del prefab en la carpeta Resources
    private string NombreRecurso()
    {
        switch (objetosEquipo)
        {
            case ObjetosEquipo.SaludPeque: return "BotonPocionPeque";
            case ObjetosEquipo.SaludMed: return "BotonPocionMed";
            default: return "BotonPocionVel";
        }
    }

    public void UsarObjeto()
    {
        Personaje personaje = FindFirstObjectByType<Personaje>();
        UIManager uiManager = FindFirstObjectByType<UIManager>();

        if (personaje == null || uiManager == null) return;

        switch (objetosEquipo)
        {
            case ObjetosEquipo.SaludPeque:
                personaje.SumaVida();
                break;
            case ObjetosEquipo.SaludMed:
                personaje.SumaVida();
                personaje.SumaVida();
                break;
            case ObjetosEquipo.Velocidad:
                personaje.AumentarVelocidad();
                break;
        }

        DatosJugador.inventario.Remove(NombreRecurso());
        uiManager.TotalObjetos--;
        Destroy(this.gameObject);
    }
}