using UnityEngine;

public class PasosJugador : MonoBehaviour
{
    public AudioSource fuente; // AudioSource del jugador (Output = SFX, Play On Awake desmarcado)
    public AudioClip[] pasos; // Uno o varios sonidos de paso (si pones varios, elige uno al azar)

    // Tiempo entre un paso y otro
    public float intervaloCaminar = 0.5f;
    public float intervaloCorrer = 0.3f;

    public float velocidadMinima = 0.5f; 
    public float velocidadCorrer = 7f; 

    private Rigidbody rb;
    private PlayerController jugador;
    private float proximoPaso = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        jugador = GetComponent<PlayerController>();

        // Si no asignas el AudioSource, usa el que esté en este mismo objeto
        if (fuente == null) fuente = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (rb == null || fuente == null || pasos == null || pasos.Length == 0) return;
        if (Time.timeScale == 0f) return; // En pausa no suenan pasos
        if (jugador != null && jugador.haciendoDash) return; // Durante el dash no suenan pasos

        // Velocidad horizontal y vertical del jugador
        Vector3 velocidad = rb.linearVelocity;
        float horizontal = new Vector2(velocidad.x, velocidad.z).magnitude;
        bool enElAire = Mathf.Abs(velocidad.y) > 0.5f; // Saltando o cayendo

        if (horizontal < velocidadMinima || enElAire)
        {
            proximoPaso = 0f; // Al volver a moverse suena un paso enseguida
            return;
        }

        if (Time.time >= proximoPaso)
        {
            float intervalo = horizontal >= velocidadCorrer ? intervaloCorrer : intervaloCaminar;
            proximoPaso = Time.time + intervalo;

            AudioClip clip = pasos[Random.Range(0, pasos.Length)];
            fuente.pitch = Random.Range(0.92f, 1.08f); // Pequeña variación para que no suene repetido
            fuente.PlayOneShot(clip);
        }
    }
}