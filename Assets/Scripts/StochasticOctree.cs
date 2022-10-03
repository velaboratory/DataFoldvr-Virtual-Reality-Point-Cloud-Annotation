using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

class StochasticOctree
{
	public StochasticOctree[] myChildren = new StochasticOctree[8];
	GameObject myGameObject;
	public Vector3 center = Vector3.zero;
	public Vector3 bounds = Vector3.zero;
	public int depth;
	float probability = 1.0f;
	[System.NonSerialized] public float[] lodLayers;
	public MeshRenderer rend;
	public LODGroup lodGroup;

	/// <summary>
	/// Whether points should be added to children objects that were already added to the parent object
	/// </summary>
	private const bool pointsAreAdditive = false;

	public StochasticOctree(float[] lodLayers, int depth = 0)
	{
		this.depth = depth;
		this.lodLayers = lodLayers;
		float sum = 0;
		for (int i = 0; i < depth; i++)
		{
			sum += lodLayers[i];
		}
		probability = lodLayers[depth] - sum;
	}
	List<PCDPoint> myPCDPoints = new List<PCDPoint>();
	List<PCDPoint> tempPCDPoints = new List<PCDPoint>();

	public void ClearPoints()
	{
		tempPCDPoints.Clear();
	}

	/// <summary>
	/// Adds a point to the octree
	/// </summary>
	/// <param name="p">new Point</param>
	public void AddPoint(PCDPoint p)
	{
		int i = tempPCDPoints.Count;
		if (i == 0)
		{
			center = p.position;
		}
		else
		{
			float ratio = 1.0f / (tempPCDPoints.Count + 1);
			center = center * (1 - ratio) + p.position * ratio;
		}

		tempPCDPoints.Add(p);
	}

	public void ApplyPoints(ref float loadProgress)
	{
		Stopwatch timer = new Stopwatch();
		timer.Start();
		// presumed to be in the node's space 
		// compute the extents of this octree
		for (int i = 0; i < tempPCDPoints.Count; i++)
		{
			Vector3 tempBounds = tempPCDPoints[i].position - center;
			tempBounds = new Vector3(Mathf.Abs(tempBounds.x), Mathf.Abs(tempBounds.y), Mathf.Abs(tempBounds.z));
			if (tempBounds.x > bounds.x)
			{
				bounds.x = tempBounds.x;
			}
			if (tempBounds.y > bounds.y)
			{
				bounds.y = tempBounds.y;
			}
			if (tempBounds.z > bounds.z)
			{
				bounds.z = tempBounds.z;
			}
		}
		timer.Stop();
		var time = timer.ElapsedMilliseconds;   // 1758
		timer.Restart();

		foreach (var child in myChildren)
		{
			if (child != null)
			{
				child.ClearPoints();
			}
		}
		timer.Stop();
		time = timer.ElapsedMilliseconds;   // 0
		timer.Restart();

		// now that we have the center we can add to the octree stochastically
		System.Random rand = new System.Random();
		myPCDPoints.Clear();
		for (int i = 0; i < tempPCDPoints.Count; i++)
		{
			Vector3 p = tempPCDPoints[i].position;
			bool addedToParent = false;
			// either add the point to the current level
			if (rand.NextDouble() <= probability)
			{
				myPCDPoints.Add(tempPCDPoints[i]);
				addedToParent = true;
			}
			// or/and add it to a child
			if (!addedToParent || pointsAreAdditive)
			{
				if (lodLayers.Length > depth + 1)
				{
					// fun function to generate an index for which child this is.
					int offset = (p.y > center.y ? 0 : 4) + (p.z > center.z ? 0 : 2) + (p.x > center.x ? 0 : 1);
					tempPCDPoints[i].lod_id = (ushort)(depth * 8 + offset);
					if (myChildren[offset] == null)
					{
						myChildren[offset] = new StochasticOctree(lodLayers, depth + 1);
					}

					myChildren[offset].AddPoint(tempPCDPoints[i]);
				}
			}
		}
		timer.Stop();
		time = timer.ElapsedMilliseconds;   // 4257
		timer.Restart();
		for (int i = 0; i < myChildren.Length; i++)
		{
			loadProgress += (1 - loadProgress) / 50f;
			if (myChildren[i] != null)
			{
				myChildren[i].ApplyPoints(ref loadProgress);
			}
		}
		timer.Stop();
		time = timer.ElapsedMilliseconds;
		timer.Restart();
	}

	public void BuildGameObject(GameObject g)
	{
		myGameObject = g;
		//let's create a box per point
		MeshFilter mf = g.FindOrAddComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		rend = g.FindOrAddComponent<MeshRenderer>();

		int[] indices = new int[myPCDPoints.Count];
		for (int i = 0; i < myPCDPoints.Count; i++)
		{
			indices[i] = i;
		}
		mesh.SetVertices(myPCDPoints.ToPositionsArray());
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.SetIndices(indices, MeshTopology.Points, 0);
		mesh.SetColors(myPCDPoints.ToColorsArray());
		mesh.RecalculateBounds();
		mf.mesh = mesh;

		lodGroup = g.FindOrAddComponent<LODGroup>();
		lodGroup.SetLODs(new LOD[] { new LOD(depth / 4f, new Renderer[] { rend }) });
		lodGroup.RecalculateBounds();

		//// increment the loading bar
		////loadProgress += .5f / Mathf.Pow(8, lodLayers.Length - 1);
		//loadProgress += .1f;

		for (int i = 0; i < myChildren.Length; i++)
		{
			if (myChildren[i] != null)
			{
				Transform g_child = g.transform.FindOrAddGameObject(g.name + "_child" + i);

				myChildren[i].BuildGameObject(g_child.gameObject);
			}
		}

	}


	public void ApplyMaterial(Material m)
	{
		myGameObject.GetComponent<MeshRenderer>().sharedMaterial = m;
		for (int i = 0; i < myChildren.Length; i++)
		{
			if (myChildren[i] != null)
			{
				myChildren[i].ApplyMaterial(m);
			}
		}
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
		var obj = transform.Find(name);
		if (obj == null)
		{
			obj = new GameObject(name).transform;
			obj.SetParent(transform);
			obj.localPosition = Vector3.zero;
			obj.localScale = Vector3.one;
			obj.localRotation = Quaternion.identity;
		}

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
