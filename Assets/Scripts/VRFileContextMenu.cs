using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using unityutilities;
using Logger = unityutilities.Logger;

public class VRFileContextMenu : MonoBehaviour
{
	public VRFile vrFile;

	public enum ContextMenuMode
	{
		Normal,
		Segmenting,
		Counting,
		PaintCount,
		StudySegment,
		FileBrowser
	}

	public GameObject contextMenuButtonPrefab;
	public GameObject contextMenuTogglePrefab;
	public GameObject contextMenuSliderPrefab;
	public GameObject contextListIncrementer;
	public GameObject contextMenuInstructionsPrefab;
	public GameObject pcdLayerToggleRowPrefab;

	public bool teleportPlayerOnClick;

	[Header("Menu Items")] public RectTransform contextList;
	public RectTransform contextListFileBrowser;
	public RectTransform contextListSegmentColors;
	public RectTransform contextListPaintCount;
	public RectTransform contextListSegmenting;
	public RectTransform contextListStudySegment;
	public RectTransform contextListCounting;
	public RectTransform contextListHorizontalLayout;
	public NumpadController numpadController;

	private List<PCDLayerToggleRowController> layerButtons;

	public Material pointMaterial;
	private static readonly int PointSize = Shader.PropertyToID("_PointSize");
	private TextMeshProUGUI accuracyCounter;

	public bool studyMode = true;
	public bool vrMode = true;


