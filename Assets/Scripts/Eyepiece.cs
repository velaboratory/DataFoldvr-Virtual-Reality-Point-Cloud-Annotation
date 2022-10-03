using UnityEngine;

public class Eyepiece : MonoBehaviour {
	public Camera cam;
	private RenderTexture rend;
	public int res = 256;
	private float fov;
	private Vector3 localPos;
	private Vector3 localRot;

	private void Awake() {
		localPos = cam.transform.localPosition;
		localRot = cam.transform.localEulerAngles;
		fov = cam.fieldOfView;
	}

	void Start() {
		if (cam) {
			rend = new RenderTexture(res, res, 16);
			cam.targetTexture = rend;
			GetComponent<Renderer>().material.mainTexture = rend;
			cam.fieldOfView = fov;
			cam.transform.localPosition = localPos;
			cam.transform.localEulerAngles = localRot;
		}
	}
}
