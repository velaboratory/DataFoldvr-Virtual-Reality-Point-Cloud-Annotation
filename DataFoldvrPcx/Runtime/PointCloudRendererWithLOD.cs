using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace DataFoldvrPcx
{
	/// A renderer class that renders a point cloud contained by PointCloudData.
	[ExecuteInEditMode]
	public sealed class PointCloudRendererWithLOD : MonoBehaviour
	{
		#region Editable attributes

		[SerializeField] private PointCloudData _sourceData = null;

		public PointCloudData sourceData
		{
			get => _sourceData;
			set => _sourceData = value;
		}

		[SerializeField] Color _pointTint = new Color(0.5f, 0.5f, 0.5f, 1);

		public Color pointTint
		{
			get => _pointTint;
			set => _pointTint = value;
		}

		[SerializeField] float _pointSize = 0.05f;

		public float pointSize
		{
			get => _pointSize;
			set => _pointSize = value;
		}

		private int visibleLayersBitmask = ~0;

		[Range(0, 1)] public float subsampleFactor;
		public int pointBudget = 2000000;
		public string estimatedNumPointsVisible;
		[Range(0, 255)] public int currentLayerIndex;
		public float handRadius = .2f;
		public float mouseRadius = .2f;
		public ComputeShader subsampleComputeShader;
		public Transform leftHandPosition;
		public Transform rightHandPosition;

		#endregion

		#region Public properties (nonserialized)

		public ComputeBuffer sourceBuffer { get; set; }
		public ComputeBuffer initialPositionsBuffer;
		public ComputeBuffer outputBuffer;
		public ComputeBuffer argBuffer;


		public ComputeShader boundsCalculationShader;
		private Bounds lastBounds;

		#endregion

		#region Internal resources

		[SerializeField] Shader _customDiskShader = null;

		#endregion

		#region Private objects

		[FormerlySerializedAs("_customDiskMaterial")]
		public Material material;

		private bool alreadySent = false;
		private Bounds bounds;
		private static readonly int TintProperty = Shader.PropertyToID("_Tint");
		private static readonly int TransformProperty = Shader.PropertyToID("_Transform");
		private static readonly int PointBufferProperty = Shader.PropertyToID("_PointBuffer");
		private static readonly int PointSizeProperty = Shader.PropertyToID("_PointSize");

		#endregion

		#region MonoBehaviour implementation

		void OnValidate()
		{
			_pointSize = Mathf.Max(0, _pointSize);
		}

		void OnDestroy()
		{
			if (material != null)
			{
				if (Application.isPlaying)
				{
					Destroy(material);
				}
				else
				{
					DestroyImmediate(material);
				}
			}

			outputBuffer?.Release();
			argBuffer?.Release();
		}

		private void OnRenderObject()
		{
			// We need a source data or an externally given buffer.
			if (_sourceData == null && sourceBuffer == null) return;


			// Check the camera condition.
			Camera cam = Camera.current;
			if ((cam.cullingMask & (1 << gameObject.layer)) == 0) return;
			if (cam.name == "Preview Scene Camera") return;

			// TODO: Do view frustum culling here.

			// Lazy initialization
			if (material == null)
			{
				if (_customDiskShader == null)
				{
					_customDiskShader = Shader.Find("Custom/DataFoldvrDisk");
				}

				material = new Material(_customDiskShader) {hideFlags = HideFlags.DontSave};
				material.EnableKeyword("_COMPUTE_BUFFER");
			}

			// Use the external buffer if given any.
			ComputeBuffer pointBuffer = sourceBuffer ?? _sourceData.computeBuffer;

			if (pointBuffer.count == 0) return;

			outputBuffer ??= new ComputeBuffer(pointBuffer.count, pointBuffer.stride, ComputeBufferType.Append);
			argBuffer ??= new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);


			if (subsampleComputeShader != null)
			{
				
				ActuallyRender();
				
				int kernel = subsampleComputeShader.FindKernel("CSMain");
				// if (!alreadySent)
				{
					subsampleComputeShader.SetBuffer(kernel, "_PointBuffer", pointBuffer);
					subsampleComputeShader.SetBuffer(kernel, "_OutputBuffer", outputBuffer);
					alreadySent = true;
				}
				subsampleComputeShader.SetVector("cameraPosition", transform.InverseTransformPoint(Camera.current.transform.position));
				subsampleComputeShader.SetVector("cameraForward", transform.InverseTransformDirection(Camera.current.transform.forward));

				// hands
				if (leftHandPosition != null && rightHandPosition != null)
				{
					subsampleComputeShader.SetFloat("handRadius", handRadius);

					subsampleComputeShader.SetVector("leftHandPosition", transform.InverseTransformPoint(leftHandPosition.position));
					subsampleComputeShader.SetVector("rightHandPosition", transform.InverseTransformPoint(rightHandPosition.position));
				}

				subsampleComputeShader.SetBool("leftHandActive", leftHandPosition != null && XRSettings.enabled);
				subsampleComputeShader.SetBool("rightHandActive", rightHandPosition != null && XRSettings.enabled);

				subsampleComputeShader.SetBool("mouseActive", !XRSettings.enabled);
				if (!XRSettings.enabled)
				{
					Plane plane = new Plane(cam.transform.forward, Vector3.zero);
					Ray ray = cam.ScreenPointToRay(Input.mousePosition);
					if (plane.Raycast(ray, out float rayDistance))
					{
						Vector3 point = cam.transform.InverseTransformPoint(ray.GetPoint(rayDistance));

						subsampleComputeShader.SetVector("mousePosition", new Vector2(point.x, point.y));
						subsampleComputeShader.SetFloat("mouseRadius", mouseRadius);
					}
				}


				// calculate subsampling from point budget
				subsampleFactor = Mathf.Clamp01((float) pointBudget / pointBuffer.count);


				// subsampleComputeShader.SetMatrix("cameraMatrix", transform.localToWorldMatrix * camera.transform.worldToLocalMatrix);
				subsampleComputeShader.SetMatrix("cameraMatrix", cam.transform.worldToLocalMatrix * transform.localToWorldMatrix);
				subsampleComputeShader.SetVector("cameraPosition", transform.InverseTransformPoint(cam.transform.position));
				subsampleComputeShader.SetFloat("subsampleFactor", subsampleFactor);
				subsampleComputeShader.SetInt("visibleLayersBitmask", visibleLayersBitmask);
				estimatedNumPointsVisible = (pointBuffer.count * subsampleFactor).ToString("N0");

				// only render every nth frame
				if (Time.frameCount % 1 == 0)
				{
					int[] args = {0, 1, 0, 0};
					argBuffer.SetData(args);
					ComputeBuffer.CopyCount(outputBuffer, argBuffer, 0);
					argBuffer.GetData(args);
					args[1] = 2;
					argBuffer.SetData(args);
					outputBuffer.SetCounterValue(0);

					subsampleComputeShader.Dispatch(
						kernel,
						pointBuffer.count / 1024,
						1,
						1
					);
					
					// argBuffer.GetData(args);
					// outputBuffer.GetData();
				}
			}
			else
			{
				Debug.LogError("No compute shader defined!!");
			}



			void ActuallyRender()
			{
				material.SetPass(0);
				material.SetColor(TintProperty, _pointTint);
				material.SetMatrix(TransformProperty, transform.localToWorldMatrix);
				material.SetBuffer(PointBufferProperty, outputBuffer);
				material.SetFloat(PointSizeProperty, pointSize * transform.lossyScale.x);
#if UNITY_2019_1_OR_NEWER
				// Graphics.DrawProceduralNow(MeshTopology.Points, outputBuffer.count, 1);
				Graphics.DrawProceduralIndirectNow(MeshTopology.Points, argBuffer, 0);
				// if (++renderIterator == 0)
				// Graphics.DrawProceduralIndirect(material, lastBounds, MeshTopology.Points, argBuffer, 0, null, null,ShadowCastingMode.Off, false, 0);
#else
            // Graphics.DrawProcedural(MeshTopology.Points, pointBuffer.count, 1);
#endif
			}
		}

		public Bounds CalculateBounds(Vector3 firstPoint)
		{
			ComputeBuffer pointBuffer = sourceBuffer ?? _sourceData.computeBuffer;
			if (pointBuffer == null)
			{
				Debug.LogError("Can't calculate bounds. No points.");
				lastBounds = new Bounds(Vector3.zero, Vector3.one);
				return lastBounds;
			}

			int kernel = boundsCalculationShader.FindKernel("CSMain");
			boundsCalculationShader.SetBuffer(kernel, "_PointBuffer", pointBuffer);
			ComputeBuffer boundsBuffer = new ComputeBuffer(6, sizeof(float));
			float[] boundsArray =
			{
				firstPoint.x, firstPoint.y, firstPoint.z,
				firstPoint.x, firstPoint.y, firstPoint.z,
			};
			boundsBuffer.SetData(boundsArray);
			boundsCalculationShader.SetBuffer(kernel, "_BoundsBuffer", boundsBuffer);
			// only do a subsample. /1024 would be the full point cloud
			boundsCalculationShader.Dispatch(0, pointBuffer.count / 1024, 1, 1);	
			boundsBuffer.Release();

			Vector3 centerPos = new Vector3(
				(boundsArray[0] + boundsArray[3]) / 2,
				(boundsArray[1] + boundsArray[4]) / 2,
				(boundsArray[2] + boundsArray[5]) / 2);
			Vector3 size = new Vector3(
				Mathf.Abs(boundsArray[0] - boundsArray[3]),
				Mathf.Abs(boundsArray[1] - boundsArray[4]),
				Mathf.Abs(boundsArray[2] - boundsArray[5])
			);
			lastBounds = new Bounds(centerPos, size);
			return lastBounds;
		}


		public void ShowLayer(int layerIndex, bool show)
		{
			if (show)
			{
				visibleLayersBitmask |= 1 << layerIndex;
			}
			else
			{
				visibleLayersBitmask &= ~(1 << layerIndex);
			}
		}

		#endregion
	}
}