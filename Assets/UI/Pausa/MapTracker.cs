using UnityEngine;

public class MapTracker : MonoBehaviour
{
    [Header("Referencias del Jugador y UI")]
    public Transform playerTransform;   // Objeto 3D del jugador
    public RectTransform mapIconRect;   // Ícono del jugador en el mapa UI
    public RectTransform mapPanelRect;  // Panel de la imagen del mapa UI

    [Header("Límites del Mundo 3D")]
    public Vector2 worldMinBounds = new Vector2(-100f, -100f);
    public Vector2 worldMaxBounds = new Vector2(100f, 100f);

    void Update()
    {
        if (playerTransform == null || mapIconRect == null || mapPanelRect == null)
            return;

        // Calcular posición relativa del jugador (0 a 1)
        float normalizedX = Mathf.InverseLerp(worldMinBounds.x, worldMaxBounds.x, playerTransform.position.x);
        float normalizedZ = Mathf.InverseLerp(worldMinBounds.y, worldMaxBounds.y, playerTransform.position.z);

        // Convertir a coordenadas dentro de la imagen del mapa UI
        float mapWidth = mapPanelRect.rect.width;
        float mapHeight = mapPanelRect.rect.height;

        float posX = (normalizedX - 0.5f) * mapWidth;
        float posY = (normalizedZ - 0.5f) * mapHeight;

        mapIconRect.anchoredPosition = new Vector2(posX, posY);
    }
}