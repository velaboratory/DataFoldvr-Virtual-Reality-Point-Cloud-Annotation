using UnityEngine;

public class MeshManager : MonoBehaviour
{
	// =================================================================
	private const int VERTEX_SIZE = 28;
	public struct _Vertex
	{
		public float position_x, position_y, position_z;
		public float r, g, b, a;
		//public byte layer;

		public _Vertex(Vector3 position, Color color)
		{
			position_x = position.x;
			position_y = position.y;
			position_z = position.z;

			r = color.r;
			g = color.g;
			b = color.b;
			a = color.a;

			//layer = 0;
		}
	}

	public enum RenderMethod
	{
		WithStandardAPI, WithDrawProcedural
	}

	// =================================================================

	public RenderMethod m_renderingMethod = RenderMethod.WithStandardAPI;

	public ComputeShader m_computeShader;
	public Transform m_HandPosition;
	public float radius = .1f;


	// _________________________________

	private _Vertex[] m_vertexBufferCPU;
	private Vector3[] m_verticesPosition;       // The original Vertices position of the mesh. Used once on initialize
	private Color[] m_verticesColor;
	private int[] m_indexBuffer;


	private ComputeBuffer GPU_VertexBuffer;
	private ComputeBuffer GPU_defaultPositionsBuffer; // Read only Bufffer
	private ComputeBuffer GPU_defaultColorsBuffer; // Read only Bufffer


	public MeshFilter mesh;
	private Mesh m_mesh;
	private Material[] m_mats;
	private Renderer m_meshRenderer;
	private Bounds m_meshBound;

	private const string kernelName = "CSMain";

	private GraphicsBuffer GPU_IndexBuffer;

	// =================================================================
	void OnDestroy()
	{
		GPU_VertexBuffer.Release();
		GPU_defaultPositionsBuffer.Release();
	}


	void Start()
	{
		InitializeMesh();
		InitializeCPUBuffers();
		InitializeGPUBuffers();
		InitializeShaderParameters();

		if (m_renderingMethod == RenderMethod.WithDrawProcedural) m_meshRenderer.enabled = false;

	}
	// Update is called once per frame
	void Update()
	{
		UpdateRuntimeShaderParameter();
		RunShader();

		IssueProceduralDrawCommand();

	}


	public ComputeBuffer GetVertexBuffer()
	{
		return GPU_VertexBuffer;
	}


	// magintude of a vector in world space is sqrt(x^2 +y^2 + z^2). The scaling matrix is diagonal and multiplies the xyz component so, 
	// the magnitude of any given vector would be sqrt((x*scale.x)^2 + (x*scale.y)^2+ (x*scale.z)^2) in local space
	// in this case, I will only allow uniform scaling, so scale.x= scale.y = scale.z = scalef
	// this means we can factor the scale out and we will have worldMag / scalef = meshMagnitude
	private float ScaleFromWorldtoMeshSpace(float scale)
	{
		float scalef = transform.localScale.x;
		return scale / scalef;
	}

	private void IssueProceduralDrawCommand()
	{
		Matrix4x4 M = m_meshRenderer.localToWorldMatrix;
		for (int i = 0; i < m_mats.Length; i++)
		{
			Material m = m_mats[i];

			m.SetMatrix("_MATRIX_M", M);


			Graphics.DrawProcedural(m, m_meshBound, MeshTopology.Triangles, GPU_IndexBuffer, m_indexBuffer.Length, 1, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0);
		}
	}



	// =================================================================
	void InitializeMesh()
	{
		m_mesh = mesh.mesh;
		mesh.sharedMesh = m_mesh;

		Debug.Log(string.Format("Initialized the mesh: {0}, for the compute shader", m_mesh));
	}

	void InitializeCPUBuffers()
	{
		m_verticesPosition = m_mesh.vertices;
		m_verticesColor = m_mesh.colors;
		if (m_verticesColor.Length != m_verticesPosition.Length)
		{
			m_verticesColor = new Color[m_verticesPosition.Length];
		}
		m_vertexBufferCPU = new _Vertex[m_verticesPosition.Length];

		for (int i = 0; i < m_vertexBufferCPU.Length; i++)
		{
			_Vertex v = new _Vertex(m_verticesPosition[i], m_verticesColor[i]);
			m_vertexBufferCPU[i] = v;
		}


		m_indexBuffer = m_mesh.triangles;


		Debug.Log(string.Format("Initialized the cpu buffers with {0} vertices, for the compute shader", m_verticesPosition.Length));
	}

