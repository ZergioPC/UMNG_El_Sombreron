using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NPCIdentity))]
public class TramposoNPC : MonoBehaviour
{
    private NavMeshAgent agent;
    private NPCIdentity identidad;
    private Animator animator;
    private Transform jugador;

    [Header("Parámetros de huida")]
    public float velocidadTranquilo = 2.5f;
    public float velocidadHuida = 7f;
    public float radioAlerta = 8f;         // Distancia a la que detecta al Sombrerón y empieza a huir
    public float distanciaEscape = 2.5f;   // Si el Sombrerón se acerca sin Dash a esta distancia, activa el truco

    [Header("Bomba de humo y duplicación")]
    public GameObject efectoHumoPrefab;    // Sistema de partículas de humo al lanzar
    public GameObject clonTramposoPrefab;  // Prefab idéntico del Tramposo para duplicarse
    public int fotogramaLanzamiento = 60;  // Fotograma en el que inicia el humo y la clonación
    public float fallbackTiempoHumo = 1f;  // Tiempo de respaldo si no encuentra el clip de ataque
    public float duracionHuidaDoble = 2f;  // Tiempo que corren separados tras lanzar la bomba
    public bool esClon = false;            // True en el duplicado, false en el original

    [Header("Captura y VFX de clon")]
    public GameObject vfxCapturaClonPrefab; // VFX que sale si atrapas al clon en vez del original
    public float duracionVfxClon = 2.5f;    // Tiempo antes de destruir el VFX del clon

    // Mientras lanza la bomba quieto, no es capturable
    public bool PuedeSerCapturado => !usandoHumo;
    public bool OtorgaPuntos => !esClon;

    private bool aturdido = false;
    private bool huyendo = false;
    private bool usandoHumo = false;
    private bool yaClono = false;
    private bool humoYaEjecutado = false;
    private bool tieneParametroMovement = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        identidad = GetComponent<NPCIdentity>();
        animator = GetComponent<Animator>();

