using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PlantillaBanner
{
    const string Carpeta = "Assets/Elements";

    [MenuItem("Tools/Crear Banner")]
    public static void Crear()
    {
        AsegurarCarpeta(Carpeta);

        Color colorTexto = new Color(0.05f, 0.20f, 0.27f);

        GameObject raiz = new GameObject("BannerCaptura_Item", typeof(RectTransform), typeof(BannerItem));
        raiz.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 140f);

        GameObject cont = Hijo("Contenido", raiz.transform, typeof(Image), typeof(CanvasGroup));
        var rtC = cont.GetComponent<RectTransform>();
        rtC.anchorMin = Vector2.zero; rtC.anchorMax = Vector2.one;
        rtC.offsetMin = rtC.offsetMax = Vector2.zero;
        var fondo = cont.GetComponent<Image>();
        fondo.color = new Color(0.78f, 0.80f, 0.80f, 0.95f);
        fondo.raycastTarget = false;

        GameObject icono = Hijo("Icono", cont.transform, typeof(Image));
        var rtI = icono.GetComponent<RectTransform>();
        rtI.anchorMin = rtI.anchorMax = new Vector2(0f, 0.5f);
        rtI.pivot = new Vector2(0f, 0.5f);
        rtI.anchoredPosition = new Vector2(24f, 0f);
        rtI.sizeDelta = new Vector2(98f, 98f);
        var imgI = icono.GetComponent<Image>();
        imgI.preserveAspect = true;
        imgI.raycastTarget = false;

        TMP_Text pre = Texto("Pretexto", cont.transform, "Atrapaste un", 34f, FontStyles.Normal, colorTexto,
                             new Vector2(0.28f, 0.52f), new Vector2(0.96f, 0.94f));
        TMP_Text nom = Texto("Nombre", cont.transform, "Borracho", 58f, FontStyles.Bold, colorTexto,
                             new Vector2(0.28f, 0.06f), new Vector2(0.96f, 0.55f));

        var item = raiz.GetComponent<BannerItem>();
        item.contenido = rtC;
        item.grupo = cont.GetComponent<CanvasGroup>();
        item.fondo = fondo;
        item.icono = imgI;
        item.pretexto = pre;
        item.nombre = nom;

        string ruta = $"{Carpeta}/BannerCaptura_Item.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);

        BannerCapturados banner = Object.FindAnyObjectByType<BannerCapturados>();
        if (banner != null)
        {
            banner.prefabBanner = prefab.GetComponent<BannerItem>();
            EditorUtility.SetDirty(banner);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Prefab del banner creado en " + ruta + ". Haz doble clic para editarlo.");
    }

    static void AsegurarCarpeta(string rutaCarpeta)
    {
        if (AssetDatabase.IsValidFolder(rutaCarpeta)) return;

        string[] partes = rutaCarpeta.Split('/');
        string actual = partes[0];

        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = $"{actual}/{partes[i]}";
            if (!AssetDatabase.IsValidFolder(siguiente))
            {
                AssetDatabase.CreateFolder(actual, partes[i]);
            }
            actual = siguiente;
        }
    }

    static GameObject Hijo(string nombre, Transform padre, params System.Type[] comps)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        foreach (var c in comps) go.AddComponent(c);
        return go;
    }

    static TMP_Text Texto(string nombre, Transform padre, string texto, float tam, FontStyles estilo,
                          Color color, Vector2 min, Vector2 max)
    {
        GameObject go = Hijo(nombre, padre);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.enableAutoSizing = true;
        t.fontSizeMax = tam;
        t.fontSizeMin = tam * 0.5f;
        t.raycastTarget = false;
        return t;
    }
}