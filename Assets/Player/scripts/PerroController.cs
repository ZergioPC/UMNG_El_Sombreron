using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerroController : MonoBehaviour
{
    private Rigidbody rb;
    private bool estaActivo = false;

    public Transform jugador;
    public Renderer rendererPerro;

// Variables para controlar el movimiento del perro
    public Vector3 offsetSeguimiento = new Vector3(-1f, 0, -1f); // Desplazamiento de la cámara respecto al perro
    public float VelocidadSeguimiento = 10f; // Velocidad de seguimiento del perro
    public float smoothSpeed = 0.125f; // Velocidad de suavizado del movimiento de la cámara

// Variables para controlar la invocación del perro
    public float VelocidadInvocacion = 8f; // Velocidad de movimiento del perro durante la invocación
    public float tiempoInvocacion = 5f; // Tiempo que el perro estará activo después de ser invocado

// Variables para controlar el movimiento del perro
    public float velocidad = 8f; // Velocidad de movimiento del perro
    public float rangoDeteccion = 20f; // Rango de detección del jugador
    public LayerMask capaNPC; // Capa de los NPCs para detectar colisiones

    private Vector3 forward, right; // Vectores de dirección para el movimiento del perro

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rendererPerro == null)
        {
            rendererPerro = GetComponent<Renderer>();
        }
        DesactivarPerro();
        
    }

    // Update is called once per frame
    private void Update()
    {
        if(estaActivo)
        {
            ActualizarCamara();
            ProcesarMovimientoPerro();
            BuscarNPCs();  
        }
        else
        {
            seguirJugador();
        }

    }
    void ActualizarCamara()
    {
        if (Camera.main != null)
            {
                forward = Camera.main.transform.forward;
                forward.y = 0;
                forward.Normalize();

                right = Camera.main.transform.right;
                right.y = 0;
                right.Normalize();
            }
    }

    void seguirJugador() // Metodo para que el perro siga al jugador
    {
        if (jugador == null) return;
        
            Vector3 posicionDeseada = jugador.position + jugador.TransformDirection(offsetSeguimiento);
            transform.position = Vector3.Lerp(transform.position, posicionDeseada, smoothSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, jugador.rotation, smoothSpeed);        
    }

    public void ActivarPerro(Vector3 puntoInvocacion) // Metodo para activar el perro
    {
        transform.position = puntoInvocacion;
        estaActivo = true;

        if (rendererPerro != null)
        {
            rendererPerro.enabled = true; // Hacer visible el perro
        }

        // Recalcular direccion segun la camara
        forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();

        right = Camera.main.transform.right;
        right.y = 0;
        right.Normalize();
    }

    public void DesactivarPerro() // Metodo para desactivar el perro
    {
        estaActivo = false;

        if (rendererPerro != null)
        {
            rendererPerro.enabled = false; // Hacer invisible el perro
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Detener el movimiento del perro
        }
    }

    void ProcesarMovimientoPerro() // Metodo para procesar el movimiento del perro
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = (forward * moveVertical + right * moveHorizontal).normalized;

        rb.linearVelocity = new Vector3(movement.x * velocidad, rb.linearVelocity.y, movement.z * velocidad);
    
        if (movement != Vector3.zero)
        {
            Quaternion nuevaRotacion = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, nuevaRotacion, Time.deltaTime * 10f);
        }
    }

    void BuscarNPCs() // Metodo para buscar NPCs cercanos al perro
    {
        Collider[] npcsEncontrados = Physics.OverlapSphere(transform.position, rangoDeteccion, capaNPC);

        foreach (Collider npcCollider in npcsEncontrados)
        {
            NPCIdentity npc = npcCollider.GetComponent<NPCIdentity>();
            if (npc != null)
            {
                npc.RevelarNPC();
            }
        }
    }

    private void OneDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}