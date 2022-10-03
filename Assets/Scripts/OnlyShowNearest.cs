using System.Collections.Generic;
using UnityEngine;
using unityutilities;

public class OnlyShowNearest : MonoBehaviour
{
	private List<(Transform, Transform)> list = new List<(Transform, Transform)>();
	private Transform target;
	private int lastShown = -1;

	private void Start()
	{
		target = Camera.main.transform;
	}

	// Update is called once per frame
	private void Update()
	{
		Vector3 targetPos;
		if (InputMan.Button1(Side.Left))
		{
			targetPos = PlayerManager.instance.leftHand.transform.position;
		}
		else if (InputMan.Button1(Side.Right))
		{
			targetPos = PlayerManager.instance.rightHand.transform.position;
		}
		else
		{
			targetPos = target.position;
		}

		float minDist = float.MaxValue;
		int minIndex = -1;
		for (int i = 0; i < list.Count; i++)
		{
			float dist = Vector3.Distance(list[i].Item1.position, targetPos);
			if (dist < minDist)
			{
				minDist = dist;
				minIndex = i;
			}
		}

		// if the visible obj has changed
		if (lastShown != minIndex)
		{
			// show the new one instead
			if (lastShown > 0) list[lastShown].Item2.gameObject.SetActive(false);
			list[minIndex].Item2.gameObject.SetActive(true);
			lastShown = minIndex;
		}
	}

	public void Add(Transform parent, Transform child)
	{
		list.Add((parent, child));
	}
}