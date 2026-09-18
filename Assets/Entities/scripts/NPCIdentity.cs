using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCIdentity : MonoBehaviour
{
    public enum TipoNPC
{
        Borracho,   // Malo: Tambalea (+10 pts)
        Peleador,   // Malo: Ataca/Empuja (+25 pts)
        Tramposo,   // Malo: Huye rápido a casas (+40 pts)
        Aldeano,    // Bueno: Inocente normal (-15 pts)
        Dama        // Bueno: Inocente especial (-30 pts)
    }

    public TipoNPC tipo = TipoNPC.Aldeano;
    public int puntosModificador = 15;

    // Visual
    public Color colorOculto = Color.gray;     // Todos son iguales
    public Color colorObjetivo = Color.green;  // Se revela si es (Borracho, Peleador, Tramposo)
    public Color colorInocente = Color.red;    // Se revela si es (Aldeano real o Dama)

    private Renderer render;
    private bool estaRevelado = false;

    void Start()
    {
        render = GetComponent<Renderer>();
        ActualizarPuntosSegunRol();

        if (render != null)
        {
            render.material.color = colorOculto;
        }        
    }

    // Update is called once per frame
    void Update()
    {

    }
    void ActualizarPuntosSegunRol()
    {
        switch (tipo)
        {
            case TipoNPC.Borracho:puntosModificador = 10; break;
            case TipoNPC.Peleador:puntosModificador = 25; break;
            case TipoNPC.Tramposo:puntosModificador = 40; break;
            case TipoNPC.Aldeano:puntosModificador = 15; break;
            case TipoNPC.Dama:puntosModificador = 30; break;
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
        Renderer render = GetComponent<Renderer>();
        if (render != null)
        {
            // Cambia a Verde si es culpable o Rojo si es inocente
            render.material.color = esCulpable() ? Color.green : Color.red;
        }
    }

}
