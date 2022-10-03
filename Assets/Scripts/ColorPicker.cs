using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
	public Color color = Color.red;
	public int colorIndex = 0;
	public List<Color> allColors = new List<Color>();
	public Transform selectionOutline;

	private Button[] colorButtons;

	[Serializable]
	public class ColorPickedEvent : UnityEvent { }

	[Space]
	[Tooltip("Optional. Still sends the normal actions")]
	public ColorPickedEvent colorChanged;

	private void Start()
	{
		colorButtons = GetComponentsInChildren<Button>();
		color = colorButtons[0].colors.normalColor;

		foreach (Button item in colorButtons)
		{
			allColors.Add(item.colors.normalColor);
			item.onClick.AddListener(() =>
			{
				SetColor(item.transform.localPosition, item.colors.normalColor);
				Debug.Log(colorIndex);
			});
		}

		selectionOutline.gameObject.SetActive(true);
		selectionOutline.transform.localPosition = colorButtons[0].transform.localPosition;
	}

	public void SetColor(Vector3 position, Color color)
	{
		colorChanged?.Invoke();
		this.color = color;
		colorIndex = allColors.FindIndex(c => c == color);

		selectionOutline.gameObject.SetActive(true);
		selectionOutline.transform.localPosition = position;
	}

	[Obsolete("maybe useful for networking, idk")]
	public void SetColor(Vector3 position, string hex, int index = 0)
	{
		ColorUtility.TryParseHtmlString(hex, out color);
		colorChanged?.Invoke();

		selectionOutline.gameObject.SetActive(true);
		selectionOutline.transform.localPosition = position;
	}
}
