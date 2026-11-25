using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BannerCapturados : MonoBehaviour
{
    public static BannerCapturados Instancia { get; private set; }

    [System.Serializable]
    public class TipoNotificacion
    {
        public GameObject prefabNPC;   // Prefab del NPC (se compara por su script)
        public string textoMostrar = "Borracho";
        public Sprite icono;
    }

    [Header("Diseño (edítalo en el prefab)")]
    public BannerItem prefabBanner;

    [Header("Tipos de NPC objetivo")]
    public List<TipoNotificacion> tipos = new List<TipoNotificacion>();

    [Header("Posición y comportamiento")]
    public Vector2 margen = new Vector2(30f, -30f);
    public float separacion = 12f;
    public float duracion = 3f;
    public int maximoVisibles = 3;

    private RectTransform canvasRect;
    private RectTransform contenedor;
    private readonly List<GameObject> activos = new List<GameObject>();

    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null || prefabBanner == null)
        {
            Debug.LogError("BannerCapturados: falta el Canvas o el Prefab Banner (Tools > Crear Banner).");
            enabled = false;
            return;
        }
        canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        CrearContenedor();
    }

    // ---------- API pública ----------
    public void Mostrar(GameObject npc)
    {
        if (!enabled || npc == null) return;
        TipoNotificacion datos = Buscar(npc);
        if (datos == null) return;
        MostrarBanner(datos.textoMostrar, datos.icono);
    }

    [ContextMenu("Probar banner (en Play)")]
    private void Probar()
    {
        if (tipos.Count > 0) MostrarBanner(tipos[0].textoMostrar, tipos[0].icono);
    }

    // ---------- Banner ----------
    private void CrearContenedor()
    {
        Vector2 tam = prefabBanner.GetComponent<RectTransform>().sizeDelta;

        GameObject go = new GameObject("ContenedorBanners", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contenedor = go.GetComponent<RectTransform>();
        contenedor.SetParent(canvasRect, false);
        contenedor.anchorMin = contenedor.anchorMax = contenedor.pivot = new Vector2(0f, 1f);
        contenedor.anchoredPosition = margen;
        contenedor.sizeDelta = new Vector2(tam.x, tam.y * maximoVisibles);

        var v = go.GetComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.UpperLeft;
        v.spacing = separacion;
        v.childControlWidth = false;
        v.childControlHeight = false;
        v.childForceExpandWidth = false;
        v.childForceExpandHeight = false;
    }

    private void MostrarBanner(string nombre, Sprite icono)
    {
        while (activos.Count >= maximoVisibles)
        {
            GameObject viejo = activos[0];
            activos.RemoveAt(0);
            if (viejo != null) Destroy(viejo);
        }

        BannerItem item = Instantiate(prefabBanner, contenedor);
        item.name = "Banner";
        item.Configurar(nombre, icono);
        activos.Add(item.gameObject);
        StartCoroutine(Animar(item));
    }

    private IEnumerator Animar(BannerItem item)
    {
        if (item == null) yield break;

        RectTransform contenido = item.contenido;
        CanvasGroup cg = item.grupo;
        float entrada = 0.35f, salida = 0.4f;
        float desplazamiento = item.GetComponent<RectTransform>().sizeDelta.x + 80f;

        for (float t = 0f; t < entrada; t += Time.unscaledDeltaTime)
        {
            if (item == null) yield break;
            float k = t / entrada;
            float e = 1f - Mathf.Pow(1f - k, 3f);
            if (contenido != null) contenido.anchoredPosition = new Vector2(Mathf.Lerp(-desplazamiento, 0f, e), 0f);
            if (cg != null) cg.alpha = k;
            yield return null;
        }
        if (item == null) yield break;
        if (contenido != null) contenido.anchoredPosition = Vector2.zero;
        if (cg != null) cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(duracion);

        for (float t = 0f; t < salida; t += Time.unscaledDeltaTime)
        {
            if (item == null) yield break;
            float k = t / salida;
            if (contenido != null) contenido.anchoredPosition = new Vector2(Mathf.Lerp(0f, -60f, k), 0f);
            if (cg != null) cg.alpha = 1f - k;
            yield return null;
        }

        if (item != null)
        {
            activos.Remove(item.gameObject);
            Destroy(item.gameObject);
        }
    }

    // ---------- Detección del tipo ----------
    private TipoNotificacion Buscar(GameObject npc)
    {
        foreach (TipoNotificacion tipo in tipos)
        {
            if (tipo.prefabNPC == null) continue;
            if (MismoTipo(npc, tipo.prefabNPC)) return tipo;
        }
        return null;
    }

    private static bool MismoTipo(GameObject npc, GameObject prefab)
    {
        if (npc.GetComponentInParent<BorrachoNPC>() != null && prefab.GetComponentInChildren<BorrachoNPC>() != null) return true;
        if (npc.GetComponentInParent<PeleadorNPC>() != null && prefab.GetComponentInChildren<PeleadorNPC>() != null) return true;
        if (npc.GetComponentInParent<TramposoNPC>() != null && prefab.GetComponentInChildren<TramposoNPC>() != null) return true;
        return false;
    }
}