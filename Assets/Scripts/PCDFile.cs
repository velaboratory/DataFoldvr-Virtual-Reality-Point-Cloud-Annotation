using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using unityutilities;
using Debug = UnityEngine.Debug;

public enum PCDFieldType
{
	Time,
	x,
	y,
	z,
	rgb,
	normal_x,
	normal_y,
	normal_z,
	layer,
	ref_layer,
	_,
	Scalar_field,
	lod_id
}

public enum PCDDataType
{
	ascii,
	binary
}


public class PCDPoint
{
	public float time;
	public Vector3 position;
	public Color color;
	public Vector3 normal;
	public float Scalar_field;
	public byte layer;
	public byte ref_layer;
	public ushort lod_id;
}

public class PCDFile
{
	public string version;
	public string fields;

	public List<PCDFieldType> fieldsList;

	public string size;
	public string type;
	public string count;
	public string width;
	public string height;
	public string viewpoint;
	public string points;
	public uint numPoints;
	public PCDDataType dataType;
	public List<PCDPoint> data = new List<PCDPoint>();
	public List<int> allLayers;

	public float loadProgress;
	public bool loadFinishedFlag;

	public Vector3[] vertices {
		get { return data.Select(d => d.position).ToArray(); }
	}

	public Color[] color {
		get { return data.Select(d => d.color).ToArray(); }
	}

	public static bool IsProxyFilename(string filename)
	{
		return filename.EndsWith("_annotated.pcd") || filename.EndsWith("_countcloud.pcd");
	}


	#region Writing


	private static string AppendFieldToLine(PCDPoint p, PCDFieldType fieldType)
	{
		switch (fieldType)
		{
			case PCDFieldType.Time:
				return p.time.ToString();
			case PCDFieldType.x:
				return p.position.x.ToString();
			case PCDFieldType.y:
				return p.position.y.ToString();
			case PCDFieldType.z:
				return p.position.z.ToString();
			case PCDFieldType.rgb:
				byte[] bytes = {
					(byte) (p.color.b * 255),
					(byte) (p.color.g * 255),
					(byte) (p.color.r * 255),
					0
				};
				float color = BitConverter.ToSingle(bytes, 0);
				return color.ToString();
			case PCDFieldType.normal_x:
				return p.normal.x.ToString();
			case PCDFieldType.normal_y:
				return p.normal.y.ToString();
			case PCDFieldType.normal_z:
				return p.normal.z.ToString();
			case PCDFieldType.layer:
				return p.layer.ToString();
			case PCDFieldType.ref_layer:
				return p.ref_layer.ToString();
			case PCDFieldType._:
				return "0"; // idk what this actually is
			case PCDFieldType.Scalar_field:
				return p.Scalar_field.ToString(); // idk what this actually is
			case PCDFieldType.lod_id:
				return p.lod_id.ToString();
			default:
				return "0"; // error case
		}
	}

	private static void AppendFieldToLine(BinaryWriter w, PCDPoint p, PCDFieldType fieldType)
	{
		switch (fieldType)
		{
			case PCDFieldType.Time:
				w.Write(p.time);
				break;
			case PCDFieldType.x:
				w.Write(p.position.x);
				break;
			case PCDFieldType.y:
				w.Write(p.position.y);
				break;
			case PCDFieldType.z:
				w.Write(p.position.z);
				break;
			case PCDFieldType.rgb:
				w.Write((byte)(p.color.b * 255));
				w.Write((byte)(p.color.g * 255));
				w.Write((byte)(p.color.r * 255));
				w.Write((byte)(p.color.a * 255));
				break;
			case PCDFieldType.normal_x:
				w.Write(p.normal.x);
				break;
			case PCDFieldType.normal_y:
				w.Write(p.normal.y);
				break;
			case PCDFieldType.normal_z:
				w.Write(p.normal.z);
				break;
			case PCDFieldType.layer:
				w.Write(p.layer);
				break;
			case PCDFieldType.ref_layer:
				w.Write(p.ref_layer);
				break;
			case PCDFieldType._:
				w.Write(0);
				break; // idk what this actually is
			case PCDFieldType.Scalar_field:
				// idk what this actually is in provided files
				w.Write(p.Scalar_field);
				break;
			case PCDFieldType.lod_id:
				w.Write(p.lod_id);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Not one of the known types");
		}
	}

