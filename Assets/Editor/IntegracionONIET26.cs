using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Integracion de recursos ONIET26 y pulido de controles/mapas.
/// Usa el arte original incluido en ONIET26/Boss y victoria.zip y el Arbol.prefab
/// existente. Repetible: no duplica decoracion ni estados.
/// </summary>
public static class IntegracionONIET26
{
    private const string SceneMenu = "Assets/Scenes/MenuPrincipal.unity";
    private const string SceneVillage = "Assets/Scenes/SampleScene.unity";
    private const string SceneCave = "Assets/Scenes/SampleScene2.unity";
    private const string SceneBoss = "Assets/Scenes/JefeFinal.unity";
    private const string BossFolder = "Assets/Sprites/Boss/ONIET26";
    private const string BossControllerPath = "Assets/Animaciones/JefeONIET26.controller";
    private const string BossWalkClipPath = "Assets/Animaciones/JefeONIET26-Camina.anim";
    private const string BossAttackClipPath = "Assets/Animaciones/JefeONIET26-Ataca.anim";
    private const string BossDeathClipPath = "Assets/Animaciones/JefeONIET26-Muere.anim";
    private const string GoblinPrefab = "Assets/Prefabs/OrcoFinal.prefab";
    private const string GoblinControllerPath = "Assets/Animaciones/AnimacionOrco.controller";
    private const string GoblinWalkSheet = "Assets/Sprites/Orco/Golblin-S1-Walk-Sheet.png";
    private const string TreePrefab = "Assets/Sprites/Environment/Arbol.prefab";

    private static readonly Color MenuFondo = new Color(0.078f, 0.063f, 0.047f, 1f);
    private static readonly Color MenuPanel = new Color(0.129f, 0.105f, 0.082f, 1f);
    private static readonly Color MenuAmbar = new Color(0.851f, 0.635f, 0.294f, 1f);
    private static readonly Color MenuTexto = new Color(0.949f, 0.906f, 0.835f, 1f);

