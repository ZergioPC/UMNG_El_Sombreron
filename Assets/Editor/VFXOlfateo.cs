using System.IO;
using UnityEditor;
using UnityEngine;

public class VFXOlfateo : MonoBehaviour
{
    const string Carpeta = "Assets/Player/VFX";
    const string CarpetaMat = Carpeta + "/Materiales";
    const string CarpetaTex = Carpeta + "/Texturas";

    static readonly Color Durazno   = new Color(1.00f, 0.80f, 0.65f);
    static readonly Color Naranja   = new Color(0.95f, 0.45f, 0.15f); 
    static readonly Color Marron    = new Color(0.25f, 0.10f, 0.02f);
    static readonly Color Dorado = new Color(1f, 0.80f, 0.30f);

    [MenuItem("Tools/Crear VFX Olfateo Perro")]
    public static void Crear()
    {
        AsegurarCarpeta(Carpeta);
        AsegurarCarpeta(CarpetaMat);
        AsegurarCarpeta(CarpetaTex);

        // ---------- Texturas procedurales ----------
        Texture2D texAnillo = CrearTextura("TexOlfAnillo", 128, 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float banda = Mathf.Exp(-Mathf.Pow((d - 0.88f) / 0.07f, 2f));
            float relleno = 0.10f * (1f - d);
            return (banda + Mathf.Max(0f, relleno)) * Mathf.Clamp01((1f - d) * 10f);
        });

        Texture2D texVoluta = CrearTextura("TexOlfVoluta", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float baseSuave = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
            return baseSuave * (0.35f + 0.65f * Mathf.PerlinNoise(u * 4f + 3f, v * 4f + 7f));
        });

        Texture2D texMota = CrearTextura("TexOlfMota", 32, 32, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            return Mathf.Pow(Mathf.Clamp01(1f - d), 3f);
        });

        // ---------- Materiales (todos aditivos: es un efecto luminoso) ----------
        Material matAnillo = CrearMaterial("MatOlf_Anillo", texAnillo);
        Material matVoluta = CrearMaterial("MatOlf_Voluta", texVoluta);
        Material matMota = CrearMaterial("MatOlf_Mota", texMota);

        // ---------- Raíz (PlayerController llama vfxOlfateoPerro.Play(true) sobre ella) ----------
        GameObject raiz = new GameObject("VFX_OlfateoPerro");
        ParticleSystem psRaiz = raiz.AddComponent<ParticleSystem>();
        psRaiz.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var emRaiz = psRaiz.emission; emRaiz.enabled = false;
        var shRaiz = psRaiz.shape; shRaiz.enabled = false;
        var mRaiz = psRaiz.main; mRaiz.playOnAwake = false; mRaiz.loop = false;
        raiz.GetComponent<ParticleSystemRenderer>().enabled = false;

        CrearOndas(raiz.transform, matAnillo);
        CrearDestello(raiz.transform, matMota);
        CrearVolutas(raiz.transform, matVoluta);
        CrearMotas(raiz.transform, matMota);

        string ruta = Carpeta + "/VFX_OlfateoPerro.prefab";
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        Debug.Log("VFX_OlfateoPerro creado en " + ruta);
    }

    // Anillos que se expanden por el suelo: tres "olfateos" seguidos y una pausa
    static void CrearOndas(Transform padre, Material mat)
    {
        ParticleSystem ps = NuevoSistema("OndasOlor", padre, mat, new Vector3(0f, 0.05f, 0f));

        var main = ps.main;
        main.duration = 1.8f;
        main.loop = true;
        main.startLifetime = 1.1f;
        main.startSpeed = 0f;
        main.startSize = 9f;
        main.startColor = new Color(Durazno.r, Durazno.g, Durazno.b, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1, (short)3, 0.4f) });

        var sh = ps.shape; sh.enabled = false;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.03f), new Keyframe(0.3f, 0.6f), new Keyframe(1f, 1f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Degradado(Color.white, Durazno, 1f, 0f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
    }

    // Pulso de luz en cada olfateo
    static void CrearDestello(Transform padre, Material mat)
    {
        ParticleSystem ps = NuevoSistema("PulsoNariz", padre, mat, new Vector3(0f, 0.6f, 0f));

        var main = ps.main;
        main.duration = 1.8f;
        main.loop = true;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 4f;
        main.startColor = new Color(Durazno.r, Durazno.g, Durazno.b, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1, (short)3, 0.4f) });

        var sh = ps.shape; sh.enabled = false;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(1f, 1f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Degradado(Color.white, Color.white, 1f, 0f);
    }

    // Volutas de aroma que suben y serpentean
    static void CrearVolutas(Transform padre, Material mat)
    {
        ParticleSystem ps = NuevoSistema("VolutasAroma", padre, mat, new Vector3(0f, 0.1f, 0f));
        ps.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // el cono apunta hacia arriba

        var main = ps.main;
        main.duration = 1.8f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(Degradado(Durazno, Marron, 0.35f, 0.35f))
        { mode = ParticleSystemGradientMode.RandomColor };

        var em = ps.emission;
        em.rateOverTime = 14f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 10f;
        sh.radius = 1.0f;

        var n = ps.noise;
        n.enabled = true;
        n.strength = 0.7f;
        n.frequency = 0.5f;
        n.scrollSpeed = 0.6f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(1f, 1.5f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    // Motas luminosas (verdes y doradas) flotando hacia arriba
    static void CrearMotas(Transform padre, Material mat)
    {
        ParticleSystem ps = NuevoSistema("MotasLuz", padre, mat, new Vector3(0f, 0.1f, 0f));
        ps.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        var main = ps.main;
        main.duration = 1.8f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(Degradado(Durazno, Dorado, 1f, 1f))
        { mode = ParticleSystemGradientMode.RandomColor };

        var em = ps.emission;
        em.rateOverTime = 25f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 4f;
        sh.radius = 2.5f;
        sh.radiusThickness = 1f; // emite desde todo el disco, no solo el borde

        var n = ps.noise;
        n.enabled = true;
        n.strength = 0.4f;
        n.frequency = 0.8f;
        n.scrollSpeed = 0.8f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
    }

    // ---------- Utilidades ----------

    static ParticleSystem NuevoSistema(string nombre, Transform padre, Material mat, Vector3 posLocal)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        if (mat != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    static Gradient Degradado(Color a, Color b, float alphaInicio, float alphaFin)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
            new[] { new GradientAlphaKey(alphaInicio, 0f), new GradientAlphaKey(alphaFin, 1f) });
        return g;
    }

    static Shader BuscarShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s != null) return s;
        s = Shader.Find("Legacy Shaders/Particles/Additive");
        if (s != null) return s;
        return Shader.Find("Sprites/Default");
    }

    static Material CrearMaterial(string nombre, Texture2D tex)
    {
        string ruta = CarpetaMat + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = m == null;
        if (nuevo) m = new Material(BuscarShader());

        if (m.HasProperty("_Surface")) // URP Particles/Unlit
        {
            var uno = UnityEngine.Rendering.BlendMode.One;
            var src = UnityEngine.Rendering.BlendMode.SrcAlpha;
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f); // Additive
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)uno);
            m.SetFloat("_SrcBlendAlpha", (float)uno);
            m.SetFloat("_DstBlendAlpha", (float)uno);
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
            {
                float a = Mathf.Clamp01(alfa((x + 0.5f) / w, (y + 0.5f) / h));
                tmp.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
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