	Thread saveThread;
	public bool Save(string filename, PCDDataType pcdDataType = PCDDataType.binary, int chooseLayer = -2)
	{
		if (saveThread != null && saveThread.IsAlive)
		{
			// can't save - already saving
			Debug.LogWarning("Can't save, already saving");
			return false;
		}

		saveThread = new Thread(() => SaveThread(filename, pcdDataType, chooseLayer));
		saveThread.Start();
		return true;
	}

	private void SaveThread(string filename, PCDDataType dataType = PCDDataType.binary, int chooseLayer = -2)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		bool saveRefLayer = true;
		
		// loop through and find the points that are on the chosen layer
		List<PCDPoint> newData = new List<PCDPoint>();
		foreach (PCDPoint point in data)
		{
			if (chooseLayer >= 0 && point.layer != chooseLayer) continue;

			// make the exported file not just have a uniform layer
			if (saveRefLayer)
			{
				// save the old layer as the reference layer instead
				point.ref_layer = point.layer;
				point.layer = 0;
			}
			newData.Add(point);
		}


		if (saveRefLayer)
		{
			if (!fieldsList.Contains(PCDFieldType.ref_layer))
			{
				fieldsList.Add(PCDFieldType.ref_layer);
			}
		}


		StringBuilder file = new StringBuilder();

		file.AppendLine("# This PCD file was written by DataFoldvr.");
		file.AppendLine("# Contact Anton Franzluebbers for more info.");
		file.AppendLine("# .PCD v0.7 - Point Cloud Data file format");

		file.AppendLine(version);
		file.Append("FIELDS");
		fieldsList.ForEach(f =>
		{
			file.Append(' ');
			file.Append(f.ToString());
		});
		file.Append("\nSIZE");
		fieldsList.ForEach(f =>
		{
			file.Append(' ');
			file.Append(f.FieldSize().ToString());
		});
		file.Append("\nTYPE");
		fieldsList.ForEach(f =>
		{
			file.Append(' ');
			file.Append(f.FieldType());
		});
		file.Append("\nCOUNT");
		fieldsList.ForEach(f =>
		{
			file.Append(' ');
			file.Append(f.FieldCount());
		});
		file.Append('\n');
		file.Append("WIDTH ");
		file.AppendLine(newData.Count.ToString());
		file.AppendLine("HEIGHT 1");
		file.AppendLine(viewpoint);
		file.AppendLine("POINTS " + newData.Count);
		
		Debug.Log($"Time spent saving header: {sw.Elapsed.TotalSeconds:N3}");
		
		switch (dataType)
		{
			case PCDDataType.binary:
				{
					file.AppendLine("DATA binary");
					
					File.WriteAllText(filename, file.ToString());
					using BinaryWriter w = new BinaryWriter(File.Open(filename, FileMode.Append), Encoding.UTF8);
					foreach (PCDPoint point in newData)
					{	
						foreach (PCDFieldType column in fieldsList)
						{
								AppendFieldToLine(w, point, column);
						}
					}

					break;
				}
			case PCDDataType.ascii:
				{
					file.AppendLine("DATA ascii");

					foreach (PCDPoint point in newData)
					{
						foreach (PCDFieldType column in fieldsList)
						{
							file.Append(AppendFieldToLine(point, column));
							file.Append(' ');
						}

						file.Append('\n');
					}

					File.WriteAllText(filename, file.ToString());
					break;
				}
			default:
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Must be binary or ascii");
		}

