using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using unityutilities;

public class FolderManager : MonoBehaviour
{
	public string syncFolder;
	private float offset;
	public Dictionary<string, VRFile> loadedFiles;

	public VRFile filePrefab;
	public VRFile imageFilePrefab;

	public ColorPicker colorPicker;

	public static FolderManager instance;

	public static Action<VRFile> FileLoaded;
	public static Action<string> FileUnloaded;

	private void Start()
	{
		Init();
	}

	private void Init()
	{
		instance = this;

		syncFolder = PlayerPrefsJson.GetString("datafoldvr_path", syncFolder);

		loadedFiles = new Dictionary<string, VRFile>();

		StartCoroutine(SyncFiles());
	}

	/// <summary>
	/// Loads files from the folder specified
	/// </summary>
	private IEnumerator SyncFiles()
	{
		while (true)
		{
			string[] files = Directory.GetFileSystemEntries(syncFolder);

			//first check all of our files to see if they are still there (they could have been deleted)
			Dictionary<string, VRFile> newLoadedFiles = new Dictionary<string, VRFile>();
			foreach (KeyValuePair<string, VRFile> kvp in loadedFiles)
			{
				// delete removed files from the list
				if (!files.Contains(kvp.Key))
				{
					//delete it
					kvp.Value.destroy();
					Destroy(kvp.Value.gameObject);
				}
				// add files to the list that are still there
				else
				{
					newLoadedFiles.Add(kvp.Key, kvp.Value);
				}
			}

			loadedFiles = newLoadedFiles;

			//the check to see if there are new files, and add if there are
			//foreach (string s in files)
			//{
			//	LoadFile(s);
			//}

			RefreshContentsAndPositionForNewObjects();

			yield return new WaitForSeconds(1.0f);
		}
	}

	private void RefreshContentsAndPositionForNewObjects()
	{
		// loop through the current files (dict: filename, fileobj)
		foreach (KeyValuePair<string, VRFile> kvp in loadedFiles)
		{
			// only set the position if this is the first time loading this object
			bool first = !PlayerPrefsJson.HasKey(Path.GetFileName(kvp.Key),
				Path.Combine(syncFolder, "datafoldvr_prefs.json"));


			// Set the position of the objects
			Dictionary<string, object> data = PlayerPrefsJson.GetDictionary(Path.GetFileName(kvp.Key),
				new Dictionary<string, object>
				{
					{"pos", transform.position + transform.forward * offset + Vector3.up * .5f},
					{"rot", kvp.Value.transform.eulerAngles},
					{"localScale", kvp.Value.transform.localScale},
					{"contentPos", Vector3.zero},
					{"contentRot", new Vector3(-90, 0, 0)},
					{"contentScale", Vector3.one},
					{"pointSize", .01f},
				}, Path.Combine(syncFolder, "datafoldvr_prefs.json"));

			kvp.Value.transform.position = data["pos"].ToVector3();
			kvp.Value.transform.eulerAngles = data["rot"].ToVector3();
			kvp.Value.transform.localScale = data["localScale"].ToVector3();

			if (first) offset -= 1.1f;

			// Set the content of the file by passing in the filename
			kvp.Value.UpdateContents(kvp.Key, this);
		}
	}

	/// <summary>
	/// Loads a file into the world
	/// </summary>
	/// <param name="s">The full file name of the file</param>
	public VRFile LoadFile(string s)
	{
		if (!loadedFiles.ContainsKey(s))
		{
			VRFile f = Instantiate(VRFile.GetFileType(s) == VRFile.FileType.image ? imageFilePrefab : filePrefab,
				transform.position, transform.rotation, transform);

			f.gameObject.name = Path.GetFileName(s);
			loadedFiles.Add(s, f);
			RefreshContentsAndPositionForNewObjects();
			
			FileLoaded?.Invoke(f);

			return f;
		}
		else
		{
			return null;
		}
	}

	public void UnloadFile(string s)
	{
		if (loadedFiles.ContainsKey(s))
		{
			Destroy(loadedFiles[s].gameObject);
			loadedFiles.Remove(s);
			
			FileUnloaded?.Invoke(s);
		}
	}

	public static void UnloadFile()
	{
		instance.UnloadFile(instance.loadedFiles.Last().Key);
	}

	public bool ShowingFile(string s)
	{
		return loadedFiles.ContainsKey(s);
	}
}