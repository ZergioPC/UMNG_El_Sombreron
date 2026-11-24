using System.IO;
using UnityEditor;
using UnityEngine;

public class VFXDash : MonoBehaviour
{
    const string Carpeta = "Assets/Player/VFX";
    const string CarpetaMat = Carpeta + "/Materiales";
    const string CarpetaTex = Carpeta + "/Texturas";

    static readonly Color Blanco = new Color(0.85f, 0.95f, 1f);
    static readonly Color Cian = new Color(0.30f, 0.85f, 1f);
    static readonly Color AzulElectrico = new Color(0.10f, 0.35f, 1f);
    static readonly Color AzulOscuro = new Color(0.03f, 0.06f, 0.50f);
    static readonly Color Negro = new Color(0.01f, 0.01f, 0.03f);

    [MenuItem("Tools/Crear VFX Dash Sombrerón")]
    public static void Crear()
    {
        AsegurarCarpeta(Carpeta);
        AsegurarCarpeta(CarpetaMat);
        AsegurarCarpeta(CarpetaTex);

        // ---------- Texturas procedurales ----------
        Texture2D texGlow = CrearTextura("TexGlow", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
        });

        Texture2D texHumo = CrearTextura("TexHumo", 64, 64, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float base_ = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
            return base_ * (0.55f + 0.45f * Mathf.PerlinNoise(u * 5f, v * 5f));
        });

        // Esquirla: rombo alargado y puntiagudo
        Texture2D texEsquirla = CrearTextura("TexEsquirla", 32, 64, (u, v) =>
        {
            float centro = 0.3f;
            float hw = 0.5f * (1f - Mathf.Abs(v - centro) / (v < centro ? centro : 1f - centro));
            return Mathf.Clamp01((hw - Mathf.Abs(u - 0.5f)) * 25f);
        });

        // Línea de trail: brillante al centro, se desvanece hacia los bordes (eje v = ancho)
        Texture2D texLinea = CrearTextura("TexLinea", 4, 32, (u, v) =>
            Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(2f * v - 1f)), 0.7f));

        // ---------- Materiales ----------
        Material matGlow = CrearMaterial("MatV2_Glow", texGlow, true);
        Material matEsqAdd = CrearMaterial("MatV2_EsquirlaAditiva", texEsquirla, true);
        Material matLineaAdd = CrearMaterial("MatV2_LineaAditiva", texLinea, true);
        Material matLineaAlpha = CrearMaterial("MatV2_LineaNegra", texLinea, false);
        Material matEsqAlpha = CrearMaterial("MatV2_EsquirlaNegra", texEsquirla, false);
        Material matHumo = CrearMaterial("MatV2_Humo", texHumo, false);

        // ---------- Raíz (PlayerController llama vfxDash.Play(true) sobre ella) ----------
        GameObject raiz = new GameObject("VFX_DashSombreron");
        ParticleSystem psRaiz = raiz.AddComponent<ParticleSystem>();
        psRaiz.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var emRaiz = psRaiz.emission; emRaiz.enabled = false;
        var shRaiz = psRaiz.shape; shRaiz.enabled = false;
        var mRaiz = psRaiz.main; mRaiz.playOnAwake = false; mRaiz.loop = false;
        raiz.GetComponent<ParticleSystemRenderer>().enabled = false;

        CrearResplandor(raiz.transform, matGlow);
        CrearHumo(raiz.transform, matHumo);

        // Rayos azules: núcleo blanco -> cian -> azul -> transparente
        Gradient gradAzul = Degradado3(Blanco, Cian, AzulElectrico, 1f, 0.9f, 0f);
        CrearRayos(raiz.transform, "RayosNucleo", matLineaAdd, gradAzul,
                   ancho: 0.07f, rateDist: 5f, burst: 6, ruido: 1.6f, frecuencia: 1.6f);
        Gradient gradAzul2 = Degradado3(Cian, AzulElectrico, AzulOscuro, 0.9f, 0.7f, 0f);
        CrearRayos(raiz.transform, "RayosAzules", matLineaAdd, gradAzul2,
                   ancho: 0.14f, rateDist: 4f, burst: 5, ruido: 2.2f, frecuencia: 1.2f);

        // Rayos negros finos (alpha blend, para que el negro SÍ se vea)
        Gradient gradNegro = Degradado3(Negro, Negro, Negro, 1f, 0.9f, 0f);
        CrearRayos(raiz.transform, "RayosNegros", matLineaAlpha, gradNegro,
                   ancho: 0.05f, rateDist: 2.5f, burst: 4, ruido: 2.6f, frecuencia: 1.5f);

        // Esquirlas
        CrearEsquirlas(raiz.transform, "EsquirlasAzules", matEsqAdd,
                       Degradado(Cian, AzulElectrico, 1f, 1f), 14, 6f, 0f);
        CrearEsquirlas(raiz.transform, "EsquirlasNegras", matEsqAlpha,
                       Degradado(Negro, AzulOscuro, 1f, 1f), 12, 5f, 0.4f);

        string ruta = Carpeta + "/VFX_DashSombreron.prefab";
        PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        Debug.Log("VFX_DashSombreron creado en " + ruta);
    }

    // ---------- Sistemas ----------

    // Rayos zigzagueantes: partículas invisibles que solo dibujan su trail, movidas por Noise
    static void CrearRayos(Transform padre, string nombre, Material matTrail, Gradient colorTrail,
                           float ancho, float rateDist, short burst, float ruido, float frecuencia)
    {
        ParticleSystem ps = NuevoSistema(nombre, padre, null, new Vector3(0f, 0.9f, 0f));

        var main = ps.main;
        main.duration = 0.25f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(-7f, -2f); // hacia atrás
        main.startSize = 0.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = rateDist;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(0.5f, 1.7f, 0.1f);

        var n = ps.noise;
        n.enabled = true;
        n.strength = ruido;
        n.frequency = frecuencia;
        n.scrollSpeed = 3f;
        n.damping = false;
        n.octaveCount = 1;

        var t = ps.trails;
        t.enabled = true;
        t.mode = ParticleSystemTrailMode.PerParticle;
        t.ratio = 1f;
        t.lifetime = 1f;
        t.minVertexDistance = 0.12f; // más alto = rayo más angular
        t.worldSpace = true;
        t.dieWithParticles = false;
        t.textureMode = ParticleSystemTrailTextureMode.Stretch;
        t.sizeAffectsWidth = false;
        t.sizeAffectsLifetime = false;
        t.inheritParticleColor = false;
        t.widthOverTrail = new ParticleSystem.MinMaxCurve(ancho, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
        t.colorOverTrail = colorTrail;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.None; // solo se ven los trails
        r.trailMaterial = matTrail;
    }

    // Fragmentos angulares que salen disparados hacia atrás
    static void CrearEsquirlas(Transform padre, string nombre, Material mat, Gradient color,
                               short cantidad, float rateDist, float gravedad)
    {
        ParticleSystem ps = NuevoSistema(nombre, padre, mat, new Vector3(0f, 0.9f, 0f));
        ps.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var main = ps.main;
        main.duration = 0.25f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = gravedad;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(color) { mode = ParticleSystemGradientMode.RandomColor };

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.rateOverDistance = rateDist;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, cantidad) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(0.6f, 1.6f, 0.2f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Degradado(Color.white, Color.white, 1f, 0f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
    }

    // Glow azul pegado al cuerpo (simulación local, sigue al jugador)
    static void CrearResplandor(Transform padre, Material mat)
    {
        ParticleSystem ps = NuevoSistema("Resplandor", padre, mat, new Vector3(0f, 0.9f, 0f));

        var main = ps.main;
        main.duration = 0.25f;
        main.loop = true;
        main.startLifetime = 0.18f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f, 3.2f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(0.15f, 0.5f, 1f, 0.45f);

        var em = ps.emission;
        em.rateOverTime = 25f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.3f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Degradado(Color.white, Color.white, 1f, 0f);
    }

    // Humo oscuro suave
    static void CrearHumo(Transform padre, Material mat)
    {
        ParticleSystem ps = NuevoSistema("HumoNegro", padre, mat, new Vector3(0f, 0.8f, 0f));

        var main = ps.main;
        main.duration = 0.25f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(Degradado(Negro, AzulOscuro, 0.7f, 0.7f))
        { mode = ParticleSystemGradientMode.RandomColor };

        var em = ps.emission;
        em.rateOverTime = 10f;
        em.rateOverDistance = 6f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.4f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = Degradado(Color.white, Color.white, 0.8f, 0f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
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

    static Gradient Degradado3(Color a, Color b, Color c, float alphaA, float alphaB, float alphaC)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.4f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(alphaA, 0f), new GradientAlphaKey(alphaB, 0.5f), new GradientAlphaKey(alphaC, 1f) });
        return g;
    }

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

        if (m.HasProperty("_Surface")) // URP Particles/Unlit
        {
            var src = UnityEngine.Rendering.BlendMode.SrcAlpha;
            var uno = UnityEngine.Rendering.BlendMode.One;
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