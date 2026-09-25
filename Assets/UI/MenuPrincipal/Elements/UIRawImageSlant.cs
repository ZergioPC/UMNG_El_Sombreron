using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRawImageSlant : BaseMeshEffect
{
	[Header("Slant")]
	[Tooltip("Inclinación máxima en unidades locales.")]
	public float amplitude = 20f;

	[Tooltip("Cantidad de oscilaciones por segundo.")]
	public float frequency = 1f;

	[Tooltip("Desfase de la oscilación.")]
	public float phase = 0f;

	protected override void OnEnable()
	{
		base.OnEnable();
		GetComponent<Graphic>().SetVerticesDirty();
	}

	void Update()
	{
		GetComponent<Graphic>().SetVerticesDirty();
	}

	public override void ModifyMesh(VertexHelper vh)
	{
		if (!IsActive())
			return;

		UIVertex vertex = new UIVertex();

		float slant = Mathf.Sin(
			(Time.time * frequency * Mathf.PI * 2f) + phase
		) * amplitude;

		int count = vh.currentVertCount;

		for (int i = 0; i < count; i++)
		{
			vh.PopulateUIVertex(ref vertex, i);

			// La parte superior se desplaza respecto a la inferior.
			float normalizedY = Mathf.InverseLerp(
				-GetComponent<RectTransform>().rect.height / 2f,
				GetComponent<RectTransform>().rect.height / 2f,
				vertex.position.y
			);

			vertex.position.x += slant * normalizedY;

			vh.SetUIVertex(vertex, i);
		}
	}
}