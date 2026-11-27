using System.Collections;
using UnityEngine;

public enum TipoLetrero { Culpable, Inocente, Clon }

public class Letrero : MonoBehaviour
{
    public static Letrero Instancia { get; private set; }

    [System.Serializable]
    public class ConfigLetrero
    {
        public string texto = "¡Culpable!";
        public Color color = Color.white;
        public Sprite icono;
        public AudioClip sonido; // Audio de diálogo que suena al salir este cartel
    }

    // Diseño (se edita en el prefab)
    public LetreroItem prefabLetrero;

    // Textos, colores, iconos y sonidos
    public ConfigLetrero culpable = new ConfigLetrero
    { texto = "Otro objetivo atrapado", color = new Color(1f, 0.85f, 0.3f) };

    public ConfigLetrero inocente = new ConfigLetrero
    { texto = "¡Nos equivocamos! Era inocente", color = new Color(1f, 0.45f, 0.4f) };

    public ConfigLetrero clon = new ConfigLetrero
    { texto = "Nos engaño... era un clon", color = new Color(0.75f, 0.85f, 1f) };

    public bool mostrarPuntos = true;

    // AudioSource de los diálogos (Output = Voces, Play On Awake desmarcado)
    public AudioSource fuenteVoz;

    // Burbuja sobre el jugador
    public Transform objetivo; // Si está vacío, busca al PlayerController
    public Vector3 offsetMundo = new Vector3(0f, 2.4f, 0f); // Punta de la colita sobre la cabeza
    public Vector2 offsetPantalla = Vector2.zero; // Ajuste fino en píxeles
    public bool reemplazarAnterior = true;
    public float margenBordes = 20f; // Para que no se salga de la pantalla

    // Animación
    public float duracion = 1.8f;
    public float distanciaSubida = 25f;

    private Canvas canvas;
    private RectTransform canvasRect;
    private LetreroItem actual;

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;

        // Si no asignas el AudioSource, usa el que esté en este mismo objeto
        if (fuenteVoz == null) fuenteVoz = GetComponent<AudioSource>();

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null || prefabLetrero == null)
        {
            Debug.LogError("LetreroCaptura: falta el Canvas o el Prefab Letrero (Tools > Crear Prefab Letrero).");
            enabled = false;
            return;
        }
        canvas = canvas.rootCanvas;
        canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        if (objetivo == null)
        {
            PlayerController jugador = FindAnyObjectByType<PlayerController>();
            if (jugador != null) objetivo = jugador.transform;
        }
    }

    // ---------- API pública ----------
    public void Mostrar(TipoLetrero tipo, int puntos = 0)
    {
        if (!enabled) return;
        if (objetivo == null) Start();
        if (objetivo == null) return;

        ConfigLetrero cfg = tipo == TipoLetrero.Culpable ? culpable
                          : tipo == TipoLetrero.Inocente ? inocente
                          : clon;

        string textoPuntos = "";
        if (mostrarPuntos && tipo == TipoLetrero.Culpable) textoPuntos = "+" + puntos;
        if (mostrarPuntos && tipo == TipoLetrero.Inocente) textoPuntos = "-" + puntos;

        if (reemplazarAnterior && actual != null) Destroy(actual.gameObject);

        LetreroItem item = Instantiate(prefabLetrero, canvasRect);
        item.name = "Letrero";
        item.Configurar(cfg.texto, textoPuntos, cfg.color, cfg.icono);

        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f); // La punta de la colita queda en el punto calculado

        actual = item;
        Posicionar(item);
        Sonar(cfg.sonido);
        StartCoroutine(Animar(item));
    }

    [ContextMenu("Probar culpable (en Play)")]
    private void ProbarCulpable() { Mostrar(TipoLetrero.Culpable, 100); }

    [ContextMenu("Probar inocente (en Play)")]
    private void ProbarInocente() { Mostrar(TipoLetrero.Inocente, 100); }

    [ContextMenu("Probar clon (en Play)")]
    private void ProbarClon() { Mostrar(TipoLetrero.Clon); }

    // ---------- Seguimiento ----------
    private void LateUpdate()
    {
        if (actual != null) Posicionar(actual);
    }

    private void Posicionar(LetreroItem item)
    {
        Camera cam = Camera.main;
        if (cam == null || objetivo == null) return;

        Vector3 sp = cam.WorldToScreenPoint(objetivo.position + offsetMundo);
        if (sp.z <= 0f) return;

        Camera camUI = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, camUI, out local)) return;

        local += offsetPantalla;

        // Mantener la burbuja dentro de la pantalla
        RectTransform rt = item.GetComponent<RectTransform>();
        Vector2 mitadCanvas = canvasRect.rect.size * 0.5f;
        float mitadAncho = rt.rect.width * 0.5f;
        local.x = Mathf.Clamp(local.x, -mitadCanvas.x + mitadAncho + margenBordes, mitadCanvas.x - mitadAncho - margenBordes);
        local.y = Mathf.Clamp(local.y, -mitadCanvas.y + margenBordes, mitadCanvas.y - rt.rect.height - margenBordes);

        rt.anchoredPosition = local;
    }

    // ---------- Animación (solo mueve "contenido", la raíz sigue al jugador) ----------
    private IEnumerator Animar(LetreroItem item)
    {
        RectTransform contenido = item.contenido;
        CanvasGroup cg = item.grupo;
        float entrada = 0.2f;
        float salida = Mathf.Max(0.2f, duracion * 0.3f);
        float espera = Mathf.Max(0f, duracion - entrada - salida);

        // Entrada: "pop" que crece desde la punta de la colita
        for (float t = 0f; t < entrada; t += Time.unscaledDeltaTime)
        {
            if (item == null) yield break;
            float k = t / entrada;
            float escala = k < 0.7f ? Mathf.Lerp(0f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f);
            contenido.localScale = Vector3.one * escala;
            cg.alpha = Mathf.Clamp01(k * 2f);
            yield return null;
        }
        if (item == null) yield break;
        contenido.localScale = Vector3.one;
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(espera);

        // Salida: se desvanece y sube un poco
        for (float t = 0f; t < salida; t += Time.unscaledDeltaTime)
        {
            if (item == null) yield break;
            float k = t / salida;
            contenido.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, distanciaSubida, k));
            cg.alpha = 1f - k;
            yield return null;
        }

        if (item != null)
        {
            if (actual == item) actual = null;
            Destroy(item.gameObject);
        }
    }

    // Reproduce el diálogo por el AudioSource (así obedece el slider de Voces)
    private void Sonar(AudioClip clip)
    {
        if (clip == null) return;

        if (fuenteVoz != null)
        {
            fuenteVoz.PlayOneShot(clip);
        }
        else if (Camera.main != null)
        {
            // Respaldo: suena, pero no pasa por el mixer
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }
}