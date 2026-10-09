using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ronda 2 de arreglos, todos reportados al jugar:
///  1. Las animaciones del goblin usaban los sprites del murcielago (por eso se veia un ojo).
///  2. El murcielago ahora tiene su propio controlador (si no, al arreglar (1) se veria goblin).
///  3. El proyectil de la bruja pasa a ser una bola de magia verde (ya no la flecha).
///  4. La tienda no debe estar abierta en ninguna escena (y en la del jefe no debe existir).
///  5. Los botones del menu principal se superponian.
/// </summary>
public static class ArreglosRonda2
{
    private const string EscenaUno = "Assets/Scenes/SampleScene.unity";
    private const string EscenaDos = "Assets/Scenes/SampleScene2.unity";
    private const string EscenaJefe = "Assets/Scenes/JefeFinal.unity";
    private const string EscenaMenu = "Assets/Scenes/MenuPrincipal.unity";

    private const string ClipCamina = "Assets/Animaciones/OrcoCamina.anim";
    private const string ClipAtaca = "Assets/Animaciones/OrcoAtaca.anim";
    private const string ClipMuere = "Assets/Animaciones/OrcoMuere.anim";
    private const string ClipQuieto = "Assets/Animaciones/OrcoQuieto.anim";

    private const string SheetCamina = "Assets/Sprites/Orco/Golblin-S1-Walk-Sheet.png";
    private const string SheetAtaca = "Assets/Sprites/Orco/Goblin-S1-Attack-Sheet.png";
    private const string SheetDano = "Assets/Sprites/Orco/Golblin-S1-Damage-Sheet.png";

    private const string PrefabMurcielago = "Assets/Prefabs/Murcielago Variant.prefab";
    private const string PrefabProyectil = "Assets/Prefabs/ProyectilBruja.prefab";
    private const string ControlMurcielago = "Assets/Animaciones/OrcoFinal (2).controller";

    [MenuItem("The Warrior Path/Arreglos ronda 2")]
    public static void Arreglar()
    {
        ArreglarAnimacionesDelGoblin();
        ArreglarMurcielago();
        ProyectilDeMagia();
        CerrarLaTienda(EscenaUno, false);
        CerrarLaTienda(EscenaDos, false);
        CerrarLaTienda(EscenaJefe, true);
        AlinearBotonesDelMenu();

        AssetDatabase.SaveAssets();
        Debug.Log("[Ronda2] Arreglos aplicados.");
    }

    // ---------------------------------------------------- 1. animaciones del goblin

    private static void ArreglarAnimacionesDelGoblin()
    {
        List<Sprite> camina = SpritesDe(SheetCamina);
        List<Sprite> ataca = SpritesDe(SheetAtaca);
        List<Sprite> dano = SpritesDe(SheetDano);

        if (camina.Count == 0)
        {
            Debug.LogWarning("[Ronda2] No pude leer los sprites de " + SheetCamina);
            return;
        }

        PonerSprites(ClipCamina, camina, 0.15f);
        PonerSprites(ClipQuieto, camina, 0.15f);
        PonerSprites(ClipAtaca, ataca.Count > 0 ? ataca : camina, 0.25f);
        PonerSprites(ClipMuere, dano.Count > 0 ? dano : camina, 0.25f);

        Debug.Log(string.Format("[Ronda2] Animaciones del goblin: camina {0} sprites, ataca {1}, muere {2}",
            camina.Count, ataca.Count, dano.Count));
    }

    private static void PonerSprites(string rutaClip, List<Sprite> sprites, float paso)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(rutaClip);
        if (clip == null || sprites.Count == 0)
        {
            Debug.LogWarning("[Ronda2] Falta el clip " + rutaClip);
            return;
        }

        EditorCurveBinding[] enlaces = AnimationUtility.GetObjectReferenceCurveBindings(clip);

        // Los clips de ataque y muerte no tenian ninguna curva de sprite: se crea.
        EditorCurveBinding enlace = enlaces.Length > 0
            ? enlaces[0]
            : EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

