using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Construye el contenido final del juego desde el editor: la escena del jefe
/// final, el portal que lleva a ella, el boton "Continuar" del menu y el cierre
/// de la cinematica. Es idempotente: se puede volver a ejecutar sin duplicar nada.
/// Se corre desde el menu "The Warrior Path" o por linea de comandos:
///   Unity.exe -batchmode -projectPath ... -executeMethod ConstruirContenidoFinal.Construir -quit
/// </summary>
public static class ConstruirContenidoFinal
{
    private const string EscenaJefe = "Assets/Scenes/JefeFinal.unity";
    private const string EscenaBase = "Assets/Scenes/SampleScene2.unity";
    private const string EscenaMenu = "Assets/Scenes/MenuPrincipal.unity";
    private const string EscenaCinematica = "Assets/Scenes/SampleScene3.unity";
    private const string PrefabJefe = "Assets/Prefabs/OrcoFinal.prefab";
    private const string PrefabEsbirro = "Assets/Prefabs/Orco.prefab";
    private const string CarpetaBuild = "Builds/Windows";

    private static readonly string[] FueraDeLaArena =
    {
        "Bruja", "Bruja (1)", "Bruja (2)", "Bruja (3)", "Murcielago", "Murcielago Variant",
        "Tendero", "JefePueblo", "Portal", "Textos", "E", "ReiniciarDatos"
    };

    [MenuItem("The Warrior Path/Construir contenido final")]
    public static void Construir()
    {
        CrearEscenaJefe();
        PonerPortalHaciaElJefe();
        AgregarCierreDeCinematica();
        OrdenarBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log("[WarriorPath] Contenido final construido: escena del jefe, portal, boton Continuar y cierre de cinematica.");
    }

    [MenuItem("The Warrior Path/Compilar para Windows")]
    public static void CompilarWindows()
    {
        string[] escenas = RutasDeEscenas().ToArray();

        if (!Directory.Exists(CarpetaBuild)) Directory.CreateDirectory(CarpetaBuild);

        BuildPlayerOptions opciones = new BuildPlayerOptions();
        opciones.scenes = escenas;
        opciones.locationPathName = CarpetaBuild + "/TheWarriorPath.exe";
        opciones.target = BuildTarget.StandaloneWindows64;
        opciones.options = BuildOptions.None;

        var informe = BuildPipeline.BuildPlayer(opciones);
        Debug.Log("[WarriorPath] Build: " + informe.summary.result + " errores=" + informe.summary.totalErrors);
    }

    // ---------------------------------------------------------------- escena del jefe

    private static void CrearEscenaJefe()
    {
        if (!File.Exists(EscenaJefe))
        {
            // Copia a nivel de texto: Unity le genera un GUID nuevo y la escena
            // hereda camara, UI, jugador y NavMesh ya horneado del nivel base.
            File.WriteAllText(EscenaJefe, File.ReadAllText(EscenaBase));
            AssetDatabase.ImportAsset(EscenaJefe);
            Debug.Log("[WarriorPath] Escena del jefe creada desde " + EscenaBase);
        }

        Scene escena = EditorSceneManager.OpenScene(EscenaJefe, OpenSceneMode.Single);

        LimpiarArena(escena);

        if (Object.FindAnyObjectByType<JefeFinal>() == null)
        {
            GameObject jugador = BuscarPorNombre(escena, "Personaje");
            Vector3 centro = jugador != null ? jugador.transform.position + new Vector3(4f, 0f, 0f) : Vector3.zero;

            CrearJefe(centro, jugador);
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    private static void LimpiarArena(Scene escena)
    {
        List<GameObject> borrar = new List<GameObject>();

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;

                foreach (string prohibido in FueraDeLaArena)
                {
                    if (t.name == prohibido || t.name.StartsWith(prohibido))
                    {
                        borrar.Add(t.gameObject);
                        break;
                    }
                }
            }
        }

        foreach (GameObject go in borrar)
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        if (borrar.Count > 0)
        {
            Debug.Log("[WarriorPath] Arena limpia: " + borrar.Count + " objetos del nivel anterior quitados.");
        }
    }

