using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenuPrincipal : MonoBehaviour {

	[Header("Referencias UI")]
	[SerializeField] private GameObject menuPanel;


	[Header("Submenus")]
	[SerializeField] private GameObject menuInfo;
	[SerializeField] private GameObject menuSettings;
	[SerializeField] private GameObject menuCredits;


	void Awake()
	{
		menuPanel.SetActive (false);
		menuInfo.SetActive (false);
		menuSettings.SetActive (false);
		menuCredits.SetActive (false);
	}

	// Panel methods
	public void ShowPanel()
	{
		menuPanel.SetActive (true);
	}

	public void OnClosePanel()
	{
		menuPanel.SetActive (false);
	}

	// Botones Menu Principal
	public void OnPlay()
	{
		Debug.Log ("Jugar el Juego");
	}

	public void OnInfo()
	{
		Debug.Log ("Instrucciones");

		menuInfo.SetActive (true);
		menuSettings.SetActive (false);
		menuCredits.SetActive (false);

		ShowPanel ();
	}

	public void OnSettings()
	{
		Debug.Log ("Ajustes");

		menuInfo.SetActive (false);
		menuSettings.SetActive (true);
		menuCredits.SetActive (false);

		ShowPanel ();
	}

	public void OnCredits()
	{
		Debug.Log ("Creditos");

		menuInfo.SetActive (false);
		menuSettings.SetActive (false);
		menuCredits.SetActive (true);

		ShowPanel ();
	}

	public void OnExit()
	{
		Debug.Log ("Chao");
	}
}
