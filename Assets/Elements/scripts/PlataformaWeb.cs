using UnityEngine;

public class PlataformaWeb : MonoBehaviour
{
    public GameObject[] ocultarEnWeb;

    void Awake()
    {
#if UNITY_WEBGL
        foreach (GameObject g in ocultarEnWeb)
        {
            if (g != null) g.SetActive(false);
        }
#endif
    }
}