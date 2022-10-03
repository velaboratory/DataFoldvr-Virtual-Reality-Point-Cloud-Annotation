using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using unityutilities;
using unityutilities.VRInteraction;
using Logger = unityutilities.Logger;

public class OrbitMouseInput : MonoBehaviour
{
	public float sensitivity = .5f;
	public float scrollSensitivity = .02f;
	public float verticalMoveSensitivity = .001f;
	public float dialSensitivity = .5f;
	public float sliderSensitivity = 1;

	public Camera camera;
	public Transform cameraTarget;
	private Vector2 mousePos = Vector2.zero;
	[ReadOnly] public Vector3 mouseWorldPosition;

	public static int blockMovement = 0;

	private VRGrabbable grabbed;
	private VRGrabbable lastHovered;
	private float timeHeld;

	// Update is called once per frame
	void Update()
	{
		float deltaX = Input.mousePosition.x - mousePos.x;
		float deltaY = Input.mousePosition.y - mousePos.y;


		if (blockMovement == 0 || !EventSystem.current.IsPointerOverGameObject())
		{
			#region ORBIT CAM

			if (Input.GetMouseButtonDown(1))
			{
				deltaX = 0;
				deltaY = 0;
			}

			if (Input.GetMouseButton(1))
			{
				transform.RotateAround(transform.position, Vector3.up, deltaX * sensitivity);
				transform.RotateAround(transform.position, transform.right, -deltaY * sensitivity);
			}

			float scrollDelta = Input.mouseScrollDelta.y;
			if (scrollDelta != 0)
			{
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					transform.Translate(0, scrollDelta * scrollSensitivity, 0, Space.World);
				}
				else
				{
					Vector3 pos = cameraTarget.transform.localPosition;
					pos.z += scrollDelta * scrollSensitivity;
					cameraTarget.transform.localPosition = pos;
				}
			}

			if (Input.GetMouseButton(2))
			{
				transform.Translate(0, -deltaY * verticalMoveSensitivity, 0, Space.World);
			}

			#endregion
		}

		mousePos.x = Input.mousePosition.x;
		mousePos.y = Input.mousePosition.y;

		timeHeld += Time.deltaTime;
	}
}