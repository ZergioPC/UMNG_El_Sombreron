using UnityEngine;

public class Crosshair : MonoBehaviour
{
    private Camera camaraMain;

//Configuración de la mirilla
    public RectTransform rectTransformMirilla;
    public GameObject mirilla;
    private GameObject mirillaInstanciada;

//Capas
    public LayerMask capaSuelo;

    void Start()
    {
        camaraMain = Camera.main;
        if (mirilla != null && rectTransformMirilla == null)
        {
            mirillaInstanciada = Instantiate(mirilla);
        }
        Cursor.visible = false; // Oculta el cursor del sistema operativo
    }

    void Update()
    {
        ActualizarPosicionMirilla();
    }

    void ActualizarPosicionMirilla()
    {
        if (rectTransformMirilla != null)
        rectTransformMirilla.position = Input.mousePosition;

        Ray rayo = camaraMain.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(rayo, out RaycastHit hit, Mathf.Infinity, capaSuelo))
        {
            if (mirillaInstanciada != null)
            {
                mirillaInstanciada.transform.position = hit.point + Vector3.up * 0.1f; // Ajusta la altura de la mirilla sobre el suelo
            }
        }
    }
}
