using System.IO;
using UnityEditor;
using UnityEngine;
public class VFXBorracho : MonoBehaviour
{
    const string Carpeta = "Assets/Entities/VFX";
    const string CarpetaMat = Carpeta + "/Materiales";
    const string CarpetaTex = Carpeta + "/Texturas";

    static readonly Color Blanco = new Color(1f, 1f, 1f);
    static readonly Color Champan = new Color(1f, 0.90f, 0.55f);
    static readonly Color Celeste = new Color(0.65f, 0.90f, 1f);
    static readonly Color Rosa = new Color(1f, 0.75f, 0.90f);

    [MenuItem("Tools/Crear VFX Borracho")]
    public static void Crear()
    {
        AsegurarCarpeta(Carpeta);
        AsegurarCarpeta(CarpetaMat);
        AsegurarCarpeta(CarpetaTex);

        // Burbuja: borde marcado, relleno casi transparente y un brillo arriba a la izquierda
        Texture2D texBurbuja = CrearTextura("TexBorracho_Burbuja", 128, 128, (u, v) =>
        {
            Vector2 p = new Vector2(u, v);
            float d = Vector2.Distance(p, new Vector2(0.5f, 0.5f)) * 2f;
            float borde = Mathf.Exp(-Mathf.Pow((d - 0.88f) / 0.07f, 2f));
            float relleno = 0.12f * Mathf.Clamp01(1f - d);
            float brillo = Mathf.Exp(-Vector2.Distance(p, new Vector2(0.33f, 0.68f)) * 14f) * 0.9f;
            return (borde * 0.9f + relleno + brillo) * Mathf.Clamp01((1f - d) * 12f);
        });

        Material mat = CrearMaterial("MatBorracho_Burbuja", texBurbuja);

        // Raíz: tiene su propio ParticleSystem para poder usar Play(true)/Stop(true) desde código si quieres
        GameObject raiz = new GameObject("VFX_Borracho");
        ParticleSystem psRaiz = raiz.AddComponent<ParticleSystem>();
        psRaiz.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var emRaiz = psRaiz.emission; emRaiz.enabled = false;
        var shRaiz = psRaiz.shape; shRaiz.enabled = false;
        var mRaiz = psRaiz.main; mRaiz.playOnAwake = true; mRaiz.loop = true;
        raiz.GetComponent<ParticleSystemRenderer>().enabled = false;

        // Burbujas grandes
        var grandes = Nuevo("BurbujasGrandes", raiz.transform, mat, new Vector3(0f, 1.6f, 0.2f));
        Config(grandes, 2f, new ParticleSystem.MinMaxCurve(2f, 3.2f), new ParticleSystem.MinMaxCurve(0.3f, 0.7f),
               new ParticleSystem.MinMaxCurve(0.15f, 0.32f), 3f);
        Colores(grandes);
        Cono(grandes, 18f, 0.12f);
        Vaiven(grandes, 0.35f, 0.6f);
        Aparecer(grandes);

        // Burbujitas pequeñas
        var pequenas = Nuevo("BurbujasPequenas", raiz.transform, mat, new Vector3(0f, 1.6f, 0.2f));
        Config(pequenas, 2f, new ParticleSystem.MinMaxCurve(1.2f, 2.2f), new ParticleSystem.MinMaxCurve(0.4f, 1f),
               new ParticleSystem.MinMaxCurve(0.04f, 0.1f), 10f);
        Colores(pequenas);
        Cono(pequenas, 25f, 0.15f);
        Vaiven(pequenas, 0.25f, 0.9f);
        Aparecer(pequenas);

        // Hipo: tanda de burbujas cada 3 segundos
        var hipo = Nuevo("Hipo", raiz.transform, mat, new Vector3(0f, 1.6f, 0.2f));
        Config(hipo, 3f, new ParticleSystem.MinMaxCurve(1.5f, 2.5f), new ParticleSystem.MinMaxCurve(0.8f, 1.8f),
               new ParticleSystem.MinMaxCurve(0.1f, 0.28f), 0f);
        Colores(hipo);
        var eh = hipo.emission;
        eh.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)6, (short)10, 1, 0f) });
        Cono(hipo, 35f, 0.1f);
        Vaiven(hipo, 0.4f, 0.8f);
        Aparecer(hipo);

        string ruta = Carpeta + "/VFX_Borracho.prefab";
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        Debug.Log("VFX_Borracho creado en " + ruta);
    }

    // ---------- Configuración ----------
    static ParticleSystem Nuevo(string nombre, Transform padre, Material mat, Vector3 pos)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // el cono apunta hacia arriba
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    static void Config(ParticleSystem ps, float duracion, ParticleSystem.MinMaxCurve vida,
                       ParticleSystem.MinMaxCurve vel, ParticleSystem.MinMaxCurve tam, float rate)
    {
        var m = ps.main;
        m.playOnAwake = true;
        m.loop = true;
        m.duration = duracion;
        m.startLifetime = vida;
        m.startSpeed = vel;
        m.startSize = tam;
        m.gravityModifier = -0.03f; // flotan hacia arriba
        m.simulationSpace = ParticleSystemSimulationSpace.World; // se quedan atrás cuando el borracho camina

        var e = ps.emission;
        e.rateOverTime = rate;
    }

    static void Colores(ParticleSystem ps)
    {
        var m = ps.main;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Blanco, 0f), new GradientColorKey(Champan, 0.4f),
                    new GradientColorKey(Celeste, 0.7f), new GradientColorKey(Rosa, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        m.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
    }

    static void Cono(ParticleSystem ps, float angulo, float radio)
    {
        var s = ps.shape;
        s.enabled = true;
        s.shapeType = ParticleSystemShapeType.Cone;
        s.angle = angulo;
        s.radius = radio;
    }

    static void Vaiven(ParticleSystem ps, float fuerza, float frecuencia)
    {
        var n = ps.noise;
        n.enabled = true;
        n.strength = fuerza;
        n.frequency = frecuencia;
        n.scrollSpeed = 0.7f;
    }

    // Nacen pequeñas, crecen un poco y se desvanecen rápido al final (como si "reventaran")
    static void Aparecer(ParticleSystem ps)
    {
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.2f, 1f), new Keyframe(0.9f, 1.1f), new Keyframe(1f, 1.4f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.12f),
                    new GradientAlphaKey(0.9f, 0.85f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    // ---------- Materiales y texturas ----------
    static Shader BuscarShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s != null) return s;
        s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (s != null) return s;
        return Shader.Find("Sprites/Default");
    }

    static Material CrearMaterial(string nombre, Texture2D tex)
    {
        string ruta = CarpetaMat + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = m == null;
        if (nuevo) m = new Material(BuscarShader());

        if (m.HasProperty("_Surface"))
        {
            var uno = UnityEngine.Rendering.BlendMode.One;
            var src = UnityEngine.Rendering.BlendMode.SrcAlpha;
            var inv = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f); // Alpha: se ven bien también con fondos claros
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)inv);
            m.SetFloat("_SrcBlendAlpha", (float)uno);
            m.SetFloat("_DstBlendAlpha", (float)inv);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
        }

        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
        }

        if (nuevo) AssetDatabase.CreateAsset(m, ruta);
        else EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D CrearTextura(string nombre, int w, int h, System.Func<float, float, float> alfa)
    {
        string ruta = CarpetaTex + "/" + nombre + ".png";
        Texture2D existente = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (existente != null) return existente;

        Texture2D tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tmp.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alfa((x + 0.5f) / w, (y + 0.5f) / h))));
        tmp.Apply();

        string rutaDisco = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ruta);
        File.WriteAllBytes(rutaDisco, tmp.EncodeToPNG());
        Object.DestroyImmediate(tmp);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace("\\", "/");
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}