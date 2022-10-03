using System;
using UnityEngine;
using TMPro;

public class Incrementer : MonoBehaviour
{
	public TMP_InputField inputField;
	public Action<int> OnIncrement;

	public int Value
	{
		get =>int.Parse(inputField.text);
		set => inputField.text = value.ToString();
	}
	public KeyCode incrementKey;
	public KeyCode decrementKey;
	
	

	private void Start()
	{
		inputField.onValueChanged.AddListener(str => { OnIncrement?.Invoke(Value); });
	}

	private void Update()
	{
		if (Input.GetKeyDown(incrementKey))
		{
			Increment(1);
		}
		if (Input.GetKeyDown(decrementKey))
		{
			Increment(-1);
		}
	}

	public void Increment(int inc)
	{
		inputField.text = $"{Value + inc}";
	}
}