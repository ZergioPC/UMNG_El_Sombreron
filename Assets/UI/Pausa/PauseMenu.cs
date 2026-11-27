using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject MenuPausa;
    public bool juegoPausado = false;
    public string escenaMenu = "Inicio"; // Nombre exacto de la escena de inicio

    private void Update()
    {
        // Si la partida ya terminó, no se puede pausar ni reanudar
        if (GestorTiempo.Instancia != null && GestorTiempo.Instancia.Terminado) return;

        // Si hay una pantalla abierta (configuración, instrucciones, créditos), Escape no hace nada
        if (EscenasUI.panelAbierto) return;

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado) Reanudar();
            else Pausar();
        }
    }

    public void Reanudar()
    {
        MenuPausa.SetActive(false);
        Time.timeScale = 1;
        juegoPausado = false;
    }

    public void Pausar()
    {
        MenuPausa.SetActive(true);
        Time.timeScale = 0;
        juegoPausado = true;
    }

    // Botón "Reiniciar partida" del menú de pausa
    public void ReiniciarPartida()
    {
        Time.timeScale = 1;
        juegoPausado = false;
        EscenasUI.panelAbierto = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Botón "Volver al menú" del menú de pausa
    public void VolverAlMenu()
    {
        Time.timeScale = 1;
        juegoPausado = false;
        EscenasUI.panelAbierto = false;

        if (Application.CanStreamedLevelBeLoaded(escenaMenu))
            SceneManager.LoadScene(escenaMenu);
        else
            Debug.LogError("PauseMenu: la escena '" + escenaMenu + "' no existe o no está en Scenes In Build.");
    }

    public void OnDisable()
    {
        // No reanudar el tiempo si la partida terminó
        if (GestorTiempo.Instancia != null && GestorTiempo.Instancia.Terminado) return;

        if (juegoPausado && Time.timeScale == 0f) Time.timeScale = 1f;
    }
}