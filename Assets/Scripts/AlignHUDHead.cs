using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignHUDHead : MonoBehaviour
{
	public Transform VR_Cam;
	public Transform Normal_Cam;

	// Start is called before the first frame update
	void Start()
	{
		transform.parent = StaticData.isVR switch
		{
			true => VR_Cam,
			_ => Normal_Cam,
		};
		transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
	}
}