		sw.Stop();
		Debug.Log($"Time spent saving {filename}: {sw.Elapsed.TotalSeconds:N3}");
	}

	#endregion

	#region Reading

	public void LoadThread(BinaryReader reader)
	{
		bool finished = false;
		while (!finished)
		{
			string header_line = reader.ReadLine();
			if (string.IsNullOrEmpty(header_line)) continue;

			string type = header_line.Substring(0, header_line.IndexOf(" "));
			switch (type)
			{
				case "#":
					// ignore comments
					break;
				case "VERSION":
					version = header_line;
					break;
				case "FIELDS":
					fields = header_line;

					string[] fieldsArr = header_line.Split(' ');
					fieldsList = new List<PCDFieldType>();
					foreach (string field in fieldsArr)
					{
						// try to convert the word into a data type enum and add it to the list
						if (Enum.TryParse(field, out PCDFieldType result))
						{
							fieldsList.Add(result);
						}
					}

					break;
				case "SIZE":
					size = header_line;
					break;
				case "TYPE":
					type = header_line;
					break;
				case "COUNT":
					count = header_line;
					break;
				case "WIDTH":
					width = header_line;
					break;
				case "HEIGHT":
					height = header_line;
					break;
				case "VIEWPOINT":
					viewpoint = header_line;
					break;
				case "POINTS":
					points = header_line;
					numPoints = uint.Parse(header_line.Split(' ')[1]);
					break;
				case "DATA":
					if (!Enum.TryParse(header_line.Split(' ')[1].ToLower(), out dataType))
					{
						Debug.LogError("Can't parse data type. Needs to be ascii or binary");
						return;
					}

					// read the rest of the data
					data = new List<PCDPoint>();
					switch (dataType)
					{
						case PCDDataType.binary:
							{
								for (int i = 0; i < numPoints; i++)
								{
									loadProgress = (float)i / numPoints;
									data.Add(ReadPCDDataRow(fieldsList, reader));
								}

								break;
							}
						case PCDDataType.ascii:
							{
								for (int i = 0; i < numPoints; i++)
								{
									loadProgress = (float)i / numPoints;

									string line = reader.ReadLine();
									data.Add(ReadPCDDataRow(fieldsList, line));
								}

								break;
							}
						default:
							throw new ArgumentOutOfRangeException(nameof(dataType), "Must be binary or ascii");
					}

					finished = true; // stop looping through the file lines
					break; // break out of the switch case
			}
		}


		loadProgress = 1;

		loadFinishedFlag = true; // notify Update that we are finished reading the file
	}

	/// <summary>
	/// For reading binary data.
	/// </summary>
	private static PCDPoint ReadPCDDataRow(IReadOnlyList<PCDFieldType> fieldsList, BinaryReader reader)
	{
		PCDPoint newPoint = new PCDPoint();

		foreach (PCDFieldType fieldType in fieldsList)
		{
			switch (fieldType)
			{
				case PCDFieldType.Time:
					// TODO verify
					newPoint.time = reader.ReadSingle();
					break;
				case PCDFieldType.x:
					newPoint.position.x = reader.ReadSingle();
					break;
				case PCDFieldType.y:
					newPoint.position.y = reader.ReadSingle();
					break;
				case PCDFieldType.z:
					newPoint.position.z = reader.ReadSingle();
					break;
				case PCDFieldType.rgb:
					float b = reader.ReadByte() / 255f;
					float g = reader.ReadByte() / 255f;
					float r = reader.ReadByte() / 255f;
					float a = reader.ReadByte() / 255f;
					newPoint.color = new Color(
						r, g, b, a
					);
					break;
				case PCDFieldType.normal_x:
					newPoint.normal.x = reader.ReadSingle();
					break;
				case PCDFieldType.normal_y:
					newPoint.normal.y = reader.ReadSingle();
					break;
				case PCDFieldType.normal_z:
					newPoint.normal.z = reader.ReadSingle();
					break;
				case PCDFieldType.layer:
					newPoint.layer = reader.ReadByte();
					break;
				case PCDFieldType.ref_layer:
					newPoint.ref_layer = reader.ReadByte();
					break;
				case PCDFieldType._:
					// read empty bytes for some reason
					reader.ReadBytes(4);
					break;
				case PCDFieldType.Scalar_field:
					// TODO verify
					newPoint.Scalar_field = reader.ReadSingle();
					break;
				case PCDFieldType.lod_id:
					newPoint.lod_id = reader.ReadUInt16();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(fieldType), "Unknown field type");
			}
		}

		return newPoint;
	}

	/// <summary>
	/// For reading ASCII pcd files
	/// </summary>
	private PCDPoint ReadPCDDataRow(IReadOnlyList<PCDFieldType> fieldsList, string row)
	{
		PCDPoint newPoint = new PCDPoint();

		string[] elems = row.Split(' ');
		if (elems.Length > fieldsList.Count + 1)
		{
			Debug.LogError("Field list length and amount of data doesn't match.");
		}

		for (int elemIndex = 0; elemIndex < fieldsList.Count; elemIndex++)
		{
			PCDFieldType fieldType = fieldsList[elemIndex];
			string element = elems[elemIndex];

			switch (fieldType)
			{
				case PCDFieldType.Time:
					// TODO verify
					newPoint.time = float.Parse(element);
					break;
				case PCDFieldType.x:
					newPoint.position.x = float.Parse(element);
					break;
				case PCDFieldType.y:
					newPoint.position.y = float.Parse(element);
					break;
				case PCDFieldType.z:
					newPoint.position.z = float.Parse(element);
					break;
				case PCDFieldType.rgb:
					float value = float.Parse(element);
					byte[] bytes = BitConverter.GetBytes(value);
					newPoint.color = new Color(
						bytes[2] / 255f,
						bytes[1] / 255f,
						bytes[0] / 255f,
						bytes[3] / 255f
					);
					break;
				case PCDFieldType.normal_x:
					newPoint.normal.x = float.Parse(element);
					break;
				case PCDFieldType.normal_y:
					newPoint.normal.y = float.Parse(element);
					break;
				case PCDFieldType.normal_z:
					newPoint.normal.z = float.Parse(element);
					break;
				case PCDFieldType.layer:
					newPoint.layer = byte.Parse(element);
					break;
				case PCDFieldType.ref_layer:
					newPoint.ref_layer = byte.Parse(element);
					break;
				case PCDFieldType.lod_id:
					newPoint.lod_id = ushort.Parse(element);
					break;
				case PCDFieldType._:
					// TODO
					break;
				case PCDFieldType.Scalar_field:
					newPoint.Scalar_field = float.Parse(element);
					// TODO
					break;
			}
		}

		return newPoint;
	}

	#endregion

	
}


