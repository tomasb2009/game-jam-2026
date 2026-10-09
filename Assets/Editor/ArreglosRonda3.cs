using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ronda 3: el bloqueo del menu y el balance.
///
/// El menu no mostraba nada porque la fuente `Crumbline-Regular Bitmap.asset`
/// esta VACIA (0 glifos, sin atlas ni material): TMP no puede dibujar texto.
/// La .ttf original si esta en el proyecto, asi que se regenera la fuente como
/// SDF y se reemplaza en todas las escenas.
/// Tambien baja la latencia de audio y ajusta el ritmo del jefe final.
/// </summary>
public static class ArreglosRonda3
{
    private const string FuenteTtf = "Assets/Fuentes/Crumbline-Regular.ttf";
    private const string FuenteVacia = "Assets/Fuentes/Crumbline-Regular Bitmap.asset";
    private const string FuenteNueva = "Assets/Fuentes/Crumbline SDF.asset";
    private const string EscenaJefe = "Assets/Scenes/JefeFinal.unity";

    private static readonly string[] Escenas =
    {
        "Assets/Scenes/MenuPrincipal.unity",
        "Assets/Scenes/SampleScene.unity",
        "Assets/Scenes/SampleScene2.unity",
        "Assets/Scenes/JefeFinal.unity",
        "Assets/Scenes/SampleScene3.unity"
    };

