using System.Collections.Generic;
using UnityEngine;

public class Rastro : MonoBehaviour
{
    [Header("Referencias")]
    public PerroController perro;        // Destino/referencia del rastro (si está vacío, usa al jugador)
    public Transform origenManual;       // Opcional: fuerza otro punto de llegada/referencia (por ejemplo, el jugador)
    public Material material;            // Lo asigna Tools > Material Rastro

    [Header("Alcance y Transparencia")]
    public float rangoMaximo = 80f;      // Metros hasta donde empieza a aparecer el rastro
    // 0 = cerca (más claro/visible), 1 = rango máximo (invisible / transparente)
    public AnimationCurve caida = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.75f, 0.25f),
        new Keyframe(1f, 0f)
    );

    [Header("Aspecto")]
    public Color colorRastro = new Color(0.65f, 0.04f, 0.04f); // Rojo oscuro
    public Color colorSecundario = new Color(0.04f, 0.04f, 0.04f); // Negro para mezclar con el rojo
    public int hebras = 3;               // Cintas por objetivo
    public int segmentos = 40;
    public float anchoCerca = 0.9f;      // Ancho cuando está cerca
    public float anchoLejos = 0.2f;      // Ancho cuando está lejos
    public float amplitudOnda = 1.6f;    // Cuánto serpentea
    public float frecuenciaOnda = 0.12f; // Curvas por metro
    public float velocidadOnda = 1.2f;   // Qué tan rápido se mueve la ondulación
    public float separacionHebras = 0.3f;

    [Header("Suelo")]
    public float alturaSuelo = 0.2f;
    public LayerMask capaSuelo;          // Si lo asignas, el rastro sigue el relieve del terreno

    [Header("Tiempos")]
    public float tiempoEntrada = 0.4f;
    public float tiempoSalida = 1.0f;
    public float intervaloBusqueda = 0.4f;

    private readonly Dictionary<NPCIdentity, LineRenderer[]> lineas = new Dictionary<NPCIdentity, LineRenderer[]>();
    private readonly List<NPCIdentity> candidatos = new List<NPCIdentity>();
    private readonly List<NPCIdentity> basura = new List<NPCIdentity>();
    private Transform jugador;
    private float visibilidad;
    private float proximaBusqueda;

    private void Start()
    {
        PlayerController p = FindAnyObjectByType<PlayerController>();
        if (p != null) jugador = p.transform;
        if (perro == null) perro = FindAnyObjectByType<PerroController>();
    }

    private void LateUpdate()
    {
        bool activo = PerroController.estaOlfateando;
        float tiempo = activo ? tiempoEntrada : tiempoSalida;
        visibilidad = Mathf.MoveTowards(visibilidad, activo ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, tiempo));

        if (visibilidad <= 0f) { OcultarTodo(); return; }

        Transform destino = origenManual != null ? origenManual : (perro != null ? perro.transform : jugador);
        if (destino == null) return;

        if (Time.time >= proximaBusqueda)
        {
            proximaBusqueda = Time.time + intervaloBusqueda;
            BuscarCulpables();
        }

        foreach (NPCIdentity npc in candidatos)
        {
            if (npc == null) continue;
            ActualizarRastro(npc, destino.position);
        }

        LimpiarDestruidos();
    }

    private void BuscarCulpables()
    {
        candidatos.Clear();
        foreach (NPCIdentity npc in FindObjectsByType<NPCIdentity>())
        {
            if (!npc.esCulpable()) continue;
            TramposoNPC tramposo = npc.GetComponentInParent<TramposoNPC>();
            if (tramposo != null && tramposo.esClon) continue; // Los clones no dejan rastro
            candidatos.Add(npc);
        }
    }

    private void ActualizarRastro(NPCIdentity npc, Vector3 destino)
    {
        // Origen (a) en el NPC objetivo; destino (b) en el perro o jugador
        Vector3 a = npc.transform.position;
        Vector3 b = destino;

        Vector3 dir = b - a;
        dir.y = 0f;
        float dist = dir.magnitude;

        LineRenderer[] hs = ObtenerLineas(npc);

        float factorDistancia = Mathf.Clamp01(dist / Mathf.Max(0.01f, rangoMaximo));
        float cercania = Mathf.Clamp01(caida.Evaluate(factorDistancia));
        float intensidad = cercania * visibilidad;

        if (dist > rangoMaximo || dist < 1.5f || intensidad <= 0.005f)
        {
            foreach (var lr in hs)
                if (lr != null) lr.enabled = false;
            return;
        }

        dir /= dist;
        Vector3 lado = new Vector3(-dir.z, 0f, dir.x);
        float semillaNPC = Mathf.Abs(npc.GetEntityId().GetHashCode() % 100);
        int numSegmentos = Mathf.Max(1, segmentos);

        for (int h = 0; h < hs.Length; h++)
        {
            LineRenderer lr = hs[h];
            if (lr == null) continue;
            lr.enabled = true;
            if (lr.positionCount != numSegmentos + 1)
                lr.positionCount = numSegmentos + 1;

            bool principal = h == 0;
            bool usaColorSecundario = (h % 2) == 1;
            Color colorHebras = usaColorSecundario ? colorSecundario : colorRastro;

            float semilla = semillaNPC + h * 37.1f;
            float centrado = h - (hs.Length - 1) * 0.5f;

            for (int i = 0; i <= numSegmentos; i++)
            {
                float t = i / (float)numSegmentos;
                Vector3 p = Vector3.Lerp(a, b, t);

                float envolvente = Mathf.Sin(t * Mathf.PI); // Los extremos quedan fijos
                float ruido = (Mathf.PerlinNoise(t * dist * frecuenciaOnda + Time.time * velocidadOnda, semilla) - 0.5f) * 2f;
                float onda = ruido * amplitudOnda * (principal ? 0.5f : 1f);
                p += lado * ((onda + centrado * separacionHebras) * envolvente);

                p.y = a.y + alturaSuelo;
                if (capaSuelo.value != 0)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(p + Vector3.up * 50f, Vector3.down, out hit, 120f, capaSuelo))
                        p.y = hit.point.y + alturaSuelo;
                }
                lr.SetPosition(i, p);
            }

            float alfa = Mathf.Clamp01(intensidad * (principal ? 0.9f : 0.7f));
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(colorHebras, 0f),
                    new GradientColorKey(colorHebras, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(alfa, 0f),
                    new GradientAlphaKey(alfa * 0.65f, 0.5f),
                    new GradientAlphaKey(alfa * 0.15f, 1f)
                }
            );
            lr.colorGradient = grad;
            lr.widthMultiplier = Mathf.Lerp(anchoLejos, anchoCerca, cercania) * (principal ? 1f : 0.65f);
        }
    }

    private LineRenderer[] ObtenerLineas(NPCIdentity npc)
    {
        LineRenderer[] hs;
        if (lineas.TryGetValue(npc, out hs)) return hs;

        int numSegmentos = Mathf.Max(1, segmentos);
        hs = new LineRenderer[Mathf.Max(1, hebras)];
        for (int h = 0; h < hs.Length; h++)
        {
            GameObject go = new GameObject("Rastro_" + npc.name + "_" + h);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = numSegmentos + 1;
            lr.sharedMaterial = material;
            lr.textureMode = LineTextureMode.Stretch;
            lr.alignment = LineAlignment.View;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 0.8f), new Keyframe(1f, 0.15f));
            lr.enabled = false;
            hs[h] = lr;
        }
        lineas[npc] = hs;
        return hs;
    }

    private void OcultarTodo()
    {
        foreach (var par in lineas)
            foreach (var lr in par.Value)
                if (lr != null && lr.enabled) lr.enabled = false;
    }

    private void LimpiarDestruidos()
    {
        basura.Clear();
        foreach (var par in lineas)
            if (par.Key == null) basura.Add(par.Key);

        if (basura.Count == 0) return;
        var vivos = new Dictionary<NPCIdentity, LineRenderer[]>();
        foreach (var par in lineas)
        {
            if (par.Key == null)
            {
                foreach (var lr in par.Value) if (lr != null) Destroy(lr.gameObject);
            }
            else vivos[par.Key] = par.Value;
        }
        lineas.Clear();
        foreach (var par in vivos) lineas[par.Key] = par.Value;
    }
}