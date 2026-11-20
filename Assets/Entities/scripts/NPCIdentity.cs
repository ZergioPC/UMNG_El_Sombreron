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
    public Color colorObjetivo = new Color(1f, 0.2663138f, 0f);  // Se revela si es (Borracho, Peleador, Tramposo)
    public Color colorInocente = new Color(0.1320967f, 0.3342422f, 0.5084495f);    // Se revela si es (Aldeano real o Dama)

    public float duracionRevelacion = 5f; // Duración de la revelación en segundos

    // Variables de rastro
    public GameObject rastroVisual;
    public float duracionVidaRastro = 3f; // Tiempo que permanece cada instancia del rastro antes de destruirse
    public float intervaloRastro = 0.5f;  // Tiempo mínimo entre instanciar un rastro y otro
    public float distanciaMinimaRastro = 1f;
    private Vector3 ultimaPosicionRastro;
    private float temporizadorRastro = 0f;

    // Variables de estado
    private Renderer[] renderers;
    private Color[] coloresOriginales;
    private bool estaRevelado = false;
    private Coroutine corrutinaOcultar;
    private TramposoNPC tramposoScript;
    private PerroController perroController;
    public bool EstaRevelado => estaRevelado;

    public float alturaRastro = 0.15f;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        tramposoScript = GetComponent<TramposoNPC>();
        perroController = FindAnyObjectByType<PerroController>();

        GuardarColoresOriginales();
        ActualizarPuntosSegunRol();
        ultimaPosicionRastro = transform.position;
    }

    void GuardarColoresOriginales()
    {
        if (renderers == null) return;

        coloresOriginales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material != null)
            {
                coloresOriginales[i] = renderers[i].material.color;
            }
        }
    }

    void Update()
    {
        // Si está revelado, verificar si salió de la vista/rango del perro o si el perro se desactivó
        if (estaRevelado && !EstaEnVistaDelPerro())
        {
            RestaurarVisualNormal();
        }

        // Solo los NPCs culpables (y que no sean clon de Tramposo) dejan rastro cuando el perro olfatea
        bool puedeDejarRastro = esCulpable()
            && PerroController.estaOlfateando
            && (tramposoScript == null || !tramposoScript.esClon);

        if (puedeDejarRastro && rastroVisual != null)
        {
            temporizadorRastro += Time.deltaTime;

            if (temporizadorRastro >= intervaloRastro && Vector3.Distance(transform.position, ultimaPosicionRastro) >= distanciaMinimaRastro)
            {
                Vector3 direccionRastro = transform.position - ultimaPosicionRastro;
                direccionRastro.y = 0f;

                Quaternion rotacionRastro = direccionRastro.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direccionRastro.normalized)
                    : transform.rotation;

                GameObject instanciaRastro = Instantiate(rastroVisual, transform.position + Vector3.up * alturaRastro, rotacionRastro);

                if (duracionVidaRastro > 0f)
                {
                    Destroy(instanciaRastro, duracionVidaRastro);
                }

                ultimaPosicionRastro = transform.position;
                temporizadorRastro = 0f;
            }
        }
    }

    bool EstaEnVistaDelPerro()
    {
        if (perroController == null)
        {
            perroController = FindAnyObjectByType<PerroController>();
            if (perroController == null) return false;
        }

        // Verificar si el perro está activo (sus renderers habilitados y dentro del rango de detección)
        Renderer perroRenderer = perroController.GetComponentInChildren<Renderer>();
        bool perroVisible = perroRenderer != null && perroRenderer.enabled;
        if (!perroVisible) return false;

        float distancia = Vector3.Distance(transform.position, perroController.transform.position);
        return distancia <= perroController.rangoDeteccion;
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
        if (corrutinaOcultar != null)
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
        RestaurarVisualNormal();
    }

    public void RestaurarVisualNormal()
    {
        if (corrutinaOcultar != null)
        {
            StopCoroutine(corrutinaOcultar);
            corrutinaOcultar = null;
        }

        estaRevelado = false;

        if (renderers != null && coloresOriginales != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material != null && i < coloresOriginales.Length)
                {
                    renderers[i].material.color = coloresOriginales[i];
                }
            }
        }
    }

    private void AplicarColor(Color nuevoColor)
    {
        if (renderers != null && renderers.Length > 0)
        {
            foreach (Renderer rend in renderers)
            {
                if (rend != null && rend.material != null)
                {
                    rend.material.color = nuevoColor;
                }
            }
        }
    }
}