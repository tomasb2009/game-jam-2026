using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Revisa la integridad de todas las escenas del build: componentes con script
/// faltante, jugador con sus piezas, UI, porticos con destino y camara.
/// Devuelve codigo 1 si encuentra algo roto, para poder usarlo como chequeo
/// automatico (en el editor: menu "The Warrior Path"; en lote: -executeMethod).
/// </summary>
public static class VerificarEscenas
{
    private class Resultado
    {
        public int GameObjects;
        public int ScriptsFaltantes;
        public int ScriptsPropios;
        public List<string> Problemas = new List<string>();
        public List<string> Avisos = new List<string>();
    }

    [MenuItem("The Warrior Path/Verificar escenas")]
    public static void Verificar()
    {
        List<string> escenas = new List<string>();
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
        {
            if (e.enabled && File.Exists(e.path)) escenas.Add(e.path);
        }

        int problemasTotales = 0;
        int faltantesTotales = 0;

        foreach (string ruta in escenas)
        {
            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            Resultado r = Revisar(escena);

            problemasTotales += r.Problemas.Count;
            faltantesTotales += r.ScriptsFaltantes;

            Debug.Log(string.Format("[Verificar] {0}: {1} GameObjects, {2} scripts propios, {3} con script faltante, {4} problemas",
                Path.GetFileName(ruta), r.GameObjects, r.ScriptsPropios, r.ScriptsFaltantes, r.Problemas.Count));

            foreach (string p in r.Problemas) Debug.LogError("[Verificar] " + Path.GetFileName(ruta) + " -> " + p);
            foreach (string a in r.Avisos) Debug.LogWarning("[Verificar] " + Path.GetFileName(ruta) + " -> " + a);
        }

        Debug.Log(string.Format("[Verificar] RESULTADO: {0} escenas, {1} scripts faltantes, {2} problemas.",
            escenas.Count, faltantesTotales, problemasTotales));

        if (problemasTotales > 0 || faltantesTotales > 0)
        {
            Debug.LogError("[Verificar] Hay problemas de integridad.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[Verificar] Todo en orden.");
        }
    }

    private static Resultado Revisar(Scene escena)
    {
        Resultado r = new Resultado();

        GameObject jugador = null;
        GameObject ui = null;
        int portales = 0;
        int camaras = 0;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                r.GameObjects++;

                GameObject go = t.gameObject;

                if (go.GetComponent<Personaje>() != null) jugador = go;
                if (go.GetComponent<UIManager>() != null) ui = go;
                if (go.GetComponent<Portal>() != null) portales++;
                if (go.GetComponent<Camera>() != null) camaras++;

                foreach (Component c in go.GetComponents<Component>())
                {
                    if (c == null) r.ScriptsFaltantes++;
                    else if (c is MonoBehaviour && c.GetType().Namespace != "UnityEngine" ) r.ScriptsPropios++;
                }
            }
        }

        // Problemas que romperian el juego:
        if (jugador != null && !jugador.CompareTag("Player"))
        {
            r.Problemas.Add("el jugador no tiene el tag 'Player' (las monedas, el arco y el portal lo buscan por tag)");
        }

        if (jugador != null)
        {
            if (jugador.GetComponent<Rigidbody2D>() == null) r.Problemas.Add("el jugador no tiene Rigidbody2D");
            if (jugador.GetComponentInChildren<Animator>(true) == null) r.Problemas.Add("el jugador no tiene Animator");
        }

        if (camaras == 0) r.Problemas.Add("no hay camara en la escena");

        // Avisos (no rompen, pero conviene saberlos)
        if (ui == null) r.Avisos.Add("no hay UIManager en la escena");
        if (portales == 0 && escena.name != "MenuPrincipal" && escena.name != "JefeFinal")
        {
            r.Avisos.Add("no hay Portal: si es un nivel, no se puede avanzar");
        }

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                Portal portal = t.GetComponent<Portal>();
                if (portal != null && string.IsNullOrEmpty(portal.Destino()))
                {
                    r.Problemas.Add("el Portal '" + t.name + "' no tiene escena de destino");
                }
            }
        }

        // Componentes de UI clave del lienzo principal
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            Canvas canvas = raiz.GetComponentInChildren<Canvas>(true);
            if (canvas != null && canvas.GetComponentInChildren<Button>(true) == null && escena.name != "SampleScene" && escena.name != "SampleScene2" && escena.name != "JefeFinal")
            {
                r.Avisos.Add("el Canvas principal no tiene ningun boton (menus, pausa, victoria)");
            }
        }

        return r;
    }
}
