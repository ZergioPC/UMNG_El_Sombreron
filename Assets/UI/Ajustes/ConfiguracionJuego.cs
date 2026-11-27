using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum CanalAudio { Master, Musica, Sfx, Voces }

public static class ConfiguracionJuego
{
    // Valores actuales
    public static float VolMaster { get; private set; } = 1f;
    public static float VolMusica { get; private set; } = 0.8f;
    public static float VolSfx { get; private set; } = 1f;
    public static float VolVoces { get; private set; } = 1f;
    public static bool Silenciado { get; private set; } = false;

    public static int Ancho { get; private set; }
    public static int Alto { get; private set; }
    public static bool ResolucionAuto { get; private set; } = true; // True = usa la resolución del monitor
    public static int ModoPantalla { get; private set; } = 2; // 0 = completa, 1 = ventana, 2 = sin bordes
    public static int Calidad { get; private set; } = 2; // 0 = baja, 1 = media, 2 = alta
    public static float Brillo { get; private set; } = 0.5f; // 0 a 1 (0.5 = normal)
    public static int Daltonismo { get; private set; } = 0; // 0 = ninguno, 1 = protanopia, 2 = deuteranopia, 3 = tritanopia

    // Recursos (se cargan desde Assets/Resources)
    private static AudioMixer mixer;
    private static Material matDaltonismo;
    private static ColorAdjustments ajusteColor;

    // Valores originales del URP Asset (se restauran al cerrar, para no ensuciar el proyecto en el editor)
    private static float urpEscalaOriginal, urpSombrasOriginal;
    private static int urpMsaaOriginal;
    private static bool urpGuardado = false;

    // Se ejecuta solo al iniciar el juego, en cualquier escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Iniciar()
    {
        mixer = Resources.Load<AudioMixer>("MezclaAudio");
        matDaltonismo = Resources.Load<Material>("MatDaltonismo");
        CrearVolumenBrillo();
        Cargar();

        AplicarAudio();
        AplicarCalidad();
        AplicarBrillo();
        AplicarDaltonismo();

        // Solo se fuerza la pantalla si el jugador ya la había elegido antes
        if (PlayerPrefs.HasKey("ancho")) AplicarPantalla();

        Application.quitting += RestaurarUrp;
    }

    private static void Cargar()
    {
        VolMaster = PlayerPrefs.GetFloat("volMaster", 1f);
        VolMusica = PlayerPrefs.GetFloat("volMusica", 0.8f);
        VolSfx = PlayerPrefs.GetFloat("volSfx", 1f);
        VolVoces = PlayerPrefs.GetFloat("volVoces", 1f);
        Silenciado = PlayerPrefs.GetInt("silenciado", 0) == 1;

        ModoPantalla = PlayerPrefs.GetInt("modoPantalla", ModoActual());
        ResolucionAuto = PlayerPrefs.GetInt("resAuto", 1) == 1;
        Ancho = PlayerPrefs.GetInt("ancho", Screen.currentResolution.width);
        Alto = PlayerPrefs.GetInt("alto", Screen.currentResolution.height);

        // En automático se recalcula siempre, por si el jugador cambió de monitor
        if (ResolucionAuto)
        {
            Vector2Int r = ResolucionAutomatica(ModoPantalla);
            Ancho = r.x;
            Alto = r.y;
        }

        Calidad = PlayerPrefs.GetInt("calidad", 2);
        Brillo = PlayerPrefs.GetFloat("brillo", 0.5f);
        Daltonismo = PlayerPrefs.GetInt("daltonismo", 0);
    }

