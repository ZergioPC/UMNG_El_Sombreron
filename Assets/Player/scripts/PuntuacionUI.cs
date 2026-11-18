using UnityEngine;
using TMPro;

public class PuntuacionUI : MonoBehaviour
{
    public TextMeshProUGUI textoPuntuacion;
    public PlayerController playerController;

    void Update()
    {
        if (playerController != null && textoPuntuacion != null)
        {
            textoPuntuacion.text = "Puntuación: " + playerController.puntuacionActual;
        }
        
    }
}
