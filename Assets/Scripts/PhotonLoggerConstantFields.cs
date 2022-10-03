using System;
using UnityEngine;
using unityutilities;

public class PhotonLoggerConstantFields : LoggerConstantFields
{
	public override string[] GetConstantFields()
	{
		try
		{
			return new string[] {
			StudyMan.participantId.ToString()
		};
		}
		catch (Exception)
		{
			// default values
			return new string[] { "-1" };
		}
	}
}
