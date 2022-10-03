using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestComputeShaderManager : MonoBehaviour
{
	public ComputeShader computeShader;

	public RenderTexture renderTexture;

	// Start is called before the first frame update
	private void Start()
	{
		renderTexture = new RenderTexture(1024, 1024, 24) {enableRandomWrite = true};
		renderTexture.Create();

		computeShader.SetTexture(0, "Result", renderTexture);
		computeShader.Dispatch(0, renderTexture.width/8, renderTexture.height/8, 1);
	}

	// Update is called once per frame
	void Update()
	{
	}
}