using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// File browser UI. There can be multiple of these in a scene.
/// There is only one FolderManager, which has references to the loaded objects
/// </summary>
public class FileBrowser : MonoBehaviour
{
	public FolderManager baseFolder;
	public GameObject fileRowPrefab;
	public Transform fileListScrollViewContent;
	private readonly List<FileBrowserButton> fileBrowserButtons = new List<FileBrowserButton>();

	private float lastRefresh;

	private void OnEnable()
	{
		lastRefresh = 0;
	}

	private void Update()
	{
		if (Time.time - lastRefresh > 2)
		{
			Refresh();
			lastRefresh = Time.time;
		}
	}

	public void Refresh()
	{
		StartCoroutine(GetFilesList());
	}

	public IEnumerator GetFilesList()
	{

		string path = baseFolder.syncFolder;

		// get list of files in the current folder
		List<string> files = new List<string>(Directory.GetFiles(path));

		// get list of folders in the current folder
		string[] folders = Directory.GetDirectories(path);

		//Array.Reverse(files);
		// files.OrderBy(f => new FileInfo(f).LastWriteTime);

		// check if there are any new files
		if (files.Count != fileBrowserButtons.Count)
		{
			// remove the old buttons
			while (fileBrowserButtons.Count > 0)
			{
				Destroy(fileBrowserButtons[0].gameObject);
				fileBrowserButtons.RemoveAt(0);

				// this is not necessary, but it'll create a slight animation
				yield return null;
			}

			// add the new ones
			foreach (string file in files)
			{
				FileBrowserButton button = Instantiate(fileRowPrefab, fileListScrollViewContent).GetComponentInChildren<FileBrowserButton>();
				fileBrowserButtons.Add(button);
				button.Filename = Path.GetFileNameWithoutExtension(file);
				button.ModifiedDate = new FileInfo(file).LastWriteTime.ToString();
				button.Size = new FileInfo(file).Length / 1000000f + " MB";

				button.button.onClick.AddListener(delegate
				{
					button.vrFile = baseFolder.LoadFile(file);
					button.ShowingContextMenu = true;
				});

				// this is not necessary, but it'll create a slight animation
				yield return null;
			}
		}

		// update the context menu visibility for open files
		foreach (FileBrowserButton button in fileBrowserButtons)
		{
			button.ShowingContextMenu = button.vrFile != null && baseFolder.ShowingFile(button.vrFile.fullFileName);
		}
	}
}
