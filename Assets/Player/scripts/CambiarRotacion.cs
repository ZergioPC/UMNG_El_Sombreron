using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambiarRotacion : MonoBehaviour
{
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private Camera camara;

    private void Start()
    {
        if (camara == null)
        {
            camara = Camera.main;
        }
    }

    private void Update()
    {
        Ray rayo = camara.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(rayo, out RaycastHit hit, Mathf.Infinity, capaSuelo))
        {
            Vector3 puntoObjetivo = hit.point;
            puntoObjetivo.y = transform.position.y;
            Vector3 direccion = (puntoObjetivo - transform.position).normalized;
            
            if (direccion != Vector3.zero)
            {
                Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * 15f);
            }
        }
    }
}
