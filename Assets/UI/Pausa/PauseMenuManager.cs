using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Paneles de la UI")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject mapPanel;

    [Header("Sliders de Ajustes")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private bool isPaused = false;

    void Start()
    {
        // Asignar listeners a los sliders para imprimir en consola al cambiar valor
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    void Update()
    {
        // Tecla P para abrir/cerrar pausa y congelar el juego
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // --- MENÚ DE PAUSA ---
    public void PauseGame()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f; // Congelar el juego
        isPaused = true;
    }

    public void ResumeGame()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        Time.timeScale = 1f; // Descongelar el juego
        isPaused = false;
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenMap()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        // Imprimir en consola como pide la Issue #9
        Debug.Log("Volviendo al Menú Principal...");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // --- BOTÓN REGRESAR EN PANELES ---
    public void ReturnToPauseMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    // --- IMPRIMIR VALORES DE SLIDERS EN CONSOLA ---
    public void OnMusicVolumeChanged(float value)
    {
        Debug.Log("Volumen de Música: " + value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        Debug.Log("Volumen de SFX: " + value);
    }
}