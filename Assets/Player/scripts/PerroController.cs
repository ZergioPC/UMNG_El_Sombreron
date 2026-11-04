using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerroController : MonoBehaviour
{
// Referencias a componentes del perro
    private Animator animator;
    private Rigidbody rb;
    private bool estaActivo = false;
    public Transform jugador;

// Variables de seguimiento del jugador
    public Vector3 offsetSeguimiento = new Vector3(-1.5f, 0, -1.5f);
    public float velocidadSeguimiento = 10f;

// Variables de movimiento del perro
    public float velocidad = 8f;
    public float rangoDeteccion = 15f;
    public LayerMask capaNPC;

// Habilidades del perro
    public float rangoAturdimiento = 2.5f;

    private Vector3 forward, right; // Vectores de dirección para el movimiento del perro

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();     
        DesactivarPerro();
    }

    private void Update()
    {
        if (estaActivo)
        {
            ActualizarCamara();
            ProcesarMovimientoPerro();
            BuscarNPCs();
            ProcesarAturdimiento();
        }
        else
        {
            SeguirJugador();
        }
    }

    void ActualizarCamara()
    {
        if (Camera.main != null)
        {
            // Actualizamos los vectores de dirección basados en la cámara principal
            forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();

            // Actualizamos el vector derecho basado en la cámara principal
            right = Camera.main.transform.right;
            right.y = 0;
            right.Normalize();
        }
    }

    void SeguirJugador()
    {
        if (jugador == null) return;

        // Calculamos la posición deseada del perro detrás del jugador
        Vector3 posicionDeseada = jugador.position + jugador.TransformDirection(offsetSeguimiento);
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, velocidadSeguimiento * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, jugador.rotation, velocidadSeguimiento * Time.deltaTime);
    }

    public void ActivarPerro(Vector3 puntoInvocacion)
    {
        // Posicionamos al perro en el punto de invocación y lo activamos
        transform.position = puntoInvocacion;
        estaActivo = true;

        // Activamos las mallas visuales del perro
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }

        ActualizarCamara();
    }

    public void DesactivarPerro()
    {
        estaActivo = false;

        // Ocultamos las mallas visuales sin apagar el GameObject
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        // Detenemos el movimiento del perro
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    void ProcesarMovimientoPerro()
    {
        // Logica de movimiento del perro
        float moveHorizontal = Input.GetAxis("Horizontal"); //
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = (forward * moveVertical + right * moveHorizontal).normalized;

        rb.linearVelocity = new Vector3(movement.x * velocidad, rb.linearVelocity.y, movement.z * velocidad);

        if (movement != Vector3.zero)
        {
            Quaternion nuevaRotacion = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, nuevaRotacion, Time.deltaTime * 10f);
        }
        if (animator != null)
        {
            animator.SetFloat("movement", movement.magnitude);
        }
    }

    void BuscarNPCs()
    {
        // Detectar NPCs dentro del rango de detección
        Collider[] npcsEncontrados = Physics.OverlapSphere(transform.position, rangoDeteccion, capaNPC);

        // Revelar la identidad de cada NPC encontrado
        foreach (Collider npcCollider in npcsEncontrados)
        {
            NPCIdentity npc = npcCollider.GetComponent<NPCIdentity>();
            if (npc != null)
            {
                npc.RevelarNPC();
            }
        }
    }

    void ProcesarAturdimiento()
    {
        if (Input.GetMouseButtonDown(0)) // Botón izquierdo del ratón
        {
            if (animator != null)
            {
                animator.SetTrigger("attack");
            }

            // Detectar NPCs dentro del rango de aturdimiento
            Collider[] npcsEncontrados = Physics.OverlapSphere(transform.position, rangoAturdimiento, capaNPC);

            // Aturdir a cada NPC encontrado
            foreach (Collider npcCollider in npcsEncontrados)
            {
                NPCIdentity npc = npcCollider.GetComponentInParent<NPCIdentity>();
                if (npc != null)
                {
                    npc.RevelarNPC();
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar un gizmo para visualizar el rango de detección del perro
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
}