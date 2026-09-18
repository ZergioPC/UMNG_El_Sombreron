using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]

public class NPCMovement : MonoBehaviour
{
    private NPCIdentity identity;
    private NavMeshAgent agent;
    private Transform jugador;

// parametros de comportamiento
    public float radioPatrulla = 15f;
    public float distanciaEmpuje = 3f;
    public float fuerzaEmpuje = 10f;

    private float timeZigzag = 0f;

    void Start()
    {
        identity = GetComponent<NPCIdentity>();
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        agent.isStopped = false;
        MoverNuevoPunto();
    }

    void Update()
    {
        if (jugador == null) return;
        if (!agent.isOnNavMesh) return;

        switch (identity.tipo)
        {
            case NPCIdentity.TipoNPC.Borracho:
                ComportamientoBorracho();
                break;

            case NPCIdentity.TipoNPC.Tramposo:
                ComportamientoTramposo();
                break;

            case NPCIdentity.TipoNPC.Peleador:
                ComportamientoPeleador();
                break;

            case NPCIdentity.TipoNPC.Aldeano:
            case NPCIdentity.TipoNPC.Dama:
                ComportamientoAldeano();
                break;
        }
    }

    void ComportamientoBorracho()
    {
        agent.speed = 2f;
        timeZigzag += Time.deltaTime;
        
        // Direccion errante cada 2 segundos
        if (timeZigzag >= 2f || agent.remainingDistance < 0.5f)
        {
            MoverNuevoPunto();
            timeZigzag = 0f;
        }
    }

    void ComportamientoTramposo()
    {
        agent.speed = 6f;
        float distanciaAlJugador = Vector3.Distance(transform.position, jugador.position);
        
        if (distanciaAlJugador < 8f)
        {
            Vector3 dirrecionHuida = transform.position - jugador.position;
            Vector3 destinoHuida = transform.position + dirrecionHuida.normalized * 5f;
            agent.SetDestination(destinoHuida);
        }
        else if (agent.remainingDistance < 0.5f)
        {
            MoverNuevoPunto();
        }
    }

    void ComportamientoPeleador()
    {
        agent.speed = 4f;
        float distanciaJugador = Vector3.Distance(transform.position, jugador.position);

        if (distanciaJugador < distanciaEmpuje)
        {
            PlayerController playerScript = jugador.GetComponent<PlayerController>();
            bool jugadorEnDash = playerScript != null && playerScript.haciendoDash;

            Vector3 direccionHaciaJugador = (jugador.position - transform.position).normalized;

            if (jugadorEnDash)
            {
                Debug.Log("¡Sorprendiste al Peleador! Quedó aturdido y no pudo empujarte.");
                Debug.Log("Peleador atrapado");
                agent.isStopped = true;
                return;
            }

            Rigidbody rbJugador = jugador.GetComponent<Rigidbody>();
            if (rbJugador != null)
            {
                Vector3 direccionEmpuje = (jugador.position - transform.position).normalized + Vector3.up * 0.5f;
                rbJugador.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode.Impulse);
                Debug.Log("¡El Peleador te mando a monserrate");
            }
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

    void ComportamientoAldeano()
    {
        agent.speed = 4f;
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            MoverNuevoPunto();
        }
        
    }

    void MoverNuevoPunto()
    {
        Vector3 puntoAleatorio = Random.insideUnitSphere * radioPatrulla + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, radioPatrulla, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
