using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SlotHabilidad
{
    public Image sombra;
    public TMP_Text texto; 
    public CanvasGroup grupo; 
}

public class HUDHabilidades : MonoBehaviour
{
    public PlayerController jugador;
    public SlotHabilidad dash;
    public SlotHabilidad perro;

    public float opacidadRecargando = 0.55f;

    void Start()
    {
        if (jugador == null) jugador = FindAnyObjectByType<PlayerController>();
    }

    void Update()
    {
        if (jugador == null) return;

        ActualizarSlot(dash, jugador.DashRestante, jugador.DashTiempoTotal, "");

        if (jugador.PerroActivo) ActualizarSlot(perro, 0f, 1f, "");
        else ActualizarSlot(perro, jugador.PerroRestante, jugador.PerroTiempoTotal, "");
    }

    void ActualizarSlot(SlotHabilidad slot, float restante, float total, string textoFijo)
    {
        if (slot == null) return;

        bool recargando = restante > 0.01f;

        if (slot.sombra != null)
            slot.sombra.fillAmount = recargando && total > 0f ? Mathf.Clamp01(restante / total) : 0f;

        if (slot.texto != null)
        {
            if (recargando) slot.texto.text = restante >= 1f ? Mathf.CeilToInt(restante).ToString() : restante.ToString("0.0");
            else slot.texto.text = textoFijo;
        }

        if (slot.grupo != null)
            slot.grupo.alpha = recargando ? opacidadRecargando : 1f;
    }
}