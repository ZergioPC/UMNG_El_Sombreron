using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]
public class BorrachoNPC : MonoBehaviour
{
    private NavMeshAgent agent;
    private NPCIdentity identidad;

    [Header("Parámetros del Borracho")]
    public float velocidadLenta = 1.8f;      // Camina muy despacio
    public float radioZigzag = 4f;           // Radio corto para cambiar de dirección seguido
    public float tiempoCambioDireccion = 1.5f; // Cambia de rumbo frecuentemente
    private float temporizadorCambio = 0f;

    [Header("Efecto de Bamboleo / Tambaleo Visual")]
    public float velocidadTambaleo = 4f;     // Qué tan rápido se balancea
    public float anguloTambaleo = 8f;        // Grados de inclinación a los lados
    private Transform modeloVisual;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        identidad = GetComponent<NPCIdentity>();

        // Forzar tipo a Borracho en la identidad
        identidad.tipo = NPCIdentity.TipoNPC.Borracho;

        // Intentar obtener el hijo donde está el modelo 3D para inclinarlo visualmente
        if (transform.childCount > 0)
        {
            modeloVisual = transform.GetChild(0);
        }

        // Posicionar en el NavMesh
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

        // 1. Lógica de movimiento en zigzag / errante
        temporizadorCambio += Time.deltaTime;

        if (temporizadorCambio >= tiempoCambioDireccion || agent.remainingDistance < 0.5f)
        {
            MoverNuevaposicionErrante();
            temporizadorCambio = 0f;
        }

        // 2. Tambaleo visual (inclinación lateral para simular borrachera)
        AplicarTambaleoVisual();
    }

    void MoverNuevaposicionErrante()
    {
        // Genera un punto aleatorio muy cercano para cambiar de dirección bruscamente
        Vector3 direccionAleatoria = Random.insideUnitSphere * radioZigzag + transform.position;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radioZigzag, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void AplicarTambaleoVisual()
    {
        // Si el borracho se está moviendo, balanceamos suavemente el modelo visual
        if (agent.velocity.magnitude > 0.1f && modeloVisual != null)
        {
            float zTilt = Mathf.Sin(Time.time * velocidadTambaleo) * anguloTambaleo;
            modeloVisual.localRotation = Quaternion.Euler(0, modeloVisual.localRotation.eulerAngles.y, zTilt);
        }
    }
}