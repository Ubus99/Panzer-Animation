using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
	public nDMapping VR_body;
	public nDMapping VR_turret;

	public float GetAxis(string code)
	{
		float outp;
		if (StaticData.isVR)
		{
			switch (code)
			{
				case "Vertical":
					outp = VR_body.values["Z"] - 0.5f;
					break;
				case "Horizontal":
					outp = VR_body.values["X"] - 0.5f;
					break;
				case "Rotate":
					outp = VR_body.values["Y"] - 0.5f;
					break;
				case "Mouse X":
					outp = VR_body.values["Z"] - 0.5f;
					break;
				case "Mouse Y":
					outp = VR_body.values["X"] - 0.5f;
					break;
				default:
					Debug.Log("no such Axis in VR: " + code);
					outp = 0.0f;
					break;
			}
		}
		else
		{
			switch (code)
			{
				case "Mouse X":
					outp = -Mathf.Clamp(Input.GetAxis(code), -1.0f, 1.0f);
					break;
				case "Mouse Y":
					outp = Mathf.Clamp(Input.GetAxis(code), -1.0f, 1.0f);
					break;
				default:
					outp = Input.GetAxis(code);
					break;
			}
		}
		Debug.Log("requestedInp " + code + ": " + outp);
		return outp;
	}
}
