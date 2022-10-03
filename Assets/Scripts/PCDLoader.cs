using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine.Networking;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DataFoldvrPcx;
using TMPro;
using Unity.Mathematics;
using UnityEngine.Serialization;
using unityutilities;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;


public struct HalfPoint
{
	public Vector3 position;
	public float distance;
}


public class PCDLoader : MonoBehaviour
{
	public string filename;
	[NonSerialized] public VRFile vrFile;

	[Tooltip("Reads filename from StreamingAssets at start for independent usage")]
	public bool autoLoadAtStart = false;

	public float cullDistance = 200f;

	public PCDFile pcdFile;

	private LODGroup lodGroup;

	private StochasticOctree octree;
	public float[] lodLayerProbabilities = new float[] {.1f, .3f, 1};

	[FormerlySerializedAs("octreeMaterial")]
	public Material pointMaterial;

	private List<List<Renderer>> lodRenderers = new List<List<Renderer>>();
	public Action<PCDLoader> FinishedLoading;

	// progress bar
	public Transform progressSliderCanvas;
	public Slider progressSlider;
	[Range(0, 1)] [ReadOnly] public float loadProgress = 1;

	/// <summary>
	/// Whether to show layers as colored points
	/// </summary>
	public bool ShowLayerColors
	{
		get => showLayerColors;
		set
		{
			showLayerColors = value;
			Shader.SetGlobalInt("_ShowLayerColors", value ? 1 : 0);
		}
	}
	private bool showLayerColors;

	public bool LimitPaintDepth { get; set; } = true;

	/// <summary>
	/// false to show normally, true to hide
	/// </summary>
	public Dictionary<int, bool> hiddenLayers = new Dictionary<int, bool>();

	public ComputeBuffer pointsBufferGPU;
	private PointCloudData.Point[] pointsBufferCPU;

	public ComputeShader handSegmentingComputeShader;
	public ComputeShader commitLayerComputerShader;
	public ComputeShader percentCorrectComputeShader;
	public ComputeShader closestPointComputeShader;
	public ComputeShader mouseSegmentingSpitOutComputeShader;
	public bool[] handsActive = new bool[2];
	public float refAccuracy;
	public double falsePositives;
	public double truePositives;
	public double falseNegatives;
	public double trueNegatives;


	public PointCloudRendererWithLOD pcxRenderer;

	private Camera mainCamera;


	private void Start()
	{
		if (progressSliderCanvas != null) progressSliderCanvas.SetParent(transform.parent);

		mainCamera = Camera.main;

		if (autoLoadAtStart) LoadPCDPoints(true);
	}


	private void Update()
	{
		if (pcdFile != null)
		{
			if (pcdFile.loadFinishedFlag)
			{
				pcdFile.loadFinishedFlag = false;
				RefreshGameObjects();
				
				FinishedLoading?.Invoke(this);
			}
			

			if (progressSlider != null)
			{
				if (pcdFile.loadProgress >= 1)
				{
					progressSlider.gameObject.SetActive(false);
				}
				else
				{
					progressSlider.gameObject.SetActive(true);
					progressSlider.value = pcdFile.loadProgress;
					progressSlider.transform.parent.LookAt(mainCamera.transform.position);
				}
			}

			if (pcdFile.loadProgress < 1)
			{
				loadProgress = pcdFile.loadProgress;
				return;
			}

			if (vrFile.CurrentEditMode == VRFile.EditType.Segment || 
			    vrFile.CurrentEditMode == VRFile.EditType.PaintCount || 
				vrFile.CurrentEditMode == VRFile.EditType.StudySegment 
				)
			{
				if (handsActive[0] || handsActive[1])
				{
					int kernel = handSegmentingComputeShader.FindKernel("CSMain");
					handSegmentingComputeShader.SetBuffer(kernel, "_PointBuffer", pointsBufferGPU);
					handSegmentingComputeShader.Dispatch(0, pointsBufferGPU.count / 1024, 1, 1);
				}
				
				
				// calculate accuracy
				int accuracyKernel = percentCorrectComputeShader.FindKernel("CSMain");
				percentCorrectComputeShader.SetBuffer(accuracyKernel, "_PointBuffer", pointsBufferGPU);
				uint[] correctCount = {0, 0, 0, 0};
				ComputeBuffer correctCountBuffer = new ComputeBuffer(correctCount.Length, sizeof(uint));
				correctCountBuffer.SetData(correctCount);
				percentCorrectComputeShader.SetBuffer(accuracyKernel, "_CorrectCount", correctCountBuffer);
				percentCorrectComputeShader.Dispatch(0, pointsBufferGPU.count / 1024, 1, 1);
				correctCountBuffer.GetData(correctCount);
				correctCountBuffer.Release();

				truePositives = correctCount[0];
				falsePositives = correctCount[1];
				trueNegatives = correctCount[2];
				falseNegatives = correctCount[3];
				
				double precision = truePositives / (truePositives + falsePositives);
				double recall = truePositives / (truePositives + falseNegatives);

				double f1Score = 0;
				if (!double.IsNaN(precision) && !double.IsNaN(recall) && precision + recall != 0)
				{
					f1Score = 2 * (precision * recall) / (precision + recall);
				}
				refAccuracy = (float) f1Score;
			}
		}
	}