    [MenuItem("The Warrior Path/Arreglos ronda 3 (fuente, audio y jefe)")]
    public static void Arreglar()
    {
        TMP_FontAsset fuente = RegenerarFuente();
        if (fuente == null)
        {
            Debug.LogError("[Ronda3] No pude generar la fuente: se corta para no dejar el juego sin texto.");
            return;
        }

        int escenas = 0;
        foreach (string ruta in Escenas)
        {
            if (CambiarFuenteEnEscena(ruta, fuente)) escenas++;
        }

        LimpiarUIManagerDelMenu();
        BajarLatenciaDeAudio();
        AjustarAudioDeLaEscena();
        AjustarJefeFinal();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Ronda3] Listo: fuente regenerada, escenas con texto " + escenas + ", audio y jefe ajustados.");
    }

    // ------------------------------------------------ fuente

    private static TMP_FontAsset RegenerarFuente()
    {
        TMP_FontAsset existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FuenteNueva);
        if (existente != null && existente.characterTable != null && existente.characterTable.Count > 0)
        {
            Debug.Log("[Ronda3] La fuente nueva ya existe con " + existente.characterTable.Count + " glifos.");
            return existente;
        }

        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FuenteTtf);
        if (ttf == null)
        {
            Debug.LogError("[Ronda3] Falta " + FuenteTtf);
            return null;
        }

        TMP_FontAsset fuente = TMP_FontAsset.CreateFontAsset(
            ttf, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, true);

        if (fuente == null)
        {
            Debug.LogError("[Ronda3] TMP_FontAsset.CreateFontAsset devolvio null");
            return null;
        }

        // Se generan los glifos que usa el juego para que queden dentro del asset
        fuente.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?¡¿ÁÉÍÓÚÑáéíóúñ$()-+%/");

        fuente.name = "Crumbline SDF";
        AssetDatabase.CreateAsset(fuente, FuenteNueva);

        if (fuente.atlasTextures != null && fuente.atlasTextures.Length > 0 && fuente.atlasTextures[0] != null)
        {
            fuente.atlasTextures[0].name = "Crumbline SDF Atlas";
            AssetDatabase.AddObjectToAsset(fuente.atlasTextures[0], fuente);
        }

        if (fuente.material != null)
        {
            fuente.material.name = "Crumbline SDF Material";
            AssetDatabase.AddObjectToAsset(fuente.material, fuente);
        }

        EditorUtility.SetDirty(fuente);
        AssetDatabase.SaveAssets();

        Debug.Log("[Ronda3] Fuente creada: " + FuenteNueva + " con " + fuente.characterTable.Count + " glifos.");
        return fuente;
    }

    private static bool CambiarFuenteEnEscena(string ruta, TMP_FontAsset nueva)
    {
        if (!File.Exists(ruta)) return false;

        Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
        int cambiados = 0;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (TMP_Text texto in raiz.GetComponentsInChildren<TMP_Text>(true))
            {
                string actual = texto.font == null ? "" : AssetDatabase.GetAssetPath(texto.font);

                // Solo se reemplaza la fuente rota; las que funcionan se dejan como estan
                if (actual != FuenteVacia && !(texto.font != null && texto.font.characterTable != null && texto.font.characterTable.Count == 0 && texto.font.name.Contains("Crumbline")))
                {
                    continue;
                }

                texto.font = nueva;
                texto.fontSharedMaterial = nueva.material;
                EditorUtility.SetDirty(texto);
                cambiados++;
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[Ronda3] " + Path.GetFileName(ruta) + ": textos con la fuente arreglada = " + cambiados);
        return true;
    }

    // ------------------------------------------------ menu

    // En la escena del menu el UIManager tiene asignado el Panel principal en el campo
    // 'tienda': apagarlo borraba el menu entero. Se limpian esos campos.
    private static void LimpiarUIManagerDelMenu()
    {
        Scene escena = EditorSceneManager.OpenScene("Assets/Scenes/MenuPrincipal.unity", OpenSceneMode.Single);

        UIManager ui = null;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            ui = raiz.GetComponentInChildren<UIManager>(true);
            if (ui != null) break;
        }

        if (ui == null)
        {
            Debug.LogWarning("[Ronda3] No hay UIManager en el menu");
            return;
        }

        SerializedObject so = new SerializedObject(ui);
        string[] sobrantes = { "tienda", "panelEquipo", "cajaTexto", "textoDialogo", "textoMonedas", "imagenBarraVida" };
        int limpiados = 0;

        foreach (string campo in sobrantes)
        {
            SerializedProperty p = so.FindProperty(campo);
            if (p == null) continue;
            if (p.objectReferenceValue == null) continue;

            p.objectReferenceValue = null;
            limpiados++;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[Ronda3] UIManager del menu: campos que apuntaban a objetos del menu limpiados = " + limpiados);
    }

    // ------------------------------------------------ audio

    private static void BajarLatenciaDeAudio()
    {
        // El tamano del buffer DSP ya no se puede cambiar por codigo en Unity 6:
        // se dejo en "Best latency" en ProjectSettings/AudioManager.asset (m_DSPBufferSize: 1).
        int largo, cantidad;
        AudioSettings.GetDSPBufferSize(out largo, out cantidad);
        Debug.Log("[Ronda3] Buffer de audio actual: " + largo + " muestras x " + cantidad + " buffers.");
    }

    private static void AjustarAudioDeLaEscena()
    {
        foreach (string ruta in Escenas)
        {
            if (!File.Exists(ruta)) continue;

            Scene escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            int fuentes = 0;

            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                foreach (AudioSource audio in raiz.GetComponentsInChildren<AudioSource>(true))
                {
                    // Efectos en 2D y sin arrancar solos: evita volumen y retrasos raros
                    audio.spatialBlend = 0f;
                    audio.playOnAwake = false;
                    audio.dopplerLevel = 0f;
                    EditorUtility.SetDirty(audio);
                    fuentes++;
                }
            }

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        Debug.Log("[Ronda3] Fuentes de audio puestas en 2D y sin autoarranque.");
    }

    // ------------------------------------------------ jefe final

    private static void AjustarJefeFinal()
    {
        if (!File.Exists(EscenaJefe)) return;

        Scene escena = EditorSceneManager.OpenScene(EscenaJefe, OpenSceneMode.Single);
        Orco jefe = null;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                Orco o = t.GetComponent<Orco>();
                if (o != null && t.GetComponent<JefeFinal>() != null) { jefe = o; break; }
            }

            if (jefe != null) break;
        }

        if (jefe == null)
        {
            Debug.LogWarning("[Ronda3] No encontre al jefe final");
            return;
        }

        // Golpe cuerpo a cuerpo mas espaciado y embestida menos seguida
        SerializedObject soOrco = new SerializedObject(jefe);
        SerializedProperty cooldown = soOrco.FindProperty("cooldownAtaque");
        if (cooldown != null) cooldown.floatValue = 2.9f;
        SerializedProperty dano = soOrco.FindProperty("tiempoInvulnerable");
        if (dano != null) dano.floatValue = 0.55f;   // se puede golpear un poco mas seguido
        soOrco.ApplyModifiedPropertiesWithoutUndo();

        JefeFinal final = jefe.GetComponent<JefeFinal>();
        SerializedObject soFinal = new SerializedObject(final);

        SerializedProperty intervalo = soFinal.FindProperty("intervaloEmbestida");
        if (intervalo != null) intervalo.floatValue = 10.5f;

        SerializedProperty aviso = soFinal.FindProperty("avisoEmbestida");
        if (aviso != null) aviso.floatValue = 0.45f;

        SerializedProperty velocidad = soFinal.FindProperty("velocidadEmbestida");
        if (velocidad != null) velocidad.floatValue = 11f;

        SerializedProperty duracion = soFinal.FindProperty("duracionEmbestida");
        if (duracion != null) duracion.floatValue = 0.45f;

        SerializedProperty invocacion = soFinal.FindProperty("intervaloInvocacion");
        if (invocacion != null) invocacion.floatValue = 15f;

        soFinal.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(jefe.gameObject);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log("[Ronda3] Jefe final mas lento: golpe cada 2,9 s, embestida cada 10,5 s con 0,45 s de aviso.");
    }
}
