using UnityEngine;
using UnityEngine.SceneManagement;

public class EscenasUI : MonoBehaviour
{
    public static bool panelAbierto = false;

    public void Abrir(string nombreEscena)
    {
        if (panelAbierto) return;
        if (!Application.CanStreamedLevelBeLoaded(nombreEscena)) return;

        panelAbierto = true;
        SceneManager.LoadSceneAsync(nombreEscena, LoadSceneMode.Additive);
    }

    public void Cerrar()
    {
        panelAbierto = false;
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}