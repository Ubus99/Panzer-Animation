using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bulletImpact : MonoBehaviour
{
	public float VFXduration;
	public GameObject VFXHit;
	public AudioClip AFXHit;

	private Dictionary<float, GameObject> VFXList = new Dictionary<float, GameObject>();

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void FixedUpdate()
	{
		GarbageHandler();
	}
	public void hitByAA(RaycastHit Hit)
	{
		Quaternion HitNormal = Quaternion.LookRotation(Hit.normal);
		VFXList.Add(Time.time, Instantiate(VFXHit, Hit.point, HitNormal));//todo effect allignment
		if (AFXHit != null)
		{
			AudioSource.PlayClipAtPoint(AFXHit, Hit.point);
		}
	}

	private void GarbageHandler()
	{
		List<float> timesToRemove = new List<float>();

		foreach (float t in VFXList.Keys) //find dead objects
		{
			if (Time.time - t > VFXduration)
			{
				timesToRemove.Add(t);
			}
		}

		foreach (float t in timesToRemove) //delete dead objects
		{
			Destroy(VFXList[t]);
			VFXList.Remove(t);
		}
	}

	private void OnDestroy()
	{
		foreach (GameObject o in VFXList.Values) //find dead objects
		{
			Destroy(o);
		}
	}
}