	void InitializeGPUBuffers()
	{


		int sizeOfVector3 = System.Runtime.InteropServices.Marshal.SizeOf((object)Vector3.zero);
		int sizeOfColor = System.Runtime.InteropServices.Marshal.SizeOf((object)Color.black);
		GPU_VertexBuffer = new ComputeBuffer(m_vertexBufferCPU.Length, VERTEX_SIZE);
		GPU_VertexBuffer.SetData(m_vertexBufferCPU);

		GPU_defaultPositionsBuffer = new ComputeBuffer(m_verticesPosition.Length, sizeOfVector3);
		GPU_defaultPositionsBuffer.SetData(m_verticesPosition);

		GPU_defaultColorsBuffer = new ComputeBuffer(m_verticesPosition.Length, sizeOfColor);
		GPU_defaultColorsBuffer.SetData(m_verticesColor);


		int kernel = m_computeShader.FindKernel(kernelName);

		m_computeShader.SetBuffer(kernel, "_VertexBuffer", GPU_VertexBuffer);
		m_computeShader.SetBuffer(kernel, "_InitialPositionBuffer", GPU_defaultPositionsBuffer);
		m_computeShader.SetBuffer(kernel, "_InitialColorBuffer", GPU_defaultColorsBuffer);

		m_computeShader.SetFloat("_radius", ScaleFromWorldtoMeshSpace(radius));

		Debug.Log(string.Format("Initialized the GPU buffers with {0} vertices, for the compute shader", m_verticesPosition.Length));


		if (m_renderingMethod == RenderMethod.WithDrawProcedural)
		{
			GPU_IndexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, m_indexBuffer.Length, sizeof(int));
			GPU_IndexBuffer.SetData(m_indexBuffer);
		}

	}

	void InitializeShaderParameters()
	{



		// Mesh shader parameters
		m_meshRenderer = mesh.GetComponent<Renderer>();
		if (m_meshRenderer == null)
		{
			Debug.LogError(string.Format("Attempted to acces non exisiting mesh Renderer, on game Object {0}", this.gameObject.name));
			return;
		}

		m_mats = m_meshRenderer.materials;

		Matrix4x4 M = Matrix4x4.identity; // This is so that I can use the same shader for both draw procedural and standard API. The standard API one, the mvp matrix is taken care of by unity itself, so I will just pass on identity matrix. 
										  // technically  this way I am wasting 16 multiply and 16 add instruction on the vertex shader, but for the porpuses of demonstration, I dont have to maintain two shaders/ uniforms/ materials.  
		foreach (Material m in m_mats)
		{

			m.SetMatrix("_MATRIX_M", M);
			m.SetBuffer("_VertexBuffer", GPU_VertexBuffer);
		}

		m_meshBound = m_meshRenderer.bounds;
		m_meshBound.size = m_meshBound.size * 100f;

		Debug.Log(string.Format("Initialized Shader Parameters. {0} materials were found", m_mats.Length));
	}

	void UpdateRuntimeShaderParameter()
	{
		m_computeShader.SetFloat("_radius", ScaleFromWorldtoMeshSpace(radius));

		Vector3 posInObjectLocal = mesh.transform.worldToLocalMatrix
			* new Vector4(m_HandPosition.position.x, m_HandPosition.position.y, m_HandPosition.position.z, 1.0f);
		m_computeShader.SetVector("_HandPos", m_HandPosition.position);
	}

	void RunShader()
	{
		int kernel = m_computeShader.FindKernel(kernelName);
		m_computeShader.Dispatch(kernel, m_verticesPosition.Length, 1, 1);

	}

	void PullResults()
	{
		GPU_VertexBuffer.GetData(m_vertexBufferCPU);

		for (int i = 0; i < m_vertexBufferCPU.Length; i++)
		{
			m_verticesPosition[i] = new Vector3(m_vertexBufferCPU[i].position_x, m_vertexBufferCPU[i].position_y, m_vertexBufferCPU[i].position_z);

		}
		m_mesh.vertices = m_verticesPosition;
	}

}
