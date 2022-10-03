#undef UDesktopDup

using MenuTablet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using unityutilities;
using unityutilities.VRInteraction;


public class Hand : MonoBehaviour
{
	public VRGrabbableHand hand;

#if !UNITY_ANDROID && UDesktopDup
	[ReadOnly] public uDesktopDuplication.Texture[] uddTextures;
#endif

	public XRRayInteractor interactor;
	public XRInteractorLineVisual lineVisual;

	public Transform handMarker;
	private float lastHandSegmentTime = 0;
	private Vector3 lastHandSegmentPos = Vector3.zero;

	public ColorPicker colorPicker;

	private GameObject currentlyHoveredSphere;

	[ReadOnly] public int numThreads;

	private struct HandPos
	{
		public Vector3 pos;
		public float radius;
		public Color color;
	}

	private List<HandPos> unprocessedHandPositions = new List<HandPos>();


	public SnapUICursorAlignment snapUICursor;
	public Transform sphereVisualizer;

	private void Start()
	{
		// make sure the tool is switched back when the tablet is hidden
		MenuTabletMover.OnHide += (_) =>
		{
			sphereVisualizer.gameObject.SetActive(true);
			snapUICursor.gameObject.SetActive(false);
		};
	}


	// Update is called once per frame
	private void Update()
	{
		bool laserHit = false;
#if !UNITY_ANDROID && UDesktopDup
		if (uddTextures.Length == 0)
		{
			uddTextures = FindObjectsOfType<uDesktopDuplication.Texture>();
		}

		foreach (var uddTexture in uddTextures)
		{
			var result = uddTexture.RayCast(transform.position, transform.forward * rayDistance);
			if (result.hit)
			{
				laserHit = true;
				ClickMouse.SetCursorPos((int)result.desktopCoord.x, (int)result.desktopCoord.y);

				worldMouse.SetLaserLength(Vector3.Distance(transform.position, result.position));

				if (InputMan.TriggerDown(hand.side))
				{
					ClickMouse.LeftClickDown();
				}

				if (InputMan.TriggerUp(hand.side))
				{
					ClickMouse.LeftClickUp();
				}

				if (InputMan.Button1Down(hand.side))
				{
					ClickMouse.OpenFile();
				}

				// TODO scroll with thumbstick
			}
		}

#endif

		bool isValid = interactor.TryGetHitInfo(out Vector3 pos, out Vector3 _, out int _, out bool validTarget);
		laserHit |= validTarget;
		lineVisual.enabled = laserHit;


		// perform other interactions if not lasering
		if (!laserHit)
		{
			switch (PlayerManager.instance.CurrentEditMode)
			{
				case VRFile.EditType.Count:
					{
						// highlight hovered spheres

						if (InputMan.Button1Up(hand.side))
						{
							PlayerManager.instance.currentlyEditedFile.AddCounter(hand.transform.position,
								handMarker.localScale.x);
						}

						if (InputMan.Button2Up(hand.side))
						{
							if (hand.selectedVRGrabbable != null)
							{
								bool marker = hand.selectedVRGrabbable.CompareTag("CountCloudMarker");

								if (marker)
								{
									PlayerManager.instance.currentlyEditedFile.RemoveCounter(
										hand.selectedVRGrabbable.gameObject);
								}
							}
						}

						ScaleHandMarker();
						break;
					}

				case VRFile.EditType.Segment:
				case VRFile.EditType.PaintCount:
				case VRFile.EditType.StudySegment:
					{
						VRFile currentFile = PlayerManager.instance.currentlyEditedFile;
						PCDLoader pcdLoader = currentFile.pointCloudInstances[0];

						pcdLoader.handsActive[(int)hand.side] = false;
						
						
						
						// for highlighting shader
						Shader.SetGlobalVector($"_{hand.side.ToString()}HandPosition",
							pcdLoader.transform.InverseTransformPoint(transform.position));
						Shader.SetGlobalFloat($"_{hand.side.ToString()}HandRadius",
							handMarker.localScale.x / 2f / pcdLoader.transform.lossyScale.x);


						if (InputMan.Trigger(hand.side) || InputMan.Button1(hand.side))
						{
							Vector3 newHandPos = handMarker.position;
							// if (Vector3.Distance(newHandPos, lastHandSegmentPos) > .001f &&
							//     Time.time - lastHandSegmentTime > .1f)
							{
								lastHandSegmentPos = newHandPos;
								lastHandSegmentTime = Time.time;
								pcdLoader.handsActive[(int)hand.side] = true;

								byte layerIndex = (byte)colorPicker.colorIndex;

								// always erase with button 2
								if (InputMan.Button1(hand.side)) layerIndex = 0;

								pcdLoader.handSegmentingComputeShader.SetInt("_LayerIndex", layerIndex);
								pcdLoader.handSegmentingComputeShader.SetBool($"_{hand.side.ToString()}HandActive", true);
								pcdLoader.handSegmentingComputeShader.SetVector($"_{hand.side.ToString()}HandPosition",
									pcdLoader.transform.InverseTransformPoint(transform.position));
								pcdLoader.handSegmentingComputeShader.SetFloat($"_{hand.side.ToString()}HandRadius",
									handMarker.localScale.x / 2f / pcdLoader.transform.lossyScale.x);

								if (!pcdLoader.pcdFile.fieldsList.Contains(PCDFieldType.layer))
								{
									pcdLoader.pcdFile.fieldsList.Add(PCDFieldType.layer);
								}
							}
						}

						if (InputMan.TriggerDown(hand.side))
						{
						}

						if (InputMan.TriggerUp(hand.side))
						{
							pcdLoader.handSegmentingComputeShader.SetBool($"_{hand.side.ToString()}HandActive", true);
							// // see if there are any hand positions left to process
							// if (unprocessedHandPositions.Count > 0)
							// {
							// 	StartCoroutine(ProcessUnprocessedHandPositions());
							// }
						}

						ScaleHandMarker();
						break;
					}
				default:
					handMarker.localScale = Vector3.one * .1f;
					break;
			}
		}
	}

