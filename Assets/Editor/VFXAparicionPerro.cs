using System.IO;
using UnityEditor;
using UnityEngine;
public class VFXAparicionPerro : MonoBehaviour
{
    const string Carpeta = "Assets/Player/VFX";
    const string CarpetaMat = Carpeta + "/Materiales";
    const string CarpetaTex = Carpeta + "/Texturas";

    static readonly Color Durazno   = new Color(1.00f, 0.80f, 0.65f);
    static readonly Color Naranja   = new Color(0.95f, 0.45f, 0.15f); 
    static readonly Color Marron    = new Color(0.25f, 0.10f, 0.02f);
    static readonly Color Negro     = new Color(0.04f, 0.03f, 0.02f); 

    [MenuItem("Tools/Crear VFX Aparicion Perro")]
    public static void Crear()
    {
        AsegurarCarpeta(Carpeta);
        AsegurarCarpeta(CarpetaMat);
        AsegurarCarpeta(CarpetaTex);

        Texture2D texAnillo = CrearTextura("TexApar_Anillo", 128, 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float banda = Mathf.Exp(-Mathf.Pow((d - 0.85f) / 0.08f, 2f));
            return banda * Mathf.Clamp01((1f - d) * 10f);
        });
        Texture2D texGlow = CrearTextura("TexApar_Glow", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
        });
        Texture2D texHumo = CrearTextura("TexApar_Humo", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            return Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d)) * (0.5f + 0.5f * Mathf.PerlinNoise(u * 5f, v * 5f));
        });

        Material matAnillo = CrearMaterial("MatApar_Anillo", texAnillo, true);
        Material matGlow = CrearMaterial("MatApar_Glow", texGlow, true);
        Material matHumo = CrearMaterial("MatApar_Humo", texHumo, false);

        // Raíz: tiene su propio ParticleSystem para poder llamar Play(true) sobre ella
        GameObject raiz = new GameObject("VFX_AparicionPerro");
        ParticleSystem psRaiz = raiz.AddComponent<ParticleSystem>();
        psRaiz.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var emRaiz = psRaiz.emission; emRaiz.enabled = false;
        var shRaiz = psRaiz.shape; shRaiz.enabled = false;
        var mRaiz = psRaiz.main; mRaiz.playOnAwake = false; mRaiz.loop = false; mRaiz.duration = 0.5f;
        raiz.GetComponent<ParticleSystemRenderer>().enabled = false;

        // Anillo en el suelo
        var anillo = Nuevo("Anillo", raiz.transform, matAnillo, new Vector3(0f, 0.05f, 0f));
        Base(anillo, 0.5f, 0.45f, 0f, 5f, Color.white, ParticleSystemSimulationSpace.World);
        Rafaga(anillo, 1);
        Forma(anillo, false);
        Curva(anillo, new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.4f, 0.8f), new Keyframe(1f, 1f)));
        Color_(anillo, Degradado(Durazno, Naranja, 1f, 0f));
        anillo.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        // Destello central
        var flash = Nuevo("Destello", raiz.transform, matGlow, new Vector3(0f, 0.6f, 0f));
        Base(flash, 0.5f, 0.25f, 0f, 3.5f, Color.white, ParticleSystemSimulationSpace.World);
        Rafaga(flash, 1);
        Forma(flash, false);
        Curva(flash, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.5f)));
        Color_(flash, Degradado(Durazno, Naranja, 1f, 0f));

        // Chispas hacia arriba
        var chispas = Nuevo("Chispas", raiz.transform, matGlow, new Vector3(0f, 0.1f, 0f));
        chispas.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        Base(chispas, 0.5f, new ParticleSystem.MinMaxCurve(0.3f, 0.5f), new ParticleSystem.MinMaxCurve(2f, 5f),
             new ParticleSystem.MinMaxCurve(0.08f, 0.18f), Color.white, ParticleSystemSimulationSpace.World);
        var mc = chispas.main;
        mc.startColor = new ParticleSystem.MinMaxGradient(Degradado(Durazno, Naranja, 1f, 1f)) { mode = ParticleSystemGradientMode.RandomColor };
        Rafaga(chispas, 30);
        var shc = chispas.shape; shc.shapeType = ParticleSystemShapeType.Cone; shc.angle = 30f; shc.radius = 0.5f;
        Curva(chispas, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        Color_(chispas, Degradado(Color.white, Color.white, 1f, 0f));

        // Bocanada de humo
        var humo = Nuevo("Humo", raiz.transform, matHumo, new Vector3(0f, 0.3f, 0f));
        Base(humo, 0.5f, new ParticleSystem.MinMaxCurve(0.4f, 0.5f), new ParticleSystem.MinMaxCurve(0.5f, 2f),
             new ParticleSystem.MinMaxCurve(0.8f, 1.4f), Color.white, ParticleSystemSimulationSpace.World);
        var mh = humo.main;
        mh.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mh.startColor = new ParticleSystem.MinMaxGradient(Degradado(Negro, Marron, 0.75f, 0.75f)) { mode = ParticleSystemGradientMode.RandomColor };
        Rafaga(humo, 8);
        var shh = humo.shape; shh.shapeType = ParticleSystemShapeType.Sphere; shh.radius = 0.5f;
        Curva(humo, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.4f)));
        Color_(humo, Degradado(Color.white, Color.white, 0.8f, 0f));

        string ruta = Carpeta + "/VFX_AparicionPerro.prefab";
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        Debug.Log("VFX_AparicionPerro creado en " + ruta);
    }

    // ---------- Helpers de configuración ----------
    static ParticleSystem Nuevo(string nombre, Transform padre, Material mat, Vector3 pos)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    static void Base(ParticleSystem ps, float duracion, ParticleSystem.MinMaxCurve vida, ParticleSystem.MinMaxCurve vel,
                     ParticleSystem.MinMaxCurve tam, Color color, ParticleSystemSimulationSpace espacio)
    {
        var m = ps.main;
        m.playOnAwake = false;
        m.loop = false;
        m.duration = duracion;
        m.startLifetime = vida;
        m.startSpeed = vel;
        m.startSize = tam;
        m.startColor = color;
        m.simulationSpace = espacio;
    }

    static void Rafaga(ParticleSystem ps, short n)
    {
        var e = ps.emission;
        e.rateOverTime = 0f;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, n) });
    }

    static void Forma(ParticleSystem ps, bool activa) { var s = ps.shape; s.enabled = activa; }

    static void Curva(ParticleSystem ps, AnimationCurve c)
    {
        var s = ps.sizeOverLifetime; s.enabled = true; s.size = new ParticleSystem.MinMaxCurve(1f, c);
    }

    static void Color_(ParticleSystem ps, Gradient g)
    {
        var c = ps.colorOverLifetime; c.enabled = true; c.color = g;
    }

    static Gradient Degradado(Color a, Color b, float alphaInicio, float alphaFin)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
            new[] { new GradientAlphaKey(alphaInicio, 0f), new GradientAlphaKey(alphaFin, 1f) });
        return g;
    }

    // ---------- Materiales y texturas ----------
    static Shader BuscarShader(bool aditivo)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s != null) return s;
        s = Shader.Find(aditivo ? "Legacy Shaders/Particles/Additive" : "Legacy Shaders/Particles/Alpha Blended");
        if (s != null) return s;
        return Shader.Find("Sprites/Default");
    }

    static Material CrearMaterial(string nombre, Texture2D tex, bool aditivo)
    {
        string ruta = CarpetaMat + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = m == null;
        if (nuevo) m = new Material(BuscarShader(aditivo));

        if (m.HasProperty("_Surface"))
        {
            var uno = UnityEngine.Rendering.BlendMode.One;
            var src = UnityEngine.Rendering.BlendMode.SrcAlpha;
            var inv = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", aditivo ? 2f : 0f);
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)(aditivo ? uno : inv));
            m.SetFloat("_SrcBlendAlpha", (float)uno);
            m.SetFloat("_DstBlendAlpha", (float)(aditivo ? uno : inv));
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