	private void OnDestroy()
	{
		pointsBufferGPU.Release();
		pointsBufferCPU = null;
	}

	public void LoadPCDPoints(bool useStreamingAssets = false)
	{
		StartCoroutine(useStreamingAssets ? ReadFromStreamingAssets() : ReadFromFile());
	}

	private IEnumerator ReadFromStreamingAssets()
	{
		string filePath = Path.Combine(Application.streamingAssetsPath, filename);

		UnityWebRequest www = UnityWebRequest.Get(filePath);
		yield return www.SendWebRequest();

		BinaryReader reader = new BinaryReader(new MemoryStream(www.downloadHandler.data));

		pcdFile = new PCDFile();
		Thread readThread = new Thread(() => pcdFile.LoadThread(reader));
		readThread.Start();
	}

	private IEnumerator ReadFromFile()
	{
		string filePath = filename;

		UnityWebRequest www = UnityWebRequest.Get(filePath);
		yield return www.SendWebRequest();

		BinaryReader reader = new BinaryReader(new MemoryStream(www.downloadHandler.data));

		pcdFile = new PCDFile();
		Thread readThread = new Thread(() => pcdFile.LoadThread(reader));
		readThread.Start();
	}

	/// <summary>
	/// Saves a the pcd file to disk with optional parameters
	/// </summary>
	/// <param name="fileNameAppend">Appends the desired text right before '.pcd'</param>
	/// <param name="chooseLayer">Which layer to save, -1 for all visible layers, -2 for all layers</param>
	public void SavePCDFile(string fileNameAppend = "", int chooseLayer = -2)
	{
		LoadFromComputeBuffer();

		if (!filename.Contains(fileNameAppend))
		{
			filename = filename.Insert(filename.LastIndexOf(".pcd"), fileNameAppend);
		}

		pcdFile.Save(filename, PCDDataType.binary, chooseLayer);
	}

	public void CommitLayer()
	{
		int kernel = commitLayerComputerShader.FindKernel("CSMain");
		commitLayerComputerShader.SetBuffer(kernel, "_PointBuffer", pointsBufferGPU);
		commitLayerComputerShader.SetBool("_Undo", false);
		commitLayerComputerShader.Dispatch(0, pointsBufferGPU.count / 1024, 1, 1);
	}
	
	public void UndoCommitLayer()
	{
		int kernel = commitLayerComputerShader.FindKernel("CSMain");
		commitLayerComputerShader.SetBuffer(kernel, "_PointBuffer", pointsBufferGPU);
		commitLayerComputerShader.SetBool("_Undo", true);
		commitLayerComputerShader.Dispatch(0, pointsBufferGPU.count / 1024, 1, 1);
	}


	public void RefreshGameObjects()
	{
		// StartCoroutine(BuildOctree());
		CreateComputeBuffer();

		pcxRenderer.leftHandPosition = PlayerManager.instance.leftHand.transform;
		pcxRenderer.rightHandPosition = PlayerManager.instance.rightHand.transform;
	}

	private void CreateComputeBuffer()
	{
		// create the array of points
		pointsBufferCPU = new PointCloudData.Point[pcdFile.data.Count];
		for (int i = 0; i < pcdFile.data.Count; i++)
		{
			pointsBufferCPU[i] = new PointCloudData.Point
			{
				position = pcdFile.data[i].position,
				color = PointCloudData.EncodeColorAndLayer(pcdFile.data[i].color, pcdFile.data[i].layer),
				ref_layer = pcdFile.data[i].ref_layer,
			};
		}

		// create the compute buffer for the GPU
		pointsBufferGPU = new ComputeBuffer(pointsBufferCPU.Length, PointCloudData.pointStructSize);
		pointsBufferGPU.SetData(pointsBufferCPU);

		// int kernel = computeShader.FindKernel("CSMain");
		// computeShader.SetBuffer(kernel, "_PointBuffer", GPU_VertexBuffer);
		// // computeShader.SetFloat("resolution", points.Length);
		// computeShader.Dispatch(kernel, Math.Min(m_vertexBufferCPU.Length,ushort.MaxValue), 1,1);
		//
		// GPU_VertexBuffer.GetData(m_vertexBufferCPU);

		Debug.Log($"Initialized the GPU buffer with {pointsBufferCPU.Length:N0} vertices for the compute shader");

		pcxRenderer.sourceBuffer = pointsBufferGPU;

		Stopwatch timer = new Stopwatch();
		timer.Start();
		Bounds bounds = pcxRenderer.CalculateBounds(new Vector3(pointsBufferCPU[0].position.x, pointsBufferCPU[0].position.y, pointsBufferCPU[0].position.z));
		timer.Stop();
		Debug.Log($"Took {timer.ElapsedMilliseconds:N}ms to calculate the bounds on the GPU");

		// move the renderer to the center of the bounds.
		float scale = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
		pcxRenderer.transform.localScale = Vector3.one / scale;
		pcxRenderer.transform.Translate(-bounds.center / scale * vrFile.transform.localScale.x, Space.Self);


		loadProgress = 1;
	}

