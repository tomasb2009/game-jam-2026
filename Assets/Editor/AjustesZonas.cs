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
/// Segunda tanda de ajustes pedidos por el diseno:
///  - Zona 1 (pueblo): queda sin bruja, solo los goblins colocados a mano.
///  - Zona 2 (cavernas): cada bruja invoca 1 esbirro cada 5 s con tope de 6 vivos,
///    y ademas se mueve (huye del jugador) y ataca con un proyectil propio.
///  - Interfaces: se limpia el texto de prueba, se ordenan las etiquetas de la
///    tienda, se agrega el aviso de monedas y el boton de reiniciar nivel en la pausa.
///
/// Idempotente y ejecutable en lote con -executeMethod AjustesZonas.Ajustar.
/// </summary>
public static class AjustesZonas
{
    private const string EscenaUno = "Assets/Scenes/SampleScene.unity";
    private const string EscenaDos = "Assets/Scenes/SampleScene2.unity";
    private const string EscenaJefe = "Assets/Scenes/JefeFinal.unity";
    private const string PrefabFlecha = "Assets/Prefabs/Flecha.prefab";
    private const string PrefabProyectil = "Assets/Prefabs/ProyectilBruja.prefab";
    private const string PrefabMurcielago = "Assets/Prefabs/Murcielago Variant.prefab";

    [MenuItem("The Warrior Path/Ajustar zonas, brujas e interfaces")]
    public static void Ajustar()
    {
        CrearProyectilDeLaBruja();
        AjustarZonaUno();
        AjustarZonaDos();
        AjustarInterfaces(EscenaUno);
        AjustarInterfaces(EscenaDos);
        AjustarInterfaces(EscenaJefe);

        AssetDatabase.SaveAssets();
        Debug.Log("[Ajustes] Zonas, brujas e interfaces ajustadas.");
    }

    // ------------------------------------------------------------ proyectil

    private static void CrearProyectilDeLaBruja()
    {
        if (File.Exists(PrefabProyectil))
        {
            Debug.Log("[Ajustes] El proyectil de la bruja ya existe.");
            return;
        }

        GameObject flecha = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFlecha);
        if (flecha == null)
        {
            Debug.LogWarning("[Ajustes] No encontre " + PrefabFlecha);
            return;
        }

        GameObject temporal = (GameObject)PrefabUtility.InstantiatePrefab(flecha);
        temporal.name = "ProyectilBruja";

        Flecha vieja = temporal.GetComponent<Flecha>();
        if (vieja != null) Object.DestroyImmediate(vieja);

        if (temporal.GetComponent<ProyectilBruja>() == null) temporal.AddComponent<ProyectilBruja>();

        PrefabUtility.SaveAsPrefabAsset(temporal, PrefabProyectil);
        Object.DestroyImmediate(temporal);
        AssetDatabase.Refresh();