public static class PCDFieldTypeExtensions
{
	public static int FieldSize(this PCDFieldType fieldType)
	{
		switch (fieldType)
		{
			case PCDFieldType.Time:
			case PCDFieldType.x:
			case PCDFieldType.y:
			case PCDFieldType.z:
			case PCDFieldType.rgb:
			case PCDFieldType.normal_x:
			case PCDFieldType.normal_y:
			case PCDFieldType.normal_z:
			case PCDFieldType.Scalar_field:
				return 4;
			case PCDFieldType.lod_id:
				return 2;
			case PCDFieldType.layer:
			case PCDFieldType.ref_layer:
			case PCDFieldType._:
				return 1;
			default:
				throw new Exception("NO PLS NO");
		}
	}

	public static string FieldType(this PCDFieldType fieldType)
	{
		switch (fieldType)
		{
			case PCDFieldType.Time:
			case PCDFieldType.x:
			case PCDFieldType.y:
			case PCDFieldType.z:
			case PCDFieldType.rgb:
			case PCDFieldType.normal_x:
			case PCDFieldType.normal_y:
			case PCDFieldType.normal_z:
			case PCDFieldType.Scalar_field:
				return "F";
			case PCDFieldType.layer:
			case PCDFieldType.ref_layer:
			case PCDFieldType.lod_id:
			case PCDFieldType._:
				return "U";
			default:
				throw new Exception("NO PLS NO");
		}
	}

	public static int FieldCount(this PCDFieldType fieldType)
	{
		switch (fieldType)
		{
			case PCDFieldType._:
				return 4;
			case PCDFieldType.Time:
			case PCDFieldType.x:
			case PCDFieldType.y:
			case PCDFieldType.z:
			case PCDFieldType.rgb:
			case PCDFieldType.normal_x:
			case PCDFieldType.normal_y:
			case PCDFieldType.normal_z:
			case PCDFieldType.Scalar_field:
			case PCDFieldType.layer:
			case PCDFieldType.ref_layer:
			case PCDFieldType.lod_id:
				return 1;
			default:
				throw new Exception("NO PLS NO");
		}
	}

}