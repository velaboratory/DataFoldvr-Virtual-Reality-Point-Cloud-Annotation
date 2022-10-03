// Pcx - Point cloud importer & renderer for Unity
// https://github.com/keijiro/Pcx

using UnityEngine;
using System.Collections.Generic;

namespace DataFoldvrPcx
{
	/// A container class optimized for compute buffer.
	public sealed class PointCloudData : ScriptableObject
	{
		#region Public properties

		/// Byte size of the point element.
		public const int pointStructSize = sizeof(float) * 4 + sizeof(uint);

		/// Number of points.
		public int pointCount
		{
			get { return _pointData.Length; }
		}

		/// Get access to the compute buffer that contains the point cloud.
		public ComputeBuffer computeBuffer
		{
			get
			{
				if (_pointBuffer == null)
				{
					_pointBuffer = new ComputeBuffer(pointCount, pointStructSize);
					_pointBuffer.SetData(_pointData);
				}

				return _pointBuffer;
			}
		}

		#endregion

		#region ScriptableObject implementation

		ComputeBuffer _pointBuffer;

		void OnDisable()
		{
			if (_pointBuffer != null)
			{
				_pointBuffer.Release();
				_pointBuffer = null;
			}
		}

		#endregion

		#region Serialized data members

		[System.Serializable]
		public struct Point
		{
			public Vector3 position;
			public uint color;
			public uint ref_layer;
		}

		[SerializeField] Point[] _pointData;

		#endregion

		#region Editor functions


		public static uint EncodeColor(Color c)
		{
			const float kMaxBrightness = 16;

			float y = Mathf.Max(Mathf.Max(c.r, c.g), c.b);
			y = Mathf.Clamp(Mathf.Ceil(y * 255 / kMaxBrightness), 1, 255);

			Vector3 rgb = new Vector3(c.r, c.g, c.b);
			rgb *= 255 * 255 / (y * kMaxBrightness);

			return ((uint) rgb.x) |
			       ((uint) rgb.y << 8) |
			       ((uint) rgb.z << 16) |
			       ((uint) y << 24);
		}

		public static uint EncodeColorAndLayer(Color c, int layer)
		{
			return (uint) ((byte) (c.r * 255) |
			               ((byte) (c.g * 255) << 8) |
			               ((byte) (c.b * 255) << 16) |
			               ((byte) layer << 24));
		}
		
		public static (Color, byte) DecodeColorAndLayer(uint colorAndLayer)
		{
			Color c = new Color(
				(colorAndLayer >> 0) & 0xFF,
				(colorAndLayer >> 8) & 0xFF,
				(colorAndLayer >> 16) & 0xFF);
			byte layer = (byte)((colorAndLayer >> 24) & 0xFF);
			
			return (c, layer);
		}

#if UNITY_EDITOR
		public void Initialize(List<Vector3> positions, List<Color32> colors)
		{
			_pointData = new Point[positions.Count];
			for (int i = 0; i < _pointData.Length; i++)
			{
				_pointData[i] = new Point
				{
					position = positions[i],
					color = EncodeColorAndLayer(colors[i], 0),
				};
			}
		}

#endif

		#endregion
	}
}