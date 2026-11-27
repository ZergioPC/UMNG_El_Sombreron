using UnityEngine;

public class Crosshair : MonoBehaviour
{
    // Imagen de la mirilla (el objeto de UI dentro del Canvas)
    public RectTransform rectTransformMirilla;

    // Cursor que se usa en el menú de pausa
    public Texture2D cursorMenu; // Imagen con Texture Type = Cursor
    public Vector2 puntoClic = Vector2.zero; // Punta del cursor (0,0 = esquina superior izquierda)

    private bool modoMenu = false;

    void Start()
    {
        SetModoMenu(false);
    }

    void Update()
    {
        // Si el juego se pausa (Time.timeScale = 0), cambia solo al cursor del menú
        bool pausado = Time.timeScale == 0f;
        if (pausado != modoMenu) SetModoMenu(pausado);

        // La mirilla sigue al mouse solo durante el juego
        if (!modoMenu && rectTransformMirilla != null)
            rectTransformMirilla.position = Input.mousePosition;
    }

    public void SetModoMenu(bool enMenu)
    {
        modoMenu = enMenu;

        if (enMenu)
        {
            // Pausa: se oculta la mirilla y se muestra el cursor personalizado
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Cursor.SetCursor(cursorMenu, puntoClic, CursorMode.Auto);

            if (rectTransformMirilla != null)
                rectTransformMirilla.gameObject.SetActive(false);
        }
        else
        {
            // Juego: cursor del sistema oculto y la mirilla visible
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;

            if (rectTransformMirilla != null)
                rectTransformMirilla.gameObject.SetActive(true);
        }
    }

    // Al salir de la escena de juego se devuelve el cursor, para que no quede oculto en el menú
    void OnDestroy()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}