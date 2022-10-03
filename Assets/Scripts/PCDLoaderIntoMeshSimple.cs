using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine.Networking;
using System;
using System.Linq;

namespace SimplePCDReader
{
	public class PCDLoaderIntoMeshSimple : MonoBehaviour
	{
		public string filename;

		[Tooltip("Reads filename from StreamingAssets at start for independent usage")]
		public bool autoLoadAtStart;

		public PCDFile pcdFile;
		public Material material;
		private MeshRenderer rend;

		// progress bar
		[Range(0, 1)] public float loadProgress = 1;

		private void Start()
		{

			if (autoLoadAtStart) LoadPCDPoints();
		}


		private void Update()
		{
			if (pcdFile != null)
			{
				if (pcdFile.loadFinishedFlag)
				{
					pcdFile.loadFinishedFlag = false;
					RefreshGameObjects();
				}

				if (pcdFile.loadProgress < 1)
				{
					loadProgress = pcdFile.loadProgress;
				}
			}
		}

		public void LoadPCDPoints()
		{
			StartCoroutine(ReadFromFile());
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

		public void SavePCDPoints(PCDDataType pcdDataType = PCDDataType.binary)
		{
			pcdFile.Save(filename, pcdDataType);
		}


		public void RefreshGameObjects()
		{
			BuildGameObject();
		}

		private void BuildGameObject()
		{	
			MeshFilter mf = gameObject.FindOrAddComponent<MeshFilter>();
			Mesh mesh = new Mesh();
			rend = gameObject.FindOrAddComponent<MeshRenderer>();

			int[] indices = new int[pcdFile.data.Count];
			for (int i = 0; i < pcdFile.data.Count; i++)
			{
				indices[i] = i;
			}
			mesh.SetVertices(pcdFile.data.ToPositionsArray());
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.SetIndices(indices, MeshTopology.Points, 0);
			mesh.SetColors(pcdFile.data.ToColorsArray());
			mesh.RecalculateBounds();
			mf.mesh = mesh;

			if (material == null)
			{
				material = new Material(Shader.Find("Unlit/Color"));
			}
			rend.sharedMaterial = material;
		}
	}
	
	static class GameObjectExtensions
	{
		public static T FindOrAddComponent<T>(this GameObject gameObject) where T : Component
		{
			T component = gameObject.GetComponent<T>();
			if (component == null) component = gameObject.AddComponent<T>();
			return component;
		}

		public static Transform FindOrAddGameObject(this Transform transform, string name)
		{
			Transform obj = transform.Find(name);
			if (obj != null) return obj;
			obj = new GameObject(name).transform;
			obj.SetParent(transform);
			obj.localPosition = Vector3.zero;
			obj.localScale = Vector3.one;
			obj.localRotation = Quaternion.identity;

			return obj;
		}

		public static Vector3[] ToPositionsArray(this List<PCDPoint> points)
		{
			return points.Select(d => d.position).ToArray();
		}

		public static Color[] ToColorsArray(this List<PCDPoint> points)
		{
			return points.Select(d => d.color).ToArray();
		}
	}
}