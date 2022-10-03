using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileBrowserButton : MonoBehaviour
{
	public Button button;
	private float normalHeight;
	private RectTransform rect;
	public VRFile vrFile;
	private LayoutGroup vertLayout;

	private void Start()
	{
		rect = GetComponent<RectTransform>();
		vertLayout = GetComponentInParent<LayoutGroup>();
		normalHeight = rect.rect.height;
	}

	private string filename;

	public string Filename
	{
		get => filename;
		set
		{
			filename = value;
			if (filenameText != null) filenameText.text = value;
		}
	}

	[SerializeField] private TextMeshProUGUI filenameText;

	private string modifiedDate;

	public string ModifiedDate
	{
		get => modifiedDate;
		set
		{
			modifiedDate = value;
			if (modifiedDateText != null) modifiedDateText.text = value;
		}
	}

	[SerializeField] private TextMeshProUGUI modifiedDateText;

	//private string notes;
	//public string Notes {
	//	get => notes;
	//	set {
	//		notes = value;
	//		notesText.text = value;
	//	}
	//}
	//[SerializeField]
	//private TextMeshProUGUI notesText;

	private string size;

	public string Size
	{
		get => size;
		set
		{
			size = value;
			if (sizeText != null) sizeText.text = value;
		}
	}

	[SerializeField] private TextMeshProUGUI sizeText;

	private bool showingContextMenu;
	public VRFileContextMenu contextMenuPrefab;
	private VRFileContextMenu contextMenu;

	public bool ShowingContextMenu
	{
		get => showingContextMenu;
		set
		{
			showingContextMenu = value;
			if (value)
			{
				if (contextMenu == null && contextMenuPrefab != null)
				{
					rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, normalHeight * 2);
					contextMenu = Instantiate(contextMenuPrefab, transform);
					contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.FileBrowser);
					var contextRect = contextMenu.GetComponent<RectTransform>();
					contextRect.anchorMin = new Vector2(0, 0);
					contextRect.anchorMax = new Vector2(1, 0);
					contextRect.pivot = new Vector2(0, 0);
					contextRect.localRotation = Quaternion.identity;
					contextRect.offsetMax = new Vector2(0, 0);
					contextRect.offsetMin = new Vector2(0, 0);
					contextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
					contextMenu.vrFile = vrFile;
					LayoutRebuilder.ForceRebuildLayoutImmediate(vertLayout.GetComponent<RectTransform>());
				}
			}
			else
			{
				if (rect != null)
				{
					rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, normalHeight);
					LayoutRebuilder.ForceRebuildLayoutImmediate(vertLayout.GetComponent<RectTransform>());
				}

				if (contextMenu != null)
				{
					Destroy(contextMenu.gameObject);
					contextMenu = null;
				}
			}
		}
	}
}