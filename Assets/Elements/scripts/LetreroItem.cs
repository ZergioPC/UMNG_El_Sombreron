using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetreroItem : MonoBehaviour
{
    public RectTransform contenido;  // Lo que se anima (pivote abajo, en la punta de la colita)
    public CanvasGroup grupo;
    public Image fondo;              // Cuerpo de la burbuja
    public Image cola;               // Colita que apunta al jugador
    public Image icono;
    public TMP_Text titulo;
    public TMP_Text puntos;

    public void Configurar(string textoTitulo, string textoPuntos, Color color, Sprite sprite)
    {
        if (titulo != null)
        {
            titulo.text = textoTitulo;
            titulo.color = color;
        }

        if (puntos != null)
        {
            puntos.gameObject.SetActive(!string.IsNullOrEmpty(textoPuntos));
            puntos.text = textoPuntos;
            puntos.color = color;
        }

        if (icono != null)
        {
            icono.sprite = sprite;
            icono.enabled = sprite != null;
        }

        if (cola != null && fondo != null) cola.color = fondo.color; // Misma tinta que el cuerpo
    }
}