    [MenuItem("The Warrior Path/Integrar recursos ONIET26")]
    public static void Integrar()
    {
        AssetDatabase.Refresh();
        ConfigurarTexturasJefe();
        ConfigurarVictoria();
        AnimationClip caminar = CrearClip("Walkboss", "JefeONIET26-Camina.anim", true);
        AnimationClip atacar = CrearClip("Attackboss", "JefeONIET26-Ataca.anim", false);
        AnimationClip morir = CrearClipMuerte("JefeONIET26-Muere.anim");
        AnimatorController jefeController = CrearControladorJefe(caminar, atacar, morir);
        ConfigurarGoblinDeZonaUno();
        ConfigurarEscenaJefe(jefeController, SpriteJefe("Walkboss1.png"));
        AplicarMenuYControles();
        DecorarZonaUnoConArboles();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ONIET26] Integracion terminada.");
    }

    // ------------------------------------------------ sprites del jefe

    private static void ConfigurarTexturasJefe()
    {
        string[] files = Directory.GetFiles(BossFolder, "*.png");
        foreach (string absolute in files)
        {
            string path = absolute.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Debug.Log("[ONIET26] Frames del jefe configurados como sprites pixel-art (PPU 32).");
    }

    private static void ConfigurarVictoria()
    {
        const string path = "Assets/Resources/ONIET26/Victoria.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[ONIET26] Falta la ilustracion " + path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static Sprite SpriteJefe(string nombre)
    {
        string path = BossFolder + "/" + nombre;
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static List<Sprite> SpritesJefe(string prefijo)
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 1; i <= 4; i++)
        {
            Sprite s = SpriteJefe(prefijo + i + ".png");
            if (s != null) sprites.Add(s);
        }
        return sprites;
    }

    private static AnimationClip CrearClip(string prefijo, string ruta, bool loop)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaciones/" + ruta);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.name = Path.GetFileNameWithoutExtension(ruta);
            AssetDatabase.CreateAsset(clip, "Assets/Animaciones/" + ruta);
        }

        List<Sprite> sprites = SpritesJefe(prefijo);
        if (sprites.Count == 0)
        {
            Debug.LogError("[ONIET26] No se importaron sprites para " + prefijo);
            return clip;
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe { time = i * 0.12f, value = sprites[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.frameRate = 8f;
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimationClip CrearClipMuerte(string ruta)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animaciones/" + ruta);
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.name = Path.GetFileNameWithoutExtension(ruta);
            AssetDatabase.CreateAsset(clip, "Assets/Animaciones/" + ruta);
        }

        // Sin hoja de muerte para el jefe: usa el ultimo frame de ataque y se desvanece
        Sprite ultimo = SpriteJefe("Attackboss4.png");
        if (ultimo != null)
        {
            EditorCurveBinding sprite = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, sprite, new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = SpriteJefe("Attackboss4.png") },
                new ObjectReferenceKeyframe { time = 0.2f, value = ultimo }
            });
        }

        EditorCurveBinding alpha = EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_Color.a");
        AnimationCurve curva = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.3f, 0.8f), new Keyframe(0.8f, 0f));
        AnimationUtility.SetEditorCurve(clip, alpha, curva);
        clip.frameRate = 8f;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CrearControladorJefe(AnimationClip caminar, AnimationClip atacar, AnimationClip morir)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(BossControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(BossControllerPath);
        }

        AsegurarTrigger(controller, "Ataca");
        AsegurarTrigger(controller, "Muere");

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState walk = BuscarEstado(machine, "JefeCamina");
        if (walk == null) walk = machine.AddState("JefeCamina", new Vector3(250f, 100f, 0f));
        walk.motion = caminar;
        machine.defaultState = walk;

        AnimatorState attack = BuscarEstado(machine, "JefeAtaca");
        if (attack == null) attack = machine.AddState("JefeAtaca", new Vector3(500f, 100f, 0f));
        attack.motion = atacar;

        AnimatorState death = BuscarEstado(machine, "JefeMuere");
        if (death == null) death = machine.AddState("JefeMuere", new Vector3(500f, 250f, 0f));
        death.motion = morir;

        if (attack.transitions.Length == 0)
        {
            AnimatorStateTransition regreso = attack.AddTransition(walk);
            regreso.hasExitTime = true;
            regreso.exitTime = 1f;
            regreso.duration = 0.08f;
        }

        AsegurarTransicionAnyState(machine, attack, "Ataca");
        AsegurarTransicionAnyState(machine, death, "Muere");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState BuscarEstado(AnimatorStateMachine machine, string nombre)
    {
        foreach (ChildAnimatorState state in machine.states)
        {
            if (state.state.name == nombre) return state.state;
        }
        return null;
    }

    private static void AsegurarTrigger(AnimatorController controller, string nombre)
    {
        foreach (AnimatorControllerParameter p in controller.parameters)
        {
            if (p.name == nombre) return;
        }
        controller.AddParameter(nombre, AnimatorControllerParameterType.Trigger);
    }

    private static void AsegurarTransicionAnyState(AnimatorStateMachine machine, AnimatorState state, string trigger)
    {
        foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
        {
            if (transition.destinationState == state) return;
        }

        AnimatorStateTransition t = machine.AddAnyStateTransition(state);
        t.hasExitTime = false;
        t.duration = 0.05f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }

    // ------------------------------------------------ corregir goblins que aun usaban el ojo del murcielago

    private static void ConfigurarGoblinDeZonaUno()
    {
        GameObject contenido = PrefabUtility.LoadPrefabContents(GoblinPrefab);
        Animator animator = contenido.GetComponentInChildren<Animator>(true);
        SpriteRenderer renderer = contenido.GetComponentInChildren<SpriteRenderer>(true);
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GoblinControllerPath);
        Sprite[] walk = AssetDatabase.LoadAllAssetsAtPath(GoblinWalkSheet) as Sprite[];
        if (walk == null || walk.Length == 0)
        {
            List<Sprite> list = new List<Sprite>();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(GoblinWalkSheet))
            {
                Sprite s = o as Sprite;
                if (s != null) list.Add(s);
            }
            list.Sort((a, b) => a.name.CompareTo(b.name));
            walk = list.ToArray();
        }

        if (animator != null && controller != null) animator.runtimeAnimatorController = controller;
        if (renderer != null && walk.Length > 0)
        {
            renderer.sprite = walk[0];
            renderer.color = Color.white;
        }

        PrefabUtility.SaveAsPrefabAsset(contenido, GoblinPrefab);
        PrefabUtility.UnloadPrefabContents(contenido);
        AssetDatabase.Refresh();
        Debug.Log("[ONIET26] Variantes colocadas en Zona 1 vuelven al sprite/controlador de goblin.");
    }

    // ------------------------------------------------ jefe en la escena final

    private static void ConfigurarEscenaJefe(AnimatorController controller, Sprite spriteInicial)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/JefeFinal.unity", OpenSceneMode.Single);
        JefeFinal jefe = BuscarComponente<JefeFinal>(scene);
        if (jefe == null)
        {
            Debug.LogWarning("[ONIET26] No encuentro JefeFinal en la escena.");
            return;
        }

        Transform root = jefe.transform;
        root.localScale = new Vector3(2.8f, 2.8f, 1f);
        Orco orco = jefe.GetComponent<Orco>();
        if (orco != null)
        {
            SerializedObject stats = new SerializedObject(orco);
            SerializedProperty vida = stats.FindProperty("vidaOrco");
            if (vida != null) vida.intValue = 30;
            SerializedProperty rango = stats.FindProperty("rangoAtaque");
            if (rango != null) rango.floatValue = 1.8f;
            stats.ApplyModifiedPropertiesWithoutUndo();
        }

        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null) animator.runtimeAnimatorController = controller;

        SpriteRenderer sprite = root.GetComponentInChildren<SpriteRenderer>(true);
        if (sprite != null)
        {
            sprite.sprite = spriteInicial;
            sprite.color = Color.white;
            sprite.sortingOrder = 2;
        }

        // Ajusta el cuerpo a su nuevo tamaño sin convertirlo en una pared infranqueable.
        foreach (BoxCollider2D box in root.GetComponentsInChildren<BoxCollider2D>(true))
        {
            if (box.GetComponent<AtaqueOrco>() != null)
            {
                box.size = new Vector2(0.9f, 0.75f);
            }
            else
            {
                box.size = new Vector2(0.55f, 0.72f);
            }
        }

        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ONIET26] Jefe actualizado: 30 HP, escala 2.8, sprite/controlador ONIET26.");
    }

    // ------------------------------------------------ menú + controles + pausa

    private static void AplicarMenuYControles()
    {
        foreach (string path in new[] { SceneMenu, SceneVillage, SceneCave, SceneBoss })
        {
            if (!File.Exists(path)) continue;
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MenuPausa pause in root.GetComponentsInChildren<MenuPausa>(true))
                {
                    PintarPausa(pause);
                }
            }

            if (path == SceneMenu)
            {
                GameObject panel = Buscar(scene, "Panel");
                if (panel != null)
                {
                    Image image = panel.GetComponent<Image>();
                    // Dejar visible la ilustracion Home-Screen original de ONIET26.
                    if (image != null) image.color = Color.white;
                }

                // La ilustracion ya incluye el titulo. Evitar que se dibuje duplicado por encima.
                DesactivarSiExiste(scene, "TituloJuego");
                DesactivarSiExiste(scene, "SubtituloJuego");

            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[ONIET26] Menu conserva Home-Screen original; pausa mantiene paleta y usa P/ESC.");
    }

    private static void PintarPausa(MenuPausa pause)
    {
        // Los paneles son hermanos del gestor en el Canvas; obtenerlos de los campos
        // serializados evita depender de una jerarquia concreta de cada escena.
        SerializedObject so = new SerializedObject(pause);
        SerializedProperty panelProperty = so.FindProperty("panelPausa");
        SerializedProperty gradientProperty = so.FindProperty("panelGradiante");
        GameObject panel = panelProperty == null ? null : panelProperty.objectReferenceValue as GameObject;
        GameObject gradient = gradientProperty == null ? null : gradientProperty.objectReferenceValue as GameObject;

        if (panel != null)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null) image.color = new Color(0.129f, 0.105f, 0.082f, 0.96f);
            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text.ToLower().Contains("pausa")) text.color = MenuAmbar;
                else text.color = MenuTexto;
            }
        }

        if (gradient != null)
        {
            Image shade = gradient.GetComponent<Image>();
            if (shade != null) shade.color = new Color(MenuFondo.r, MenuFondo.g, MenuFondo.b, 0.72f);
        }
    }

    // ------------------------------------------------ arboles decorativos seguros en el pueblo

    private static void DecorarZonaUnoConArboles()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        if (Buscar(scene, "ONIET26_Arbol_0") != null)
        {
            Debug.Log("[ONIET26] Decoracion de arboles ya aplicada.");
            return;
        }

        Tilemap floor = BuscarTilemap(scene, "Tilemap_Suelo");
        Tilemap collision = BuscarTilemap(scene, "Tilemap_Colisiones");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefab);

        if (floor == null || collision == null || prefab == null)
        {
            Debug.LogWarning("[ONIET26] Falta mapa de suelo/colision o el prefab de arbol; no se agregan decoraciones.");
            return;
        }

        // El prefab original de Arbol no incluye TilemapCollider2D; cada instancia
        // vuelve a desactivar cualquier Collider2D si la importacion lo agrega en el futuro.

        List<Transform> interactivos = new List<Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.CompareTag("Player") || t.GetComponent<Portal>() != null || t.GetComponent<Tendero>() != null)
                    interactivos.Add(t);
            }
        }

        BoundsInt bounds = floor.cellBounds;
        List<Vector3Int> candidates = new List<Vector3Int>();
        int step = Mathf.Max(3, Mathf.Min(bounds.size.x, bounds.size.y) / 8);

        for (int y = bounds.yMin + 3; y < bounds.yMax - 3; y += step)
        {
            for (int x = bounds.xMin + 3; x < bounds.xMax - 3; x += step)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!floor.HasTile(cell)) continue;
                if (!ArbolCabeSinBloquear(cell, floor, collision)) continue;

                Vector3 world = floor.GetCellCenterWorld(cell);
                bool lejos = true;
                foreach (Transform t in interactivos)
                {
                    if (Vector2.Distance(world, t.position) < 5f) { lejos = false; break; }
                }
                if (lejos) candidates.Add(cell);
            }
        }

        // Spread the trees across different quadrants, at most five.
        candidates.Sort((a, b) => DistanceToCenter(b, bounds).CompareTo(DistanceToCenter(a, bounds)));
        int count = Mathf.Min(5, candidates.Count);
        List<Vector3> placed = new List<Vector3>();
        Tilemap prefabMap = prefab.GetComponentInChildren<Tilemap>(true);

        for (int i = 0; i < candidates.Count && placed.Count < count; i++)
        {
            Vector3 center = floor.GetCellCenterWorld(candidates[i]);
            bool spread = true;
            foreach (Vector3 p in placed) if (Vector2.Distance(center, p) < 7f) { spread = false; break; }
            if (!spread) continue;

            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            tree.name = "ONIET26_Arbol_" + placed.Count;
            Tilemap treeMap = tree.GetComponentInChildren<Tilemap>(true);

            if (treeMap != null)
            {
                Vector3 treeCenterWorld = treeMap.transform.TransformPoint(treeMap.localBounds.center);
                Vector3 delta = center - treeCenterWorld;
                tree.transform.position += delta;
                TilemapRenderer renderer = treeMap.GetComponent<TilemapRenderer>();
                if (renderer != null) renderer.sortingOrder = 3;
            }

            foreach (Collider2D c in tree.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            placed.Add(center);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ONIET26] Arboles decorativos de ONIET26 colocados en el pueblo: " + placed.Count + "; sin colliders.");
    }

    private static int DistanceToCenter(Vector3Int cell, BoundsInt b)
    {
        int cx = (b.xMin + b.xMax) / 2;
        int cy = (b.yMin + b.yMax) / 2;
        int dx = cell.x - cx;
        int dy = cell.y - cy;
        return dx * dx + dy * dy;
    }

    private static bool ArbolCabeSinBloquear(Vector3Int center, Tilemap floor, Tilemap collision)
    {
        // El prefab ocupa aproximadamente 4x4 celdas. Requerir suelo y ninguna
        // colision en todo el rectangulo evita ponerlo sobre paredes/portales.
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -2; x <= 2; x++)
            {
                Vector3Int cell = center + new Vector3Int(x, y, 0);
                if (!floor.HasTile(cell) || collision.HasTile(cell)) return false;
            }
        }
        return true;
    }

    // ------------------------------------------------ utilidades

    private static void DesactivarSiExiste(Scene scene, string nombre)
    {
        GameObject go = Buscar(scene, nombre);
        if (go != null) go.SetActive(false);
    }

    private static Tilemap BuscarTilemap(Scene scene, string nombre)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Tilemap map in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (map.name == nombre) return map;
            }
        }
        return null;
    }

    private static GameObject Buscar(Scene scene, string nombre)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre) return t.gameObject;
            }
        }
        return null;
    }

    private static GameObject BuscarHijo(Transform parent, string nombre)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == nombre) return t.gameObject;
        }
        return null;
    }

    private static T BuscarComponente<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }
}
