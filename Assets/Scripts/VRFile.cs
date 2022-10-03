using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System.Threading;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using unityutilities.VRInteraction;
using unityutilities;

/// <summary>
/// A generic object that can represent any file.  
/// </summary>
public class VRFile : MonoBehaviour
{
	/// <summary>
	/// The folder this file came from
	/// </summary>
	[ReadOnly] public FolderManager folderManager;

	public VRGrabbable vrGrabbable;
	[ReadOnly] public string folderName;

	/// <summary>
	/// Just the filename with extension. No path.
	/// </summary>
	[ReadOnly] public string fileName;

	/// <summary>
	/// Full path of file with extension
	/// </summary>
	public string fullFileName => Path.Combine(folderName, fileName);

	public string fileNameWithoutExtensionOrPostfixes
	{
		get
		{
			string name = Path.GetFileNameWithoutExtension(fileName);
			string postfix = "_annotated";
			if (name.EndsWith(postfix))
			{
				name = name.Substring(0, name.IndexOf(postfix));
			}

			return name;
		}
	}

	[ReadOnly] public string lastTimestamp;

	public FileType fileType => GetFileType(fileName);

	public GameObject[] labelContainers;

	public Transform[] rotateToFaceObjs;

	public enum FileType
	{
		none,
		image,
		pdf,
		pcd,
		directory,
		table,
		video,
		text,
		youtube
	}

	private Texture2D imageTexture;
	public Transform content;
	public GameObject content2D;
	public Texture2D folderImage;
	public TableFile tableFilePrefab;
	public TextFile textFilePrefab;
	public VideoFile videoFilePrefab;
	public PDFFile pdfFilePrefab;
	public PCDLoader pointCloudPrefab;
	public List<TextFile> textInstances = new List<TextFile>();
	public List<VideoFile> videoInstances = new List<VideoFile>();
	public List<PDFFile> pdfInstances = new List<PDFFile>();
	public List<TableFile> tableInstances = new List<TableFile>();

	public List<PCDLoader> pointCloudInstances = new List<PCDLoader>();
	public PCDFile countCloud;
	public Transform countCloudParent;
	public GameObject countMarkerPrefab;
	public Dictionary<GameObject, PCDPoint> countCloudObjects = new Dictionary<GameObject, PCDPoint>();

	/// <summary>
	/// Count sphere added or removed. int is the new count.
	/// </summary>
	public Action<int> CountChanged;

	//public GameObject contentText;
	public Canvas contextMenuCanvas;
	public VRFileContextMenu contextMenu;
	public VRFileContextMenu contextMenu2D;
	public VRFileContextMenu contextMenuTablet;
	public Transform colliderHandle;

	private EditType currentEditMode;

	public enum EditType
	{
		None,
		View,
		Count,
		Segment,
		PaintCount,
		StudySegment
	}

	public EditType CurrentEditMode
	{
		set
		{
			if (value == EditType.None)
			{
				contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Normal);
				contextMenu2D.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Normal);
				contextMenuTablet.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Normal);
				// colliderHandle.gameObject.SetActive(true);
				//GetComponent<Rigidbody>().isKinematic = false;

				if (fileType == FileType.pcd && pointCloudInstances[0] != null)
				{
					// pointCloudInstances[0].SavePCDFile("_annotated");
				}

