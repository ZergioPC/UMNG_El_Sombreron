using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamaraController : MonoBehaviour
{
    public Transform Player; // Referencia al transform del jugador
    public Vector3 offset; // Desplazamiento de la cámara respecto al jugador
    public float smoothSpeed = 10f; // Velocidad de suavizado del movimiento de la cámara

    public float velocidadTransicion = 5f; // Velocidad de transición entre posiciones de la cámara
    private Transform targetActual; // Referencia a la posición actual de la cámara

    private void Start()
    {
        targetActual = Player; // Inicializar la posición actual de la cámara con la posición del jugador
    }

    void LateUpdate()
    {
        if (targetActual == null)
        {
            return;
        }
        Vector3 posicionDeseada = targetActual.position + offset; // Calcular la posición deseada de la cámara

        transform.position = Vector3.Lerp(transform.position, posicionDeseada, smoothSpeed); // Actualizar la posición de la cámara
    }
    public void CambiarTarget(Transform nuevoObjetivo)
{
    if (nuevoObjetivo != null)
    {
        targetActual = nuevoObjetivo;
        Debug.Log($"Cámara siguiendo a: {nuevoObjetivo.name}");
    }
    else
    {
        targetActual = Player;
    }
}
}
