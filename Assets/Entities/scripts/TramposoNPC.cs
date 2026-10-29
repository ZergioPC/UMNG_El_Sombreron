using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]
public class TramposoNPC : MonoBehaviour
{
    private NavMeshAgent agent;
    private NPCIdentity identidad;
    private Transform jugador;

    [Header("Parámetros de Huida")]
    public float velocidadTranquilo = 2.5f;
    public float velocidadHuida = 7f;
    public float radioAlerta = 8f;        // Distancia a la que detecta al Sombrerón y empieza a huir
    public float distanciaEscape = 2.5f;   // Si el Sombrerón se acerca sin Dash a esta distancia, desaparece

    [Header("Efecto de Desaparición")]
    public GameObject efectoHumoPrefab;   // Opcional: Sistema de partículas de humo al desaparecer

    private bool aturdido = false;
    private bool huyendo = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        identidad = GetComponent<NPCIdentity>();

        // Forzar el tipo a Tramposo en el script de identidad
        identidad.tipo = NPCIdentity.TipoNPC.Tramposo;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }

        // Posicionar correctamente en NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        agent.speed = velocidadTranquilo;
        MoverNuevoPunto();
    }

    void Update()
    {
        if (jugador == null || !agent.isOnNavMesh || aturdido) return;

        float distanciaAlJugador = Vector3.Distance(transform.position, jugador.position);
        PlayerController playerScript = jugador.GetComponent<PlayerController>();
        bool jugadorEnDash = playerScript != null && playerScript.haciendoDash;

        // 1. SI EL JUGADOR USA DASH CERCA: Se aturde por sorpresa
        if (jugadorEnDash && distanciaAlJugador <= radioAlerta)
        {
            StartCoroutine(Aturdir());
            return;
        }

        // 2. SI EL JUGADOR SE ACERCA DEMASIADO SIN DASH: Desaparece/Escapa
        if (distanciaAlJugador <= distanciaEscape)
        {
            EscaparYDesaparecer();
            return;
        }

        // 3. SI DETECTA AL JUGADOR EN RADIO DE ALERTA: Empieza a correr para perderse
        if (distanciaAlJugador <= radioAlerta)
        {
            HuirDelJugador();
        }
        else
        {
            // Movimiento normal cuando no ve peligro
            if (huyendo)
            {
                huyendo = false;
                agent.speed = velocidadTranquilo;
            }

            if (!agent.hasPath || agent.remainingDistance < 0.8f)
            {
                MoverNuevoPunto();
            }
        }
    }

    void HuirDelJugador()
    {
        huyendo = true;
        agent.speed = velocidadHuida;

        // Calcula una dirección opuesta al Sombrerón
        Vector3 direccionOpuesta = (transform.position - jugador.position).normalized;
        Vector3 puntoHuida = transform.position + direccionOpuesta * 6f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoHuida, out hit, 6f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void EscaparYDesaparecer()
    {
        Debug.Log("¡El Tramposo vio al Sombrerón muy cerca y logró desaparecer entre los callejones!");

        if (efectoHumoPrefab != null)
        {
            Instantiate(efectoHumoPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject); // Desaparece sin dar puntos
    }

    IEnumerator Aturdir()
    {
        aturdido = true;
        agent.isStopped = true;
        Debug.Log("¡Sorprendiste al Tramposo con un Dash! Quedó acorralado e inmóvil.");

        yield return new WaitForSeconds(2.5f); // 2.5 segundos de ventana para atraparlo con clic izquierdo

        if (this != null)
        {
            agent.isStopped = false;
            aturdido = false;
        }
    }

    void MoverNuevoPunto()
    {
        Vector3 puntoAleatorio = Random.insideUnitSphere * 12f + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, 12f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}