using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GestorTiempo : MonoBehaviour
{
    public static GestorTiempo Instancia { get; private set; }

    public static event Action OnAmanecer;          // Fin de la partida
    public static event Action<int> OnCambioHora;   // 1, 2, 3, 4, 5, 6

    [Header("Duración")]
    public float duracionPartida = 300f; // 5 minutos reales
    public float horaInicio = 0f;        // 0 = 12:00 AM
    public float horaFin = 6f;           // 6:00 AM

    [Header("UI")]
    public TMP_Text textoTitulo;
    public TMP_Text textoHora;
    public Image iconoVela;
    public bool mostrarCuentaRegresiva = false; // true = horas que faltan para el amanecer

    [Header("Luz (opcional)")]
    public Light luzDireccional;
    public Gradient colorLuz;
    public AnimationCurve intensidadLuz = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);

    [Header("Fin de partida")]
    public GameObject panelFinPartida;   // Panel que se activa al amanecer (puede estar desactivado en la escena)
    public TMP_Text textoResultado;      // Opcional: muestra el puntaje final
    public string mensajeFin = "¡Amaneció!";

    public float Progreso { get; private set; }
    public float HoraActual { get; private set; }
    public bool Terminado { get; private set; }
    public bool EnPausa { get; private set; }

    private float tiempoTranscurrido;
    private int ultimaHoraEntera = -1;

    private void Awake()
    {
        Instancia = this;
        Time.timeScale = 1f; // por si venimos de una partida terminada
    }

    private void Start()
    {
        if (textoTitulo != null) textoTitulo.text = "Tiempo hasta el amanecer";
        if (panelFinPartida != null) panelFinPartida.SetActive(false);
        ActualizarHora();
        ActualizarUI();
    }

    private void Update()
    {
        if (Terminado || EnPausa) return;

        tiempoTranscurrido += Time.deltaTime;
        ActualizarHora();
        ActualizarUI();

        if (tiempoTranscurrido >= duracionPartida)
        {
            FinalizarPartida();
        }
    }

    private void ActualizarHora()
    {
        Progreso = Mathf.Clamp01(tiempoTranscurrido / duracionPartida);
        HoraActual = Mathf.Lerp(horaInicio, horaFin, Progreso);
    }

    private void ActualizarUI()
    {
        // Vela y luz avanzan de forma continua
        if (iconoVela != null) iconoVela.fillAmount = 1f - Progreso;

        if (luzDireccional != null)
        {
            luzDireccional.color = colorLuz.Evaluate(Progreso);
            luzDireccional.intensity = intensidadLuz.Evaluate(Progreso);
        }

        // El reloj solo cambia cuando pasa una hora completa
        int horaEntera = Mathf.FloorToInt(HoraActual + 0.0001f);
        if (horaEntera == ultimaHoraEntera) return;
        ultimaHoraEntera = horaEntera;

        if (horaEntera > 0) OnCambioHora?.Invoke(horaEntera);
        if (textoHora == null) return;

        if (mostrarCuentaRegresiva)
        {
            int faltan = Mathf.Max(0, Mathf.RoundToInt(horaFin) - horaEntera);
            textoHora.text = faltan + ":00";
        }
        else
        {
            int hora12 = horaEntera % 12 == 0 ? 12 : horaEntera % 12;
            string sufijo = horaEntera < 12 ? "AM" : "PM";
            textoHora.text = hora12 + ":00 " + sufijo;
        }
    }

    private void FinalizarPartida()
    {
        Terminado = true;
        Progreso = 1f;
        HoraActual = horaFin;
        ActualizarUI();

        // Bloquear al jugador y al perro
        PlayerController jugador = FindAnyObjectByType<PlayerController>();
        if (jugador != null)
        {
            if (textoResultado != null)
                textoResultado.text = mensajeFin + "\nPuntaje: " + jugador.puntuacionActual;
            jugador.enabled = false;
        }
        else if (textoResultado != null)
        {
            textoResultado.text = mensajeFin;
        }

        PerroController perro = FindAnyObjectByType<PerroController>();
        if (perro != null) perro.enabled = false;

        if (panelFinPartida != null) panelFinPartida.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        OnAmanecer?.Invoke();
    }

    public float SegundosRealesRestantes() => Mathf.Max(0f, duracionPartida - tiempoTranscurrido);

    public void Pausar() => EnPausa = true;
    public void Reanudar() => EnPausa = false;

    // Para el botón "Jugar de nuevo" del panel final
    public void ReiniciarEscena()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}