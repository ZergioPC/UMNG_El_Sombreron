using System.Collections;
using TMPro;
using UnityEngine;

public class Letrero : MonoBehaviour
{
    public static Letrero Instancia { get; private set; }

    [Header("Diseño")]
    public TMP_Text prefabLetrero;       // Si lo asignas, el aspecto se edita en el prefab
    public TMP_FontAsset fuente;         // Solo se usa si no hay prefab

    [Header("Textos")]
    public string textoCulpable = "¡Objetivo atrapado! Vamos por el siguiente";
    public string textoInocente = "¡Era inocente!";
    public string textoClon = "Nos engaño, era solo un clon!";
    public bool mostrarPuntos = true;

    [Header("Posición")]
    public bool enParteInferior = true;          // true = abajo del Canvas, false = sobre el NPC
    public float margenInferior = 120f;          // Píxeles desde el borde inferior
    public float alturaLetrero = 2.2f;           // Solo si enParteInferior = false (metros sobre el NPC)
    public bool reemplazarAnterior = true;       // Un solo letrero a la vez en la parte inferior

    [Header("Animación")]
    public float tamanoTexto = 64f;              // Solo si no hay prefab
    public float duracionLetrero = 1.8f;
    public float distanciaSubida = 60f;          // Píxeles que sube mientras se desvanece

    [Header("Colores")]
    public Color colorCulpable = new Color(1f, 0.85f, 0.3f);
    public Color colorInocente = new Color(1f, 0.35f, 0.3f);
    public Color colorClon = new Color(0.7f, 0.8f, 0.9f);

    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject letreroActual;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LetreroCaptura: no hay ningún Canvas en la escena.");
            enabled = false;
            return;
        }
        canvas = canvas.rootCanvas;
        canvasRect = canvas.GetComponent<RectTransform>();
    }

    // ---------- API pública ----------
    public void Mostrar(Vector3 posicion, bool culpable, int puntos, bool esClon = false)
    {
        if (!enabled) return;

        if (esClon)
        {
            CrearLetrero(posicion, textoClon, colorClon);
        }
        else if (culpable)
        {
            string txt = mostrarPuntos ? textoCulpable + " +" + puntos : textoCulpable;
            CrearLetrero(posicion, txt, colorCulpable);
        }
        else
        {
            string txt = mostrarPuntos ? textoInocente + "\n-" + puntos : textoInocente;
            CrearLetrero(posicion, txt, colorInocente);
        }
    }

    [ContextMenu("Probar letrero culpable (en Play)")]
    private void ProbarCulpable() { Mostrar(PosPrueba(), true, 100); }

    [ContextMenu("Probar letrero inocente (en Play)")]
    private void ProbarInocente() { Mostrar(PosPrueba(), false, 100); }

    private Vector3 PosPrueba()
    {
        return Camera.main != null
            ? Camera.main.transform.position + Camera.main.transform.forward * 6f
            : Vector3.zero;
    }

    // ---------- Letrero ----------
    private void CrearLetrero(Vector3 posMundo, string texto, Color color)
    {
        Vector2 posInicial;

        if (enParteInferior)
        {
            posInicial = new Vector2(0f, margenInferior);
            if (reemplazarAnterior && letreroActual != null) Destroy(letreroActual);
        }
        else
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 sp = cam.WorldToScreenPoint(posMundo + Vector3.up * alturaLetrero);
            if (sp.z <= 0f) return;

            Camera camUI = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, camUI, out posInicial)) return;
        }

        RectTransform rt;
        TextMeshProUGUI t;

        if (prefabLetrero != null)
        {
            TMP_Text copia = Instantiate(prefabLetrero, canvasRect);
            rt = copia.rectTransform;
            t = copia as TextMeshProUGUI;
            if (t == null)
            {
                Debug.LogError("LetreroCaptura: el prefab debe ser un TextMeshPro - Text (UI).");
                Destroy(copia.gameObject);
                return;
            }
        }
        else
        {
            GameObject go = new GameObject("Letrero", typeof(RectTransform));
            rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvasRect, false);
            rt.sizeDelta = new Vector2(900f, 140f);
            t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = tamanoTexto;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(10, 30, 40, 255);
            if (fuente != null) t.font = fuente;
        }

        // Abajo: ancla al borde inferior central. Sobre el NPC: ancla al centro.
        Vector2 ancla = enParteInferior ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = enParteInferior ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posInicial;
        t.text = texto;
        t.color = color;
        t.raycastTarget = false;

        if (enParteInferior) letreroActual = rt.gameObject;
        StartCoroutine(AnimarLetrero(rt, t, posInicial));
    }

    private IEnumerator AnimarLetrero(RectTransform rt, TextMeshProUGUI t, Vector2 inicio)
    {
        float dur = duracionLetrero;
        for (float tiempo = 0f; tiempo < dur; tiempo += Time.unscaledDeltaTime)
        {
            if (rt == null) yield break;
            float k = tiempo / dur;

            // "Pop": 0 -> 1.25 -> 1
            float escala;
            if (tiempo < 0.15f) escala = Mathf.Lerp(0f, 1.25f, tiempo / 0.15f);
            else if (tiempo < 0.25f) escala = Mathf.Lerp(1.25f, 1f, (tiempo - 0.15f) / 0.10f);
            else escala = 1f;
            rt.localScale = Vector3.one * escala;

            rt.anchoredPosition = inicio + Vector2.up * (distanciaSubida * Mathf.Sqrt(k));

            Color c = t.color;
            c.a = k < 0.65f ? 1f : Mathf.InverseLerp(1f, 0.65f, k);
            t.color = c;

            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }
}