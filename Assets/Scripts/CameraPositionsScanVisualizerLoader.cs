using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using unityutilities;

public class CameraPositionsScanVisualizerLoader
{
	public class ShotsJSON
	{
		public Feature[] features;

		public class Feature
		{
			public Properties properties;

			public string Filename
			{
				get => properties.filename;
			}

			public Vector3 Position
			{
				get => properties.translation.ToVector3();
			}

			public Vector3 Rotation
			{
				get => properties.rotation.ToVector3();
			}

			public class Properties
			{
				public string filename;
				public float[] translation;
				public float[] rotation;
			}
		}
	}

	public class ReconstructionShot
	{
		public float[] rotation;
		public float[] translation;
		public string camera;

		public Vector3 Position
		{
			get => translation.ToVector3();
		}

		public Vector3 Rotation
		{
			get => rotation.ToVector3();
		}
	}

	/// <summary>
	/// Reads the camera positions *for* the specified filename.
	/// The filename is not the images.json
	/// </summary>
	public void ReadCameraPositions_reconstruction(VRFile file)
	{
		string postfix = "_reconstruction.json";
		string imagesFilePath = Path.Combine(file.folderName, file.fileNameWithoutExtensionOrPostfixes + postfix);

		if (!File.Exists(imagesFilePath)) return;

		string imagesJson = File.ReadAllText(imagesFilePath);

		var data = JsonConvert.DeserializeObject<JArray>(imagesJson)[0];
		JObject images = (JObject) (data["shots"]);
		JObject points = (JObject) (data["points"]);

		List<Vector3> positions = new List<Vector3>();
		List<Vector3> pointPositions = new List<Vector3>();
		List<ReconstructionShot> imgObjects = new List<ReconstructionShot>();
		foreach (KeyValuePair<string, JToken> image in images)
		{
			ReconstructionShot obj = image.Value.ToObject<ReconstructionShot>();
			imgObjects.Add(obj);
			positions.Add(obj.Position);
		}

		int count = 0;
		foreach (var point in points)
		{
			if (count++ > 5000) break;
			pointPositions.Add(point.Value["coordinates"].ToObject<float[]>().ToVector3());
		}

		Vector3 avg = new Vector3(
			positions.Average(x => x.x),
			positions.Average(x => x.y),
			positions.Average(x => x.z));

		foreach (KeyValuePair<string, JToken> image in images)
		{
			ReconstructionShot imgObj = image.Value.ToObject<ReconstructionShot>();
			GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cube.name = image.Key;
			cube.transform.SetParent(file.pointCloudInstances[0].transform);
			cube.transform.localScale = Vector3.one;
			cube.transform.localPosition = imgObj.Position;
			cube.transform.localEulerAngles = imgObj.Rotation;
		}

		foreach (var p in pointPositions)
		{
			GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cube.transform.SetParent(file.pointCloudInstances[0].transform);
			cube.transform.localScale = Vector3.one;
			cube.transform.localPosition = p;
		}
	}

	/// <summary>
	/// Reads the camera positions *for* the specified filename.
	/// The filename is not the images.json
	/// </summary>
	public void ReadCameraPositions_shots(VRFile file)
	{
		string postfix = "_shots.geojson";
		string imagesFilePath = Path.Combine(file.folderName, file.fileNameWithoutExtensionOrPostfixes + postfix);

		if (!File.Exists(imagesFilePath)) return;

		string imagesJson = File.ReadAllText(imagesFilePath);

		ShotsJSON.Feature[] features = JsonConvert.DeserializeObject<ShotsJSON>(imagesJson).features;

		List<Vector3> positions = features.Select(image => image.Position).ToList();

		Vector3 avg = new Vector3(
			positions.Average(x => x.x),
			positions.Average(x => x.y),
			positions.Average(x => x.z));

		Transform shotsParent = new GameObject("shots").transform;
		shotsParent.SetParent(file.pointCloudInstances[0].transform);
		shotsParent.localPosition =
			PlayerPrefsJson.GetVector3(file.folderName + "_" + file.fileName + "_shots_position");
		PlayerPrefsJson.SetVector3(file.folderName + "_" + file.fileName + "_shots_position",
			shotsParent.localPosition);
		shotsParent.localRotation = Quaternion.identity;
		shotsParent.localScale = Vector3.one;
		OnlyShowNearest onlyShowNearest = shotsParent.gameObject.AddComponent<OnlyShowNearest>();
		foreach (ShotsJSON.Feature image in features)
		{
			// add the shot location indicator
			Transform cube = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
			cube.name = image.Filename;
			cube.SetParent(shotsParent);
			cube.localScale = Vector3.one;
			cube.localPosition = image.Position - avg;
			cube.localEulerAngles = image.Rotation;

			// add a camera plane
			Transform imagePlane = GameObject.CreatePrimitive((PrimitiveType.Quad)).transform;
			imagePlane.name = image.Filename;

			// load the texture
			MeshRenderer mr = imagePlane.GetComponent<MeshRenderer>();
			mr.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
			string filePath = Path.Combine(
				file.folderName,
				Path.Combine(
					file.fileNameWithoutExtensionOrPostfixes + "_images",
					image.Filename));

			Texture2D tex = new Texture2D(1, 1);
			if (File.Exists(filePath))
			{
				byte[] bytes = File.ReadAllBytes(filePath);
				tex.LoadImage(bytes);
				mr.material.mainTexture = tex;
			}

			// set the position
			float aspect = (float) tex.height / tex.width;
			imagePlane.SetParent(cube);
			const float scaleOffset = 50;
			imagePlane.localScale = new Vector3(-1, aspect) * scaleOffset;
			imagePlane.localPosition = -Vector3.forward * scaleOffset;
			imagePlane.localEulerAngles = image.Rotation + new Vector3(180, 0, 0);
			imagePlane.gameObject.SetActive(false);

			onlyShowNearest.Add(cube, imagePlane);
		}
	}
}


public static class JsonExtensions
{
	public static Vector3 ToVector3(this float[] list)
	{
		return new Vector3(
			list[0],
			list[1],
			list[2]
		);
	}
}