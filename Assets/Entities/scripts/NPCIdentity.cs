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
    public Color colorObjetivo = new Color(1f, 0.2663138f, 0f);  // Se revela si es (Borracho, Peleador, Tramposo)
    public Color colorInocente = new Color(0.1320967f, 0.3342422f, 0.5084495f);    // Se revela si es (Aldeano real o Dama)

    public float duracionRevelacion = 5f; // Duración de la revelación en segundos

    // Variables de rastro
    public GameObject rastroVisual;
    public float duracionRastro = 3f; // Duración del rastro en segundos
    public float intervaloRastro = 0.5f; // Intervalo entre cada rastro en segundos
    private Vector3 ultimaPosicionRastro;

    // Variables de estado
    private Renderer[] renderers;
    private bool estaRevelado = false;
    private Coroutine corrutinaOcultar;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        ActualizarPuntosSegunRol();
        ultimaPosicionRastro = transform.position; 
    }

    void Update()
    {
        //Solo los NPCs culpables dejan rastro visual
        if (esCulpable() && rastroVisual != null)
        {
            //si el NPC se mueve lo suficiente, se genera un rastro visual
            if (Vector3.Distance(transform.position, ultimaPosicionRastro) >= 1f)
            {
                duracionRastro += Time.deltaTime;
                if (duracionRastro >= intervaloRastro)
                {
                    Instantiate(rastroVisual, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                    duracionRastro = 0f;
                }
            }
            ultimaPosicionRastro = transform.position;
        }
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