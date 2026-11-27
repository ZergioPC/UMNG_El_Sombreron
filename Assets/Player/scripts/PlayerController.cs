//Si mueves este codigo, es porque estas dispuesto a arreglarlo si lo dañas.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb; // Referencia al Rigidbody del jugador
    private bool enSuelo = true; // Variable para controlar si el jugador está en el suelo

// Animaciones
    private Animator animator;

// Fotograma en el que desaparece el NPC durante la animación de ataque
    public int frameDesaparicionAtaque = 19;
    public string nombreEstadoAtaque = "attack";

//Direcciones relativas a la cmara
    private Vector3 forward, right; // Vectores de dirección para el movimiento del jugador

// Referencias a otros scripts
    private CamaraController scriptCamara; // Referencia directa al script de la cámara

// Variables para controlar el movimiento del jugador
    public float speed = 5f; // Velocidad de movimiento del jugador
    public float runSpeed = 10f; // Velocidad de carrera del jugador
    public float jumpForce = 5f; // Fuerza de salto del jugador

// Variables para controlar el dash del jugador
    public float dashForce = 15f; // Fuerza de impulso para el dash
    public float dashDuration = 0.2f; // Duración del dash
    public float dashCooldown = 1f; // Tiempo de recarga del dash
    public bool haciendoDash = false; // Variable para controlar si el jugador está haciendo un dash
    private bool dashDisponible = true; // Variable para controlar si el dash está disponible
    public ParticleSystem vfxDash; // VFX activado durante el dash

// Variables para controlar el atrapar objetos del jugador
    public float rangoAtrapar = 2f; // Rango de atrapar objetos
    public LayerMask capaNPC; // Capa de los NPCs para detectar colisiones

//  Variables para controlar el puntaje del jugador
    public int puntuacionActual = 0; // Almacenar el puntaje

// Variables para controlar la invocación del perro
    public PerroController perro; // Referencia al objeto del perro
    public Transform puntoInvocacion; // Punto de invocación del perro
    public float tiempoInvocacion = 5f; // Tiempo de invocación del perro
    public float tiempoCooldownInvocacion = 10f; // Tiempo de recarga de la invocación del perro
    private bool invocacionDisponible = true; // Variable para controlar si la invocación del perro está disponible
    private bool bajoControlJugador = true; // Variable para controlar si el jugador tiene control del personaje
    private bool perroInvocado = false; // True mientras el perro está activo
    private Coroutine rutinaInvocacionPerro;

// Control de ataque y stun
    private Coroutine rutinaAtrapar;
    private bool estaAturdido = false;

// HUD: datos de enfriamiento para el contador de habilidades
    private float finCooldownDash = 0f; // HUD: momento (Time.time) en que el dash vuelve a estar listo
    private float finCooldownPerro = 0f; // HUD: momento (Time.time) en que el perro vuelve a estar listo

    public float DashRestante { get { return dashDisponible ? 0f : Mathf.Max(0f, finCooldownDash - Time.time); } } // HUD
    public float DashTiempoTotal { get { return dashDuration + dashCooldown; } } // HUD
    public bool PerroActivo { get { return perroInvocado; } } // HUD
    public float PerroRestante { get { return invocacionDisponible || perroInvocado ? 0f : Mathf.Max(0f, finCooldownPerro - Time.time); } } // HUD
    public float PerroTiempoTotal { get { return tiempoCooldownInvocacion; } } // HUD

// Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (Camera.main != null)
        {
            forward = Camera.main.transform.forward;
            forward.y = 0; // Mantener la altura del jugador constante
            forward = Vector3.Normalize(forward);

            right = Camera.main.transform.right;
            right.y = 0; // Mantener la altura del jugador constante
            right = Vector3.Normalize(right);
        }

        if (scriptCamara == null)
        {
            scriptCamara = FindAnyObjectByType<CamaraController>();
        }
    }

