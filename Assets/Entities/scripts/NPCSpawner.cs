using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;

public class NPCSpawner : MonoBehaviour
{
// Serializacion
    [System.Serializable]
    public struct CandidatoNPC
    {
        public GameObject prefab;
        [Range(0.1f, 100f)]
        public float peso; //Probabilidad de aparicion
    }

// Prefabs de los NPCs
    public List<CandidatoNPC> candidatos = new List<CandidatoNPC>();
    
// Ubicación en el mapa
    public Transform[] puntosSpawn;
    public Vector3[] offsetsDesdeSpawner = new Vector3[0];
    public float radioArea = 20f;
    public float radioDispersion = 2f;
    private bool usarNavMesh = true;
    public float radioBusqueda = 2.5f;

// Configuracion de spawn
    public float radioChequeoObstaculos = 0.6f;
    public LayerMask capaObstaculos;
    public Transform jugador;
    public float distanciaMinimaAlJugador = 5f;

// Ritmo y límite de población
    public int poblacionObjetivo = 10; // Numero maximo alcanzado
    public int spawnInicial = 6; // Numero inicial
    public float intervaloRevision = 2.5f; //Segundos entre revisiones de población
    public int maxSpawnsPorCiclo = 2; // Máximo de NPCs generados por cada ciclo de revisión

    private readonly List<GameObject> npcsActivos = new List<GameObject>();
    private Coroutine rutinaSpawns;
    private bool didInit = false;

    public int CantidadActivos
    {
        get
        {
            LimpiarReferenciasDestruidas();
            return npcsActivos.Count;
        }
    }

    void Awake()
    {
        didInit = true;
    }

    void Start()
    {
        LimpiarReferenciasDestruidas();

        int iniciales = Mathf.Clamp(spawnInicial, 0, poblacionObjetivo);
        for (int i = 0; i < iniciales; i++)
        {
            if (!IntentarSpawnearNPC()) break;
        }
        rutinaSpawns = StartCoroutine(RutinaAutoSpawn());
    }

    void OnEnable()
    {
        if (rutinaSpawns == null && Application.isPlaying && didInit)
        {
            rutinaSpawns = StartCoroutine(RutinaAutoSpawn());
        }
    }

    void OnDisable()
    {
        if (rutinaSpawns != null)
        {
            StopCoroutine(rutinaSpawns);
            rutinaSpawns = null;
        }
    }

    IEnumerator RutinaAutoSpawn()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.25f, intervaloRevision));

            LimpiarReferenciasDestruidas();

            int faltantes = poblacionObjetivo - npcsActivos.Count;
            if (faltantes <= 0) continue;

            int aGenerar = Mathf.Min(faltantes, Mathf.Max(1, maxSpawnsPorCiclo));
            for (int i = 0; i < aGenerar; i++)
            {
                IntentarSpawnearNPC();
            }
        }
    }

    public bool IntentarSpawnearNPC()
    {
        LimpiarReferenciasDestruidas();
        if (npcsActivos.Count >= poblacionObjetivo) return false;

        GameObject prefabElegido = SeleccionarPrefab();
        if (prefabElegido == null) return false;

        if (!BuscarPosicionLibre(out Vector3 pos, out Quaternion rot))
        {
            return false;
        }

        GameObject nuevoNPC = Instantiate(prefabElegido, pos, rot);
        npcsActivos.Add(nuevoNPC);
        return true;
    }

    GameObject SeleccionarPrefab()
    {
        if (candidatos == null || candidatos.Count == 0) return null;

        float totalPesos = 0f;
        for (int i = 0; i < candidatos.Count; i++)
        {
            if (candidatos[i].prefab != null && candidatos[i].peso > 0f)
            {
                totalPesos += candidatos[i].peso;
            }
        }

        if (totalPesos <= 0f)
        {
            for (int i = 0; i < candidatos.Count; i++)
            {
                if (candidatos[i].prefab != null) return candidatos[i].prefab;
            }
            return null;
        }

        float tirada = Random.Range(0f, totalPesos);
        float acumulado = 0f;

        for (int i = 0; i < candidatos.Count; i++)
        {
            if (candidatos[i].prefab == null || candidatos[i].peso <= 0f) continue;

            acumulado += candidatos[i].peso;
            if (tirada <= acumulado)
            {
                return candidatos[i].prefab;
            }
        }

        return null;
    }

    bool BuscarPosicionLibre(out Vector3 posicionFinal, out Quaternion rotacionFinal)
    {
        posicionFinal = transform.position;
        rotacionFinal = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        const int maxIntentos = 15;
        bool tienePuntosTransform = puntosSpawn != null && puntosSpawn.Length > 0;
        bool tieneOffsets = offsetsDesdeSpawner != null && offsetsDesdeSpawner.Length > 0;

        for (int i = 0; i < maxIntentos; i++)
        {
            Vector3 candidata;

            if (tienePuntosTransform)
            {
                Transform punto = puntosSpawn[Random.Range(0, puntosSpawn.Length)];
                if (punto == null) continue;

                Vector2 offset = Random.insideUnitCircle * radioDispersion;
                candidata = punto.position + new Vector3(offset.x, 0f, offset.y);
            }
            else if (tieneOffsets)
            {
                Vector3 offsetBase = offsetsDesdeSpawner[Random.Range(0, offsetsDesdeSpawner.Length)];
                Vector2 offset = Random.insideUnitCircle * radioDispersion;
                candidata = transform.position + offsetBase + new Vector3(offset.x, 0f, offset.y);
            }
            else
            {
                Vector2 circulo = Random.insideUnitCircle * radioArea;
                candidata = transform.position + new Vector3(circulo.x, 0f, circulo.y);
            }

            if (usarNavMesh)
            {
                if (!NavMesh.SamplePosition(candidata, out NavMeshHit hit, radioBusqueda, NavMesh.AllAreas))
                {
                    continue;
                }
                candidata = hit.position;
            }

            if (jugador != null && distanciaMinimaAlJugador > 0f)
            {
                float distJugador = Vector3.Distance(
                    new Vector3(candidata.x, 0f, candidata.z),
                    new Vector3(jugador.position.x, 0f, jugador.position.z)
                );
                if (distJugador < distanciaMinimaAlJugador)
                {
                    continue;
                }
            }

            bool hayObstaculo = capaObstaculos.value != 0 &&
                Physics.CheckSphere(candidata + Vector3.up * 0.5f, radioChequeoObstaculos, capaObstaculos);

            if (!hayObstaculo)
            {
                posicionFinal = candidata;
                return true;
            }
        }

        return false;
    }

    public void LimpiarReferenciasDestruidas()
    {
        for (int i = npcsActivos.Count - 1; i >= 0; i--)
        {
            if (npcsActivos[i] == null)
            {
                npcsActivos.RemoveAt(i);
            }
        }
    }
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Handles.color = Color.blue;
        Handles.DrawWireDisc(transform.position, Vector3.up, radioArea);

        if (puntosSpawn != null)
        {
            Handles.color = Color.yellow;
            foreach (Transform p in puntosSpawn)
            {
                if (p != null)
                {
                    Handles.DrawWireDisc(p.position, Vector3.up, Mathf.Max(0.25f, radioDispersion));
                }
            }
        }

        if (offsetsDesdeSpawner != null)
        {
            Handles.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            foreach (Vector3 off in offsetsDesdeSpawner)
            {
                Handles.DrawWireDisc(transform.position + off, Vector3.up, Mathf.Max(0.25f, radioDispersion));
            }
        }
    }
    #endif
}