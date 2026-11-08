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
    public float fuerzaEmpuje = 3f; // Fuerza con la que el NPC empuja al jugador
   
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
        if (Time.time >= siguienteEmpuje)
        {
            if (animator != null)
            {
                animator.SetTrigger("attack");
            }
            StartCoroutine(EsperarImpacto());
            siguienteEmpuje = Time.time + tiempoCooldownEmpuje; // Reiniciar el cooldown de empuje
        }
    }

    IEnumerator EsperarImpacto()
    {
        float tiempoImpacto = 40f/60f; 
        yield return new WaitForSeconds(tiempoImpacto); // Esperar un tiempo antes de ejecutar el empuje
        EjecutarAtaque();
    }

    void EjecutarAtaque()
    {
        if (jugador == null) return;
        float distanciaJugador = Vector3.Distance(transform.position, jugador.position);

        if (distanciaJugador <= distanciaAtaque + 0.5f)
        {
            PlayerController playerScript = jugador.GetComponent<PlayerController>();
            bool jugadorEnDash = playerScript != null && playerScript.haciendoDash;

            if (jugadorEnDash) return;

            Rigidbody rbJugador = jugador.GetComponent<Rigidbody>();
            if (rbJugador != null)
            {
                if (playerScript != null)
                {
                    playerScript.EstablecerStun(true);
                }
                rbJugador.linearVelocity = Vector3.zero;
                Vector3 direccionEmpuje = (jugador.position - transform.position).normalized;
                direccionEmpuje.y = 0.4f; // Mantener el empuje en el plano horizontal
                rbJugador.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode.Impulse);
                
                StartCoroutine(LiberarStun(playerScript, 1f)); 
            }
        }
    }

    IEnumerator LiberarStun(PlayerController playerScript, float duracion)
    {
        yield return new WaitForSeconds(duracion);
        if (playerScript != null)
        {
            playerScript.EstablecerStun(false);
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
}
