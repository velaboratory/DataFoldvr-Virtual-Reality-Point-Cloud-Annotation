using UnityEngine;
using UnityEngine.Serialization;

public class MousePlayer : MonoBehaviour
{
	public Camera cam;
	private const float distance = 5;
	private Vector2 mousePos = Vector2.zero;
	public float sensitivity = .5f;
	public float sensitivityOrtho = .0025f;
	public float scrollSensitivity = .1f;
	public float scrollSensitivityOrtho = .1f;
	public Transform orbitCenter;
	public GameObject orbitCenterVisualizer;

	[FormerlySerializedAs("circleVisible")]
	public bool holdingShift;

	public Transform cursorCircle;
	public float cursorScaleMultiplier = 130f;
	public ColorPicker colorPicker;
	private bool orbitView = true;
	private Vector3 closestPoint = Vector3.zero;
	public bool instantiateSphere;
	public ComputeShader mouseSegmentingComputeShader;

	// Update is called once per frame
	private void Update()
	{
		float deltaX = Input.mousePosition.x - mousePos.x;
		float deltaY = Input.mousePosition.y - mousePos.y;


		holdingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);


		if (holdingShift)
		{
			cursorCircle.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);

			// zooming
			float scrollDelta = Input.mouseScrollDelta.y;
			if (scrollDelta != 0)
			{
				cursorCircle.localScale *= 1 + -scrollDelta * scrollSensitivityOrtho;
			}