	/// <summary>
	/// pulls data back from GPU compute shader to CPU in array
	/// </summary>
	private void LoadFromComputeBuffer()
	{
		pointsBufferGPU.GetData(pointsBufferCPU);

		if (pointsBufferCPU.Length != pcdFile.data.Count)
		{
			Debug.LogError("Data from GPU and original data are not the same length. Not fun.");
		}

		for (int i = 0; i < pointsBufferCPU.Length; i++)
		{
			pcdFile.data[i].layer = PointCloudData.DecodeColorAndLayer(pointsBufferCPU[i].color).Item2;
		}
	}

	private void BuildOctreeThread()
	{
		octree.ClearPoints();

		int i = 0;
		//add points to the octree
		foreach (PCDPoint point in pcdFile.data)
		{
			loadProgress = (float) i++ / 2 / pcdFile.numPoints;
			octree.AddPoint(point);
			//if (point.layer == 0 || !showLayerColors)
			//{
			//}
			//// if the hidden layers list has been initialized or this layer isn't hidden
			//else if (hiddenLayers.Keys.Count == 0 || !hiddenLayers[point.layer])
			//{
			//	octree.AddPoint(point.position, layerColors[point.layer - 1]);
			//}
		}

		octree.ApplyPoints(ref loadProgress); //actually does the division into new octrees
	}

	/// <summary>
	/// Actually creates the octree and objects
	/// </summary>
	IEnumerator BuildOctree()
	{
		Thread thread = new Thread(BuildOctreeThread);
		thread.Start();
		while (thread.IsAlive)
		{
			yield return null;
		}

		octree.BuildGameObject(gameObject); //actually creates the real points

		octree.ApplyMaterial(pointMaterial);


		// generate the lod group
		List<StochasticOctree> octreesToSearch = new List<StochasticOctree> {octree};
		while (octreesToSearch.Count > 0)
		{
			StochasticOctree currOct = octreesToSearch[0];
			octreesToSearch.RemoveAt(0);

			// an octree could be null here if there happen to be no points in the that area
			if (currOct != null)
			{
				if (lodRenderers.Count <= currOct.depth)
				{
					lodRenderers.Add(new List<Renderer>());
				}

				lodRenderers[currOct.depth].Add(currOct.rend);

				if (currOct.myChildren.Length > 0 && currOct.myChildren[0] != null)
				{
					octreesToSearch.AddRange(currOct.myChildren);
				}
			}
		}

		LOD[] lods = new LOD[lodRenderers.Count];
		for (int i = 0; i < lodRenderers.Count; i++)
		{
			lods[i] = new LOD((lodRenderers.Count - i - .5f) / (lodRenderers.Count + 1), lodRenderers[i].ToArray());
		}

		loadProgress = 1;

		//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		//sw.Start();
		//// sort by lod_id, so that we can cheat when loading
		//pcdFile.data.Sort((p1, p2) => p1.lod_id.CompareTo(p2.lod_id));
		//Debug.Log("Sorting took: " + sw.ElapsedMilliseconds);
		//sw.Restart();


		if (!pcdFile.fieldsList.Contains(PCDFieldType.lod_id)) pcdFile.fieldsList.Add(PCDFieldType.lod_id);
		if (pcdFile.fieldsList.Contains(PCDFieldType._)) pcdFile.fieldsList.Remove(PCDFieldType._);

		// resave the file immediately if the proxy file doesn't already exist
		if (!PCDFile.IsProxyFilename(filename))
		{
			pcdFile.Save(filename, PCDDataType.binary);
		}

		FinishedLoading?.Invoke(this);
	}

