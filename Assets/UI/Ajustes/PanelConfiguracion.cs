using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class FilaVolumen
{
    public Slider slider; // De 0 a 1
    public TMP_Text texto; // Opcional: porcentaje
}

public class PanelConfiguracion : MonoBehaviour
{
    // Audio
    public FilaVolumen master;
    public FilaVolumen musica;
    public FilaVolumen efectos; // SFX
    public FilaVolumen voces;
    public Toggle toggleSilenciar;

    // Pantalla
    public TMP_Dropdown dropdownResolucion; // La primera opción es "Automático"
    public TMP_Dropdown dropdownModo; // Pantalla completa, ventana, sin bordes
    public Button botonAplicarPantalla;
    public TMP_Dropdown dropdownCalidad; // Baja, media, alta
    public Slider sliderBrillo; // De 0 a 1

    // Accesibilidad
    public TMP_Dropdown dropdownDaltonismo;

    // Aviso para confirmar el cambio de pantalla
    public GameObject panelConfirmar; // Puede estar desactivado en la escena
    public TMP_Text textoCuenta; // Por ejemplo "Se revertirá en 15"
    public float segundosConfirmar = 15f;

    private readonly List<Vector2Int> resoluciones = new List<Vector2Int>(); // Sin contar "Automático"
    private bool confirmando = false;
    private float tiempoRestante;
    private int anteriorAncho, anteriorAlto, anteriorModo;
    private bool anteriorAuto;

    void Start()
    {
        ConectarVolumen(master, CanalAudio.Master);
        ConectarVolumen(musica, CanalAudio.Musica);
        ConectarVolumen(efectos, CanalAudio.Sfx);
        ConectarVolumen(voces, CanalAudio.Voces);

        if (toggleSilenciar != null) toggleSilenciar.onValueChanged.AddListener(ConfiguracionJuego.CambiarSilencio);
        if (sliderBrillo != null) sliderBrillo.onValueChanged.AddListener(ConfiguracionJuego.CambiarBrillo);
        if (dropdownCalidad != null) dropdownCalidad.onValueChanged.AddListener(ConfiguracionJuego.CambiarCalidad);
        if (dropdownDaltonismo != null) dropdownDaltonismo.onValueChanged.AddListener(ConfiguracionJuego.CambiarDaltonismo);
        if (botonAplicarPantalla != null) botonAplicarPantalla.onClick.AddListener(AplicarPantalla);

        PrepararOpciones();
        RefrescarControles();

        if (panelConfirmar != null) panelConfirmar.SetActive(false);
    }

    // ---------- Preparación ----------
    void ConectarVolumen(FilaVolumen fila, CanalAudio canal)
    {
        if (fila == null || fila.slider == null) return;
        fila.slider.onValueChanged.AddListener(valor =>
        {
            ConfiguracionJuego.CambiarVolumen(canal, valor);
            ActualizarTexto(fila, valor);
        });
    }

    void PrepararOpciones()
    {
        // Resoluciones del monitor, sin repetir
        resoluciones.Clear();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int v = new Vector2Int(r.width, r.height);
            if (!resoluciones.Contains(v)) resoluciones.Add(v);
        }
        Vector2Int actual = new Vector2Int(ConfiguracionJuego.Ancho, ConfiguracionJuego.Alto);
        if (!resoluciones.Contains(actual)) resoluciones.Add(actual);

        if (dropdownResolucion != null)
        {
            List<string> nombres = new List<string>();
            nombres.Add("Automático"); // Índice 0
            foreach (Vector2Int v in resoluciones) nombres.Add(v.x + " x " + v.y);
            dropdownResolucion.ClearOptions();
            dropdownResolucion.AddOptions(nombres);
        }

        if (dropdownModo != null)
        {
            dropdownModo.ClearOptions();
            dropdownModo.AddOptions(new List<string> { "Pantalla completa", "Ventana", "Sin bordes" });
        }

        if (dropdownCalidad != null)
        {
            dropdownCalidad.ClearOptions();
            dropdownCalidad.AddOptions(new List<string> { "Baja", "Media", "Alta" });
        }

