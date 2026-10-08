using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransicionEscena : MonoBehaviour
{
    private static TransicionEscena instancia;

    private const float DURACION = 0.5f; // segundos de cada fade (salida y entrada)

    private Image panel;
    private bool transicionando;

    // Esto es lo que vas a llamar desde cualquier script
    public static void CargarEscena(string nombreEscena)
    {
        if (instancia == null)
        {
            GameObject go = new GameObject("TransicionEscena");
            instancia = go.AddComponent<TransicionEscena>();
        }

        // Si ya hay una transición en curso, ignoramos la llamada (evita dobles disparos)
        if (!instancia.transicionando)
        {
            instancia.StartCoroutine(instancia.Transicion(nombreEscena));
        }
    }

    private void Awake()
    {
        // Singleton: si ya existe otro, este sobra
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        CrearPanel();
    }

    private void CrearPanel()
    {
        // Canvas que cubre toda la pantalla y se dibuja por encima de todo
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        // Imagen negra hija del canvas, estirada a pantalla completa
        GameObject panelGO = new GameObject("PanelNegro");
        panelGO.transform.SetParent(transform, false);

        panel = panelGO.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0f); // negro, pero transparente
        panel.raycastTarget = false;

        RectTransform rt = panel.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator Transicion(string nombreEscena)
    {
        transicionando = true;
        panel.raycastTarget = true;

        // 1) Fade out: transparente -> negro
        yield return Fade(0f, 1f);

        // 2) Cargar la escena en segundo plano, pero sin activarla todavía
        AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscena);
        op.allowSceneActivation = false;

        // Unity carga hasta 0.9 y se queda esperando la activación
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // Activamos la escena (acá ocurren los Awake/Start, que pueden trabar un frame)
        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            yield return null;
        }

        // 3) Esperamos unos frames para que los Start pesados terminen
        //    ANTES de empezar a mostrar la escena
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        // Fade in: negro -> transparente
        yield return Fade(1f, 0f);

        panel.raycastTarget = false;
        transicionando = false;
    }

    private IEnumerator Fade(float desde, float hasta)
    {
        float t = 0f;
        while (t < DURACION)
        {
            // Limitamos el delta: un frame lento no puede "saltarse" el fade
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            float progreso = Mathf.SmoothStep(0f, 1f, t / DURACION);
            float alpha = Mathf.Lerp(desde, hasta, progreso);
            panel.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        panel.color = new Color(0f, 0f, 0f, hasta);
    }
}