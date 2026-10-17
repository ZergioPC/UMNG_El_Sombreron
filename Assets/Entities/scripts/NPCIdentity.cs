using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCIdentity : MonoBehaviour
{
    public enum TipoNPC
{
        Borracho,   // Malo: Tambalea (+10 pts)
        Peleador,   // Malo: Ataca/Empuja (+25 pts)
        Tramposo,   // Malo: Huye rápido/desaparece (+40 pts)
        Aldeano,    // Bueno: Inocente normal (-15 pts)
        Dama        // Bueno: Inocente especial (-30 pts)
    }

    // Variables de identidad y puntuación
    public TipoNPC tipo = TipoNPC.Aldeano;
    public int puntosModificador = 15;

    // Visual
    public Color colorOculto = Color.clear;     // Todos son iguales
    public Color colorObjetivo = Color.green;  // Se revela si es (Borracho, Peleador, Tramposo)
    public Color colorInocente = Color.red;    // Se revela si es (Aldeano real o Dama)

    public float duracionRevelacion = 5f; // Duración de la revelación en segundos

    private Renderer[] renderers;
    private bool estaRevelado = false;
    private Coroutine corrutinaOcultar;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        ActualizarPuntosSegunRol();     
    }

    void ActualizarPuntosSegunRol()
    {
        switch (tipo)
        {
            case TipoNPC.Borracho: puntosModificador = 10; break;
            case TipoNPC.Peleador: puntosModificador = 25; break;
            case TipoNPC.Tramposo: puntosModificador = 40; break;
            case TipoNPC.Aldeano:  puntosModificador = 15; break;
            case TipoNPC.Dama:     puntosModificador = 30; break;
        }
    }

    public bool esCulpable()
    {
        return
        tipo == TipoNPC.Borracho ||
        tipo == TipoNPC.Peleador ||
        tipo == TipoNPC.Tramposo;
    }

    public void RevelarNPC()
    {
        if(corrutinaOcultar != null)
        {
            StopCoroutine(corrutinaOcultar);
        }

        estaRevelado = true;
        Color colorRevelado = esCulpable() ? colorObjetivo : colorInocente;
        AplicarColor(colorRevelado);

        corrutinaOcultar = StartCoroutine(OcultarDespuesDeTiempo());
    }

    IEnumerator OcultarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(duracionRevelacion);
        AplicarColor(colorOculto);
        estaRevelado = false;
        corrutinaOcultar = null;
    }

    private void AplicarColor(Color nuevoColor)
    {
        if (renderers != null && renderers.Length > 0)
        {
            foreach (Renderer rend in renderers)
            {
                rend.material.color = nuevoColor;
            }
        }
    }
}