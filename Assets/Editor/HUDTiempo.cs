using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HUDTiempo
{
    [MenuItem("Tools/Crear HUD Tiempo")]
    public static void Crear()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = c.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = c.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Panel (arriba al centro, como en el boceto)
        GameObject panel = Nuevo("HUDTiempo", canvas.transform);
        var rtPanel = panel.GetComponent<RectTransform>();
        rtPanel.anchorMin = rtPanel.anchorMax = new Vector2(0.5f, 1f);
        rtPanel.pivot = new Vector2(0.5f, 1f);
        rtPanel.anchoredPosition = new Vector2(0f, -30f);
        rtPanel.sizeDelta = new Vector2(560f, 140f);
        var fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.05f, 0.12f, 0.18f, 0.75f);

        // Vela (a la izquierda). Asigna tu sprite de la vela en el Inspector.
        GameObject vela = Nuevo("IconoVela", panel.transform);
        var rtVela = vela.GetComponent<RectTransform>();
        rtVela.anchorMin = rtVela.anchorMax = new Vector2(0f, 0.5f);
        rtVela.pivot = new Vector2(0.5f, 0.5f);
        rtVela.anchoredPosition = new Vector2(70f, 0f);
        rtVela.sizeDelta = new Vector2(90f, 110f);
        var imgVela = vela.AddComponent<Image>();
        imgVela.color = new Color(1f, 0.92f, 0.7f);
        imgVela.type = Image.Type.Filled;
        imgVela.fillMethod = Image.FillMethod.Vertical;
        imgVela.fillOrigin = (int)Image.OriginVertical.Bottom; // la vela se consume desde arriba

        // Título
        TextMeshProUGUI titulo = NuevoTexto("Titulo", panel.transform, "Tiempo hasta el amanecer", 30f,
            new Vector2(0.28f, 0.52f), new Vector2(0.97f, 0.95f));

        // Hora
        TextMeshProUGUI hora = NuevoTexto("Hora", panel.transform, "12:00 AM", 54f,
            new Vector2(0.28f, 0.05f), new Vector2(0.97f, 0.55f));
        hora.fontStyle = FontStyles.Bold;

        // Gestor
        GestorTiempo gestor = Object.FindAnyObjectByType<GestorTiempo>();
        if (gestor == null)
        {
            GameObject go = new GameObject("GestorTiempo");
            gestor = go.AddComponent<GestorTiempo>();
            Undo.RegisterCreatedObjectUndo(go, "Crear GestorTiempo");
        }
        gestor.textoTitulo = titulo;
        gestor.textoHora = hora;
        gestor.iconoVela = imgVela;

        Light luz = Object.FindAnyObjectByType<Light>();
        if (luz != null && luz.type == LightType.Directional) gestor.luzDireccional = luz;

        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.25f, 0.35f, 0.8f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.45f, 0.7f), 0.7f),
                    new GradientColorKey(new Color(1f, 0.75f, 0.5f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        gestor.colorLuz = g;

        Undo.RegisterCreatedObjectUndo(panel, "Crear HUD Tiempo");
        Selection.activeObject = panel;
        EditorUtility.SetDirty(gestor);
        Debug.Log("HUD de tiempo creado. Asigna tus sprites al panel y a la vela.");
    }

    static GameObject Nuevo(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go;
    }

    static TextMeshProUGUI NuevoTexto(string nombre, Transform padre, string texto, float tam, Vector2 min, Vector2 max)
    {
        GameObject go = Nuevo(nombre, padre);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(0.9f, 0.95f, 1f);
        return t;
    }
}