			// segmenting
			if (PlayerManager.instance.CurrentEditMode == VRFile.EditType.Segment ||
			    PlayerManager.instance.CurrentEditMode == VRFile.EditType.PaintCount ||
			    PlayerManager.instance.CurrentEditMode == VRFile.EditType.StudySegment)
			{
				cursorCircle.gameObject.SetActive(holdingShift);

				VRFile currentFile = PlayerManager.instance.currentlyEditedFile;
				PCDLoader pcdLoader = currentFile.pointCloudInstances[0];


				byte layerIndex = (byte) colorPicker.colorIndex;
				if (Input.GetMouseButton(1)) layerIndex = 0; // erase with right click

				Plane plane = new Plane(cam.transform.forward, Vector3.zero);
				Ray ray = cam.ScreenPointToRay(Input.mousePosition);
				if (plane.Raycast(ray, out float rayDistance))
				{
					Vector3 point = cam.transform.InverseTransformPoint(ray.GetPoint(rayDistance));

					if (pcdLoader.LimitPaintDepth)
					{
						// if mouse button down, find the depth of the mouse based on closest point in the circle
						// if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) 
						// if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
						{
							closestPoint = pcdLoader.FindClosestPointMouse(cam,
								cursorCircle.localScale.x * cursorScaleMultiplier * cam.orthographicSize /
								cam.scaledPixelHeight, point);
							if (instantiateSphere)
							{
								GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
								obj.transform.position = pcdLoader.transform.TransformPoint(closestPoint);
								obj.transform.localScale = .01f * Vector3.one;
							}
						}
					}

					float mouseRadius = cursorCircle.localScale.x * cursorScaleMultiplier * cam.orthographicSize /
					                    cam.scaledPixelHeight;

					mouseSegmentingComputeShader.SetInt("_LayerIndex", layerIndex);
					mouseSegmentingComputeShader.SetBool($"_MouseActive", true);
					mouseSegmentingComputeShader.SetMatrix("_CamMatrix",
						cam.transform.worldToLocalMatrix * pcdLoader.transform.localToWorldMatrix);
					mouseSegmentingComputeShader.SetVector($"_MousePosition", point);
					mouseSegmentingComputeShader.SetVector($"_ViewDirection", cam.transform.forward);
					mouseSegmentingComputeShader.SetFloat($"_MouseRadius", mouseRadius);
					mouseSegmentingComputeShader.SetFloat($"_ClosestPointRadius",
						pcdLoader.LimitPaintDepth ? mouseRadius * 4 : float.MaxValue);
					mouseSegmentingComputeShader.SetVector($"_ClosestPoint", closestPoint);

					// if actually clicking
					if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
					{
						int kernel = mouseSegmentingComputeShader.FindKernel("CSMain");
						mouseSegmentingComputeShader.SetBuffer(kernel, "_PointBuffer", pcdLoader.pointsBufferGPU);
						mouseSegmentingComputeShader.Dispatch(0, pcdLoader.pointsBufferGPU.count / 1024, 1, 1);
					}

					// for highlighting shader
					Shader.SetGlobalInt("_LayerIndex", layerIndex);
					Shader.SetGlobalInt("_MouseActive", 1);
					Shader.SetGlobalMatrix("_CamMatrix",
						cam.transform.worldToLocalMatrix * pcdLoader.transform.localToWorldMatrix);
					Shader.SetGlobalVector("_MousePosition", point);
					Shader.SetGlobalVector("_ViewDirection", cam.transform.forward);
					Shader.SetGlobalFloat("_MouseRadius", mouseRadius);
					Shader.SetGlobalFloat("_ClosestPointRadius",
						pcdLoader.LimitPaintDepth ? mouseRadius * 4 : float.MaxValue);
					Shader.SetGlobalVector("_ClosestPointMouse", closestPoint);

					if (!pcdLoader.pcdFile.fieldsList.Contains(PCDFieldType.layer))
					{
						pcdLoader.pcdFile.fieldsList.Add(PCDFieldType.layer);
					}
				}
			}
			else
			{
				cursorCircle.gameObject.SetActive(false);
			}
		}
		else
		{
			cursorCircle.gameObject.SetActive(false);
			Shader.SetGlobalFloat("_MouseRadius", 0);

			// click down
			if (Input.GetMouseButtonDown(1))
			{
				deltaX = 0;
				deltaY = 0;
			}

			orbitCenterVisualizer.SetActive(false);

			// click hold
			if (Input.GetMouseButton(2))
			{
				// pan
				orbitCenter.Translate(deltaX * -sensitivityOrtho * cam.orthographicSize,
					deltaY * -sensitivityOrtho * cam.orthographicSize, 0, Space.Self);
				orbitCenterVisualizer.SetActive(true);
			}
			else if (Input.GetMouseButton(1))
			{
				if (!orbitView)
				{
					// pan
					orbitCenter.Translate(deltaX * -sensitivityOrtho * cam.orthographicSize,
						deltaY * -sensitivityOrtho * cam.orthographicSize, 0, Space.Self);
				}
				else
				{
					// rotate
					orbitCenter.RotateAround(orbitCenter.position, Vector3.up, deltaX * sensitivity);
					orbitCenter.RotateAround(orbitCenter.position, transform.right, -deltaY * sensitivity);
				}
			}


			// zooming
			float scrollDelta = Input.mouseScrollDelta.y;
			if (scrollDelta != 0)
			{
				// if (!orbitView)
				// {
				cam.orthographicSize *= 1 + -scrollDelta * scrollSensitivityOrtho;
				// }
				// else
				// {
				// 	cam.transform.Translate((cam.transform.position - orbitCenter.position).normalized * -(scrollDelta * scrollSensitivity), Space.World);
				// }
			}
		}


		mousePos.x = Input.mousePosition.x;
		mousePos.y = Input.mousePosition.y;
	}

	public void SetSideView()
	{
		orbitView = false;
		cam.orthographic = true;
		orbitCenter.eulerAngles = new Vector3(0, 90, 0);
		// cam.transform.position = orbitCenter.position + Vector3.right * distance;
		// cam.transform.LookAt(orbitCenter.position);
		unityutilities.Logger.LogRow("events", "set-camera-view", "side");
	}

	public void SetFrontView()
	{
		orbitView = false;
		cam.orthographic = true;
		orbitCenter.eulerAngles = new Vector3(0, 0, 0);
		// cam.transform.position = orbitCenter.position + Vector3.forward * distance;
		// cam.transform.LookAt(orbitCenter.position);
		unityutilities.Logger.LogRow("events", "set-camera-view", "front");
	}

	public void SetTopView()
	{
		orbitView = false;
		cam.orthographic = true;
		orbitCenter.eulerAngles = new Vector3(90, 90, 0);
		// cam.transform.position = orbitCenter.position + Vector3.up * distance;
		// cam.transform.LookAt(orbitCenter.position);
		unityutilities.Logger.LogRow("events", "set-camera-view", "top");
	}

	public void SetFreeCamView()
	{
		orbitView = true;
		cam.orthographic = true;
		orbitCenter.eulerAngles = new Vector3(30, 0, 0);
		orbitCenter.localPosition = Vector3.up * .5f;
		cam.orthographicSize = 1;
		// cam.transform.position = orbitCenter.position + Vector3.forward * distance + Vector3.up * distance;
		// cam.transform.LookAt(orbitCenter.position);
		unityutilities.Logger.LogRow("events", "set-camera-view", "free-cam");
	}
}