        List<ObjectReferenceKeyframe> llaves = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < sprites.Count; i++)
        {
            ObjectReferenceKeyframe llave = new ObjectReferenceKeyframe();
            llave.time = i * paso;
            llave.value = sprites[i];
            llaves.Add(llave);
        }

        // La ultima llave repite el primer sprite para que el bucle cierre bien
        ObjectReferenceKeyframe cierre = new ObjectReferenceKeyframe();
        cierre.time = sprites.Count * paso;
        cierre.value = sprites[0];
        llaves.Add(cierre);

        AnimationUtility.SetObjectReferenceCurve(clip, enlace, llaves.ToArray());
        EditorUtility.SetDirty(clip);
    }

    private static List<Sprite> SpritesDe(string rutaPng)
    {
        List<Sprite> salida = new List<Sprite>();

        Object[] todo = AssetDatabase.LoadAllAssetsAtPath(rutaPng);
        foreach (Object o in todo)
        {
            Sprite s = o as Sprite;
            if (s != null) salida.Add(s);
        }

        // Orden por el numero del final del nombre (_0, _1, _2, ...)
        salida.Sort((a, b) => Numero(a.name).CompareTo(Numero(b.name)));
        return salida;
    }

    private static int Numero(string nombre)
    {
        int i = nombre.LastIndexOf('_');
        if (i < 0) return 0;

        int numero;
        return int.TryParse(nombre.Substring(i + 1), out numero) ? numero : 0;
    }

    // ---------------------------------------------------- 2. murcielago con su controlador

    private static void ArreglarMurcielago()
    {
        AnimatorController control = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControlMurcielago);
        if (control == null)
        {
            Debug.LogWarning("[Ronda2] Falta el controlador del murcielago");
            return;
        }

        // Orco.cs dispara estos triggers: si no existen, Unity tira error en consola
        AgregarTrigger(control, "Ataca");
        AgregarTrigger(control, "Muere");
        EditorUtility.SetDirty(control);

        if (!File.Exists(PrefabMurcielago)) return;

        GameObject contenido = PrefabUtility.LoadPrefabContents(PrefabMurcielago);
        Animator animator = contenido.GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.runtimeAnimatorController = control;
            EditorUtility.SetDirty(animator);
        }

        PrefabUtility.SaveAsPrefabAsset(contenido, PrefabMurcielago);
        PrefabUtility.UnloadPrefabContents(contenido);

        Debug.Log("[Ronda2] Murcielago: controlador propio asignado y triggers agregados.");
    }

    private static void AgregarTrigger(AnimatorController control, string nombre)
    {
        foreach (AnimatorControllerParameter p in control.parameters)
        {
            if (p.name == nombre) return;
        }

        control.AddParameter(nombre, AnimatorControllerParameterType.Trigger);
    }

    // ---------------------------------------------------- 3. proyectil de magia

    private static void ProyectilDeMagia()
    {
        if (!File.Exists(PrefabProyectil)) return;

        Sprite bola = null;
        Object[] todo = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/ProyectilBruja.png");
        foreach (Object o in todo)
        {
            Sprite s = o as Sprite;
            if (s != null) { bola = s; break; }
        }

        if (bola == null)
        {
            Debug.LogWarning("[Ronda2] No pude cargar el sprite del proyectil");
            return;
        }

        GameObject contenido = PrefabUtility.LoadPrefabContents(PrefabProyectil);

        SpriteRenderer render = contenido.GetComponentInChildren<SpriteRenderer>(true);
        if (render != null)
        {
            render.sprite = bola;
            render.color = Color.white;
            render.sortingOrder = 5;
            EditorUtility.SetDirty(render);
        }

        BoxCollider2D colision = contenido.GetComponentInChildren<BoxCollider2D>(true);
        if (colision != null)
        {
            colision.size = new Vector2(0.5f, 0.5f);
            EditorUtility.SetDirty(colision);
        }

        contenido.transform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(contenido, PrefabProyectil);
        PrefabUtility.UnloadPrefabContents(contenido);

        Debug.Log("[Ronda2] Proyectil de la bruja: bola de magia asignada.");
    }

    // ---------------------------------------------------- 4. la tienda, cerrada y fuera del jefe

    private static void CerrarLaTienda(string ruta, bool borrarLaTiendaDelTodo)
    {
        if (!File.Exists(ruta)) return;

        Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);

        List<GameObject> tiendas = new List<GameObject>();
        List<GameObject> tenderos = new List<GameObject>();

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "PanelTienda") tiendas.Add(t.gameObject);
                if (t.name == "Tendero") tenderos.Add(t.gameObject);
            }
        }

        int cerradas = 0;
        int borradas = 0;

        foreach (GameObject go in tiendas)
        {
            if (borrarLaTiendaDelTodo)
            {
                Object.DestroyImmediate(go);
                borradas++;
            }
            else
            {
                if (go.activeSelf) cerradas++;
                go.SetActive(false);
            }
        }

        // El tendero del nivel del jefe sobra: no hay tienda que abrir
        foreach (GameObject go in tenderos)
        {
            if (borrarLaTiendaDelTodo)
            {
                Object.DestroyImmediate(go);
                borradas++;
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log(string.Format("[Ronda2] {0}: tienda cerrada {1}, objetos de tienda borrados {2}",
            Path.GetFileName(ruta), cerradas, borradas));
    }

    // ---------------------------------------------------- 5. botones del menu

    private static void AlinearBotonesDelMenu()
    {
        if (!File.Exists(EscenaMenu)) return;

        Scene escena = EditorSceneManager.OpenScene(EscenaMenu, OpenSceneMode.Single);

        RectTransform jugar = RectTransformDe(escena, "Button");
        RectTransform continuar = RectTransformDe(escena, "BotonContinuar");

        if (jugar == null || continuar == null)
        {
            Debug.LogWarning("[Ronda2] No encontre los botones del menu");
            return;
        }

        float altura = Mathf.Max(jugar.sizeDelta.y, 1f);

        // Misma x y mismo tamaño, separados al menos la altura + 20 px
        continuar.anchorMin = jugar.anchorMin;
        continuar.anchorMax = jugar.anchorMax;
        continuar.sizeDelta = jugar.sizeDelta;
        continuar.anchoredPosition = new Vector2(jugar.anchoredPosition.x, jugar.anchoredPosition.y - (altura + 20f));

        EditorUtility.SetDirty(jugar);
        EditorUtility.SetDirty(continuar);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log(string.Format("[Ronda2] Menu: boton EMPEZAR en y={0}, Continuar en y={1} (alto {2})",
            jugar.anchoredPosition.y, continuar.anchoredPosition.y, altura));
    }

    private static RectTransform RectTransformDe(Scene escena, string nombre)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre) return t as RectTransform;
            }
        }

        return null;
    }
}
