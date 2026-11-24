using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerroController : MonoBehaviour
{
    public static bool estaOlfateando = false;

    // Referencias a componentes del perro
    private Animator animator;
    private Rigidbody rb;
    public Transform jugador;

    // VFX del perro
    public ParticleSystem vfxOlfateo;
    public ParticleSystem vfxAparicion;
    public float duracionVFXAparicion = 0.5f;
    private Coroutine rutinaVFXAparicion;

    // Variables de seguimiento del jugador
    private bool estaActivo = false;
    public Vector3 offsetSeguimiento = new Vector3(-1.5f, 0, -1.5f);
    public float velocidadSeguimiento = 10f;

    // Variables de movimiento del perro
    public float velocidad = 8f;
    public float rotacionVelocidad = 10f;
    public float rangoDeteccion = 15f;
    public LayerMask capaNPC;

    // Olfato / detección
    public float cooldownOlfateo = 3f;
    public float duracionOlfateoRastro = 3f;

    private Vector3 forward, right; // Vectores de dirección para el movimiento del perro
    private Vector3 inputMovimiento;
    private float proximoOlfateo = 0f;
    private Coroutine rutinaOlfateo;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        DesactivarPerro();
    }

    private void Update()
    {
        if (estaActivo)
        {
            ActualizarCamara();
            LeerEntradaMovimiento();
            BuscarNPCs();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (estaActivo)
        {
            ProcesarMovimientoPerro();
        }
        else
        {
            SeguirJugador();
        }
    }

    void ActualizarCamara()
    {
        if (Camera.main != null)
        {
            forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();

            right = Camera.main.transform.right;
            right.y = 0;
            right.Normalize();
        }
    }

    void LeerEntradaMovimiento()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        inputMovimiento = (forward * moveVertical + right * moveHorizontal);
        if (inputMovimiento.magnitude > 1f)
        {
            inputMovimiento.Normalize();
        }

        if (animator != null)
        {
            animator.SetFloat("movement", inputMovimiento.magnitude);
        }
    }

    void SeguirJugador()
    {
        if (jugador == null) return;

        Vector3 posicionDeseada = jugador.position + jugador.TransformDirection(offsetSeguimiento);
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, velocidadSeguimiento * Time.fixedDeltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, jugador.rotation, velocidadSeguimiento * Time.fixedDeltaTime);
    }

    public void ActivarPerro(Vector3 puntoInvocacion)
    {
        transform.position = puntoInvocacion;
        if (vfxAparicion != null)
        {
            vfxAparicion.transform.position = puntoInvocacion;
            if (!vfxAparicion.gameObject.activeSelf) vfxAparicion.gameObject.SetActive(true);
            vfxAparicion.Clear(true);
            vfxAparicion.Play(true);

            if (rutinaVFXAparicion != null) StopCoroutine(rutinaVFXAparicion);
            rutinaVFXAparicion = StartCoroutine(DetenerVFXAparicion());
        }


        estaActivo = true;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }

        ActualizarCamara();
    }

    IEnumerator DetenerVFXAparicion()
    {
        yield return new WaitForSeconds(duracionVFXAparicion);
        if (vfxAparicion != null)
        {
            vfxAparicion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        rutinaVFXAparicion = null;
    }

    public void DesactivarPerro()
    {
        estaActivo = false;
        estaOlfateando = false;
        inputMovimiento = Vector3.zero;

        if (vfxOlfateo != null)
        {
            vfxOlfateo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (rutinaOlfateo != null)
        {
            StopCoroutine(rutinaOlfateo);
            rutinaOlfateo = null;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }

        if (animator != null)
        {
            animator.SetFloat("movement", 0f);
        }
    }

    void ProcesarMovimientoPerro()
    {
        rb.linearVelocity = new Vector3(inputMovimiento.x * velocidad, rb.linearVelocity.y, inputMovimiento.z * velocidad);

        if (inputMovimiento != Vector3.zero)
        {
            Quaternion nuevaRotacion = Quaternion.LookRotation(inputMovimiento);
            transform.rotation = Quaternion.Slerp(transform.rotation, nuevaRotacion, Time.fixedDeltaTime * rotacionVelocidad);
        }
    }

    // Activado con clic izquierdo desde PlayerController mientras el perro está invocado
    public void Olfatear()
    {
        if (!estaActivo) return;

        if (vfxOlfateo != null)
        {
            if (!vfxOlfateo.gameObject.activeSelf) vfxOlfateo.gameObject.SetActive(true);
            vfxOlfateo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfxOlfateo.Play(true);
        }

        if (Time.time >= proximoOlfateo)
        {
            proximoOlfateo = Time.time + cooldownOlfateo;

            if (animator != null)
            {
                animator.SetTrigger("smell");
            }

            if (rutinaOlfateo != null)
            {
                StopCoroutine(rutinaOlfateo);
            }
            rutinaOlfateo = StartCoroutine(ActivarOlfateoRastro(duracionOlfateoRastro));
        }
    }

    void BuscarNPCs()
    {
        Collider[] npcsEncontrados = Physics.OverlapSphere(transform.position, rangoDeteccion, capaNPC);

        foreach (Collider npcCollider in npcsEncontrados)
        {
            NPCIdentity npc = npcCollider.GetComponentInParent<NPCIdentity>();
            if (npc != null)
            {
                npc.RevelarNPC();
            }
        }
    }

    IEnumerator ActivarOlfateoRastro(float duracion)
    {
        estaOlfateando = true;
        yield return new WaitForSeconds(duracion);
        estaOlfateando = false;
        rutinaOlfateo = null;
    }
}