        Debug.Log("[Ajustes] Proyectil de la bruja creado en " + PrefabProyectil);
    }

    // ------------------------------------------------------------ zona 1

    private static void AjustarZonaUno()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaUno, OpenSceneMode.Single);

        List<GameObject> quitar = new List<GameObject>();
        Recorrer(escena, t =>
        {
            if (t.name == "Bruja") quitar.Add(t.gameObject);
        });

        foreach (GameObject go in quitar) Object.DestroyImmediate(go);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[Ajustes] Zona 1: bruja(s) quitadas = " + quitar.Count + ". Quedan solo los goblins colocados a mano.");
    }

    // ------------------------------------------------------------ zona 2

    private static void AjustarZonaDos()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaDos, OpenSceneMode.Single);
        GameObject proyectil = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabProyectil);
        int brujas = 0;

        Recorrer(escena, t =>
        {
            if (!t.name.StartsWith("Bruja")) return;

            BrujaSpawner spawner = t.GetComponent<BrujaSpawner>();
            if (spawner == null) return;

            brujas++;

            SerializedObject so = new SerializedObject(spawner);
            Campo(so, "intervalo", 5f);
            Campo(so, "esbirrosPorInvocacion", 1);
            Campo(so, "maximoVivos", 6);
            Campo(so, "distancia", 1.5f);
            so.ApplyModifiedPropertiesWithoutUndo();

            Bruja bruja = t.GetComponent<Bruja>();
            if (bruja == null) bruja = t.gameObject.AddComponent<Bruja>();

            SerializedObject soBruja = new SerializedObject(bruja);
            SerializedProperty campo = soBruja.FindProperty("proyectilPrefab");
            if (campo != null) campo.objectReferenceValue = proyectil;
            soBruja.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(t.gameObject);
        });

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[Ajustes] Zona 2: " + brujas + " brujas ajustadas (1 esbirro cada 5 s, tope 6, se mueven y atacan).");
    }

    // ------------------------------------------------------------ interfaces

    private static void AjustarInterfaces(string ruta)
    {
        if (!File.Exists(ruta)) return;

        Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        UIManager ui = Buscar<UIManager>(escena);

        int basura = LimpiarTextosDePrueba(escena);
        int etiquetas = AjustarEtiquetasDeLaTienda(escena);

        if (ui != null) AsegurarAviso(escena, ui);

        MenuPausa pausa = Buscar<MenuPausa>(escena);
        bool boton = pausa != null && AsegurarBotonReiniciar(escena, pausa);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log(string.Format("[Ajustes] {0}: textos de prueba limpiados = {1}, etiquetas de tienda = {2}, boton reiniciar = {3}",
            Path.GetFileName(ruta), basura, etiquetas, boton));
    }

    private static int LimpiarTextosDePrueba(Scene escena)
    {
        int limpiados = 0;

        Recorrer(escena, t =>
        {
            TMP_Text texto = t.GetComponent<TMP_Text>();
            if (texto == null) return;

            string contenido = texto.text == null ? "" : texto.text.ToLower();
            if (contenido.Contains("dasdas"))
            {
                texto.text = "";
                EditorUtility.SetDirty(texto);
                limpiados++;
            }
        });

        return limpiados;
    }

    private static int AjustarEtiquetasDeLaTienda(Scene escena)
    {
        GameObject panel = BuscarPorNombre(escena, "PanelTienda");
        if (panel == null) return 0;

        int cambiadas = 0;

        foreach (Button boton in panel.GetComponentsInChildren<Button>(true))
        {
            string etiqueta = EtiquetaDe(boton.gameObject.name);
            if (etiqueta == null) continue;

            TMP_Text texto = boton.GetComponentInChildren<TMP_Text>(true);
            if (texto == null) continue;

            texto.text = etiqueta;
            EditorUtility.SetDirty(texto);
            cambiadas++;
        }

        return cambiadas;
    }

    // Solo caracteres que ya usa la tienda (fuente bitmap: nada de acentos raros)
    private static string EtiquetaDe(string nombre)
    {
        if (nombre.Contains("Peq")) return "Pocion Salud Pequeña $1";
        if (nombre.Contains("Med")) return "Pocion Salud Mediana $2";
        if (nombre.Contains("Vel")) return "Pocion Velocidad $5";
        return null;
    }

    private static void AsegurarAviso(Scene escena, UIManager ui)
    {
        GameObject panel = BuscarPorNombre(escena, "PanelTienda");
        if (panel == null) return;

        TMP_Text texto = null;
        Transform existente = null;

        foreach (Transform t in panel.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Aviso") { existente = t; break; }
        }

        if (existente != null)
        {
            texto = existente.GetComponent<TMP_Text>();
        }
        else
        {
            TMP_Text modelo = panel.GetComponentInChildren<TMP_Text>(true);

            GameObject go = new GameObject("Aviso", typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);

            texto = go.AddComponent<TextMeshProUGUI>();
            if (modelo != null)
            {
                texto.font = modelo.font;
                texto.fontSize = modelo.fontSize * 0.85f;
            }
            texto.color = new Color(0.98f, 0.85f, 0.45f, 1f);
            texto.text = "";
            texto.raycastTarget = false;
            texto.alignment = TextAlignmentOptions.Center;

            RectTransform rt = texto.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(460f, 70f);
            rt.anchoredPosition = new Vector2(0f, 12f);
        }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty campo = so.FindProperty("textoAviso");
        if (campo != null)
        {
            campo.objectReferenceValue = texto;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static bool AsegurarBotonReiniciar(Scene escena, MenuPausa pausa)
    {
        GameObject original = BuscarPorNombre(escena, "BotonReanudar");
        if (original == null) return false;
        if (BuscarPorNombre(escena, "BotonReiniciarNivel") != null) return true;

        GameObject clon = Object.Instantiate(original, original.transform.parent);
        clon.name = "BotonReiniciarNivel";

        RectTransform rt = clon.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = rt.anchoredPosition + new Vector2(0f, -78f);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x + 70f, rt.sizeDelta.y);
        }

        TMP_Text etiqueta = clon.GetComponentInChildren<TMP_Text>(true);
        if (etiqueta != null) etiqueta.text = "Reiniciar nivel";

        Button boton = clon.GetComponent<Button>();
        boton.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(boton.onClick, new UnityAction(pausa.ReiniciarNivel));

        EditorUtility.SetDirty(clon);
        return true;
    }

    // ------------------------------------------------------------ utilidades

    private static void Recorrer(Scene escena, System.Action<Transform> accion)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t != null) accion(t);
            }
        }
    }

    private static GameObject BuscarPorNombre(Scene escena, string nombre)
    {
        GameObject encontrado = null;

        Recorrer(escena, t =>
        {
            if (encontrado == null && t.name == nombre) encontrado = t.gameObject;
        });

        return encontrado;
    }

    private static T Buscar<T>(Scene escena) where T : Component
    {
        T encontrado = null;

        Recorrer(escena, t =>
        {
            if (encontrado == null)
            {
                T c = t.GetComponent<T>();
                if (c != null) encontrado = c;
            }
        });

        return encontrado;
    }

    private static void Campo(SerializedObject so, string nombre, int valor)
    {
        SerializedProperty p = so.FindProperty(nombre);
        if (p != null) p.intValue = valor;
        else Debug.LogWarning("[Ajustes] campo no encontrado: " + nombre);
    }

    private static void Campo(SerializedObject so, string nombre, float valor)
    {
        SerializedProperty p = so.FindProperty(nombre);
        if (p != null) p.floatValue = valor;
        else Debug.LogWarning("[Ajustes] campo no encontrado: " + nombre);
    }
}
