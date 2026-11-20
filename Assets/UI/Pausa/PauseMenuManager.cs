using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Necesario para la función de reiniciar la escena

public class PauseMenuManager : MonoBehaviour
{
    [Header("Controles del nivel (se desactivan durante la pausa)")]
    [SerializeField] private Behaviour[] gameplayControls = new Behaviour[0];
    private bool[] controlStates;
    private bool cursorWasVisible;
    private CursorLockMode cursorLockBeforePause;
    private float timeScaleBeforePause = 1f;
    private bool audioWasPaused;
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
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
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
        if (isPaused) return;
        timeScaleBeforePause = Time.timeScale;
        audioWasPaused = AudioListener.pause;
        cursorWasVisible = Cursor.visible;
        cursorLockBeforePause = Cursor.lockState;
        controlStates = new bool[gameplayControls.Length];
        for (int i = 0; i < gameplayControls.Length; i++)
        {
            if (gameplayControls[i] == null) continue;
            controlStates[i] = gameplayControls[i].enabled;
            gameplayControls[i].enabled = false;
        }
        isPaused = true;
        if (pauseCanvas != null) pauseCanvas.SetActive(true);
        ShowPanelPausa();
        Time.timeScale = 0f; // Congela el juego
        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        if (isPaused)
        {
            for (int i = 0; i < gameplayControls.Length; i++)
                if (gameplayControls[i] != null) gameplayControls[i].enabled = controlStates[i];
            Time.timeScale = timeScaleBeforePause;
            AudioListener.pause = audioWasPaused;
            Cursor.visible = cursorWasVisible;
            Cursor.lockState = cursorLockBeforePause;
        }
        isPaused = false;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
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
        ResumeGame();
        Time.timeScale = 1f; // Importante: restaurar la velocidad del tiempo antes de reiniciar
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- Funcionalidades Provisionales (Logs en Consola) ---
    public void GoToMainMenu()
    {
        ResumeGame();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("Scene_Inicio");
    }

    private void OnDestroy()
    {
        if (sliderMusica != null) sliderMusica.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (sliderSFX != null) sliderSFX.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        if (isPaused)
        {
            Time.timeScale = timeScaleBeforePause;
            AudioListener.pause = audioWasPaused;
        }
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
