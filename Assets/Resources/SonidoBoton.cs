using UnityEngine;
using UnityEngine.UI;

public class SonidoBoton : MonoBehaviour
{
    public AudioSource fuente; // AudioSource con el sonido de clic (Output = SFX)

    void Start()
    {
        // Busca todos los botones de esta escena, incluso los desactivados
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button b in botones)
        {
            b.onClick.AddListener(Sonar);
        }
    }

    void Sonar()
    {
        if (fuente != null) fuente.Play();
    }
}