	// Start is called before the first frame update
	private void Start()
	{
		//// wait a frame for the main file to call its own Start()
		//yield return null;

		Canvas c = GetComponentInParent<Canvas>();
		if (c && !c.worldCamera)
		{
			c.worldCamera = Camera.main;
		}

		GameObject obj; // reassigned a bunch

		#region File Browser

		// Teleport-to
		obj = Instantiate(contextMenuButtonPrefab, contextListFileBrowser);
		obj.GetComponent<Button>().onClick.AddListener(() =>
		{
			if (teleportPlayerOnClick)
			{
				PlayerManager.instance.TeleportToFile(vrFile);
			}
		});
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Teleport To";


		// CHANGE FILE SIZE
		obj = Instantiate(contextMenuSliderPrefab, contextListFileBrowser);
		obj.GetComponent<Slider>().value = (vrFile.FileScale + .25f) * 2f;
		obj.GetComponent<Slider>().onValueChanged.AddListener((value) => { vrFile.FileScale = (value + .25f) * 2f; });
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Change File Scale";


		// DUPLICATE
		obj = Instantiate(contextMenuButtonPrefab, contextListFileBrowser);
		obj.GetComponent<Button>().onClick.AddListener(() => PlayerManager.instance.DuplicateFile(vrFile));
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Duplicate File";

		// DELETE
		obj = Instantiate(contextMenuButtonPrefab, contextListFileBrowser);
		obj.GetComponent<Button>().onClick.AddListener(() => PlayerManager.instance.DeleteFile(vrFile));
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Delete File";


		// CLOSE FILE
		obj = Instantiate(contextMenuButtonPrefab, contextListFileBrowser);
		obj.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.instance.CloseFile(vrFile); });
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Close File";

		#endregion
		
		#region Main List

		if (!studyMode) {
			// CHANGE FILE SIZE
			obj = Instantiate(contextMenuSliderPrefab, contextList);
			obj.GetComponent<Slider>().value = (vrFile.FileScale + .25f) * 2f;
			obj.GetComponent<Slider>().onValueChanged.AddListener((value) => { vrFile.FileScale = (value + .25f) * 2f; });
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Change File Scale";

			// EDIT COUNT
			if (vrFile.fileType == VRFile.FileType.pcd || vrFile.fileType == VRFile.FileType.image)
			{
				obj = Instantiate(contextMenuButtonPrefab, contextList);
				obj.GetComponent<Button>().onClick.AddListener(() =>
				{
					PlayerManager.instance.EditFile(vrFile, VRFile.EditType.Count);
					if (teleportPlayerOnClick)
					{
						PlayerManager.instance.TeleportToFile(vrFile);
					}
				});
				obj.GetComponentInChildren<TextMeshProUGUI>().text = "Count (VR Only)";
			}

			// SEGMENT
			if (vrFile.fileType == VRFile.FileType.pcd || vrFile.fileType == VRFile.FileType.image)
			{
				obj = Instantiate(contextMenuButtonPrefab, contextList);
				obj.GetComponent<Button>().onClick.AddListener(() =>
				{
					PlayerManager.instance.EditFile(vrFile, VRFile.EditType.Segment);
					if (teleportPlayerOnClick)
					{
						PlayerManager.instance.TeleportToFile(vrFile);
					}
				});
				obj.GetComponentInChildren<TextMeshProUGUI>().text = "Paint Layers";
			}
		
		}

		// Paint Count
		if (vrFile.fileType == VRFile.FileType.pcd || vrFile.fileType == VRFile.FileType.image)
		{
			obj = Instantiate(contextMenuButtonPrefab, contextList);
			obj.GetComponent<Button>().onClick.AddListener(() =>
			{
				PlayerManager.instance.EditFile(vrFile, VRFile.EditType.PaintCount);
				if (teleportPlayerOnClick)
				{
					PlayerManager.instance.TeleportToFile(vrFile);
				}


				// this only needs to be set once, but it works here
				vrFile.pointCloudInstances[0].pcxRenderer.ShowLayer(2, false);
				vrFile.pointCloudInstances[0].pcxRenderer.ShowLayer(3, false);
				vrFile.pointCloudInstances[0].pcxRenderer.ShowLayer(4, false); // probably not used
				FolderManager.instance.colorPicker.colorIndex = 1;
			});
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Paint Count";
		}
		
		
		// Study Segment
		if (vrFile.fileType == VRFile.FileType.pcd)
		{
			obj = Instantiate(contextMenuButtonPrefab, contextList);
			obj.GetComponent<Button>().onClick.AddListener(() =>
			{
				PlayerManager.instance.EditFile(vrFile, VRFile.EditType.StudySegment);
				if (teleportPlayerOnClick)
				{
					PlayerManager.instance.TeleportToFile(vrFile);
				}

				// change the selected layer
				FolderManager.instance.colorPicker.colorIndex = 1;
			});
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Segment (Study)";
		}


		// CLOSE FILE
		obj = Instantiate(contextMenuButtonPrefab, contextList);
		obj.GetComponent<Button>().onClick.AddListener(() =>
		{
			PlayerManager.instance.CloseFile(vrFile);
			if (teleportPlayerOnClick)
			{
				PlayerManager.instance.TeleportToDesk();
			}
		});
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Close File";

		if (!studyMode)
		{
			// REFRESH
			obj = Instantiate(contextMenuButtonPrefab, contextList);
			obj.GetComponent<Button>().onClick.AddListener(() =>
			{
				PlayerManager.instance.CloseFile(vrFile);
				FolderManager.instance.LoadFile(vrFile.fullFileName);
			});
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Refresh";
		}

		if (vrFile.fileType == VRFile.FileType.pcd)
		{
			PCDLoader pcd = vrFile.pointCloudInstances[0];

			if (!studyMode)
			{
				// CHANGE POINT SIZE
				obj = Instantiate(contextMenuSliderPrefab, contextList);
				obj.GetComponent<Slider>().value = pcd.pcxRenderer.pointSize;
				obj.GetComponent<Slider>().onValueChanged.AddListener((value) =>
				{
					pcd.pcxRenderer.pointSize = value * .005f;
				});
				obj.GetComponentInChildren<TextMeshProUGUI>().text = "Change Point Scale";
			}
		}

		#endregion

		#region Counting

		// CLOSE EDITOR
		obj = Instantiate(contextMenuButtonPrefab, contextListCounting);
		obj.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.instance.CloseFileEditor(vrFile); });
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Close Editor";

		// SAVE EDITOR
		obj = Instantiate(contextMenuButtonPrefab, contextListCounting);
		obj.GetComponent<Button>().onClick.AddListener(() => PlayerManager.instance.SaveFileEditor(vrFile));
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Save Edits";

		// SHOW COUNT
		obj = Instantiate(contextMenuButtonPrefab, contextListCounting);
		TMP_Text textObj = obj.GetComponentInChildren<TextMeshProUGUI>();
		vrFile.CountChanged += (count) => textObj.text = $"Count: {count}";
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Count: ?";


		// INSTRUCTIONS
		// Counting
		obj = Instantiate(contextMenuInstructionsPrefab, contextListCounting);
		obj.GetComponentInChildren<TextMeshProUGUI>().text =
			"Press A/X to add a sphere marker. Press B/Y to remove an existing marker. Use the thumbstick up/down to change the size of the marker.";

		#endregion

		#region Segmenting

		// CLOSE EDITOR
		obj = Instantiate(contextMenuButtonPrefab, contextListSegmenting);
		obj.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.instance.CloseFileEditor(vrFile); });
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Close Editor";

		// SAVE EDITOR
		obj = Instantiate(contextMenuButtonPrefab, contextListSegmenting);
		obj.GetComponent<Button>().onClick.AddListener(() => PlayerManager.instance.SaveFileEditor(vrFile));
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Save Edits";


		// Segmenting
		obj = Instantiate(contextMenuInstructionsPrefab, contextListSegmenting);
		obj.GetComponentInChildren<TextMeshProUGUI>().text =
			"Select a layer by clicking on the colored boxes on the left. Use the trigger and drag to add points to a layer.";


		if (vrFile.fileType == VRFile.FileType.pcd)
		{
			PCDLoader pcd = vrFile.pointCloudInstances[0];
			// Show/hide layer colors
			obj = Instantiate(contextMenuTogglePrefab, contextListSegmenting);
			Toggle toggle = obj.GetComponent<Toggle>();
			toggle.isOn = true;
			pcd.ShowLayerColors = true;
			toggle.onValueChanged.AddListener((val) =>
			{
				if (pcd != null)
				{
					pcd.ShowLayerColors = val;
				}
			});
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Show/Hide Layer Colors";


			if (!vrMode)
			{
				// enable/disable depth limit
				obj = Instantiate(contextMenuTogglePrefab, contextListSegmenting);
				toggle = obj.GetComponent<Toggle>();
				toggle.isOn = true;
				toggle.onValueChanged.AddListener((val) =>
				{
					if (pcd != null)
					{
						pcd.LimitPaintDepth = val;
					}
				});
				obj.GetComponentInChildren<TextMeshProUGUI>().text = "Limit Paint Depth";
			}


			// layer colors
			int i = 1;
			if (pcd != null)
			{
				layerButtons = new List<PCDLayerToggleRowController>();

				PCDLayerToggleRowController pcdLayerToggleRowController =
					Instantiate(pcdLayerToggleRowPrefab, contextListSegmentColors)
						.GetComponent<PCDLayerToggleRowController>();
				pcdLayerToggleRowController.pcdFile = pcd;
				pcdLayerToggleRowController.layerColor = Color.white;
				pcdLayerToggleRowController.layerIndex = i;
				pcdLayerToggleRowController.allRows = layerButtons;

				layerButtons.Add(pcdLayerToggleRowController);

				foreach (Color color in FolderManager.instance.colorPicker.allColors)
				{
					pcdLayerToggleRowController = Instantiate(pcdLayerToggleRowPrefab, contextListSegmentColors)
						.GetComponent<PCDLayerToggleRowController>();
					pcdLayerToggleRowController.pcdFile = pcd;
					pcdLayerToggleRowController.layerColor = color;
					pcdLayerToggleRowController.layerIndex = ++i;
					pcdLayerToggleRowController.allRows = layerButtons;

					layerButtons.Add(pcdLayerToggleRowController);
				}

				// highlight layer 1 by default
				layerButtons.ForEach(r => r.selectedOutline.enabled = false);
				layerButtons[1].selectedOutline.enabled = true;
			}
		}

		#endregion
		
		#region Paint Count


		// CLOSE EDITOR
		obj = Instantiate(contextMenuButtonPrefab, contextListPaintCount);
		obj.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.instance.CloseFileEditor(vrFile); });
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Close Editor";
		obj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
		obj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);


		// PaintCount total counter
		GameObject totalCounterObj = Instantiate(contextMenuInstructionsPrefab, contextListPaintCount);
		TMP_Text totalCounter = totalCounterObj.GetComponentInChildren<TextMeshProUGUI>();
		totalCounter.text = "Total Count: 0";
		totalCounter.enableAutoSizing = false;
		totalCounter.fontSize = 23;
		// totalCounterObj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);

		// PaintCount Count Counter
		obj = Instantiate(contextListIncrementer, contextListPaintCount);
		Incrementer inc = obj.GetComponent<Incrementer>();
		if (vrMode && numpadController != null)
		{
			numpadController.inputField = inc.inputField;
		}


		Transform undoCommitHorizontalLayout = Instantiate(contextListHorizontalLayout, contextListPaintCount).transform;


		if (vrFile.fileType == VRFile.FileType.pcd)
		{
			PCDLoader pcd = vrFile.pointCloudInstances[0];


			if (!vrMode)
			{
				// enable/disable depth limit
				obj = Instantiate(contextMenuTogglePrefab, contextListPaintCount);
				Toggle toggle = obj.GetComponent<Toggle>();
				toggle.isOn = true;
				toggle.onValueChanged.AddListener((val) =>
				{
					if (pcd != null)
					{
						pcd.LimitPaintDepth = val;
					}
				});
				obj.GetComponentInChildren<TextMeshProUGUI>().text = "Limit Paint Depth";
			}

			// PaintCount Undo Button
			obj = Instantiate(contextMenuButtonPrefab, undoCommitHorizontalLayout);
			Button undoButton = obj.GetComponent<Button>();
			undoButton.interactable = false;
			undoButton.onClick.AddListener(() =>
			{
				if (pcd != null)
				{
					StudyMan.UndoCountCommit();
					undoButton.interactable = false;
					inc.Value = 0;
					pcd.UndoCommitLayer();
					totalCounter.GetComponent<TextMeshProUGUI>().text = $"Total Count: {StudyMan.GetTotalPaintCount()}";
				}
			});
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Undo";


			// PaintCount Count Button
			obj = Instantiate(contextMenuButtonPrefab, undoCommitHorizontalLayout);
			Button commitButton = obj.GetComponent<Button>();
			commitButton.onClick.AddListener(() =>
			{
				if (pcd != null)
				{
					StudyMan.AddCountCommit(inc.Value);
					undoButton.interactable = true;
					inc.Value = 0;
					pcd.CommitLayer();
					totalCounter.GetComponent<TextMeshProUGUI>().text = $"Total Count: {StudyMan.GetTotalPaintCount()}";
				}
			});
			obj.GetComponentInChildren<TextMeshProUGUI>().text = "Commit";
		}

		#endregion

		#region Study Segment
		
		
		// CLOSE EDITOR
		obj = Instantiate(contextMenuButtonPrefab, contextListStudySegment);
		obj.GetComponent<Button>().onClick.AddListener(() => { PlayerManager.instance.CloseFileEditor(vrFile); });
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Close Editor";
		obj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
		obj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);

		
		// Instructions
		obj = Instantiate(contextMenuInstructionsPrefab, contextListStudySegment);
		obj.GetComponentInChildren<TextMeshProUGUI>().text = "Paint the bamboo pole red.\nShift+left click to paint\nShift+right click to erase";
		// obj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);


		if (vrFile.fileType == VRFile.FileType.pcd)
		{
			PCDLoader pcd = vrFile.pointCloudInstances[0];


			if (!vrMode)
			{
				// enable/disable depth limit
				obj = Instantiate(contextMenuTogglePrefab, contextListStudySegment);
				Toggle toggle = obj.GetComponent<Toggle>();
				toggle.isOn = true;
				toggle.onValueChanged.AddListener((val) =>
				{
					if (pcd != null)
					{
						pcd.LimitPaintDepth = val;
						
						Logger.LogRow("events", "limit-paint-depth", val.ToString());
					}
				});
				TMP_Text limitDepthText = obj.GetComponentInChildren<TextMeshProUGUI>();
				limitDepthText.text = "Limit Paint Depth";
				limitDepthText.enableAutoSizing = false;
				limitDepthText.fontSize = 23;
				// obj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
			}
		}


		// accuracy relative to ref
		GameObject accuracyCounterObj = Instantiate(contextMenuInstructionsPrefab, contextListStudySegment);
		accuracyCounter = accuracyCounterObj.GetComponentInChildren<TextMeshProUGUI>();
		accuracyCounter.text = "Completion: --%";
		accuracyCounter.enableAutoSizing = false;
		accuracyCounter.fontSize = 23;
		// accuracyCounterObj.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);


		#endregion

		WorldMouseInputModule.FindCanvases();
	}

	private void Update()
	{
		if (accuracyCounter.gameObject.activeInHierarchy)
		{
			accuracyCounter.text = $"Completion: {vrFile.pointCloudInstances[0].refAccuracy * 100:N0}%";
		}
	}

	public void SetContextMenuMode(ContextMenuMode mode)
	{
		contextList.gameObject.SetActive(false);
		contextListSegmenting.gameObject.SetActive(false);
		contextListPaintCount.gameObject.SetActive(false);
		contextListCounting.gameObject.SetActive(false);
		contextListSegmentColors.gameObject.SetActive(false);
		contextListFileBrowser.gameObject.SetActive(false);
		contextListStudySegment.gameObject.SetActive(false);

		switch (mode)
		{
			case ContextMenuMode.Normal:
				contextList.gameObject.SetActive(true);
				break;
			case ContextMenuMode.Segmenting:
				contextListSegmenting.gameObject.SetActive(true);
				contextListSegmentColors.gameObject.SetActive(true);
				break;
			case ContextMenuMode.PaintCount:
				contextListPaintCount.gameObject.SetActive(true);
				break;
			case ContextMenuMode.Counting:
				contextListCounting.gameObject.SetActive(true);
				break;
			case ContextMenuMode.FileBrowser:
				contextListFileBrowser.gameObject.SetActive(true);
				break;
			case ContextMenuMode.StudySegment:
				contextListStudySegment.gameObject.SetActive(true);
				break;
		}
	}
}