// Update is called once per frame
    private void Update()
    {
        // Mientras el perro está invocado: clic izquierdo = olfatear, clic derecho = desaparecer y volver
        if (perroInvocado)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ActivarOlfateoPerro();
            }

            if (Input.GetMouseButtonDown(1))
            {
                RetirarPerroYVolver();
            }
            return;
        }

        if (!bajoControlJugador) return;

        ProcesarMovimiento();
        ProcesarSalto();
        ProcesarDash();
        procesarAtrapar();
        procesarInvocacionPerro();
    }

    private void ReproducirVFX(ParticleSystem ps)
    {
        if (ps == null) return;
        if (!ps.gameObject.activeSelf) ps.gameObject.SetActive(true);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play(true);
    }


    void ProcesarMovimiento() // Metodo para procesar el movimiento del jugador
    {
        if (haciendoDash) return;

        //Logica de movimiento del jugador
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Crear un vector de movimiento basado en la entrada del jugador y la orientación de la cámara
        Vector3 movement = (forward * moveVertical + right * moveHorizontal).normalized;
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // Ajustar la velocidad de movimiento según si el jugador está corriendo o caminando
        float velocidadActual = speed;
        bool runSpeed = Input.GetKey(KeyCode.LeftShift);

        if (runSpeed)
        {
            velocidadActual = this.runSpeed;
        }

        rb.linearVelocity = new Vector3(movement.x * velocidadActual, rb.linearVelocity.y, movement.z * velocidadActual);

        // Logica para girar al jugador en la dirección del movimiento
        if (movement != Vector3.zero)
        {
            Quaternion nuevaRotacion = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, nuevaRotacion, Time.deltaTime * 10f);
        }

        // Logica para actualizar las animaciones del jugador
        if (animator != null)
        {
            float valorAnimacion = movement.magnitude * (runSpeed ? 2f : 1f); // Ajustar la velocidad de la animación según si el jugador está corriendo o caminando
            if (haciendoDash)
            {
                valorAnimacion = 2f; // Durante el dash, la animación de correr se reproduce a velocidad completa
            }
            animator.SetFloat("movement", valorAnimacion);
        }
    }

    void ProcesarSalto() // Metodo para procesar el salto del jugador
    {
        if (Input.GetKeyDown(KeyCode.Space) && enSuelo && !haciendoDash) // Verificar si el jugador está en el suelo antes de permitir el salto
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Resetear la velocidad vertical antes de aplicar el salto
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            enSuelo = false;
        }
    }

    void ProcesarDash() // Metodo para procesar el dash del jugador
    {
        if (Input.GetKeyDown(KeyCode.Q) && dashDisponible && !haciendoDash) // Verificar si el jugador puede hacer dash y no está haciendo uno actualmente
        {
            if (animator != null)
            {
                animator.SetTrigger("dash"); // Activar la animación de dash
            }
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash() // Coroutine para realizar el dash del jugador
    {
        haciendoDash = true;
        dashDisponible = false;
        finCooldownDash = Time.time + dashDuration + dashCooldown; // HUD: cuándo vuelve a estar listo

        if (vfxDash != null)
        {
            if (!vfxDash.gameObject.activeSelf) vfxDash.gameObject.SetActive(true);
            vfxDash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfxDash.Play(true);
        }

        Vector3 direccionDash = transform.forward; // Dirección del dash basada en la orientación del jugador
        float dashStartTime = Time.time;

        while (Time.time < dashStartTime + dashDuration)
        {
            rb.linearVelocity = new Vector3(direccionDash.x * dashForce, rb.linearVelocity.y, direccionDash.z * dashForce);
            yield return null;
        }

        if (vfxDash != null)
        {
            vfxDash.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        haciendoDash = false;

        yield return new WaitForSeconds(dashCooldown);
        dashDisponible = true;
    }

    // Metodo para procesar el atrapar objetos del jugador
    void procesarAtrapar()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Detectar NPCs en el rango de atrapar usando un OverlapSphere
            Collider[] NPCsEncontrados = Physics.OverlapSphere(transform.position + transform.forward * rangoAtrapar, rangoAtrapar, capaNPC);

            // Si se encuentra al menos un NPC, atrapar al primero encontrado
            if (NPCsEncontrados.Length > 0)
            {
                NPCIdentity npc = NPCsEncontrados[0].GetComponentInParent<NPCIdentity>();

                if (npc != null)
                {
                    TramposoNPC tramposo = npc.GetComponentInParent<TramposoNPC>();
                    if (tramposo != null && !tramposo.PuedeSerCapturado)
                    {
                        return; // No es capturable mientras lanza la bomba de humo
                    }

                    if (animator != null)
                    {
                        animator.SetTrigger("attack");
                    }

                    if (rutinaAtrapar != null)
                    {
                        StopCoroutine(rutinaAtrapar);
                    }
                    rutinaAtrapar = StartCoroutine(AtraparNPC(npc));
                }
            }
        }
    }

    // Coroutine para atrapar un NPC
    IEnumerator AtraparNPC(NPCIdentity npc)
    {
        bajoControlJugador = false; // Desactivar el control del jugador mientras se atrapa al NPC

        if (rb != null) rb.linearVelocity = Vector3.zero; // Detener el movimiento del jugador mientras se atrapa al NPC

        // Logica para atrapar al NPC (animacion)
        Vector3 posicionInicial = transform.position;
        Vector3 posicionFinal = npc != null ? npc.transform.position : transform.position;
        Vector3 direccionMirar = (posicionFinal - posicionInicial).normalized;
        direccionMirar.y = 0; // Mantener la rotación en el plano horizontal

        if (direccionMirar != Vector3.zero) transform.rotation = Quaternion.LookRotation(direccionMirar);

        // Animacion de atrapar
        float tiempoAtrapar = 0.2f;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoAtrapar)
        {
            if (npc == null) break;
            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, tiempoTranscurrido / tiempoAtrapar);
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Esperar hasta llegar al fotograma indicado de la animación de ataque
        yield return StartCoroutine(EsperarHastaFotogramaAtaque(frameDesaparicionAtaque));

        // Logica para atrapar culpable o inocente
        if (npc != null)
        {
            TramposoNPC tramposo = npc.GetComponentInParent<TramposoNPC>();
            if (tramposo != null && tramposo.esClon)
            {
                bool dioPuntos;
                tramposo.IntentarCapturar(out dioPuntos); // Dispara el VFX del clon y devuelve dioPuntos = false

                if (Letrero.Instancia != null)
                    Letrero.Instancia.Mostrar(TipoLetrero.Clon);

                Destroy(npc.gameObject);
                rutinaAtrapar = null;
                if (!estaAturdido) bajoControlJugador = true;
                yield break;
            }

            bool culpable = npc.esCulpable();
            int puntos = npc.puntosModificador;

            if (culpable) puntuacionActual += puntos;
            else puntuacionActual -= puntos;

            if (Letrero.Instancia != null)
                Letrero.Instancia.Mostrar(culpable ? TipoLetrero.Culpable : TipoLetrero.Inocente, puntos);

            // Banner: solo cuando atrapas a un culpable (antes de destruir al NPC, porque lee sus scripts)
            if (culpable && BannerCapturados.Instancia != null)
                BannerCapturados.Instancia.Mostrar(npc.gameObject);

            Destroy(npc.gameObject); // Destruir el NPC atrapado en el fotograma objetivo
        }

        rutinaAtrapar = null;
        if (!estaAturdido)
        {
            bajoControlJugador = true;
        }
    }

    private IEnumerator EsperarHastaFotogramaAtaque(int fotogramaObjetivo)
    {
        if (animator == null) yield break;

        // Esperar hasta que el Animator entre al estado de ataque
        float timeout = 1.5f;
        while (timeout > 0f)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(nombreEstadoAtaque))
            {
                break;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Obtener FPS y duración real del clip actual
        float fps = 30f;
        float duracionClip = 1f;
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips != null && clips.Length > 0 && clips[0].clip != null)
        {
            AnimationClip clip = clips[0].clip;
            fps = clip.frameRate > 0f ? clip.frameRate : 30f;
            duracionClip = clip.length > 0f ? clip.length : 1f;
        }

        // Calcular el progreso normalizado exacto del fotogramaObjetivo
        float progresoObjetivo = (fotogramaObjetivo / fps) / duracionClip;

        while (animator != null)
        {
            AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
            bool sigueEnAtaque = estado.IsName(nombreEstadoAtaque);

            // Si ya salió del estado de ataque y no está en transición, no quedarse esperando
            if (!sigueEnAtaque && !animator.IsInTransition(0))
            {
                break;
            }

            // Cuando el fotograma actual (normalizedTime) alcanza o supera el fotograma objetivo
            if (sigueEnAtaque && estado.normalizedTime >= progresoObjetivo)
            {
                break;
            }

            yield return null;
        }
    }

    void procesarInvocacionPerro() // Metodo para procesar la invocación del perro
    {
        if (Input.GetMouseButtonDown(1) && invocacionDisponible && !perroInvocado)
        {
            if (perro != null && puntoInvocacion != null)
            {
                rutinaInvocacionPerro = StartCoroutine(InvocarPerro());
            }
        }
    }

    IEnumerator InvocarPerro() // Coroutine para invocar al perro
    {
        invocacionDisponible = false;
        perroInvocado = true;
        bajoControlJugador = false; // Desactivar el control del jugador mientras el perro está invocado

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Detener el movimiento del jugador mientras el perro está invocado

        perro.ActivarPerro(puntoInvocacion.position);

        // Cambiar la cámara para seguir al perro invocado
        if (scriptCamara != null)
        {
            scriptCamara.CambiarTarget(perro.transform);
        }

        //tiempo de invocación del perro
        yield return new WaitForSeconds(tiempoInvocacion);
        RetirarPerroYVolver();
    }

    private void ActivarOlfateoPerro()
    {
        if (perro != null)
        {
            perro.Olfatear();
        }
    }

    private void RetirarPerroYVolver()
    {
        if (!perroInvocado) return;

        perroInvocado = false;

        if (rutinaInvocacionPerro != null)
        {
            StopCoroutine(rutinaInvocacionPerro);
            rutinaInvocacionPerro = null;
        }

        if (perro != null)
        {
            perro.DesactivarPerro(); // Desactivar el perro al volver al jugador o terminar el tiempo
        }

        // Cambiar la cámara de vuelta al jugador
        if (scriptCamara != null)
        {
            scriptCamara.CambiarTarget(transform); // Cambiar la cámara de vuelta al jugador
        }

        bajoControlJugador = true; // Reactivar el control del jugador después de que el perro desaparezca

        StartCoroutine(CooldownInvocacion());
    }

    private IEnumerator CooldownInvocacion()
    {
        finCooldownPerro = Time.time + tiempoCooldownInvocacion; // HUD: cuándo vuelve a estar listo el perro
        yield return new WaitForSeconds(tiempoCooldownInvocacion);
        invocacionDisponible = true;
    }

    // Metodo para establecer el estado de stun del jugador
    public void EstablecerStun(bool estado)
    {
        // No aplicar stun nuevo durante dash o mientras controla al perro
        if (estado && (haciendoDash || perroInvocado))
        {
            return;
        }

        estaAturdido = estado;

        // Si entra en stun durante un ataque, cancelar la corrutina de captura
        if (estado && rutinaAtrapar != null)
        {
            StopCoroutine(rutinaAtrapar);
            rutinaAtrapar = null;
        }

        bajoControlJugador = !estado;
        if (estado && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetBool("stun", estado);
        }
    }

    // Metodo para detectar si el jugador está en contacto con el suelo
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("enSuelo"))
        {
            enSuelo = true;
        }
    }

    // Metodo para detectar si el jugador deja de estar en contacto con el suelo
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("enSuelo"))
        {
            enSuelo = false;
        }
    }
}