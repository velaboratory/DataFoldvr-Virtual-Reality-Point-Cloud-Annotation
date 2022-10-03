using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class TextFile : MonoBehaviour
{
	public string path;
	public TextMeshProUGUI t;

	public void updateContent()
	{
		string s = "";
		if (Path.GetExtension(path) == ".csv")
		{
			string[] lines = File.ReadAllLines(path);
			//maybe pretty print?
			bool first = true;
			foreach (string l in lines)
			{
				if (!first)
				{

					s = s + "\n";
				}
				else
				{
					first = false;
				}
				string[] cols = l.Split(',');
				foreach (string c in cols)
				{
					string z = c.Trim();
					if (z.Length < 20)
					{
						s = s + z.PadRight(20) + " ";
					}
					else
					{
						s = s + z.Substring(0, 17) + "... ";
					}
				}

			}
			t.text = s;

		}
		else
		{
			t.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			t.GetComponent<RectTransform>().offsetMax = new Vector2(0, t.GetComponent<RectTransform>().offsetMax.y);
			//t.horizontalOverflow = HorizontalWrapMode.Wrap;
			t.text = File.ReadAllText(path);
		}
	}
}
