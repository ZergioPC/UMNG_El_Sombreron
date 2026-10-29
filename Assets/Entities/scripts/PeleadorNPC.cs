using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]
public class PeleadorNPC : MonoBehaviour
{
// Referencias a componentes
    private NPCIdentity identity;
    private NavMeshAgent agent;
    private Transform jugador;

// parametros de patrulla
    public float radioPatrulla = 3f;
    public float velocidadPatrulla = 1.5f;

// parametros de comportamiento del peleador
    public float distanciaEmpuje = 3f;
    public float fuerzaEmpuje = 2f;
    public float tiempoEmpuje = 3f;
    public float tiempoCooldownEmpuje = 1f;


    void Start()
    {
        // Obtener referencias a los componentes necesarios
        identity = GetComponent<NPCIdentity>();
        agent = GetComponent<NavMeshAgent>();

        //Forzar que el tipo de NPC sea Peleador
        identity.tipo = NPCIdentity.TipoNPC.Peleador; 

        // Encontrar al jugador en la escena
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }

        // Asegurarse de que el NPC esté en la malla de navegación
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        // Iniciar el movimiento del NPC
        agent.speed = velocidadPatrulla;
        agent.isStopped = false;
        MoverNuevoPunto();
    }

    // Update is called once per frame
    void Update()
    {
        // Asegurarse de que el jugador y el agente estén disponibles
        if (jugador == null || !agent.isOnNavMesh) return;
        float distanciaJugador = Vector3.Distance(transform.position, jugador.position);

        // Comportamiento del peleador
        if (distanciaJugador < distanciaEmpuje)
        {
            EncararJugador();
            ProcesarAtaque();
        }
        else
        {
            agent.isStopped = false;
            if (agent.remainingDistance < 0.5f)
            {
                MoverNuevoPunto();
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
        if (jugadorEnDash)
        {
            agent.isStopped = true;
            return;
        }

        // Empujar al jugador si no está en cooldown
        if (Time.time >= tiempoCooldownEmpuje + tiempoEmpuje)
        {
            Rigidbody rbJugador = jugador.GetComponent<Rigidbody>();
            if (rbJugador != null)
            {
                // Empujar al jugador hacia atrás y un poco hacia arriba
                Vector3 direccionEmpuje = (jugador.position - transform.position).normalized + Vector3.up * 0.3f;
                rbJugador.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode.Impulse);
                
                // Reiniciar el cooldown del empuje
                tiempoCooldownEmpuje = Time.time + tiempoEmpuje;
            }
        }
        
        // Detener el movimiento del NPC mientras empuja
        agent.isStopped = false;
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
