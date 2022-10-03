using unityutilities;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

public class PlayerManager : MonoBehaviour
{
	public static PlayerManager instance;
	public Movement movement;
	public Hand leftHand;
	public Hand rightHand;
	[ReadOnly] public VRFile currentlyEditedFile;
	public Transform deskPosition;
	private VRFile.EditType currentEditMode;
	public Transform cam => movement != null ? movement.rig.head : Camera.main.transform;

	public VRFile.EditType CurrentEditMode
	{
		get => currentEditMode;
		private set
		{
			currentEditMode = value;
				
			// if in vr mode
			if (instance.movement != null)
			{
				switch (value)
				{
					case VRFile.EditType.Count:
					case VRFile.EditType.Segment:
					case VRFile.EditType.PaintCount:
					case VRFile.EditType.StudySegment:
						//movement.grabAir = true;
						movement.teleportingMovement = false;
						break;
					case VRFile.EditType.None:
						//movement.grabAir = false;
						movement.teleportingMovement = true;
						break;
				}
			}
		}
	}

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		if (currentEditMode != VRFile.EditType.None)
		{
			if (InputMan.ThumbstickPressDown(Side.Both))
			{
				CloseFileEditor(currentlyEditedFile);
			}
		}

		if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
		{
			if (Input.GetKeyDown(KeyCode.D))
			{
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					SaveDeskPosition();
				}
				else
				{
					TeleportToDesk();
				}
			}
		}

		if (InputMan.ThumbstickPressDown(Side.Both))
		{
			TeleportToDesk();
		}
	}


	public void EditFile(VRFile file, VRFile.EditType editType)
	{
		CurrentEditMode = editType;
		currentlyEditedFile = file;
		file.CurrentEditMode = editType;
	}

	/// <summary>
	/// Creates a duplicate of the file in the folder
	/// </summary>
	/// <param name="file">The file to duplicate</param>
	/// <returns>The new file's filename</returns>
	public string DuplicateFile(VRFile file)
	{
		// TODO not implemented
		Debug.Log("Duplicate File: " + file.fileName);
		return "";
	}

	/// <summary>
	/// Removes the file from the filesystem
	/// </summary>
	/// <param name="file">The file to delete</param>
	public void DeleteFile(VRFile file)
	{
		Debug.Log("Delete File: " + file.fileName);
		File.Delete(file.fullFileName);
	}

	/// <summary>
	/// Done editing a file
	/// </summary>
	/// <param name="file">The file to close</param>
	public void CloseFileEditor(VRFile file)
	{
		CurrentEditMode = VRFile.EditType.None;
		file.CurrentEditMode = VRFile.EditType.None;
	}

	/// <summary>
	/// Done editing a file
	/// </summary>
	/// <param name="file">The file to close</param>
	public void SaveFileEditor(VRFile file)
	{
		switch (CurrentEditMode)
		{
			case VRFile.EditType.Count:
			{
				StreamWriter writer = File.AppendText(Path.Combine(file.folderName,
					Path.GetFileNameWithoutExtension(file.fileName) + "_count.txt"));
				writer.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss") + "," +
				                 file.countCloud.data.Count);
				writer.Close();
				
				file.countCloud?.Save(Path.Combine(file.folderName,
					Path.GetFileNameWithoutExtension(file.fileName) + "_countcloud.pcd"));
				break;
			}
			case VRFile.EditType.Segment:
			{
				if (file.pointCloudInstances.Count > 0)
				{
					PCDLoader pcdLoader = file.pointCloudInstances[0];

					pcdLoader.SavePCDFile("_annotated");
				}

				break;
			}
		}
	}

	/// <summary>
	/// Done editing a file
	/// </summary>
	/// <param name="file">The file to close</param>
	public void CloseFile(VRFile file)
	{
		CurrentEditMode = VRFile.EditType.None;
		file.CurrentEditMode = VRFile.EditType.None;
		FolderManager.instance.UnloadFile(file.fullFileName);
	}


	public void CloseAllFiles()
	{
		CurrentEditMode = VRFile.EditType.None;
		VRFile[] files = FolderManager.instance.loadedFiles.Values.ToArray();
		foreach (VRFile file in files)
		{
			file.CurrentEditMode = VRFile.EditType.None;
			FolderManager.instance.UnloadFile(file.fullFileName);
		}
	}


	public void SaveDeskPosition()
	{
		Vector3 deskPos = movement.rig.head.position;
		deskPos.y = 0;
		Vector3 deskRot = movement.rig.head.forward;
		deskRot.y = 0;

		PlayerPrefsJson.SetVector3("DeskPos", deskPos);
		PlayerPrefsJson.SetVector3("DeskForward", deskRot);
	}

	public void TeleportToDesk()
	{
		Vector3 pos =
			PlayerPrefsJson.GetVector3("DeskPos", deskPosition != null ? deskPosition.position : Vector3.zero);
		Vector3 rot =
			PlayerPrefsJson.GetVector3("DeskForward", deskPosition != null ? deskPosition.forward : Vector3.zero);

		movement.TeleportTo(pos, rot);
	}

	public void TeleportToFile(VRFile file)
	{
		if (movement == null) return;
		Vector3 position = file.transform.position - Vector3.forward * .5f;
		position.y = -movement.rig.head.localPosition.y + 1f;
		movement.TeleportTo(position, Vector3.forward);
	}
}