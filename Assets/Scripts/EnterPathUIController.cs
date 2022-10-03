using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using unityutilities;

public class EnterPathUIController : MonoBehaviour
{
	public Text buttonText;
	public InputField path;
	public Text errorText;

	private void Start()
	{
		path.text = PlayerPrefsJson.GetString("datafoldvr_path", path.text);
	}

	public void StartClicked()
	{
		Debug.Log("Start Clicked");
		if (Directory.Exists(path.text))
		{
			Debug.Log($"Path valid: {path.text}");
			errorText.gameObject.SetActive(false);
			buttonText.text = "loading...";
			PlayerPrefsJson.SetString("datafoldvr_path", path.text);
			SceneManager.LoadSceneAsync(1);
			Debug.Log("Loading scene...");
		}
		else
		{
			const string error = "Path must be an existing directory";
			Debug.Log(error);
			errorText.text = error;
			errorText.gameObject.SetActive(true);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return) ||
		    Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			StartClicked();
		}
	}
}