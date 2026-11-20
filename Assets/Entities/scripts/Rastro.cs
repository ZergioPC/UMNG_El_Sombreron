using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ParticleSystem))]
public class Rastro : MonoBehaviour
{
//Hilo de seda
    public float grosorHilo = 0.025f; // muy delgado
    public float velocidadEstela = 2.2f; // avance hacia adelante
    public float tiempoVidaParticula = 3.5f; // largo del hilo = velocidad * vida
    public int tasaEmisionBase = 4; // pocas hebras, delicadas

//Serpenteo
    public float amplitudOnda = 1f; //cuánto se abre cada curva
    public float ciclosOnda = 2f; // número de "S" a lo largo del hilo
    [Range(0f, 1f)]
    public float variacionHebras = 0.05f; // diferencia de velocidad entre hebras
    public float fuerzaRuido = 0.12f; // ondulación orgánica extra

 //Orientación
    public bool orientarHaciaPerro = true;

  //Color
    public Color colorOrigenIntenso = new Color(1f, 0.25f, 0.05f, 1f);
    public Color colorColaClara = new Color(1f, 0.85f, 0.72f, 0f);
    [Range(1f, 8f)]
    public float multiplicadorBrilloHDR = 3f;

//Intensidad según cercanía del perro
    public float distanciaMaximaEfecto = 30f;
    public float distanciaMinimaEfecto = 2f;
    public float multiplicadorCercaniaMax = 1.8f;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;
    private Transform perroTransform;
    private AnimationCurve curvaAncho;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();
        ConfigurarSistemaParticulas();
    }

    void Start()
    {
        PerroController perro = FindAnyObjectByType<PerroController>();
        if (perro != null) perroTransform = perro.transform;
    }

    void Update()
    {
        if (ps == null) return;

        float factorCercania = 0f;
        if (perroTransform != null && perroTransform.gameObject.activeInHierarchy)
        {
            float distancia = Vector3.Distance(transform.position, perroTransform.position);
            factorCercania = Mathf.Clamp01(1f - Mathf.InverseLerp(distanciaMinimaEfecto, distanciaMaximaEfecto, distancia));

            if (orientarHaciaPerro)
            {
                Vector3 dir = Vector3.ProjectOnPlane(perroTransform.position - transform.position, Vector3.up);
                if (dir.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        var trails = ps.trails;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
            grosorHilo * Mathf.Lerp(0.8f, multiplicadorCercaniaMax, factorCercania), curvaAncho);

        var emission = ps.emission;
        emission.rateOverTime = tasaEmisionBase * Mathf.Lerp(0.8f, multiplicadorCercaniaMax, factorCercania);
    }

    void ConfigurarSistemaParticulas()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ---- Main ----
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(tiempoVidaParticula * 0.85f, tiempoVidaParticula * 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            velocidadEstela * (1f - variacionHebras * 0.5f),
            velocidadEstela * (1f + variacionHebras * 0.5f));
        main.startSize = 0.05f;
        main.gravityModifier = 0f;
        main.maxParticles = 60;
        main.playOnAwake = true;
        main.startColor = new Color(colorOrigenIntenso.r, colorOrigenIntenso.g, colorOrigenIntenso.b, 1f);

        //Emisión
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = tasaEmisionBase;

        //Shape: emite hacia adelante (Z local) desde un punto ----
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = 0.01f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        //Serpenteo
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;

        AnimationCurve onda = new AnimationCurve();
        int pasos = Mathf.Max(8, Mathf.CeilToInt(ciclosOnda * 12f));
        for (int i = 0; i <= pasos; i++)
        {
            float t = i / (float)pasos;
            onda.AddKey(new Keyframe(t, Mathf.Cos(2f * Mathf.PI * ciclosOnda * t)));
        }
        for (int i = 0; i < onda.length; i++) onda.SmoothTangents(i, 0f);

        AnimationCurve cero = AnimationCurve.Constant(0f, 1f, 0f);
        float velLateral = amplitudOnda * 2f * Mathf.PI * ciclosOnda / tiempoVidaParticula;
        vel.x = new ParticleSystem.MinMaxCurve(velLateral, onda);
        vel.y = new ParticleSystem.MinMaxCurve(1f, cero);   // los 3 ejes deben usar el mismo modo
        vel.z = new ParticleSystem.MinMaxCurve(1f, cero);

        //Ruido suave para que cada hebra se mueva como tela
        var noise = ps.noise;
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = fuerzaRuido;
        noise.strengthY = fuerzaRuido * 0.5f;
        noise.strengthZ = fuerzaRuido * 0.5f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        //Trails
        curvaAncho = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 0.45f),
            new Keyframe(1f, 0f));
        for (int i = 0; i < curvaAncho.length; i++) curvaAncho.SmoothTangents(i, 0f);

        var trails = ps.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(1f);   // la cinta recuerda todo el recorrido
        trails.minVertexDistance = 0.05f;
        trails.worldSpace = true;
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.inheritParticleColor = true;
        trails.sizeAffectsWidth = false;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(grosorHilo, curvaAncho);

        Gradient gradCinta = new Gradient();
        gradCinta.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorOrigenIntenso, 0f),
                new GradientColorKey(colorColaClara, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),      // la punta nace suave
                new GradientAlphaKey(0.9f, 0.08f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f, 1f)       // la cola se desvanece
            });
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(gradCinta);

        if (psRenderer != null)
        {
            psRenderer.renderMode = ParticleSystemRenderMode.None;   // solo se ve la cinta
            psRenderer.shadowCastingMode = ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
            psRenderer.trailMaterial = CrearMaterialBrillante();
        }

        ps.Play();
    }

    //Material aditivo con brillo HDR

    Material CrearMaterialBrillante()
    {
        Color colorHDR = new Color(
            colorOrigenIntenso.r * multiplicadorBrilloHDR,
            colorOrigenIntenso.g * multiplicadorBrilloHDR,
            colorOrigenIntenso.b * multiplicadorBrilloHDR,
            1f);

        Texture2D tex = CrearTexturaHilo();

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh != null)
        {
            Material m = new Material(sh);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", colorHDR);
            return m;
        }

        sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh != null)
        {
            Material m = new Material(sh);
            m.SetTexture("_MainTex", tex);
            m.SetColor("_TintColor", colorHDR * 0.5f);
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        Material fallback = new Material(Shader.Find("Sprites/Default"));
        fallback.mainTexture = tex;
        fallback.color = colorHDR;
        return fallback;
    }

    // Degradado solo a lo ancho: centro brillante y bordes transparentes (aspecto de seda, no sólido)
    Texture2D CrearTexturaHilo()
    {
        const int w = 4, h = 32;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < h; y++)
        {
            float v = Mathf.Abs((y / (float)(h - 1)) * 2f - 1f);   // 0 centro, 1 borde
            float a = Mathf.Clamp01(1f - v);
            a = a * a * (3f - 2f * a);
            a *= a;                                                // núcleo más fino
            for (int x = 0; x < w; x++)
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        t.Apply();
        return t;
    }
}