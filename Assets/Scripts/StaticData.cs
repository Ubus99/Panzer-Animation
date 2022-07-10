using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public static class StaticData
{
	public static bool isVR = false;
	public static bool evaluated = false;
	public static void CheckHMD()
	{
		List<XRDisplaySubsystem> displaySubsystems = new List<XRDisplaySubsystem>();

		SubsystemManager.GetInstances<XRDisplaySubsystem>(displaySubsystems);
		foreach (var subsystem in displaySubsystems)
		{
			if (subsystem.running)
			{
				StaticData.isVR = true;
				StaticData.evaluated = true;
			}
		}
	}
}
