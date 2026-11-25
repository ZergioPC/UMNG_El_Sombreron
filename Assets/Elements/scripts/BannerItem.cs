using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BannerItem : MonoBehaviour
{
    public RectTransform contenido;
    public CanvasGroup grupo;
    public Image fondo;
    public Image icono;
    public TMP_Text pretexto;
    public TMP_Text nombre;

    public void Configurar(string textoNombre, Sprite sprite, string textoPretexto = null)
    {
        if (pretexto != null && textoPretexto != null)
        {
            pretexto.text = textoPretexto;
        }

        if (nombre != null)
        {
            nombre.text = textoNombre;
        }

        if (icono != null)
        {
            icono.sprite = sprite;
            icono.enabled = sprite != null;
        }
    }
}