    private static int ModoActual()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.Windowed: return 1;
            default: return 2;
        }
    }

    // ---------- Audio ----------
    public static void CambiarVolumen(CanalAudio canal, float valor)
    {
        valor = Mathf.Clamp01(valor);
        if (canal == CanalAudio.Master) VolMaster = valor;
        else if (canal == CanalAudio.Musica) VolMusica = valor;
        else if (canal == CanalAudio.Sfx) VolSfx = valor;
        else VolVoces = valor;

        AplicarAudio();
        GuardarValores();
    }

    public static float ObtenerVolumen(CanalAudio canal)
    {
        if (canal == CanalAudio.Master) return VolMaster;
        if (canal == CanalAudio.Musica) return VolMusica;
        if (canal == CanalAudio.Sfx) return VolSfx;
        return VolVoces;
    }

    public static void CambiarSilencio(bool silenciar)
    {
        Silenciado = silenciar;
        AplicarAudio();
        GuardarValores();
    }

    private static void AplicarAudio()
    {
        float master = Silenciado ? 0f : VolMaster;

        if (mixer != null)
        {
            PonerDb("VolMaster", master);
            PonerDb("VolMusica", VolMusica);
            PonerDb("VolSfx", VolSfx);
            PonerDb("VolVoces", VolVoces);
        }
        else
        {
            // Sin mixer solo funciona el volumen general
            AudioListener.volume = master;
        }
    }

    private static void PonerDb(string parametro, float lineal)
    {
        float db = lineal <= 0.0001f ? -80f : Mathf.Log10(lineal) * 20f;
        mixer.SetFloat(parametro, db);
    }

    // ---------- Pantalla ----------
    // Resolución "automática": la del monitor (en ventana, el 80% para que quepa en pantalla)
    public static Vector2Int ResolucionAutomatica(int modo)
    {
        int w = Display.main.systemWidth;
        int h = Display.main.systemHeight;
        if (w <= 0 || h <= 0)
        {
            w = Screen.currentResolution.width;
            h = Screen.currentResolution.height;
        }

        if (modo == 1)
        {
            w = Mathf.RoundToInt(w * 0.8f) / 2 * 2;
            h = Mathf.RoundToInt(h * 0.8f) / 2 * 2;
        }

        return new Vector2Int(w, h);
    }

    public static void CambiarPantalla(int ancho, int alto, int modo, bool auto)
    {
        ResolucionAuto = auto;
        ModoPantalla = modo;

        if (auto)
        {
            Vector2Int r = ResolucionAutomatica(modo);
            ancho = r.x;
            alto = r.y;
        }

        Ancho = ancho;
        Alto = alto;
        AplicarPantalla();
        GuardarValores();
    }

    private static void AplicarPantalla()
    {
        FullScreenMode modo = ModoPantalla == 0 ? FullScreenMode.ExclusiveFullScreen
                            : ModoPantalla == 1 ? FullScreenMode.Windowed
                            : FullScreenMode.FullScreenWindow;
        Screen.SetResolution(Ancho, Alto, modo);
    }

    public static void CambiarCalidad(int nivel)
    {
        Calidad = Mathf.Clamp(nivel, 0, 2);
        AplicarCalidad();
        GuardarValores();
    }

    private static void AplicarCalidad()
    {
        // Baja = primer nivel, Media = el del medio, Alta = el último
        int total = QualitySettings.names.Length;
        int indice = Calidad == 0 ? 0 : (Calidad == 1 ? total / 2 : total - 1);
        QualitySettings.SetQualityLevel(indice, true);

        // Valores explícitos, para que la diferencia se note aunque los niveles del proyecto sean iguales
        float escala = Calidad == 0 ? 0.6f : (Calidad == 1 ? 0.8f : 1f); // Resolución interna de render
        float distanciaSombras = Calidad == 0 ? 15f : (Calidad == 1 ? 40f : 100f);
        int msaa = Calidad == 0 ? 1 : (Calidad == 1 ? 2 : 4); // Suavizado de bordes
        int texturas = Calidad == 0 ? 2 : (Calidad == 1 ? 1 : 0); // 0 = resolución completa, 2 = cuarta parte
        float lod = Calidad == 0 ? 0.5f : (Calidad == 1 ? 1f : 2f);

        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            if (!urpGuardado)
            {
                urpEscalaOriginal = urp.renderScale;
                urpSombrasOriginal = urp.shadowDistance;
                urpMsaaOriginal = urp.msaaSampleCount;
                urpGuardado = true;
            }

            urp.renderScale = escala;
            urp.shadowDistance = distanciaSombras;
            urp.msaaSampleCount = msaa;
        }

        QualitySettings.globalTextureMipmapLimit = texturas;
        QualitySettings.lodBias = lod;
    }

    private static void RestaurarUrp()
    {
        if (!urpGuardado) return;

        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = urpEscalaOriginal;
            urp.shadowDistance = urpSombrasOriginal;
            urp.msaaSampleCount = urpMsaaOriginal;
        }
    }

    // ---------- Brillo (un Volume global creado por código) ----------
    private static void CrearVolumenBrillo()
    {
        GameObject go = new GameObject("VolumenBrillo");
        Object.DontDestroyOnLoad(go);

        Volume volumen = go.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 100f; // Por encima de tu Global Volume
        volumen.weight = 1f;

        VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        ajusteColor = perfil.Add<ColorAdjustments>(true);
        ajusteColor.postExposure.overrideState = true;
        volumen.profile = perfil;
    }

    public static void CambiarBrillo(float valor)
    {
        Brillo = Mathf.Clamp01(valor);
        AplicarBrillo();
        GuardarValores();
    }

    private static void AplicarBrillo()
    {
        if (ajusteColor != null) ajusteColor.postExposure.value = (Brillo - 0.5f) * 3f;
    }

    // ---------- Accesibilidad ----------
    public static void CambiarDaltonismo(int modo)
    {
        Daltonismo = Mathf.Clamp(modo, 0, 3);
        AplicarDaltonismo();
        GuardarValores();
    }

    private static void AplicarDaltonismo()
    {
        if (matDaltonismo != null) matDaltonismo.SetFloat("_Modo", Daltonismo);
    }

    // ---------- Predeterminados y guardado ----------
    public static void RestaurarPredeterminados()
    {
        VolMaster = 1f;
        VolMusica = 0.8f;
        VolSfx = 1f;
        VolVoces = 1f;
        Silenciado = false;

        ResolucionAuto = true; // Resolución del monitor
        ModoPantalla = 2; // Sin bordes: el más seguro
        Vector2Int r = ResolucionAutomatica(ModoPantalla);
        Ancho = r.x;
        Alto = r.y;

        Calidad = 2;
        Brillo = 0.5f;
        Daltonismo = 0;

        AplicarAudio();
        AplicarPantalla();
        AplicarCalidad();
        AplicarBrillo();
        AplicarDaltonismo();
        GuardarValores();
        Guardar();
    }

    private static void GuardarValores()
    {
        PlayerPrefs.SetFloat("volMaster", VolMaster);
        PlayerPrefs.SetFloat("volMusica", VolMusica);
        PlayerPrefs.SetFloat("volSfx", VolSfx);
        PlayerPrefs.SetFloat("volVoces", VolVoces);
        PlayerPrefs.SetInt("silenciado", Silenciado ? 1 : 0);
        PlayerPrefs.SetInt("ancho", Ancho);
        PlayerPrefs.SetInt("alto", Alto);
        PlayerPrefs.SetInt("resAuto", ResolucionAuto ? 1 : 0);
        PlayerPrefs.SetInt("modoPantalla", ModoPantalla);
        PlayerPrefs.SetInt("calidad", Calidad);
        PlayerPrefs.SetFloat("brillo", Brillo);
        PlayerPrefs.SetInt("daltonismo", Daltonismo);
    }

    // Escribe en disco (se llama al cerrar el panel)
    public static void Guardar()
    {
        PlayerPrefs.Save();
    }
}