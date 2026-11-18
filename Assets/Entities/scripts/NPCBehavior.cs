using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]

public class NPCBehavior : MonoBehaviour
{
    public enum EstadoNPC {Patrullando, Huiendo, EnRefugio}
    public EstadoNPC estadoActual = EstadoNPC.Patrullando;

    private Transform jugandor;
    private NavMeshAgent agent;
    private Animator animator;


//Configuración de movimiento
    public float radioPatrulla = 30f; // Radio de patrulla alrededor del punto inicial
    public float tiempoCambioDestino = 5f; // Tiempo entre cambios de destino
    private float temporizadorDestino = 0f; // Temporizador para cambiar de destino

//Configuración de pánico/Huida
    public float distanciaDeteccion = 10f; // Distancia a la que el NPC detecta al jugador
    public float velocidadHuida = 5f; // Velocidad de huida del NPC
    public float velocidadNormal = 3.5f; // Velocidad normal del NPC
    public float tiempoHuyendo = 0f; // Tiempo que el NPC permanece huyendo antes de buscar refugio
    private Transform casaMasCercana; // Referencia a la casa más cercana para refugiarse

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        PlayerController player = FindAnyObjectByType<PlayerController>();        if (player != null)
        {
            jugandor = player.transform;
        }
        agent.speed = velocidadNormal;
        ElegirNuevoDestino();
    }       

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        if (animator != null)
        {
            animator.SetFloat("movement", agent.velocity.magnitude);
        }

        //Comportamiento egun etado actual
        switch (estadoActual)
        {
            case EstadoNPC.Patrullando:
                PatrullaNormal();
                VerificarDeteccionJugador();
                break;

            case EstadoNPC.Huiendo:
                Huir();
                break;

            case EstadoNPC.EnRefugio:
                break;
        }
    }

    // Método para patrullar de manera normal
    protected void PatrullaNormal()
    {
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

    void VerificarDeteccionJugador()
    {
        if (jugandor == null) return;

        float distanciaJugador = Vector3.Distance(transform.position, jugandor.position);
        if (distanciaJugador <= distanciaDeteccion) IniciarHuida();
    }

    protected void IniciarHuida()
    {
        BuscarCasaMasCercana();
        if (casaMasCercana != null)
        {
            estadoActual = EstadoNPC.Huiendo;
            tiempoHuyendo = 0f;
            agent.speed = velocidadHuida;
            agent.SetDestination(casaMasCercana.position);
        }

    }

    void Huir()
    {
        tiempoHuyendo += Time.deltaTime;
        if (tiempoHuyendo >= 10f && !agent.pathPending && agent.remainingDistance <= 0.8f)
        {
            estadoActual = EstadoNPC.EnRefugio;
            agent.isStopped = true;

            StartCoroutine(EsperarEnRefugio());
        }
    }

    IEnumerator EsperarEnRefugio()
    {
        agent.isStopped = true;
        float tiempoDesvanecimiento = 0.5f;
        float tiempoTranscurrido = 0f;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        while (tiempoTranscurrido < tiempoDesvanecimiento)
        {
            tiempoTranscurrido += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, tiempoTranscurrido / tiempoDesvanecimiento);

            foreach (Renderer renderer in renderers)
            {
                foreach(Material mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                }
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }

    void BuscarCasaMasCercana()
    {
        GameObject[] casas = GameObject.FindGameObjectsWithTag("Casa");
        float distanciaMinima = Mathf.Infinity;
        Transform casaCercana = null;

        foreach (GameObject casa in casas)
        {
            float distancia = Vector3.Distance(transform.position, casa.transform.position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                casaCercana = casa.transform;
            }
        }
        casaMasCercana = casaCercana;
    }
}