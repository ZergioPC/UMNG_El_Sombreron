using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UIRawImageOpacity : MonoBehaviour {

	[Header("Opacity")]
	[Range(0f, 1f)]
	public float minOpacity = 0.2f;

	[Range(0f, 1f)]
	public float maxOpacity = 0.8f;

	[Header("Animation")]
	public float frequency = 1f;

	[Tooltip("Desfase")]
	public float phase = 0f;

	private RawImage rawImage;

	private float currentOpacity;
	private float targetOpacity;
	private float timer;

	void Awake()
	{
		rawImage = GetComponent<RawImage>();

		currentOpacity = maxOpacity;
		targetOpacity = Random.Range (minOpacity, maxOpacity);
	}

	void Update()
	{
		timer += Time.deltaTime;

		float duration = 1 / frequency;

		currentOpacity = Mathf.Lerp (
			currentOpacity,
			targetOpacity,
			Time.deltaTime / duration
		);

		if (timer >= duration) 
		{
			timer = 0f;
			targetOpacity = Random.Range (minOpacity, maxOpacity);
		}

		Color color = rawImage.color;
		color.a = currentOpacity;
		rawImage.color = color;
	}
}
