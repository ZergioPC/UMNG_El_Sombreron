//Si mueves este codigo, es porque estas dispuesto a arreglarlo si lo dañas.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb; // Referencia al Rigidbody del jugador
    private bool enSuelo = true; // Variable para controlar si el jugador está en el suelo

//Direcciones relativas a la cmara
    private Vector3 forward, right; // Vectores de dirección para el movimiento del jugador

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

// Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        forward = Camera.main.transform.forward;
        forward.y = 0; // Mantener la altura del jugador constante
        forward = Vector3.Normalize(forward);

        right = Camera.main.transform.right;
        right.y = 0; // Mantener la altura del jugador constante
        right = Vector3.Normalize(right);
    }

// Update is called once per frame
    private void Update()
    {
        if (!bajoControlJugador) return;

        ProcesarMovimiento();
        ProcesarSalto();
        ProcesarDash();
        procesarAtrapar();
        procesarInvocacionPerro();
    }

    void ProcesarMovimiento() // Metodo para procesar el movimiento del jugador
    {
        if(haciendoDash) // Si el jugador está haciendo un dash, no permitir el movimiento normal
        {
            return;
        }

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
 
        // Si el jugador presiona la tecla Shift, aumentar la velocidad de movimiento
        if (Input.GetKey(KeyCode.LeftShift))
        {
            velocidadActual = runSpeed;
        }

        rb.linearVelocity = new Vector3(movement.x * velocidadActual, rb.linearVelocity.y, movement.z * velocidadActual);

        // Logica para girar al jugador en la dirección del movimiento
        if (movement != Vector3.zero)
        {
            Quaternion nuevaRotacion = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, nuevaRotacion, Time.deltaTime * 10f);
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
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash() // Coroutine para realizar el dash del jugador
    {
        haciendoDash = true;
        dashDisponible = false;

        Vector3 direccionDash = transform.forward; // Dirección del dash basada en la orientación del jugador
        float dashStartTime = Time.time;

        while (Time.time < dashStartTime + dashDuration)
        {
            rb.linearVelocity = new Vector3(direccionDash.x * dashForce, rb.linearVelocity.y, direccionDash.z * dashForce);
            yield return null;
        }

        haciendoDash = false;

        yield return new WaitForSeconds(dashCooldown);
        dashDisponible = true;
    }

    void procesarAtrapar() // Metodo para procesar el atrapar objetos del jugador
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Collider[] NPCs = Physics.OverlapSphere(transform.position + transform.forward * rangoAtrapar, rangoAtrapar, capaNPC);
            if (NPCs.Length > 0)
            {
                NPCIdentity npc = NPCs[0].GetComponent<NPCIdentity>();
                
                if (npc != null)
                {
                    // Logica atrapar culpabe o inocente
                    if (npc.esCulpable())
                    {
                        puntuacionActual += npc.puntosModificador; 
                        Debug.Log("Puntaje actual: " + puntuacionActual);
                    }
                    else
                    {
                        puntuacionActual -= npc.puntosModificador; 
                        Debug.Log("Puntaje actual: " + puntuacionActual);
                    }

                    /*
                    // Easter Egg: serenata a la dama
                    else if (npc.tipo == NPCIdentity.TipoNPC.Dama && tieneGuitarra)
                    {
                        puntuacionActual += bonificacionSerenata;
                        Debug.Log($"¡Le diste una serenata a la Dama! +{bonificaciónSerenata} pts. Total: {puntuacionActual}");
                    }
                    */

                    Destroy(npc.gameObject); // Destruir el NPC atrapado
                }
            }
        }
    }

    void procesarInvocacionPerro() // Metodo para procesar la invocación del perro
    {
        if (Input.GetMouseButtonDown(1) && invocacionDisponible)
        {
            if(perro != null && puntoInvocacion != null)
            {
                StartCoroutine(InvocarPerro());
            }
        }
    }

    IEnumerator InvocarPerro() // Coroutine para invocar al perro
    {
        invocacionDisponible = false;
        bajoControlJugador = false; // Desactivar el control del jugador mientras el perro está invocado

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Detener el movimiento del jugador mientras el perro está invocado

        perro.ActivarPerro(puntoInvocacion.position);

        // Cambiar la cámara para seguir al perro invocado
        CamaraController camara = Camera.main.GetComponent<CamaraController>();
        if (camara != null)
        {
            camara.CambiarTarget(perro.transform);
        }
        
        //tiempo de invocación del perro
        yield return new WaitForSeconds(tiempoInvocacion);
        perro.DesactivarPerro(); // Desactivar el perro después del tiempo de invocación

        // Cambiar la cámara de vuelta al jugador
        if (camara != null)
        {
            camara.CambiarTarget(transform); // Cambiar la cámara de vuelta al jugador
        }
        bajoControlJugador = true; // Reactivar el control del jugador después de que el perro desaparezca

        yield return new WaitForSeconds(tiempoCooldownInvocacion);
        invocacionDisponible = true;
    }






    private void OnCollisionStay(Collision collision) // Metodo para detectar si el jugador está en contacto con el suelo
    {
        if (collision.gameObject.CompareTag("enSuelo"))
        {
            enSuelo = true;
        }
    }

    private void OnCollisionExit(Collision collision) // Metodo para detectar si el jugador deja de estar en contacto con el suelo
    {
        if (collision.gameObject.CompareTag("enSuelo"))
        {
            enSuelo = false;
        }
    }



    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Colectible"))
        {
            Debug.Log("Player collected an item!");
            Destroy(collision.gameObject);
        }    
    }
}