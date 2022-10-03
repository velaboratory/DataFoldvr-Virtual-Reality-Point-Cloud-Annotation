using System;
using System.Collections;
using System.IO;
using System.Threading;
using DataFoldvrPcx;
using UnityEngine;
using UnityEngine.Networking;

// public struct Point
// {
// 	public Vector3 position;
// 	public Color color;
// };

public class LoadPCDIntoGPU : MonoBehaviour
{
	public string filePath;
	public bool loadOnStart;
	private PCDFile pcdFile;
	public ComputeShader computeShader;
	public Material material;
	public PointCloudRendererWithLOD pcxRenderer;
	private bool loaded;
	
	private Mesh           m_mesh;
	private MeshFilter     m_meshFilter;
	private Material[]     m_mats;
	private Renderer       m_meshRenderer;
	private Bounds         m_meshBound;
	
	
	private ComputeBuffer GPU_VertexBuffer;
	private ComputeBuffer GPU_OrigPosBuffer;
	
	private PointCloudData.Point[]       m_vertexBufferCPU;
	private int[]         m_indexBuffer;
	
	private GraphicsBuffer GPU_IndexBuffer;
	private static readonly int MatrixM = Shader.PropertyToID("_MATRIX_M");
	private static readonly int VertexBuffer = Shader.PropertyToID("_PointBuffer");

	private const string kernelName = "CSMain";

	// Start is called before the first frame update
	private void Start()
	{
		if (loadOnStart)
		{
			Load();
		}
	}

	private void Update()
	{
		if (loaded)
		{
			// UpdateRuntimeShaderParameter();
			// RunShader();

			// IssueProceduralDrawCommand();
		}
	}

	private void OnDisable()
	{
		if (GPU_VertexBuffer != null)
		{
			GPU_VertexBuffer.Release();
			GPU_VertexBuffer = null;
		}
	}
	
	private void IssueProceduralDrawCommand()
	{
		// Matrix4x4 M = transform.localToWorldMatrix;
		// material.SetMatrix(MatrixM, M);
		// Graphics.DrawProcedural(
		// 	material, 
		// 	m_meshBound, 
		// 	MeshTopology.Points, 
		// 	m_indexBuffer.Length, 
		// 	1, 
		// 	null, 
		// 	null, 
		// 	UnityEngine.Rendering.ShadowCastingMode.Off, 
		// 	false,
		// 	gameObject.layer
		// );

		if (GPU_VertexBuffer == null) return;

		// Check the camera condition.
		// Camera camera = Camera.current;
		// if ((camera.cullingMask & (1 << gameObject.layer)) == 0) return;
		// if (camera.name == "Preview Scene Camera") return;

		// TODO: Do view frustum culling here.

		// Lazy initialization
		if (material == null) return;
		
		material.SetPass(0);
		material.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1));
		material.SetMatrix("_Transform", transform.localToWorldMatrix);
		material.SetBuffer("_PointBuffer", GPU_VertexBuffer);
		material.SetFloat("_PointSize", .05f);
		material.SetFloat("_SubsampleFactor", 0);
#if UNITY_2019_1_OR_NEWER
		Graphics.DrawProceduralNow(MeshTopology.Points, GPU_VertexBuffer.count, 1);
#else
            Graphics.DrawProcedural(MeshTopology.Points, GPU_VertexBuffer.count, 1);
