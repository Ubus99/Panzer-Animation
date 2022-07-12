using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : MonoBehaviour
{
	public GameObject MFObject;
	public AudioClip GunshotAudio;

	private float SoundTimer;
	private ParticleSystem MFEmmitter;

	// Start is called before the first frame update
	void Start()
	{
		MFEmmitter = MFObject.GetComponent<ParticleSystem>();
	}

	public void startFire()
	{
		MFObject.SetActive(true);
		if (SoundTimer <= 0)
		{
			AudioSource.PlayClipAtPoint(GunshotAudio, MFObject.transform.position);
			SoundTimer = MFEmmitter.main.duration / MFEmmitter.emission.rateOverTime.constant;
		}
		else
		{
			SoundTimer -= Time.deltaTime;
		}
	}

	public void stopFire()
	{
		MFObject.SetActive(false);
	}
}
