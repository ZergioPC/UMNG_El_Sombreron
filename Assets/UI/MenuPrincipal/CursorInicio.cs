using System.Collections;
using UnityEngine;

public class CursorInicio : MonoBehaviour
{
    public Texture2D cursorMenu; // Imagen con Texture Type = Cursor (la misma de la pausa)
    public Vector2 puntoClic = Vector2.zero; // Punta del cursor (0,0 = esquina superior izquierda)

    public Texture2D cursorMenuWeb; // Versión pequeña (32 o 48 px) que se usa solo en Web
    public Vector2 puntoClicWeb = Vector2.zero; // Punta del cursor en la versión Web

    void Start()
    {
        Time.timeScale = 1f; // Por si se llega desde una partida pausada o terminada
        Aplicar();
        StartCoroutine(ReaplicarVariosFotogramas());
    }

    // En Windows el cambio inmediato a veces no se nota, así que se repite unos fotogramas
    IEnumerator ReaplicarVariosFotogramas()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            Aplicar();
        }
    }

    void OnApplicationFocus(bool tieneFoco)
    {
        if (tieneFoco) Aplicar();
    }

    void Aplicar()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        // En Web se usa la imagen pequeña (si no existe, se usa la normal)
        if (cursorMenuWeb != null)
        {
            Cursor.SetCursor(cursorMenuWeb, puntoClicWeb, CursorMode.Auto);
            return;
        }
#endif
        Cursor.SetCursor(cursorMenu, puntoClic, CursorMode.Auto);
    }
}