	private IEnumerator ProcessUnprocessedHandPositions()
	{
		VRFile currentFile = PlayerManager.instance.currentlyEditedFile;
		PCDLoader pcdLoader = currentFile.pointCloudInstances[0];
		MeshRenderer[] renderers = currentFile.content.GetComponentsInChildren<MeshRenderer>();
		//MeshFilter[] mfs = currentFile.content.GetComponentsInChildren<MeshFilter>();
		if (pcdLoader.ShowLayerColors)
		{
			//foreach (MeshFilter mf in mfs)
			foreach (MeshRenderer rend in renderers)
			{
				//Mesh mesh = mf.mesh;
				Mesh mesh = rend.GetComponent<MeshFilter>().mesh;
				Color[] colors = mesh.colors;
				Vector3[] vertices = mesh.vertices;

				foreach (var handPos in unprocessedHandPositions)
				{
					if (rend.bounds.Contains(handPos.pos)) // this ignores radius and is wrong
					{
						Thread singleRendererThread = new Thread(() =>
							ColorCloseVertices(vertices, colors, handPos.pos, handPos.radius, handPos.color));
						singleRendererThread.Start();
						numThreads++;
						StartCoroutine(CallOnThreadComplete(singleRendererThread,
							() => { mesh.SetColors(colors); }));
					}
				}

				while (numThreads > Math.Max(2, Environment.ProcessorCount - 2))
				{
					Debug.Log($"Too many threads: {numThreads}. Waiting...");
					yield return null;
				}
			}
		}

		unprocessedHandPositions.Clear();
		yield return null;
	}

	private IEnumerator CallOnThreadComplete(Thread thread, Action callback)
	{
		while (thread.IsAlive)
		{
			yield return null;
		}

		callback();
		if (numThreads > 8)
		{
			Debug.Log($"Num threads: {numThreads}");
		}

		numThreads--;
	}

	/// <summary>
	/// Thread to find vertices that are within range of the hand brush on the original data.
	/// This just sets the layer on the original data structure
	/// </summary>
	/// <param name="points">The list of original points</param>
	/// <param name="handPosLocal">Brush location in the same space as the vertices</param>
	/// <param name="handRadius">Radius of the brush (sphere)</param>
	/// <param name="layer">The index of the layer</param>
	private void FindCloseVerticesOriginalData(List<PCDPoint> points, Vector3 handPosLocal, float handRadius,
		byte layer)
	{
		for (int v = 0; v < points.Count; v++)
		{
			if (Vector3.Distance(points[v].position, handPosLocal) < handRadius)
			{
				points[v].layer = layer;
			}
		}
	}

	/// <summary>
	/// Thread to find vertices that are within range of the hand brush
	/// </summary>
	/// <param name="vertices">Read-only array of vertex positions</param>
	/// <param name="colors">Writeable array of colors</param>
	/// <param name="handPosLocal">Brush location in the same space as the vertices</param>
	/// <param name="handRadius">Radius of the brush (sphere)</param>
	private void ColorCloseVertices(Vector3[] vertices, Color[] colors, Vector3 handPosLocal, float handRadius,
		Color color)
	{
		for (int v = 0;
			v < vertices.Length;
			v++)
		{
			if (Vector3.Distance(vertices[v], handPosLocal) < handRadius && colors[v] != color)
			{
				colors[v] = color;
			}
		}
	}

	/// <summary>
	/// Thread to find vertices that are within range of the hand brush
	/// </summary>
	/// <param name="vertices">Read-only array of vertex positions</param>
	/// <param name="colors">Writeable array of colors</param>
	/// <param name="handPositions">The list of hand position structs to check for intersection</param>
	private static void ColorCloseVertices(
		IReadOnlyList<Vector3> vertices,
		IList<Color> colors,
		IReadOnlyList<HandPos> handPositions)
	{
		for (int v = 0; v < vertices.Count; v++)
		{
			// go through the hand positions backwards in order to allow short-circuiting to work properly
			// if a later position changes the color again, only the last color will be used.
			for (int i = handPositions.Count - 1; i > 0; i--)
			{
				if (Vector3.Distance(vertices[v], handPositions[i].pos) < handPositions[i].radius)
				{
					colors[v] = handPositions[i].color;
					break; // short-circuit all the intermediate hand positions
				}
			}
		}
	}

	private void ScaleHandMarker()
	{
		// nullzone
		if (Mathf.Abs(InputMan.ThumbstickY()) > .5f && Mathf.Abs(InputMan.ThumbstickX()) < .5f)
		{
			handMarker.localScale = Vector3.one * Mathf.Clamp(handMarker.localScale.x + -InputMan.ThumbstickY() * Time.deltaTime * .1f, .001f, 1);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("SnapUI"))
		{
			sphereVisualizer.gameObject.SetActive(false);
			snapUICursor.gameObject.SetActive(true);
			snapUICursor.col = other;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("SnapUI"))
		{
			sphereVisualizer.gameObject.SetActive(true);
			snapUICursor.gameObject.SetActive(false);
		}
	}
}