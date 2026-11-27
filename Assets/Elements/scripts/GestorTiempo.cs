using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GestorTiempo : MonoBehaviour
{
    public static GestorTiempo Instancia { get; private set; }

    public static event Action OnAmanecer;       
    public static event Action<int> OnCambioHora; //Salta de hora en hora

    // Duración
    public float duracionPartida = 300f; // 5 minutos reales
    public float horaInicio = 0f; // 0 = 12:00 AM
    public float horaFin = 6f; // 6:00 AM

    // UI
    public TMP_Text textoTitulo;
    public TMP_Text textoHora;
    public Image iconoVela;
    public bool mostrarCuentaRegresiva = false;

    // Luz
    public Light luzDireccional;
    public Gradient colorLuz;
    public AnimationCurve intensidadLuz = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);

    // Fin de partida
    public string escenaFinPartida = "FinPartida"; // Nombre exacto de la escena final
    public float retrasoFin = 1.5f; // Segundos (tiempo real) que se ve el amanecer antes de cambiar de escena

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

        // Guardar el puntaje y bloquear al jugador y al perro
        PlayerController jugador = FindAnyObjectByType<PlayerController>();
        if (jugador != null)
        {
            DatosPartida.PuntajeFinal = jugador.puntuacionActual;
            jugador.enabled = false;
        }

        PerroController perro = FindAnyObjectByType<PerroController>();
        if (perro != null) perro.enabled = false;

        Time.timeScale = 0f;

        OnAmanecer?.Invoke();
        StartCoroutine(IrAFinPartida());
    }

    // Espera con tiempo real (el juego está en pausa) y carga la escena final
    private IEnumerator IrAFinPartida()
    {
        yield return new WaitForSecondsRealtime(retrasoFin);

        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(escenaFinPartida))
            SceneManager.LoadScene(escenaFinPartida);
        else
            Debug.LogError("GestorTiempo: la escena '" + escenaFinPartida + "' no existe o no está en Scenes In Build.");
    }

    public float SegundosRealesRestantes() => Mathf.Max(0f, duracionPartida - tiempoTranscurrido);

    public void Pausar() => EnPausa = true;
    public void Reanudar() => EnPausa = false;
}