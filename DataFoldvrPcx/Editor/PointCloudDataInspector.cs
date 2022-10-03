// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

using UnityEngine;
using UnityEditor;

namespace DataFoldvrPcx
{
	[CustomEditor(typeof(PointCloudData))]
	public sealed class PointCloudDataInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			int count = ((PointCloudData) target).pointCount;
			EditorGUILayout.LabelField("Point Count", count.ToString("N0"));
			EditorGUILayout.LabelField("Size in Memory", $"{count * sizeof(float) * 4 / 1000000:N2} MB");
		}
	}
}