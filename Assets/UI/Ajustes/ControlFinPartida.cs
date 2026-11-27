using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlFinPartida : MonoBehaviour
{
    public TMP_Text textoPuntaje;
    public string escenaJuego = "Gameplay";
    public string escenaMenu = "Inicio"; 

    void Start()
    {
        Time.timeScale = 1f;
        if (textoPuntaje != null) textoPuntaje.text = "PUNTAJE: " + DatosPartida.PuntajeFinal;
    }


    public void JugarDeNuevo()
    {
        Cargar(escenaJuego);
    }

    public void VolverAlMenu()
    {
        Cargar(escenaMenu);
    }

    void Cargar(string nombreEscena)
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(nombreEscena))
            SceneManager.LoadScene(nombreEscena);
        else
            Debug.LogError("ControlFinPartida: la escena '" + nombreEscena + "' no existe o no está en Scenes In Build.");
    }
}