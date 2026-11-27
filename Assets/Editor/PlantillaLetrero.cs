using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PlantillaLetrero
{
    const string Carpeta = "Assets/Elements";
    const string CarpetaSprites = "Assets/Elements/png";

    [MenuItem("Tools/Crear Letrero")]
    public static void Crear()
    {
        AsegurarCarpeta(Carpeta);

        Sprite spBurbuja = CrearSprite("Burbuja", 64, 64, true, (x, y) =>
        {
            float r = 22f;
            float px = Mathf.Abs(x + 0.5f - 32f) - (32f - r);
            float py = Mathf.Abs(y + 0.5f - 32f) - (32f - r);
            float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - r;
            return Mathf.Clamp01(0.5f - d);
        });

        // Triángulo apuntando hacia abajo (la punta está en y = 0)
        Sprite spCola = CrearSprite("BurbujaCola", 64, 64, false, (x, y) =>
        {
            float mitad = 32f * ((y + 0.5f) / 64f);
            return Mathf.Clamp01(mitad - Mathf.Abs(x + 0.5f - 32f) + 0.5f);
        });

        GameObject raiz = new GameObject("Letrero_Item", typeof(RectTransform), typeof(LetreroItem));
        var rtR = raiz.GetComponent<RectTransform>();
        rtR.sizeDelta = new Vector2(560f, 150f);
        rtR.pivot = new Vector2(0.5f, 0f);

        // Contenido: ocupa todo y su pivote está abajo (punta de la colita)
        GameObject cont = Hijo("Contenido", raiz.transform);
        cont.AddComponent<CanvasGroup>();
        var rtC = cont.GetComponent<RectTransform>();
        rtC.anchorMin = Vector2.zero; rtC.anchorMax = Vector2.one;
        rtC.offsetMin = rtC.offsetMax = Vector2.zero;
        rtC.pivot = new Vector2(0.5f, 0f);

        Color colorFondo = new Color(0.05f, 0.12f, 0.18f, 0.92f);

        // Cuerpo: deja 30 px abajo para la colita
        GameObject cuerpo = Hijo("Cuerpo", cont.transform);
        var imgCuerpo = cuerpo.AddComponent<Image>();
        imgCuerpo.sprite = spBurbuja;
        imgCuerpo.type = Image.Type.Sliced;
        imgCuerpo.color = colorFondo;
        imgCuerpo.raycastTarget = false;
        var rtB = cuerpo.GetComponent<RectTransform>();
        rtB.anchorMin = Vector2.zero; rtB.anchorMax = Vector2.one;
        rtB.offsetMin = new Vector2(0f, 30f); rtB.offsetMax = Vector2.zero;

        // Colita
        GameObject colaGO = Hijo("Cola", cont.transform);
        var imgCola = colaGO.AddComponent<Image>();
        imgCola.sprite = spCola;
        imgCola.color = colorFondo;
        imgCola.raycastTarget = false;
        var rtCola = colaGO.GetComponent<RectTransform>();
        rtCola.anchorMin = rtCola.anchorMax = new Vector2(0.5f, 0f);
        rtCola.pivot = new Vector2(0.5f, 0f);
        rtCola.anchoredPosition = Vector2.zero;
        rtCola.sizeDelta = new Vector2(40f, 34f);

        // Icono
        GameObject icono = Hijo("Icono", cuerpo.transform);
        var imgI = icono.AddComponent<Image>();
        imgI.preserveAspect = true;
        imgI.raycastTarget = false;
        var rtI = icono.GetComponent<RectTransform>();
        rtI.anchorMin = rtI.anchorMax = new Vector2(0f, 0.5f);
        rtI.pivot = new Vector2(0f, 0.5f);
        rtI.anchoredPosition = new Vector2(20f, 0f);
        rtI.sizeDelta = new Vector2(70f, 70f);

        TMP_Text titulo = Texto("Titulo", cuerpo.transform, "¡Error! Es inocente", 44f,
                                TextAlignmentOptions.Midline, new Vector2(0.14f, 0.05f), new Vector2(0.78f, 0.95f));
        TMP_Text puntos = Texto("Puntos", cuerpo.transform, "-100", 48f,
                                TextAlignmentOptions.MidlineRight, new Vector2(0.78f, 0.05f), new Vector2(0.96f, 0.95f));

        var item = raiz.GetComponent<LetreroItem>();
        item.contenido = rtC;
        item.grupo = cont.GetComponent<CanvasGroup>();
        item.fondo = imgCuerpo;
        item.cola = imgCola;
        item.icono = imgI;
        item.titulo = titulo;
        item.puntos = puntos;

        string ruta = $"{Carpeta}/Letrero_Item.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);

        Letrero manager = Object.FindAnyObjectByType<Letrero>();
        if (manager != null)
        {
            manager.prefabLetrero = prefab.GetComponent<LetreroItem>();
            EditorUtility.SetDirty(manager);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }

    static Sprite CrearSprite(string nombre, int w, int h, bool conBorde, System.Func<int, int, float> alfa)
    {
        string ruta = $"{CarpetaSprites}/{nombre}.png";
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (existente != null) return existente;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alfa(x, y)));
        tex.Apply();

        string disco = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
        File.WriteAllBytes(disco, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        if (conBorde) imp.spriteBorder = new Vector4(24f, 24f, 24f, 24f);
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
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

    static GameObject Hijo(string nombre, Transform padre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go;
    }

    static TMP_Text Texto(string nombre, Transform padre, string texto, float tam,
                          TextAlignmentOptions alineacion, Vector2 min, Vector2 max)
    {
        GameObject go = Hijo(nombre, padre);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.fontStyle = FontStyles.Bold;
        t.alignment = alineacion;
        t.color = Color.white;
        t.enableAutoSizing = true;
        t.fontSizeMax = tam;
        t.fontSizeMin = tam * 0.5f;
        t.raycastTarget = false;
        return t;
    }
}