        if (dropdownDaltonismo != null)
        {
            dropdownDaltonismo.ClearOptions();
            dropdownDaltonismo.AddOptions(new List<string> { "Ninguno", "Protanopia", "Deuteranopia", "Tritanopia" });
        }
    }

    // Muestra en los controles los valores guardados, sin disparar los eventos
    void RefrescarControles()
    {
        RefrescarVolumen(master, CanalAudio.Master);
        RefrescarVolumen(musica, CanalAudio.Musica);
        RefrescarVolumen(efectos, CanalAudio.Sfx);
        RefrescarVolumen(voces, CanalAudio.Voces);

        if (toggleSilenciar != null) toggleSilenciar.SetIsOnWithoutNotify(ConfiguracionJuego.Silenciado);
        if (sliderBrillo != null) sliderBrillo.SetValueWithoutNotify(ConfiguracionJuego.Brillo);
        if (dropdownCalidad != null) dropdownCalidad.SetValueWithoutNotify(ConfiguracionJuego.Calidad);
        if (dropdownDaltonismo != null) dropdownDaltonismo.SetValueWithoutNotify(ConfiguracionJuego.Daltonismo);
        if (dropdownModo != null) dropdownModo.SetValueWithoutNotify(ConfiguracionJuego.ModoPantalla);

        if (dropdownResolucion != null)
        {
            int indice = 0; // 0 = Automático
            if (!ConfiguracionJuego.ResolucionAuto)
            {
                int pos = resoluciones.IndexOf(new Vector2Int(ConfiguracionJuego.Ancho, ConfiguracionJuego.Alto));
                indice = pos >= 0 ? pos + 1 : 0;
            }
            dropdownResolucion.SetValueWithoutNotify(indice);
        }
    }

    void RefrescarVolumen(FilaVolumen fila, CanalAudio canal)
    {
        if (fila == null || fila.slider == null) return;
        float valor = ConfiguracionJuego.ObtenerVolumen(canal);
        fila.slider.SetValueWithoutNotify(valor);
        ActualizarTexto(fila, valor);
    }

    void ActualizarTexto(FilaVolumen fila, float valor)
    {
        if (fila.texto != null) fila.texto.text = Mathf.RoundToInt(valor * 100f) + "%";
    }

    // ---------- Pantalla con cuenta regresiva ----------
    // Se conecta al botón "Aplicar" (o se asigna en el campo Boton Aplicar Pantalla)
    public void AplicarPantalla()
    {
        if (confirmando || dropdownResolucion == null || dropdownModo == null) return;

        int nuevoModo = dropdownModo.value;
        bool nuevaAuto = dropdownResolucion.value == 0;
        Vector2Int nueva = nuevaAuto
            ? ConfiguracionJuego.ResolucionAutomatica(nuevoModo)
            : resoluciones[dropdownResolucion.value - 1];

        // Si no hay cambios, no hace falta confirmar
        if (nuevaAuto == ConfiguracionJuego.ResolucionAuto && nueva.x == ConfiguracionJuego.Ancho
            && nueva.y == ConfiguracionJuego.Alto && nuevoModo == ConfiguracionJuego.ModoPantalla) return;

        // Se recuerda la configuración anterior por si hay que volver
        anteriorAncho = ConfiguracionJuego.Ancho;
        anteriorAlto = ConfiguracionJuego.Alto;
        anteriorModo = ConfiguracionJuego.ModoPantalla;
        anteriorAuto = ConfiguracionJuego.ResolucionAuto;

        ConfiguracionJuego.CambiarPantalla(nueva.x, nueva.y, nuevoModo, nuevaAuto);

        if (panelConfirmar == null) return; // Sin aviso, el cambio queda aplicado

        confirmando = true;
        tiempoRestante = segundosConfirmar;
        panelConfirmar.SetActive(true);
        ActualizarCuenta();
    }

    void Update()
    {
        if (!confirmando) return;

        // Tiempo real: funciona aunque el juego esté en pausa
        tiempoRestante -= Time.unscaledDeltaTime;
        ActualizarCuenta();

        if (tiempoRestante <= 0f) RevertirPantalla();
    }

    void ActualizarCuenta()
    {
        if (textoCuenta != null)
            textoCuenta.text = "¿Mantener esta configuración?\nSe revertirá en " + Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)) + " s";
    }

    // Botón "Mantener" del aviso
    public void ConfirmarPantalla()
    {
        confirmando = false;
        if (panelConfirmar != null) panelConfirmar.SetActive(false);
        ConfiguracionJuego.Guardar();
    }

    // Botón "Revertir" del aviso (también se llama solo al acabarse el tiempo)
    public void RevertirPantalla()
    {
        if (!confirmando) return;

        confirmando = false;
        ConfiguracionJuego.CambiarPantalla(anteriorAncho, anteriorAlto, anteriorModo, anteriorAuto);
        if (panelConfirmar != null) panelConfirmar.SetActive(false);
        RefrescarControles();
    }

    // ---------- Predeterminados ----------
    // Se conecta al botón "Restaurar valores predeterminados"
    public void RestaurarPredeterminados()
    {
        confirmando = false;
        if (panelConfirmar != null) panelConfirmar.SetActive(false);

        ConfiguracionJuego.RestaurarPredeterminados();
        RefrescarControles();
    }

    // Si se cierra el panel con un cambio sin confirmar, se revierte
    void OnDisable()
    {
        if (confirmando) RevertirPantalla();
        ConfiguracionJuego.Guardar();
    }
}