        identidad.tipo = NPCIdentity.TipoNPC.Tramposo;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "movement" && param.type == AnimatorControllerParameterType.Float)
                {
                    tieneParametroMovement = true;
                    break;
                }
            }
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            jugador = playerObj.transform;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        agent.speed = velocidadTranquilo;

        // Si nace ya como clon, empieza moviéndose para no quedarse clavado
        if (esClon)
        {
            yaClono = true;
            MoverNuevoPunto();
        }
        else
        {
            MoverNuevoPunto();
        }
    }

    void Update()
    {
        if (jugador == null || !agent.isOnNavMesh) return;

        if (animator != null && tieneParametroMovement)
        {
            float velAnim = aturdido || usandoHumo ? 0f : agent.velocity.magnitude;
            animator.SetFloat("movement", velAnim);
        }

        // Mientras lanza la bomba está quieto y no es capturable; si está aturdido, tampoco actúa
        if (aturdido || usandoHumo) return;

        float distanciaAlJugador = Vector3.Distance(transform.position, jugador.position);
        PlayerController playerScript = jugador.GetComponent<PlayerController>();
        bool jugadorEnDash = playerScript != null && playerScript.haciendoDash;

        // 1. SI EL JUGADOR USA DASH CERCA: Se aturde por sorpresa
        if (jugadorEnDash && distanciaAlJugador <= radioAlerta)
        {
            StartCoroutine(Aturdir());
            return;
        }

        // 2. SI EL JUGADOR SE ACERCA DEMASIADO SIN DASH: Se queda quieto, lanza humo y se duplica en frame 60
        if (distanciaAlJugador <= distanciaEscape && !esClon && !yaClono)
        {
            StartCoroutine(LanzarHumoYDuplicar());
            return;
        }

        // 3. SI DETECTA AL JUGADOR EN RADIO DE ALERTA: Corre para huir (tanto el original como el clon)
        if (distanciaAlJugador <= radioAlerta)
        {
            HuirDelJugador();
        }
        else
        {
            if (huyendo)
            {
                huyendo = false;
                agent.speed = velocidadTranquilo;
            }

            if (!agent.hasPath || agent.remainingDistance < 0.8f)
            {
                MoverNuevoPunto();
            }
        }
    }

    void HuirDelJugador()
    {
        huyendo = true;
        agent.speed = velocidadHuida;

        Vector3 direccionOpuesta = (transform.position - jugador.position).normalized;
        Vector3 puntoHuida = transform.position + direccionOpuesta * 6f;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoHuida, out hit, 6f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    IEnumerator LanzarHumoYDuplicar()
    {
        usandoHumo = true;
        yaClono = true;
        humoYaEjecutado = false;

        // Se queda completamente quieto mientras lanza la bomba (y no es capturable)
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Vector3 dirJugador = jugador.position - transform.position;
        dirJugador.y = 0f;
        if (dirJugador != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dirJugador);
        }

        if (animator != null)
        {
            if (tieneParametroMovement)
            {
                animator.SetFloat("movement", 0f);
            }
            animator.SetTrigger("attack");
        }

        float tiempoEspera = CalcularTiempoHastaFotograma(fotogramaLanzamiento);
        float transcurrido = 0f;

        while (transcurrido < tiempoEspera && !humoYaEjecutado)
        {
            transcurrido += Time.deltaTime;
            yield return null;
        }

        if (!humoYaEjecutado)
        {
            EjecutarEfectoHumoYClonacion();
        }

        yield return new WaitForSeconds(duracionHuidaDoble);
        usandoHumo = false;
    }

    float CalcularTiempoHastaFotograma(int frameObjetivo)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name.ToLower().Contains("attack"))
                {
                    float fps = clip.frameRate > 0f ? clip.frameRate : 60f;
                    return Mathf.Max(0f, frameObjetivo / fps);
                }
            }
        }

        return fallbackTiempoHumo;
    }

    // Puede llamarse desde un Animation Event en el frame 60 del clip
    public void EventoLanzarHumoYClonacion()
    {
        if (!usandoHumo || humoYaEjecutado) return;
        EjecutarEfectoHumoYClonacion();
    }

    void EjecutarEfectoHumoYClonacion()
    {
        humoYaEjecutado = true;

        if (efectoHumoPrefab != null)
        {
            Instantiate(efectoHumoPrefab, transform.position, Quaternion.identity);
        }

        Vector3 dirLejosJugador = jugador != null ? (transform.position - jugador.position).normalized : transform.forward;
        dirLejosJugador.y = 0f;
        if (dirLejosJugador == Vector3.zero) dirLejosJugador = transform.forward;

        // Direcciones separadas para que original y clon salgan moviéndose en rumbos distintos
        Vector3 dirOriginal = (dirLejosJugador - transform.right * 0.85f).normalized;
        Vector3 dirClon = (dirLejosJugador + transform.right * 0.85f).normalized;

        GameObject clonObj = null;
        if (clonTramposoPrefab != null)
        {
            clonObj = Instantiate(clonTramposoPrefab, transform.position, transform.rotation);
            TramposoNPC clonScript = clonObj.GetComponent<TramposoNPC>();
            if (clonScript != null)
            {
                clonScript.esClon = true;
                clonScript.yaClono = true;
                clonScript.usandoHumo = false; // El clon ya sale moviéndose, no bloqueado lanzando bomba
                clonScript.huyendo = true;
            }
        }

        // Mover al original
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            huyendo = true;
            agent.speed = velocidadHuida;
            EnviarEnDireccion(agent, dirOriginal, 8f);
        }

        // Mover al clon
        if (clonObj != null)
        {
            NavMeshAgent clonAgent = clonObj.GetComponent<NavMeshAgent>();
            if (clonAgent != null)
            {
                NavMeshHit hitClon;
                if (NavMesh.SamplePosition(transform.position, out hitClon, 3f, NavMesh.AllAreas))
                {
                    clonAgent.Warp(hitClon.position);
                }
                clonAgent.isStopped = false;
                clonAgent.speed = velocidadHuida;
                EnviarEnDireccion(clonAgent, dirClon, 8f);
            }
        }
    }

    void EnviarEnDireccion(NavMeshAgent targetAgent, Vector3 direccion, float distancia)
    {
        if (targetAgent == null || !targetAgent.isOnNavMesh) return;

        Vector3 destino = transform.position + direccion.normalized * distancia;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destino, out hit, distancia, NavMesh.AllAreas))
        {
            targetAgent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// Llama este método cuando el jugador intente capturar al Tramposo.
    /// Devuelve true si la captura fue válida (no estaba lanzando bomba).
    /// </summary>
    public bool IntentarCapturar(out bool dioPuntos)
    {
        dioPuntos = false;

        if (!PuedeSerCapturado)
        {
            return false;
        }

        if (esClon)
        {
            if (vfxCapturaClonPrefab != null)
            {
                GameObject vfx = Instantiate(vfxCapturaClonPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, duracionVfxClon);
            }
            dioPuntos = false;
        }
        else
        {
            dioPuntos = true;
        }

        Destroy(gameObject);
        return true;
    }

    IEnumerator Aturdir()
    {
        aturdido = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(2.5f);

        if (this != null)
        {
            agent.isStopped = false;
            aturdido = false;
        }
    }

    void MoverNuevoPunto()
    {
        Vector3 puntoAleatorio = Random.insideUnitSphere * 12f + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAleatorio, out hit, 12f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}