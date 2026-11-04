using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]

public class NPCBehavior : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;


//Configuración de movimiento
    public float radioPatrulla = 30f; // Radio de patrulla alrededor del punto inicial
    public float tiempoCambioDestino = 5f; // Tiempo entre cambios de destino
    private float temporizadorDestino = 0f; // Temporizador para cambiar de destino


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ElegirNuevoDestino();
    }       

    void Update()
    {
        PatrullaNormal();
    }

    // Método para patrullar de manera normal
    protected void PatrullaNormal()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (animator != null)
        {
            animator.SetFloat("movement", agent.velocity.magnitude);
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            temporizadorDestino += Time.deltaTime;
            if (temporizadorDestino >= tiempoCambioDestino)
            {
                ElegirNuevoDestino();
                temporizadorDestino = 0f;
            }
        }
    }
    
    // Método para elegir un nuevo destino de patrulla
    protected void ElegirNuevoDestino()
    {
        Vector3 nuevoDestino = transform.position + Random.insideUnitSphere * radioPatrulla;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(nuevoDestino, out hit, radioPatrulla, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