	public unsafe Vector3 FindClosestPointMouse(Camera cam, float mouseRadius, Vector3 mousePosition)
	{
		// int closestPointKernel = closestPointComputeShader.FindKernel("CSMain");
		// closestPointComputeShader.SetBuffer(closestPointKernel, "_PointBuffer", pointsBufferGPU);
		// closestPointComputeShader.SetMatrix("_CamMatrix", cam.transform.worldToLocalMatrix * transform.localToWorldMatrix);
		// closestPointComputeShader.SetVector($"_MousePosition", mousePosition);
		// closestPointComputeShader.SetVector($"_ViewDirection", cam.transform.forward);
		// closestPointComputeShader.SetFloat($"_MouseRadius", mouseRadius);
		// float[] closestPoint = {0, 0, 0, 100000};
		// ComputeBuffer closestPointBuffer = new ComputeBuffer(4, sizeof(float));
		// closestPointBuffer.SetData(closestPoint);
		// closestPointComputeShader.SetBuffer(closestPointKernel, "_ClosestPoint", closestPointBuffer);
		//
		// ComputeBuffer closestDistanceBuffer = new ComputeBuffer(1, sizeof(int));
		// closestDistanceBuffer.SetData(new int[]{0});
		// closestPointComputeShader.SetBuffer(closestPointKernel, "_ClosestDistance", closestDistanceBuffer);
		//
		Vector3 camPos = transform.InverseTransformPoint(cam.transform.position);
		// closestPointComputeShader.SetVector("_CameraPosition", camPos);
		
		
		
		int closestPointKernel = mouseSegmentingSpitOutComputeShader.FindKernel("CSMain");
		mouseSegmentingSpitOutComputeShader.SetBuffer(closestPointKernel, "_PointBuffer", pointsBufferGPU);
		mouseSegmentingSpitOutComputeShader.SetBool($"_MouseActive", true);
		mouseSegmentingSpitOutComputeShader.SetMatrix("_CamMatrix", cam.transform.worldToLocalMatrix * transform.localToWorldMatrix);
		mouseSegmentingSpitOutComputeShader.SetVector($"_MousePosition", mousePosition);
		mouseSegmentingSpitOutComputeShader.SetFloat($"_MouseRadius", mouseRadius);
		
		

		// append buffer method
		{
			ComputeBuffer appendBuffer = new ComputeBuffer(pointsBufferCPU.Length, sizeof(float)*2, ComputeBufferType.Append);
			appendBuffer.SetCounterValue(0);
			mouseSegmentingSpitOutComputeShader.SetBuffer(closestPointKernel, "_OutputBuffer", appendBuffer);
			
			
			
			
			// common from old method
			mouseSegmentingSpitOutComputeShader.Dispatch(0, pointsBufferGPU.count / 1024, 1, 1);
			// closestPointBuffer.GetData(closestPoint);
			// closestPointBuffer.Release();
			
			
			
			ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);

			ComputeBuffer.CopyCount(appendBuffer, countBuffer, 0);

			// Retrieve it into array.
			int[] counter = {0};
			countBuffer.GetData(counter);

			// Actual count in append buffer.
			int count = counter[0]; // <-- This is the answer

			// Get the append buffer data.
			// PointCloudData.Point[] data = new PointCloudData.Point[count];
			// float[] data = new float[count];
			HalfPoint[] data = new HalfPoint[count];
			appendBuffer.GetData(data);

			// Don't do this every frame.
			countBuffer.Dispose();
			appendBuffer.Dispose();



			float minDistance = 10000;
			Vector3 minPoint = Vector3.zero;
			// foreach (PointCloudData.Point point in data)
			// {
			// 	if (Vector3.Distance(camPos, point.position) < minDistance)
			// 	{
			// 		minDistance = Vector3.Distance(camPos, point.position);
			// 		minPoint = point.position;
			// 	}
			// }
			// foreach (float point in data)
			// {
			// 	if (point < minDistance)
			// 	{
			// 		minDistance = point;
			// 		minPoint = Vector3.zero;
			// 	}
			// }
			foreach (HalfPoint point in data)
			{
				if (point.distance < minDistance)
				{
					minDistance = point.distance;
					// minPoint = new Vector3(point.position.x, point.position.y, point.position.z);
					minPoint = point.position;
				}
			}
			
			// Debug.Log($"Num Points: {data.Length:N0}");
			// Debug.Log($"Closest Distance: {minDistance:N}");
			return minPoint;

		}

		// Debug.Log($"Closest Distance: {closestPoint[3]:N}");
		// return new Vector3(closestPoint[0], closestPoint[1],closestPoint[2]);
	}
}

public static class BinaryReaderExtension
{
	public static string ReadLine(this BinaryReader reader)
	{
		StringBuilder result = new StringBuilder();
		bool foundEndOfLine = false;
		while (!foundEndOfLine)
		{
			char ch;
			try
			{
				ch = reader.ReadChar();
			}
			catch (EndOfStreamException)
			{
				if (result.Length == 0) return null;
				else break;
			}

			switch (ch)
			{
				case '\r':
					if (reader.PeekChar() == '\n') reader.ReadChar();
					foundEndOfLine = true;
					break;
				case '\n':
					foundEndOfLine = true;
					break;
				default:
					result.Append(ch);
					break;
			}
		}

		return result.ToString();
	}
}