using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettings : MonoBehaviour {

	[Header("Sliders")]
	[SerializeField] private Slider musicSlider;
	[SerializeField] private Slider sfxSlider;

	// Use this for initialization
	void Start () {
		musicSlider.onValueChanged.AddListener (OnMusicSliderChanged);
		sfxSlider.onValueChanged.AddListener (OnSfxSliderChanged);
	}
	
	// Este método se ejecutará automáticamente cada vez que el slider se mueva
	private void OnMusicSliderChanged(float value)
	{
		Debug.Log("Música: " + value);
	}

	private void OnSfxSliderChanged(float value)
	{
		Debug.Log("SFX: " + value);
	}

	private void OnDestroy()
	{
		// Buena práctica: desuscribirse para evitar fugas de memoria
		if (musicSlider != null)
		{
			musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
		}

		if (sfxSlider != null)
		{
			sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
		}
	}
}
