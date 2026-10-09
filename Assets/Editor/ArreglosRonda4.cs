using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ronda 4: pedidos de diseno.
///  1. Se elimina el guardado: no hay boton "Continuar" (el juego no guarda partida).
///  2. Diseno y paleta del menu principal (titulo, subtitulo, botones y colores).
///  3. El jefe final queda mucho mas grande y con un color propio.
///  4. Animacion de recibir dano del jugador (arte PJ-S1-Damage).
/// </summary>
public static class ArreglosRonda4
{
    private const string EscenaMenu = "Assets/Scenes/MenuPrincipal.unity";
    private const string EscenaJefe = "Assets/Scenes/JefeFinal.unity";
    private const string FuenteSdf = "Assets/Fuentes/Crumbline SDF.asset";
    private const string SheetDano = "Assets/Sprites/Personaje/PJ-S1-Damage.png";
    private const string ClipDano = "Assets/Animaciones/HereoDano.anim";
    private const string ControlPersonaje = "Assets/Animaciones/AnimacionPersonaje.controller";

    // Paleta del juego: noche calida de montaña con ambar de antorcha
    private static readonly Color Fondo = new Color(0.078f, 0.063f, 0.047f, 1f);
    private static readonly Color Panel = new Color(0.129f, 0.105f, 0.082f, 1f);
    private static readonly Color Ambar = new Color(0.851f, 0.635f, 0.294f, 1f);
    private static readonly Color TextoClaro = new Color(0.949f, 0.906f, 0.835f, 1f);
    private static readonly Color TextoApagado = new Color(0.725f, 0.663f, 0.549f, 1f);
    private static readonly Color RojoVida = new Color(0.72f, 0.19f, 0.14f, 1f);

    [MenuItem("The Warrior Path/Arreglos ronda 4 (menu, jefe y dano)")]
    public static void Arreglar()
    {
        QuitarContinuar();
        DisenarMenu();
        AgrandarJefe();
        AnimacionDeDanoDelJugador();

        AssetDatabase.SaveAssets();
        Debug.Log("[Ronda4] Listo.");
    }

    // ------------------------------------------------ 1. sin guardado