    private static void CrearJefe(Vector3 posicion, GameObject jugador)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabJefe);
        if (prefab == null)
        {
            Debug.LogError("[WarriorPath] No se encontro " + PrefabJefe);
            return;
        }

        GameObject jefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        jefe.name = "JefeFinal";
        jefe.transform.position = posicion;

        Orco orco = jefe.GetComponent<Orco>();
        if (orco == null) orco = jefe.AddComponent<Orco>();

        if (jugador != null) orco.personaje = jugador.transform;

        JefeFinal final = jefe.GetComponent<JefeFinal>();
        if (final == null) final = jefe.AddComponent<JefeFinal>();

        // Vida del jefe y esbirros que invoca (los campos son privados: se tocan
        // con SerializedObject, igual que lo haria el Inspector).
        SerializedObject soOrco = new SerializedObject(orco);
        Propiedad(soOrco, "vidaOrco", 24);
        Propiedad(soOrco, "rangoAtaque", 1.6f);
        Propiedad(soOrco, "fuerzaRetroceso", 1.5f);
        Propiedad(soOrco, "cooldownAtaque", 1.8f);
        soOrco.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soJefe = new SerializedObject(final);
        SerializedProperty esbirro = soJefe.FindProperty("esbirroPrefab");
        if (esbirro != null)
        {
            esbirro.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabEsbirro);
        }
        Propiedad(soJefe, "esbirrosPorInvocacion", 3);
        soJefe.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(jefe);

        Debug.Log("[WarriorPath] Jefe final colocado en " + posicion);
    }

    private static void Propiedad(SerializedObject so, string nombre, int valor)
    {
        SerializedProperty p = so.FindProperty(nombre);
        if (p != null) p.intValue = valor;
        else Debug.LogWarning("[WarriorPath] Campo no encontrado: " + nombre);
    }

    private static void Propiedad(SerializedObject so, string nombre, float valor)
    {
        SerializedProperty p = so.FindProperty(nombre);
        if (p != null) p.floatValue = valor;
        else Debug.LogWarning("[WarriorPath] Campo no encontrado: " + nombre);
    }

    // ---------------------------------------------------------------- portal hacia el jefe

    private static void PonerPortalHaciaElJefe()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaBase, OpenSceneMode.Single);

        Portal portal = BuscarComponente<Portal>(escena);
        if (portal == null)
        {
            Debug.LogWarning("[WarriorPath] No hay Portal en " + EscenaBase);
            return;
        }

        portal.PonerDestino("JefeFinal");

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[WarriorPath] El portal de " + EscenaBase + " ahora lleva a la escena del jefe.");
    }

    // ---------------------------------------------------------------- boton Continuar


    // ---------------------------------------------------------------- cierre de la cinematica

    private static void AgregarCierreDeCinematica()
    {
        if (!File.Exists(EscenaCinematica)) return;

        Scene escena = EditorSceneManager.OpenScene(EscenaCinematica, OpenSceneMode.Single);

        if (Object.FindAnyObjectByType<FinDeCinematica>() != null)
        {
            Debug.Log("[WarriorPath] La cinematica ya tiene su cierre.");
            return;
        }

        GameObject cierre = new GameObject("CierreDeCinematica");
        cierre.AddComponent<FinDeCinematica>();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[WarriorPath] Cierre agregado a la cinematica final.");
    }

    // ---------------------------------------------------------------- build settings

    private static void OrdenarBuildSettings()
    {
        EditorBuildSettings.scenes = RutasDeEscenas().ConvertAll(ruta => new EditorBuildSettingsScene(ruta, true)).ToArray();
        Debug.Log("[WarriorPath] Build settings ordenados.");
    }

    private static List<string> RutasDeEscenas()
    {
        List<string> rutas = new List<string>();
        rutas.Add(EscenaMenu);
        rutas.Add("Assets/Scenes/SampleScene.unity");
        rutas.Add(EscenaBase);
        rutas.Add(EscenaJefe);
        rutas.Add(EscenaCinematica);

        for (int i = rutas.Count - 1; i >= 0; i--)
        {
            if (!File.Exists(rutas[i])) rutas.RemoveAt(i);
        }

        return rutas;
    }

    // ---------------------------------------------------------------- utilidades

    private static GameObject BuscarPorNombre(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre) return t.gameObject;
            }
        }

        return null;
    }

    private static T BuscarComponente<T>(Scene escena) where T : Component
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            T encontrado = raiz.GetComponentInChildren<T>(true);
            if (encontrado != null) return encontrado;
        }

        return null;
    }
}
