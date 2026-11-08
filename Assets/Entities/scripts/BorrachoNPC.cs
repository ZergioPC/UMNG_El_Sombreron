using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]
public class BorrachoNPC : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private NPCIdentity identidad;

// Estados del NPC
    public enum EstadoNPC {Patrullando, Panico, Huyendo, EnRefugio}
    public EstadoNPC estadoActual = EstadoNPC.Patrullando;

//Parametros del borracho
    public float velocidadLenta = 1.8f; // Velocidad de movimiento del borracho
    public float velocidadHuida = 5f;
    public float radioZigzag = 4f; // Radio de movimiento errante para generar puntos aleatorios cercanos
    public float tiempoCambioDireccion = 1.5f; // Tiempo en segundos para cambiar de dirección errante
    private float temporizadorCambio = 0f; // Temporizador para controlar el tiempo de cambio de dirección errante

//Parametros panico/reaccion
    public float distanciaAlarma = 6f;
    public float tiempoRetardo = 1.2f;
    private Transform jugador;
    private Transform iglesiaRefugio;
    private float Reaccion = 0f;
    private float tiempoHuyendo = 0f;

//Efecto de tambaleo visual
    public float velocidadTambaleo = 4f;
    public float anguloTambaleo = 8f;       
    private Transform modeloVisual;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        identidad = GetComponent<NPCIdentity>();

        identidad.tipo = NPCIdentity.TipoNPC.Borracho;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) jugador = playerObj.transform;

        Renderer renderModelo = GetComponentInChildren<Renderer>();
        if (renderModelo != null) modeloVisual = renderModelo.transform;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        agent.speed = velocidadLenta;
        MoverNuevaposicionErrante();
    }

    void Update()
    {
        if (!agent.isOnNavMesh) return;
        if (animator != null)
        {
            animator.SetFloat("movement", agent.velocity.magnitude);
        }

        switch (estadoActual)
        {
            case EstadoNPC.Patrullando:
            PatrullaErrante();
            VerificarAlarmaJugador();
            AplicarTambaleoVisual();
            break;

            case EstadoNPC.Panico:
            agent.isStopped = true;
            Reaccion += Time.deltaTime;
            AplicarTambaleoVisual();
            if  (Reaccion >= tiempoRetardo) HuidaIglesia();
            break;

            case EstadoNPC.Huyendo:
            GestionarHuida();
            break;

            case EstadoNPC.EnRefugio:
            break;

        }
    }

    void PatrullaErrante()
    {
        temporizadorCambio += Time.deltaTime;
        if (temporizadorCambio >= tiempoCambioDireccion || agent.remainingDistance < 0.5f)
        {
            MoverNuevaposicionErrante();
            temporizadorCambio = 0f;
        }
    }

    void MoverNuevaposicionErrante()
    {
        Vector3 direccionAleatoria = Random.insideUnitSphere * radioZigzag + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioZigzag, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void VerificarAlarmaJugador()
    {
        if (jugador == null) return;
        float distanciaJugador = Vector3.Distance(transform.position, jugador.position);
        if (distanciaAlarma >= distanciaJugador)
        {
            estadoActual = EstadoNPC.Panico;
            Reaccion = 0f;
        }
    }

    void HuidaIglesia()
    {
        BuscarIglesia();
        if (iglesiaRefugio != null)
        {
            estadoActual = EstadoNPC.Huyendo;
            tiempoHuyendo = 0f;
            agent.isStopped = false;
            agent.speed = velocidadHuida;
            agent.SetDestination(iglesiaRefugio.position);
        }
        else
        {
            estadoActual = EstadoNPC.Patrullando;
            agent.isStopped = false;
            agent.speed = velocidadLenta;
        }
    }

    void GestionarHuida()
    {
        tiempoHuyendo += Time.deltaTime;
        if (tiempoHuyendo > 1f && !agent.pathPending && agent.remainingDistance <= 1f)
        {
            estadoActual = EstadoNPC.EnRefugio;
            agent.isStopped = true;
            StartCoroutine(EntrarRefugio());
        }
    }

    IEnumerator EntrarRefugio()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float tiempoTranscurrido = 0f;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        while (tiempoTranscurrido < 0.4f)
        {
            tiempoTranscurrido += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, tiempoTranscurrido / 0.4f);

            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }
            yield return null;
        }

        gameObject.SetActive(false);
    }

    void BuscarIglesia()
    {
        GameObject[] iglesias = GameObject.FindGameObjectsWithTag("Iglesia");
        float distanciaMinima = Mathf.Infinity;
        Transform iglesiaCercana = null;

        foreach (GameObject iglesia in iglesias)
        {
            float dist = Vector3.Distance(transform.position, iglesia.transform.position);
            if (dist < distanciaMinima)
            {
                distanciaMinima = dist;
                iglesiaCercana = iglesia.transform;
            }
        }
        iglesiaRefugio = iglesiaCercana;
    }

    void AplicarTambaleoVisual()
    {
        if(modeloVisual != null)
        {
            float zTilt = Mathf.Sin(Time.time * (velocidadTambaleo * 2.5f)) * (anguloTambaleo * 1.5f);
            modeloVisual.localRotation = Quaternion.identity * Quaternion.Euler(0f, 0f, zTilt);
        }
    }
}