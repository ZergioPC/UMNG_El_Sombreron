using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para la función de reiniciar la escena

public class PauseMenuManager : MonoBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject panelMenuPausa;
    [SerializeField] private GameObject panelAjustes;
    [SerializeField] private GameObject panelMapa;

    [Header("Controles de Ajustes")]
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderSFX;

    [Header("Referencia Jugador y Mapa")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private RectTransform playerIconOnMap;
    [SerializeField] private Vector2 mapWorldBoundsMin; // Límites mínimos del mundo en 2D/3D
    [SerializeField] private Vector2 mapWorldBoundsMax; // Límites máximos del mundo en 2D/3D
    [SerializeField] private RectTransform mapImageRect;

    private bool isPaused = false;

    private void Start()
    {
        // El juego inicia despausado
        ResumeGame();

        // Escuchar cambios de los sliders e imprimir sus valores en consola
        if (sliderMusica != null)
            sliderMusica.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sliderSFX != null)
            sliderSFX.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void Update()
    {
        // Tecla P de forma temporal para abrir y cerrar el menú de pausa
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // Actualizar la posición del icono del jugador si el panel del mapa está visible
        if (panelMapa != null && panelMapa.activeSelf)
        {
            UpdatePlayerMapPosition();
        }
    }

    // --- Control del Estado de Pausa ---
    public void PauseGame()
    {
        isPaused = true;
        if (pauseCanvas != null) pauseCanvas.SetActive(true);
        ShowPanelPausa();
        Time.timeScale = 0f; // Congela el juego
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        Time.timeScale = 1f; // Descongelar el juego
    }

    // --- Navegación entre Paneles ---
    public void ShowPanelPausa()
    {
        if (panelMenuPausa != null) panelMenuPausa.SetActive(true);
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelMapa != null) panelMapa.SetActive(false);
    }

    public void ShowPanelAjustes()
    {
        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(true);
        if (panelMapa != null) panelMapa.SetActive(false);
    }

    public void ShowPanelMapa()
    {
        if (panelMenuPausa != null) panelMenuPausa.SetActive(false);
        if (panelAjustes != null) panelAjustes.SetActive(false);
        if (panelMapa != null) panelMapa.SetActive(true);
    }

    // --- Botón de Reiniciar (Recarga la escena activa) ---
    public void RestartGame()
    {
        Time.timeScale = 1f; // Importante: restaurar la velocidad del tiempo antes de reiniciar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- Funcionalidades Provisionales (Logs en Consola) ---
    public void GoToMainMenu()
    {
        Debug.Log("[UI Pausa] Volviendo al Menú Principal...");
    }

    private void OnMusicVolumeChanged(float value)
    {
        Debug.Log($"[Ajustes] Volumen Música: {value}");
    }

    private void OnSFXVolumeChanged(float value)
    {
        Debug.Log($"[Ajustes] Volumen SFX: {value}");
    }

    // --- Mapeo de Posición del Jugador en el Mapa ---
    private void UpdatePlayerMapPosition()
    {
        if (playerTransform == null || playerIconOnMap == null || mapImageRect == null) return;

        // Calcula el porcentaje (0 a 1) de la posición del jugador dentro de los límites del mundo
        float normalizedX = Mathf.InverseLerp(mapWorldBoundsMin.x, mapWorldBoundsMax.x, playerTransform.position.x);
        float normalizedY = Mathf.InverseLerp(mapWorldBoundsMin.y, mapWorldBoundsMax.y, playerTransform.position.z); // Cambiar .z por .y si tu juego es 2D

        // Escala la posición normalizada a las dimensiones de la UI del mapa
        float mapWidth = mapImageRect.rect.width;
        float mapHeight = mapImageRect.rect.height;

        float localX = (normalizedX - 0.5f) * mapWidth;
        float localY = (normalizedY - 0.5f) * mapHeight;

        playerIconOnMap.anchoredPosition = new Vector2(localX, localY);
    }
}