using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MenuTablet;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;
using unityutilities;
using Logger = unityutilities.Logger;

public class StudyMan : MonoBehaviour
{	
	public static int participantId;
	public TMP_InputField participantInputField;

	[Space]

	public KeyCode vrModeToggle;
	public GameObject vrCamera;
	public GameObject flatCamera;
	[ReadOnly] public bool vrModeEnabled = true;

	[Space]

	public WorldMouseInputModule worldMouseInputModule;
	public InputSystemUIInputModule inputSystemUIInputModule;
	public CursorWorldMouse vrCameraCursor;
	public CursorWorldMouse flatCameraCursor;

	public GameObject flatCanvas;

	public MenuTabletMover menuTablet;

	private static List<int> countCommits = new List<int>();
	private static bool undoAvailable = false;

	public MousePlayer mousePlayer;


	[Space] public KeyCode resetPlayer = KeyCode.Pause;
	public KeyCode loadFile1Demo = KeyCode.F1;
	public KeyCode loadFile2Demo = KeyCode.F2;
	public KeyCode loadFile1 = KeyCode.F3;
	public KeyCode loadFile2 = KeyCode.F4;


	private void Start()
	{
		MenuTabletMover.OnShow += (_) =>
		{
			Logger.LogRow("events", "toggle-tablet", "show");
		};
		MenuTabletMover.OnHide += (_) =>
		{
			Logger.LogRow("events", "toggle-tablet", "hide");
		};
	}


	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.PageUp)) IncrementParticipantId(1);
		if (Input.GetKeyDown(KeyCode.PageDown)) IncrementParticipantId(-1);

		if (Input.GetKeyDown(vrModeToggle))
		{
			vrModeEnabled = !vrModeEnabled;

			vrCamera.SetActive(vrModeEnabled);
			flatCamera.SetActive(!vrModeEnabled);

			//vrCameraCursor.enabled = vrModeEnabled;
			//flatCameraCursor.enabled = !vrModeEnabled;

			worldMouseInputModule.enabled = vrModeEnabled;
			inputSystemUIInputModule.enabled = !vrModeEnabled;

			menuTablet.head = vrModeEnabled ? vrCamera.transform : flatCamera.transform;

			// flatCanvas.SetActive(!vrModeEnabled);

			XRSettings.enabled = vrModeEnabled;
			
			WorldMouseInputModule.FindCanvases();
			
			
			Logger.LogRow("events", "toggle-vr-mode", vrModeEnabled.ToString());
		}


		if (Input.GetKeyDown(loadFile1Demo))
		{
			PlayerManager.instance.CloseAllFiles();
			FolderManager.instance.LoadFile(Path.Combine(FolderManager.instance.syncFolder, "segment_training.pcd"));
			PlayerManager.instance.EditFile(FolderManager.instance.loadedFiles.Values.ToArray()[0], VRFile.EditType.StudySegment);
			ResetCounts();
			
			// change the selected layer
			FolderManager.instance.colorPicker.colorIndex = 1;
			
			Logger.LogRow("events", "load-file", "1-Demo");
		}
		if (Input.GetKeyDown(loadFile2Demo))
		{
			PlayerManager.instance.CloseAllFiles();
			FolderManager.instance.LoadFile(Path.Combine(FolderManager.instance.syncFolder, "count_training.pcd"));
			VRFile file = FolderManager.instance.loadedFiles.Values.ToArray()[0];
			PlayerManager.instance.EditFile(file, VRFile.EditType.PaintCount);
			ResetCounts();
			
			// this only needs to be set once, but it works here
			file.pointCloudInstances[0].pcxRenderer.ShowLayer(2, false);
			file.pointCloudInstances[0].pcxRenderer.ShowLayer(3, false);
			file.pointCloudInstances[0].pcxRenderer.ShowLayer(4, false); // probably not used
			FolderManager.instance.colorPicker.colorIndex = 1;
			
			Logger.LogRow("events", "load-file", "2-Demo");
		}
		if (Input.GetKeyDown(loadFile1))
		{
			PlayerManager.instance.CloseAllFiles();
			FolderManager.instance.LoadFile(Path.Combine(FolderManager.instance.syncFolder, "bamboo_stick_4.5M_with_reflayer.pcd"));
			PlayerManager.instance.EditFile(FolderManager.instance.loadedFiles.Values.ToArray()[0], VRFile.EditType.StudySegment);
			ResetCounts();
			
			// change the selected layer
			FolderManager.instance.colorPicker.colorIndex = 1;
			
			Logger.LogRow("events", "load-file", "1-Study");
		}
		if (Input.GetKeyDown(loadFile2))
		{
			PlayerManager.instance.CloseAllFiles();
			FolderManager.instance.LoadFile(Path.Combine(FolderManager.instance.syncFolder, "plot_7.8M.pcd"));
			VRFile file = FolderManager.instance.loadedFiles.Values.ToArray()[0];
			PlayerManager.instance.EditFile(file, VRFile.EditType.PaintCount);
			
			// this only needs to be set once, but it works here
			file.pointCloudInstances[0].pcxRenderer.ShowLayer(2, false);
			file.pointCloudInstances[0].pcxRenderer.ShowLayer(3, false);
			file.pointCloudInstances[0].pcxRenderer.ShowLayer(4, false); // probably not used
			FolderManager.instance.colorPicker.colorIndex = 1;
			ResetCounts();
			
			Logger.LogRow("events", "load-file", "2-Study");
		}

		if (Input.GetKeyDown(KeyCode.ScrollLock))
		{
			PlayerManager.instance.EditFile(FolderManager.instance.loadedFiles.Values.ToArray()[0], VRFile.EditType.None);
		}
		
		
		// Timer stuff
		if (Input.GetKeyDown(KeyCode.F5))
		{
			TimerWithLogging.instance.StopTimer();
		}
		if (Input.GetKeyDown(KeyCode.F7))
		{
			TimerWithLogging.instance.StartTimer();
		}
		if (Input.GetKeyDown(KeyCode.F6))
		{
			TimerWithLogging.instance.ResetTimer();
		}
		
		
		

		VRFile instanceCurrentlyEditedFile = PlayerManager.instance.currentlyEditedFile;
		
		
		if (Input.GetKeyDown(resetPlayer))
		{
			if (FolderManager.instance.loadedFiles.Count > 0)
			{
				PlayerManager.instance.TeleportToFile(FolderManager.instance.loadedFiles.Values.ToArray()[0]);
			}

			PlayerManager.instance.leftHand.handMarker.localScale = Vector3.one * .05f;
			PlayerManager.instance.rightHand.handMarker.localScale = Vector3.one * .05f;
			mousePlayer.cursorCircle.localScale = Vector3.one * 1;
			mousePlayer.SetFreeCamView();
			ResetCounts();
			
			Logger.LogRow("events", "reset-player");
		}

		Logger.LogRow("study_state",
			vrModeEnabled.ToString(),
			
			// file info
			instanceCurrentlyEditedFile?.fileName,
			instanceCurrentlyEditedFile?.transform.position.x.ToString(),
			instanceCurrentlyEditedFile?.transform.position.y.ToString(),
			instanceCurrentlyEditedFile?.transform.position.z.ToString(),
			instanceCurrentlyEditedFile?.transform.localScale.x.ToString(),
			instanceCurrentlyEditedFile?.PointSize.ToString(),
			
			// edit mode stuff
			PlayerManager.instance.CurrentEditMode.ToString(),
			TimerWithLogging.instance.CurrentTime.ToString(CultureInfo.InvariantCulture),
			instanceCurrentlyEditedFile?.pointCloudInstances?[0].LimitPaintDepth.ToString(),
			
			// paint count
			GetTotalPaintCount().ToString(),
			countCommits.Count > 0 ? countCommits?.Last().ToString() : "0",
			
			// study segment
			instanceCurrentlyEditedFile?.pointCloudInstances?[0].refAccuracy.ToString(),
			instanceCurrentlyEditedFile?.pointCloudInstances?[0].falsePositives.ToString(),
			instanceCurrentlyEditedFile?.pointCloudInstances?[0].truePositives.ToString(),
			instanceCurrentlyEditedFile?.pointCloudInstances?[0].falseNegatives.ToString(),
			instanceCurrentlyEditedFile?.pointCloudInstances?[0].trueNegatives.ToString()
		);
	}

	public void IncrementParticipantId(int increment)
	{
		participantId += increment;
		participantInputField.text = participantId.ToString();
		
		Logger.LogRow("events", "set-participant-id", participantId.ToString());
	}

	public void SetParticipantId(string id)
	{
		if (int.TryParse(id, out participantId))
		{
			Debug.LogError("Failed to parse participant ID");
		}
		
		Logger.LogRow("events", "set-participant-id", participantId.ToString());
	}

	public static void AddCountCommit(int count)
	{
		int prevCount = countCommits.Sum();
		countCommits.Add(count);
		undoAvailable = true;

		Logger.LogRow("events", "add-count-commit", prevCount.ToString(), countCommits.Sum().ToString());
	}

	public static int GetTotalPaintCount()
	{
		return countCommits.Sum();
	}


	public static void UndoCountCommit()
	{
		int prevCount = countCommits.Sum();
		if (!undoAvailable) return;
		countCommits.RemoveAt(countCommits.Count - 1);
		undoAvailable = false;

		Logger.LogRow("events", "undo-count-commit", prevCount.ToString(), countCommits.Sum().ToString());
	}
	
	
	public static void ResetCounts()
	{
		Logger.LogRow("events", "reset-count", countCommits.Sum().ToString());
		
		countCommits.Clear();
		undoAvailable = false;
		
		VRFile instanceCurrentlyEditedFile = PlayerManager.instance.currentlyEditedFile;
		instanceCurrentlyEditedFile?.CountChanged?.Invoke(countCommits.Sum());
	}
}
