using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMenuPrincipal : MonoBehaviour
{
    [Header("Referencias UI existentes")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject menuInfo;
    [SerializeField] private GameObject menuSettings;
    [SerializeField] private GameObject menuCredits;

    [Header("Escenas del menú")]
    [SerializeField] private string escenaInicio = "Scene_Inicio";
    [SerializeField] private string escenaNivel = "Gameplay";
    [SerializeField] private string escenaInstrucciones = "Scene_Instrucciones";
    [SerializeField] private string escenaOpciones = "Scene_Opciones";
    [SerializeField] private string escenaCreditos = "Scene_Crediitos";

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (menuInfo != null) menuInfo.SetActive(false);
        if (menuSettings != null) menuSettings.SetActive(false);
        if (menuCredits != null) menuCredits.SetActive(false);
    }

    public void ShowPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void OnClosePanel()
    {
        if (SceneManager.GetActiveScene().name != escenaInicio) OnBack();
        else if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void OnPlay() => CargarEscena(escenaNivel);
    public void OnInfo() => CargarEscena(escenaInstrucciones);
    public void OnSettings() => CargarEscena(escenaOpciones);
    public void OnCredits() => CargarEscena(escenaCreditos);
    public void OnBack() => CargarEscena(escenaInicio);

    private void CargarEscena(string escena)
    {
        if (SceneManager.GetActiveScene().name == escena) return;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(escena);
    }

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