    private static void QuitarContinuar()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaMenu, OpenSceneMode.Single);

        GameObject boton = Buscar(escena, "BotonContinuar");
        if (boton != null)
        {
            Object.DestroyImmediate(boton);
            Debug.Log("[Ronda4] Boton Continuar eliminado: el juego no guarda partida.");
        }

        MenuPrincipal gestor = Componente<MenuPrincipal>(escena);
        if (gestor != null)
        {
            SerializedObject so = new SerializedObject(gestor);
            SerializedProperty campo = so.FindProperty("botonContinuar");
            if (campo != null)
            {
                campo.objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        Guardar(escena, EscenaMenu);
    }

    // ------------------------------------------------ 2. diseño del menu

    private static void DisenarMenu()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaMenu, OpenSceneMode.Single);
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FuenteSdf);

        // Fondo: el arte original oscurecido a noche calida
        GameObject panel = Buscar(escena, "Panel");
        if (panel != null)
        {
            Image imagen = panel.GetComponent<Image>();
            if (imagen != null)
            {
                imagen.color = Fondo;
                EditorUtility.SetDirty(imagen);
            }
        }

        // Titulo y subtitulo
        GameObject titulo = AsegurarTexto(escena, panel, "TituloJuego", "THE WARRIOR PATH",
            150f, Ambar, new Vector2(0f, 300f), new Vector2(1500f, 180f), fuente);

        AsegurarTexto(escena, panel, "SubtituloJuego", "El camino del guerrero",
            56f, TextoApagado, new Vector2(0f, 195f), new Vector2(1200f, 80f), fuente);

        // Botones: EMPEZAR y SALIR, con el estilo de la paleta
        GameObject empezar = Buscar(escena, "Button");
        int botones = 0;

        if (empezar != null)
        {
            EstiloDeBoton(empezar, "EMPEZAR", 0f, fuente);
            botones++;

            GameObject salir = Buscar(escena, "BotonSalir");
            if (salir == null)
            {
                salir = Object.Instantiate(empezar, empezar.transform.parent);
                salir.name = "BotonSalir";

                TMP_Text etiqueta = salir.GetComponentInChildren<TMP_Text>(true);
                if (etiqueta != null) etiqueta.text = "SALIR";

                Button boton = salir.GetComponent<Button>();
                boton.onClick.RemoveAllListeners();

                MenuPrincipal gestor = Componente<MenuPrincipal>(escena);
                if (gestor != null)
                {
                    UnityEventTools.AddPersistentListener(boton.onClick, new UnityAction(gestor.Salir));
                }
            }

            EstiloDeBoton(salir, "SALIR", -130f, fuente);
            botones++;
        }

        // La pausa usa la misma paleta
        GameObject panelMenu = Buscar(escena, "PanelMenu");
        if (panelMenu != null) PintarPausa(panelMenu);

        Guardar(escena, EscenaMenu);

        Debug.Log("[Ronda4] Menu rediseñado: titulo propio, " + botones + " botones y paleta noche calida/ambar.");
        Debug.Log("[Ronda4] Titulo: " + (titulo != null ? "ok" : "no se pudo crear"));
    }

    private static void EstiloDeBoton(GameObject boton, string etiqueta, float y, TMP_FontAsset fuente)
    {
        RectTransform rt = boton.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 96f);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        Image fondo = boton.GetComponent<Image>();
        if (fondo != null)
        {
            fondo.color = Panel;
            EditorUtility.SetDirty(fondo);
        }

        Outline borde = boton.GetComponent<Outline>();
        if (borde == null) borde = boton.AddComponent<Outline>();
        borde.effectColor = new Color(Ambar.r, Ambar.g, Ambar.b, 0.55f);
        borde.effectDistance = new Vector2(3f, -3f);
        EditorUtility.SetDirty(borde);

        TMP_Text texto = boton.GetComponentInChildren<TMP_Text>(true);
        if (texto != null)
        {
            texto.text = etiqueta;
            texto.color = TextoClaro;
            texto.fontSize = 64f;
            if (fuente != null)
            {
                texto.font = fuente;
                texto.fontSharedMaterial = fuente.material;
            }
            EditorUtility.SetDirty(texto);
        }

        EditorUtility.SetDirty(boton);
    }

    private static void PintarPausa(GameObject panelMenu)
    {
        Image imagen = panelMenu.GetComponent<Image>();
        if (imagen != null)
        {
            imagen.color = new Color(Fondo.r, Fondo.g, Fondo.b, 0.94f);
            EditorUtility.SetDirty(imagen);
        }

        foreach (Button b in panelMenu.GetComponentsInChildren<Button>(true))
        {
            Image fondo = b.GetComponent<Image>();
            if (fondo != null) fondo.color = Panel;

            TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null) t.color = TextoClaro;

            EditorUtility.SetDirty(b);
        }
    }

    private static GameObject AsegurarTexto(Scene escena, GameObject padre, string nombre, string contenido,
        float tamano, Color color, Vector2 posicion, Vector2 medida, TMP_FontAsset fuente)
    {
        GameObject existente = Buscar(escena, nombre);
        TMP_Text texto;

        if (existente != null)
        {
            texto = existente.GetComponent<TMP_Text>();
        }
        else
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            if (padre != null) go.transform.SetParent(padre.transform, false);

            texto = go.AddComponent<TextMeshProUGUI>();
            RecargarFuente(texto, fuente);

            RectTransform rt = texto.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = medida;
            rt.anchoredPosition = posicion;

            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;

            if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(go, "Texto del menu");
        }

        texto.text = contenido;
        texto.fontSize = tamano;
        texto.color = color;
        RecargarFuente(texto, fuente);
        EditorUtility.SetDirty(texto);

        return texto.gameObject;
    }

    private static void RecargarFuente(TMP_Text texto, TMP_FontAsset fuente)
    {
        if (fuente == null) return;

        texto.font = fuente;
        texto.fontSharedMaterial = fuente.material;
    }

    // ------------------------------------------------ 3. jefe mas grande

    private static void AgrandarJefe()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaJefe, OpenSceneMode.Single);

        GameObject jefe = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponent<JefeFinal>() != null) { jefe = t.gameObject; break; }
            }

            if (jefe != null) break;
        }

        if (jefe == null)
        {
            Debug.LogWarning("[Ronda4] No encontre al jefe final");
            return;
        }

        jefe.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

        // Color propio: mas oscuro y rojizo, se distingue de un goblin comun
        SpriteRenderer sprite = jefe.GetComponentInChildren<SpriteRenderer>(true);
        if (sprite != null)
        {
            sprite.color = new Color(0.82f, 0.55f, 0.55f, 1f);
            EditorUtility.SetDirty(sprite);
        }

        Orco orco = jefe.GetComponent<Orco>();
        if (orco != null)
        {
            SerializedObject so = new SerializedObject(orco);
            SerializedProperty escala = so.FindProperty("radioDeteccion");
            if (escala != null) escala.floatValue = 14f;   // el jefe te ve desde lejos
            SerializedProperty rango = so.FindProperty("rangoAtaque");
            if (rango != null) rango.floatValue = 1.9f;    // pega mas lejos porque es grande
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Guardar(escena, EscenaJefe);

        Debug.Log("[Ronda4] Jefe final: escala 1,8 y color propio. Detecta desde 14 y golpea a 1,9.");
    }

    // ------------------------------------------------ 4. animacion de daño del jugador

    private static void AnimacionDeDanoDelJugador()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDano);

        if (clip == null)
        {
            List<Sprite> sprites = SpritesDe(SheetDano);
            if (sprites.Count == 0)
            {
                Debug.LogWarning("[Ronda4] No pude leer " + SheetDano);
                return;
            }

            clip = new AnimationClip();
            clip.name = "HereoDano";

            EditorCurveBinding enlace = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            List<ObjectReferenceKeyframe> llaves = new List<ObjectReferenceKeyframe>();

            for (int i = 0; i < sprites.Count; i++)
            {
                ObjectReferenceKeyframe k = new ObjectReferenceKeyframe();
                k.time = i * 0.12f;
                k.value = sprites[i];
                llaves.Add(k);
            }

            AnimationUtility.SetObjectReferenceCurve(clip, enlace, llaves.ToArray());

            AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
            ajustes.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, ajustes);

            AssetDatabase.CreateAsset(clip, ClipDano);
            AssetDatabase.SaveAssets();

            Debug.Log("[Ronda4] Clip de daño creado con " + sprites.Count + " sprites.");
        }

        AnimatorController control = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControlPersonaje);
        if (control == null)
        {
            Debug.LogWarning("[Ronda4] Falta " + ControlPersonaje);
            return;
        }

        bool tieneTrigger = false;
        foreach (AnimatorControllerParameter p in control.parameters)
        {
            if (p.name == "Dano") tieneTrigger = true;
        }

        if (!tieneTrigger) control.AddParameter("Dano", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine maquina = control.layers[0].stateMachine;
        AnimatorState estadoDano = null;
        AnimatorState estadoQuieto = null;

        foreach (ChildAnimatorState c in maquina.states)
        {
            if (c.state.name == "HereoDano") estadoDano = c.state;
            if (c.state.name == "HereoQuieto") estadoQuieto = c.state;
        }

        if (estadoDano == null)
        {
            estadoDano = maquina.AddState("HereoDano", new Vector3(600f, 300f, 0f));
            estadoDano.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipDano);

            AnimatorStateTransition entra = maquina.AddAnyStateTransition(estadoDano);
            entra.hasExitTime = false;
            entra.duration = 0.05f;
            entra.canTransitionToSelf = false;
            entra.AddCondition(AnimatorConditionMode.If, 0f, "Dano");

            if (estadoQuieto != null)
            {
                AnimatorStateTransition sale = estadoDano.AddTransition(estadoQuieto);
                sale.hasExitTime = true;
                sale.exitTime = 1f;
                sale.duration = 0.1f;
            }

            Debug.Log("[Ronda4] Estado HereoDano agregado al controlador del jugador.");
        }

        EditorUtility.SetDirty(control);
        AssetDatabase.SaveAssets();
    }

    private static List<Sprite> SpritesDe(string rutaPng)
    {
        List<Sprite> salida = new List<Sprite>();

        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(rutaPng))
        {
            Sprite s = o as Sprite;
            if (s != null) salida.Add(s);
        }

        salida.Sort((a, b) => Numero(a.name).CompareTo(Numero(b.name)));
        return salida;
    }

    private static int Numero(string nombre)
    {
        int i = nombre.LastIndexOf('_');
        if (i < 0) return 0;

        int n;
        return int.TryParse(nombre.Substring(i + 1), out n) ? n : 0;
    }

    // ------------------------------------------------ utilidades

    private static void Guardar(Scene escena, string ruta)
    {
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    private static GameObject Buscar(Scene escena, string nombre)
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

    private static T Componente<T>(Scene escena) where T : Component
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            T c = raiz.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }

        return null;
    }
}
