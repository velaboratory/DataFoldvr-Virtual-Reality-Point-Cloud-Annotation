using System.IO;
using UnityEngine;
using unityutilities;
using unityutilities.VRInteraction;

public class VRScaleGrabbable : VRGrabbable
{
	[Tooltip("(Optional)\nThe object that movement should apply to. If this is null, the current transform is used.")]
	public Transform baseTransform;

	Vector3 lastGrabLocation;
	Vector3 lastGrabScale;
	float lastGrabDistance;

	private void Update()
	{
		if (listOfGrabbedByHands.Count > 0)
		{
			//Vector3 diff = listOfGrabbedByHands[0].transform.position - lastGrabLocation;
			float newDistance = Vector3.Distance(listOfGrabbedByHands[0].transform.position, baseTransform.position);
			baseTransform.localScale = lastGrabScale * newDistance / lastGrabDistance;
		}
	}

	public override void HandleGrab(VRGrabbableHand h)
	{
		base.HandleGrab(h);

		lastGrabLocation = h.transform.position;
		lastGrabDistance = Vector3.Distance(h.transform.position, baseTransform.position);
		lastGrabScale = baseTransform.localScale;
	}

	public override byte[] PackData()
	{
		using (MemoryStream outputStream = new MemoryStream())
		{
			BinaryWriter writer = new BinaryWriter(outputStream);

			writer.Write(baseTransform.localScale);
			writer.Write(baseTransform.localRotation);

			return outputStream.ToArray();
		}
	}

	public override void UnpackData(byte[] data)
	{
		using (MemoryStream inputStream = new MemoryStream(data))
		{
			BinaryReader reader = new BinaryReader(inputStream);

			baseTransform.localScale = reader.ReadVector3();
			baseTransform.localRotation = reader.ReadQuaternion();
		}
	}
}