#endif
	}

	private void Load()
	{
		StartCoroutine(ReadFromFile());
	}

	private IEnumerator ReadFromFile()
	{
		UnityWebRequest www = UnityWebRequest.Get(filePath);
		yield return www.SendWebRequest();

		BinaryReader reader = new BinaryReader(new MemoryStream(www.downloadHandler.data));

		pcdFile = new PCDFile();
		Thread readThread = new Thread(() => pcdFile.LoadThread(reader));
		readThread.Start();

		while (readThread.IsAlive)
		{
			yield return null;
		}

		// finished loading


		Initialize();
		
		// SendToGPU();

		loaded = true;
	}

	private void Initialize()
	{
		
		m_vertexBufferCPU = new PointCloudData.Point[pcdFile.data.Count];
		m_indexBuffer = new int[pcdFile.data.Count];
		for (int i = 0; i < pcdFile.data.Count; i++)
		{
			m_indexBuffer[i] = i;
			m_vertexBufferCPU[i] = new PointCloudData.Point
			{
				position = pcdFile.data[i].position, 
				color = PointCloudData.EncodeColorAndLayer(pcdFile.data[i].color, 0),
				ref_layer = pcdFile.data[i].ref_layer,
			};
		}

		GPU_VertexBuffer = new ComputeBuffer(m_vertexBufferCPU.Length, sizeof(float) * 4);
		GPU_VertexBuffer.SetData(m_vertexBufferCPU);
		
		GPU_OrigPosBuffer = new ComputeBuffer(m_vertexBufferCPU.Length, sizeof(float) * 4);
		GPU_OrigPosBuffer.SetData(m_vertexBufferCPU);
		
		// int kernel = computeShader.FindKernel("CSMain");
		// computeShader.SetBuffer(kernel, "_PointBuffer", GPU_VertexBuffer);
		// // computeShader.SetFloat("resolution", points.Length);
		// computeShader.Dispatch(kernel, Math.Min(m_vertexBufferCPU.Length,ushort.MaxValue), 1,1);
		
		// GPU_VertexBuffer.GetData(m_vertexBufferCPU);
		
		Debug.Log($"Initialized the GPU buffers with {m_vertexBufferCPU.Length:N0} vertices for the compute shader");

		pcxRenderer.sourceBuffer = GPU_VertexBuffer;
		pcxRenderer.initialPositionsBuffer = GPU_OrigPosBuffer;


		// // initialize mesh
		// m_meshRenderer = GetComponent<Renderer>();
		// if (m_meshRenderer == null)
		// {
		// 	m_meshRenderer = gameObject.AddComponent<MeshRenderer>();
		// 	m_meshRenderer.material = material;
		// }
		// m_mats = m_meshRenderer.materials;
		// m_meshFilter = GetComponent<MeshFilter>();
		// if (m_meshFilter == null)
		// {
		// 	m_meshFilter = gameObject.AddComponent<MeshFilter>();
		// }
		// m_mesh = m_meshFilter.mesh;
		// m_meshFilter.sharedMesh = m_mesh;
		//
		// // initialize CPU buffers
		// m_indexBuffer = m_mesh.GetIndices(0);
		//

		// initialize GPU buffers
		// const int colorSize = sizeof(float) * 4;
		// const int vector3Size = sizeof(float) * 3;
		// const int totalSize = colorSize + vector3Size;
		//
		// GPU_VertexBuffer = new ComputeBuffer(m_vertexBufferCPU.Length, totalSize);
		// GPU_VertexBuffer.SetData(m_vertexBufferCPU);
		//
		// int kernel = computeShader.FindKernel(kernelName);
		// computeShader.SetBuffer(kernel, "_PointBuffer", GPU_VertexBuffer);
		//
		// GPU_IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, m_indexBuffer.Length, sizeof(int));
		// GPU_IndexBuffer.SetData(m_indexBuffer);
		//
		// m_meshBound = transform.GetChild(0).GetComponent<MeshRenderer>().bounds;
		//
		// Debug.Log($"Initialized the GPU buffers with {m_vertexBufferCPU.Length:n0} vertices for the compute shader");
		//
		//
		// // Mesh shader parameters
		//
		// // This is so that I can use the same shader for both draw procedural and standard API. The standard API one, the mvp matrix is taken care of by unity itself, so I will just pass on identity matrix.
		// Matrix4x4 M = Matrix4x4.identity;
		//
		// // technically  this way I am wasting 16 multiply and 16 add instruction on the vertex shader, but for the porpuses of demonstration, I dont have to maintain two shaders/ uniforms/ materials.
		// material.SetMatrix(MatrixM, M);
		// material.SetBuffer(VertexBuffer, GPU_VertexBuffer);
	}

	// private void SendToGPU()
	// {
	// 	const int colorSize = sizeof(float) * 4;
	// 	const int vector3Size = sizeof(float) * 3;
	// 	const int totalSize = colorSize + vector3Size;
	//
	// 	ComputeBuffer pointsBuffer = new ComputeBuffer(points.Length, totalSize);
	// 	pointsBuffer.SetData(points);
	// 	
	// 	computeShader.SetBuffer(0, "points", pointsBuffer);
	// 	computeShader.SetFloat("resolution", points.Length);
	// 	computeShader.Dispatch(0, points.Length/10, 1,1);
	// }
}
