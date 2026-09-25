using UnityEngine;

public class UISkyRotation : MonoBehaviour
{
	[Header("Rotation Settings")]
	public float speed = 1f;
	public bool rotateClockwise = true;

	[Header("Scale Oscillation Settings")]
	public bool enableScaleOscillation = false;
	public float scaleSpeed = 2f;
	public Vector3 minScale = new Vector3(0.8f, 0.8f, 0.8f);
	public Vector3 maxScale = new Vector3(1.2f, 1.2f, 1.2f);

	void Update()
	{
		// 1. Rotation Direction Control
		float direction = rotateClockwise ? -1f : 1f;
		transform.Rotate(0f, 0f, direction * speed * Time.deltaTime);

		// 2. Scale Oscillation
		if (enableScaleOscillation)
		{
			// Calculate a 0 to 1 value using a sine wave over time
			float t = (Mathf.Sin(Time.time * scaleSpeed) + 1f) / 2f;
			transform.localScale = Vector3.Lerp(minScale, maxScale, t);
		}
	}
}