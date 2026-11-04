using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]
public class PeleadorNPC : MonoBehaviour
{
// Referencias a componentes
    private NavMeshAgent agent;
    private Animator animator;
    private Transform jugador;

// parametros de patrulla
    public float radioPatrulla = 8f; // Radio de patrulla alrededor del punto inicial
    public float tiempoEspera = 2f; // Tiempo de espera en segundos antes de elegir un nuevo destino
    private float temporizadorEspera = 0f; // Temporizador para controlar el tiempo de espera antes de elegir un nuevo destino
    public float velocidadPatrulla = 1.5f; // Velocidad de patrulla del NPC

// parametros de comportamiento del peleador
    public float distanciaDeteccion = 5f; // Distancia a la que el NPC detecta al jugador
    public float distanciaAtaque = 1.5f; // Distancia a la que el NPC puede atacar al jugador
    public float fuerzaEmpuje = 2f; // Fuerza con la que el NPC empuja al jugador
   
// Cooldown de empuje
    public float tiempoCooldownEmpuje = 2f; // Tiempo de cooldown entre empujes
    private float siguienteEmpuje = 0f; // Tiempo en el que el NPC puede empujar nuevamente al jugador

    void Start()
    {
        // Obtener referencias a los componentes necesarios
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Encontrar al jugador en la escena
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }
        MoverNuevoPunto(); // Elegir un nuevo punto de patrulla al inicio
    }

    // Update is called once per frame
    void Update()
    {
        // Asegurarse de que el jugador y el agente estén disponibles
        if (agent == null || !agent.isOnNavMesh) return;

        if (animator != null)
        {
            animator.SetFloat("movement", agent.velocity.magnitude);
        }

        //Si no hay jugadores asignados, el NPC patrulla normalmente
        if (jugador == null)
        {
            Patrullar();
            return;
        }

        float distanciaJugador = Vector3.Distance(transform.position, jugador.position);

        // Comportamiento del peleador
        // Si el jugador está muy cerca, encara y ataca
        if (distanciaJugador <= distanciaAtaque)
        {
            agent.isStopped = true;
            EncararJugador();
            ProcesarAtaque();
        }
        // Si esta en rango de vision lo persigue
        else if (distanciaJugador <= distanciaDeteccion)
        {
            agent.isStopped = false;
            agent.speed = velocidadPatrulla * 2f; // Aumentar la velocidad al perseguir
            agent.SetDestination(jugador.position);
        }
        // Si esta lejos, continuar patrullando normal
        else
        {
            agent.isStopped = false;
            agent.speed = velocidadPatrulla; // Restaurar la velocidad normal de patrulla
            Patrullar();
        }
    }

    void Patrullar()
    {
        // Si el NPC ha llegado a su destino, esperar un tiempo antes de elegir un nuevo destino
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            temporizadorEspera += Time.deltaTime;
            if (temporizadorEspera >= tiempoEspera)
            {
                MoverNuevoPunto();
                temporizadorEspera = 0f; // Reiniciar el temporizador de espera
            }
        }
    }

    void EncararJugador()
    {
        Vector3 direccionHaciaJugador = (jugador.position - transform.position).normalized;
        direccionHaciaJugador.y = 0; // Mantener la rotación en el plano horizontal
        if (direccionHaciaJugador != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccionHaciaJugador), Time.deltaTime * 5f);
        }
    }

    void ProcesarAtaque()
    {
        // Verificar si el jugador está haciendo dash
        PlayerController playerScript = jugador.GetComponent<PlayerController>();
        bool jugadorEnDash = playerScript != null && playerScript.haciendoDash;

        // Evitar empujar al jugador si está haciendo dash
        if (jugadorEnDash) return;

        // Empujar al jugador si no está en cooldown
        if (Time.time >= tiempoCooldownEmpuje)
        {
            Rigidbody rbJugador = jugador.GetComponent<Rigidbody>();
            if (rbJugador != null)
            {
                if (animator != null)
                {
                    animator.SetTrigger("attack");
                }
                Vector3 direccionEmpuje = (jugador.position - transform.position).normalized;
                rbJugador.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode.Impulse);
                siguienteEmpuje = Time.time + tiempoCooldownEmpuje; // Reiniciar el cooldown de empuje
            }
        }
    }

    void MoverNuevoPunto()
    {
        Vector3 randomDirection = Random.insideUnitSphere * radioPatrulla;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radioPatrulla, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar el radio de patrulla en la escena para visualización
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccion);

        // Dibujar el radio de detección del jugador
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}
