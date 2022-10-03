using System.Collections;
using UnityEngine;

public class RepeatedlyApplyWorldCameraToCanvas : MonoBehaviour
{
	private Camera cam;

	public Canvas canvas;

	private IEnumerator Start()
	{
		cam = Camera.main;
		while (true)
		{
			canvas.worldCamera = cam;
			yield return new WaitForSeconds(1);
		}
	}
}