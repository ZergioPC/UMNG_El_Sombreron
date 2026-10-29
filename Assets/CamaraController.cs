using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamaraController : MonoBehaviour
{
    [Header("Objetivos y Posición")]
    public Transform Player; // Referencia al transform del jugador
    public Vector3 offset = new Vector3(0, 8, -8); // Desplazamiento por defecto para vista isométrica
    
    [Header("Suavizado de Movimiento")]
    public float smoothSpeed = 5f; // Velocidad de suavizado del movimiento de la cámara

    private Transform targetActual; // Referencia a la posición actual de la cámara

    private void Start()
    {
        if (Player != null)
        {
            targetActual = Player; // Inicializar con el jugador
        }
    }

    void LateUpdate()
    {
        if (targetActual == null) return;

        // Calcular la posición deseada de la cámara
        Vector3 posicionDeseada = targetActual.position + offset;

        // Movimiento fluido cuadro a cuadro
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, smoothSpeed * Time.deltaTime);
    }

    public void CambiarTarget(Transform nuevoObjetivo)
    {
        if (nuevoObjetivo != null)
        {
            targetActual = nuevoObjetivo;
        }
        else
        {
            targetActual = Player;
        }
    }
}