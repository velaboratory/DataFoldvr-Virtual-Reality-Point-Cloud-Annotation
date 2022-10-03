using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using unityutilities;

public class PCDLayerToggleRowController : MonoBehaviour
{
	[ReadOnly] public PCDLoader pcdFile;
	[ReadOnly] public Color layerColor;
	[ReadOnly] public int layerIndex;
	[ReadOnly] public RectTransform backgroundImage;
	[ReadOnly] public List<PCDLayerToggleRowController> allRows;

	public Button selectLayerButton;
	public Image sampleColorImage;
	public Toggle toggleVisibility;
	public TextMeshProUGUI toggleLabel;
	public Button deleteLayerPoints;
	public Button saveLayerPoints;
	public Outline selectedOutline;

	private void Start()
	{
		sampleColorImage.color = layerColor;
		toggleLabel.text = "LAYER " + layerIndex;
		pcdFile.hiddenLayers[layerIndex] = false;

		selectLayerButton.onClick.AddListener(SelectLayer);
		deleteLayerPoints.onClick.AddListener(DeleteLayerPoints);
		saveLayerPoints.onClick.AddListener(SaveLayerPoints);
	}

	public void ShowLayer(bool show)
	{
		// pcdFile.hiddenLayers[layerIndex] = !show;
		// pcdFile.RefreshGameObjects();
		pcdFile.pcxRenderer.ShowLayer(layerIndex, show);
	}

	private void SelectLayer()
	{
		FolderManager.instance.colorPicker.colorIndex = layerIndex - 1;
		FolderManager.instance.colorPicker.color = layerIndex == 0
			? Color.clear
			: FolderManager.instance.colorPicker.allColors[layerIndex - 1];

		allRows.ForEach(r => r.selectedOutline.enabled = false);
		selectedOutline.enabled = true;
	}

	public void DeleteLayerPoints()
	{
		// TODO 
	}

	private void SaveLayerPoints()
	{
		pcdFile.SavePCDFile(fileNameAppend:$"_layer{layerIndex}", chooseLayer: layerIndex);
	}
}