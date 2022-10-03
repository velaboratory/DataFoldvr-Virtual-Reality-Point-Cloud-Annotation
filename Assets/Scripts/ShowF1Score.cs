using System;
using TMPro;
using UnityEngine;

public class ShowF1Score : MonoBehaviour
{
	public TMP_Text text;
	public GameObject[] hideObjects;


	// Update is called once per frame
	private void Update()
	{
		try
		{
			text.text =
				(PlayerManager.instance.currentlyEditedFile.pointCloudInstances[0].refAccuracy * 100).ToString("N0") +
				"%";
			foreach (GameObject hideObject in hideObjects)
			{
				hideObject.SetActive(PlayerManager.instance.CurrentEditMode == VRFile.EditType.StudySegment);
			}
		}
		catch (Exception)
		{
			text.text = "";
			foreach (GameObject hideObject in hideObjects)
			{
				hideObject.SetActive(false);
			}
		}
	}
}