				if (countCloudParent != null) countCloudParent.gameObject.SetActive(false);
				// countCloud?.Save(Path.Combine(folderName, Path.GetFileNameWithoutExtension(fileName) + "_countcloud.pcd"));
			}
			else
			{
				colliderHandle.gameObject.SetActive(false);
				//GetComponent<Rigidbody>().isKinematic = true;

				switch (value)
				{
					case EditType.Count:
					{
						contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Counting);
						contextMenu2D.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Counting);
						contextMenuTablet.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Counting);
						if (countCloud == null)
						{
							countCloud = new PCDFile
							{
								fieldsList = new List<PCDFieldType>
								{
									PCDFieldType.x,
									PCDFieldType.y,
									PCDFieldType.z,
									PCDFieldType.Scalar_field,
									PCDFieldType.layer,
								}
							};
						}

						if (countCloudParent != null) countCloudParent.gameObject.SetActive(true);
						break;
					}
					case EditType.Segment:
						if (fileType == FileType.pcd)
						{
							contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Segmenting);
							contextMenu2D.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Segmenting);
							contextMenuTablet.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.Segmenting);
						}
						else
						{
							Debug.LogError("Can't segment file that isn't a pcd");
						}

						break;
					case EditType.PaintCount:
						if (fileType == FileType.pcd)
						{
							contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.PaintCount);
							contextMenu2D.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.PaintCount);
							contextMenuTablet.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.PaintCount);
						}
						else
						{
							Debug.LogError("Can't segment file that isn't a pcd");
						}

						break;
					case EditType.StudySegment:
						if (fileType == FileType.pcd)
						{
							contextMenu.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.StudySegment);
							contextMenu2D.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.StudySegment);
							contextMenuTablet.SetContextMenuMode(VRFileContextMenu.ContextMenuMode.StudySegment);
						}
						else
						{
							Debug.LogError("Can't segment file that isn't a pcd");
						}

						break;
				}
			}

			currentEditMode = value;
		}
		get => currentEditMode;
	}

	public float FileScale
	{
		set { transform.localScale = Vector3.one * value; }
		get => transform.localScale.x;
	}

	private IEnumerator Start()
	{
		vrGrabbable.Released += ObjectPlaced;

		yield return null;
		contextMenuCanvas.worldCamera = Camera.main;
	}

	private void Update()
	{
		PlayerPrefsJson.SetDictionary(fileName, new Dictionary<string, object>
		{
			{"pos", transform.position},
			{"rot", transform.eulerAngles},
			{"localScale", transform.localScale},
			{"contentPos", content.localPosition},
			{"contentRot", content.localEulerAngles},
			{"contentScale", content.localScale},
			{"pointSize", PointSize},
		}, Path.Combine(folderName, "datafoldvr_prefs.json"));

		// rotate text elems to face the user
		if (fileType != FileType.image)
		{
			Vector3[] directions =
			{
				Vector3.up,
				-Vector3.up,
				Vector3.right,
				-Vector3.right,
				Vector3.forward,
				-Vector3.forward
			};
			Vector3 camPos = PlayerManager.instance.cam.position;
			float lowest = 1000f;
			Vector3 targetDir = Vector3.zero;
			foreach (var dir in directions)
			{
				float angle = Vector3.Angle(transform.TransformDirection(dir),
					(camPos - transform.position).normalized);
				if (angle < lowest)
				{
					targetDir = dir;
					lowest = angle;
				}
			}

			foreach (Transform item in rotateToFaceObjs)
			{
				if (item != null)
				{
					item.forward = -transform.TransformDirection(targetDir);
					//item.up = transform.up;
				}
			}
		}
	}

	public void AddCounter(Vector3 position, float radius, byte layer = 0)
	{
		PCDPoint point = new PCDPoint
		{
			layer = layer,
			position = position,
			Scalar_field = radius
		};
		countCloud.data.Add(point);
		InstantiateCountCloudSphere(point);
		CountChanged?.Invoke(countCloud.data.Count);
	}

	public void RemoveCounter(GameObject sphere)
	{
		if (!countCloudObjects.ContainsKey(sphere)) throw new Exception("Object isn't in dictionary. aah");
		countCloud.data.Remove(countCloudObjects[sphere]);
		countCloudObjects.Remove(sphere);
		Destroy(sphere);
		CountChanged?.Invoke(countCloud.data.Count);
	}

	public static FileType GetFileType(string filename)
	{
		string ext = Path.GetExtension(filename);
		if (Directory.Exists(filename))
		{
			return FileType.directory;
		}

		switch (ext)
		{
			case ".txt":
				return FileType.text;
			case ".png":
			case ".jpg":
			case ".jpeg":
				return FileType.image;
			case ".mp4":
				return FileType.video;
			case ".fbx":
				return FileType.none; // TODO
			case ".csv":
				return FileType.table;
			case ".youtube":
				return FileType.youtube;
			case ".pdf":
				return FileType.pdf;
			case ".pcd":
				return FileType.pcd;
			default:
				return FileType.none;
		}
	}

	void ObjectPlaced()
	{
		//JSONConfig.SetData(folderName, fileName, new JSONConfig.ObjectData(transform.position, transform.rotation, transform.localScale));
	}

	public float PointSize
	{
		get
		{
			if (pointCloudInstances.Count == 0) return 0;
			return pointCloudInstances[0].pcxRenderer.pointSize;
		}
		set
		{
			if (pointCloudInstances.Count == 0) return;
			pointCloudInstances[0].pcxRenderer.pointSize = value;
		}
	}

	IEnumerator loadDirectory(string s)
	{
		string folderText = Directory.GetFiles(s).Length + " Files";
		TextMeshPro[] tmp = content2D.GetComponentsInChildren<TextMeshPro>();
		content2D.GetComponent<MeshRenderer>().material.mainTexture = folderImage;
		foreach (TextMeshPro t in tmp)
		{
			t.text = folderText;
		}

		yield return null;
	}

	IEnumerator loadTableFile(string s)
	{
		content2D.gameObject.SetActive(false);
		if (tableInstances.Count == 0)
		{
			TableFile initialText = Instantiate(tableFilePrefab, transform.position, transform.rotation, content);
			initialText.path = s;
			initialText.updateContent();
			tableInstances.Add(initialText);
		}
		else
		{
			foreach (TableFile tf in tableInstances)
			{
				tf.updateContent();
			}
		}

		yield return null;
	}

	IEnumerator loadTextFile(string s)
	{
		content2D.gameObject.SetActive(false);
		if (textInstances.Count == 0)
		{
			TextFile initialText = Instantiate(textFilePrefab, transform.position, transform.rotation, content);
			initialText.path = s;
			initialText.updateContent();
			textInstances.Add(initialText);
		}
		else
		{
			foreach (TextFile tf in textInstances)
			{
				tf.updateContent();
			}
		}


		yield return null;
	}

	IEnumerator loadImageFile(string s)
	{
		WWW www = new WWW("file://" + s);
		yield return www;

		if (www.isDone)
		{
			imageTexture = new Texture2D(4, 4);

			www.LoadImageIntoTexture(imageTexture);
			content2D.GetComponent<MeshRenderer>().material.mainTexture = imageTexture;
		}

		yield return null;
	}

	IEnumerator loadVideoFile(string s)
	{
		content2D.gameObject.SetActive(false);


		content2D.gameObject.SetActive(false);
		if (videoInstances.Count == 0)
		{
			VideoFile vf = Instantiate(videoFilePrefab, transform.position, transform.rotation, content);
			vf.transform.Rotate(0, 90, 0);
			vf.path = "file://" + s;
			vf.updateContent();
			videoInstances.Add(vf);
		}
		else
		{
			foreach (VideoFile vf in videoInstances)
			{
				vf.updateContent();
			}
		}

		yield return null;
	}

	IEnumerator loadYoutubeFile(string s)
	{
		content2D.gameObject.SetActive(false);
		//VideoPlayer vp = content.AddComponent<VideoPlayer>();
		//VideoFile vp = GameObject.Instantiate<VideoFile>(videoFilePrefab, transform.position, transform.rotation, transform);
		//vp.GetComponentInChildren m_strFileName = File.ReadAllText(s);
		//yield return null;
		//yield return new WaitForSeconds(3.0f);
		//vp.Play();

		content2D.gameObject.SetActive(false);
		if (videoInstances.Count == 0)
		{
			VideoFile vp = Instantiate(videoFilePrefab, transform.position, transform.rotation, content);
			vp.path = File.ReadAllText(s);
			vp.updateContent();
			videoInstances.Add(vp);
		}
		else
		{
			foreach (VideoFile vf in videoInstances)
			{
				vf.updateContent();
			}
		}

		yield return null;
	}

	IEnumerator loadPointCloud(string s, Dictionary<string, object> defaultData)
	{
		content2D.gameObject.SetActive(false);
		if (pointCloudInstances.Count == 0)
		{
			PCDLoader pcd = Instantiate(pointCloudPrefab, content);
			pcd.filename = s;
			pcd.vrFile = this;
			pcd.LoadPCDPoints();
			if (defaultData.ContainsKey("pointSize"))
			{
				pcd.pcxRenderer.pointSize = float.Parse(defaultData["pointSize"].ToString());
			}
			pointCloudInstances.Add(pcd);
			// pcd.FinishedLoading += ScalePointCloudToUnitSize;
			CameraPositionsScanVisualizerLoader cameraPositions = new CameraPositionsScanVisualizerLoader();
			pcd.FinishedLoading += (_) =>
			{
				// cameraPositions.ReadCameraPositions_shots(this);
				PlayerManager.instance.EditFile(this, CurrentEditMode);	// make stuff has been initialized properly
			};
		}
		else
		{
			foreach (PCDLoader p in pointCloudInstances)
			{
				p.LoadPCDPoints();
			}
		}

		yield return null;
	}

	/// <summary>
	/// Loads the points that represent count positions to go with another file type
	/// </summary>
	/// <param name="s">The count cloud proxy file's full filename.</param>
	IEnumerator loadCountCloud(string s)
	{
		UnityWebRequest www = UnityWebRequest.Get(s);
		yield return www.SendWebRequest();

		BinaryReader reader = new BinaryReader(new MemoryStream(www.downloadHandler.data));

		countCloud = new PCDFile();
		Thread readThread = new Thread(() => countCloud.LoadThread(reader));
		readThread.Start();

		while (readThread.IsAlive) yield return null;
		InstantiateCountCloudSpheres();
		if (currentEditMode != EditType.Count)
		{
			countCloudParent.gameObject.SetActive(false);
		}
	}

	private void InstantiateCountCloudSpheres()
	{
		foreach (PCDPoint point in countCloud.data)
		{
			InstantiateCountCloudSphere(point);
		}

		CountChanged?.Invoke(countCloud.data.Count);
	}

	private void InstantiateCountCloudSphere(PCDPoint point)
	{
		if (countCloudParent == null)
		{
			countCloudParent = new GameObject("CountCloud").transform;
			countCloudParent.SetParent(transform);
			countCloudParent.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			// countCloudParent.gameObject.SetActive(false);
		}

		GameObject marker = Instantiate(countMarkerPrefab, point.position, Quaternion.identity, countCloudParent);
		marker.transform.localScale = Vector3.one * point.Scalar_field;
		countCloudObjects[marker] = point;
	}

	private void ScalePointCloudToUnitSize(PCDLoader pcd)
	{
		pcd.transform.localPosition = Vector3.zero;
		pcd.transform.localScale = Vector3.one;
		Bounds bounds = pcd.GetComponent<MeshRenderer>().bounds;
		Vector3 offset = Vector3.zero;
		if (bounds.size.magnitude != 0)
		{
			pcd.transform.localScale = pcd.transform.localScale * transform.localScale.x / bounds.size.magnitude;

			Vector3 boundsCenter = pcd.transform.TransformPoint(pcd.GetComponent<MeshFilter>().mesh.bounds.center);
			offset = boundsCenter - transform.position;
		}

		pcd.transform.position = transform.position - offset;
	}

	IEnumerator loadPDFFile(string s)
	{
		content2D.gameObject.SetActive(false);
		if (pdfInstances.Count == 0)
		{
			PDFFile pdf = Instantiate(pdfFilePrefab, transform.position, transform.rotation, content);
			pdf.path = s;
			pdf.updateContent();
			pdfInstances.Add(pdf);
		}
		else
		{
			foreach (PDFFile p in pdfInstances)
			{
				p.updateContent();
			}
		}

		yield return null;
	}

	public void UpdateContents(string f, FolderManager folderManager)
	{
		this.folderManager = folderManager;
		folderName = Path.GetDirectoryName(f);
		fileName = Path.GetFileName(f);


		Dictionary<string, object> data = PlayerPrefsJson.GetDictionary(
			fileName,
			new Dictionary<string, object>
			{
				{"pos", transform.position},
				{"rot", transform.eulerAngles},
				{"localScale", transform.localScale},
				{"contentPos", content.localPosition},
				{"contentRot", content.localEulerAngles},
				{"contentScale", content.localScale},
				{"pointSize", PointSize},
			},
			Path.Combine(folderName, "datafoldvr_prefs.json"));

		content.localPosition = data["contentPos"].ToVector3();
		content.localEulerAngles = data["contentRot"].ToVector3();
		content.localScale = data["contentScale"].ToVector3();

		string currTimestamp = File.GetLastWriteTime(f).ToString();
		if (currTimestamp != lastTimestamp)
		{
			lastTimestamp = currTimestamp;
			foreach (GameObject o in labelContainers)
			{
				TextMeshPro[] t = o.GetComponentsInChildren<TextMeshPro>();
				foreach (TextMeshPro tmp in t)
				{
					tmp.text = fileName + "\n" + lastTimestamp;
				}

				TextMeshProUGUI[] tmpugui = o.GetComponentsInChildren<TextMeshProUGUI>();
				foreach (TextMeshProUGUI tmp in tmpugui)
				{
					tmp.text = fileName + "\n" + lastTimestamp;
				}
			}

			switch (GetFileType(f))
			{
				case FileType.none:
					break;
				case FileType.image:
					StartCoroutine(loadImageFile(f));
					break;
				case FileType.pdf:
					StartCoroutine(loadPDFFile(f));
					break;
				case FileType.pcd:
					StartCoroutine(loadPointCloud(f, data));
					break;
				case FileType.directory:
					StartCoroutine(loadDirectory(f));
					break;
				case FileType.table:
					break;
				case FileType.video:
					StartCoroutine(loadVideoFile(f));
					break;
				case FileType.text:
					StartCoroutine(loadTextFile(f));
					break;
				case FileType.youtube:
					StartCoroutine(loadYoutubeFile(f));
					break;
			}

			// load the count points if they exist from a proxy file
			string countCloudName = Path.Combine(folderName,
				Path.GetFileNameWithoutExtension(fileName) + "_countcloud.pcd");
			if (File.Exists(countCloudName)) StartCoroutine(loadCountCloud(countCloudName));
		}

		lastTimestamp = currTimestamp;
	}


	public void destroy()
	{
		foreach (TableFile tf in tableInstances)
		{
			tf.destroy();
		}
	}
}