using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystem : MonoBehaviour
{   
    [SerializeField] private string escenaJuego = "Gameplay";

//Elegir la escena por nombre
    private void CargarEscena(string nombreEscena)
    {
        if (string.IsNullOrWhiteSpace(nombreEscena)) return;
        if (!Application.CanStreamedLevelBeLoaded(nombreEscena)) return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

// Ir a la escena de jugar
    public void jugar()
    {
        CargarEscena(escenaJuego);
    }

    public void salir()
    {
        Time.timeScale = 1f;
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

}
