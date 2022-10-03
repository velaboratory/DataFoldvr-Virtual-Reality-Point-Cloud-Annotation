using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuTabletController : MonoBehaviour
{
	public Transform tabletFileBrowserTab;
	public Transform tabletFileControlsTab;

	[Space] public FileBrowser tabletFileBrowser;
	public VRFileContextMenu contextMenuPrefab;
	private VRFileContextMenu contextMenu;

	// Start is called before the first frame update
	private void Start()
	{
		FolderManager.FileLoaded += file =>
		{
			tabletFileBrowserTab.gameObject.SetActive(false);
			tabletFileControlsTab.gameObject.SetActive(true);


			if (contextMenu != null) Destroy(contextMenu.gameObject);

			contextMenu = Instantiate(contextMenuPrefab, tabletFileControlsTab);
			contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Normal);
			contextMenu.vrFile = file;
			file.contextMenuTablet = contextMenu;

			RectTransform contextRect = contextMenu.GetComponent<RectTransform>();
			contextRect.anchorMin = new Vector2(0, 1);
			contextRect.anchorMax = new Vector2(1, 1);
			contextRect.pivot = new Vector2(0.5f, 1);
			contextRect.localRotation = Quaternion.identity;
			contextRect.offsetMax = new Vector2(0, -80);
			contextRect.offsetMin = new Vector2(12, 0);
			contextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 72);
		};

		FolderManager.FileUnloaded += s =>
		{
			tabletFileBrowserTab.gameObject.SetActive(true);
			tabletFileControlsTab.gameObject.SetActive(false);

			if (contextMenu != null) Destroy(contextMenu.gameObject);
		};
	}

	public void BackClicked()
	{
		FolderManager.UnloadFile();
	}

	public void UploadLogs()
	{
		unityutilities.Logger.UploadZip();
	}
}