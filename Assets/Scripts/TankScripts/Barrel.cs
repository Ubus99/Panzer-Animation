using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barrel : MonoBehaviour
{
    private ParticleSystem MFEmitter;
    // Start is called before the first frame update
    void Start()
    {
		MFEmitter = GetComponentInChildren<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Fire()
	{
		MFEmitter.Play();
	}
}
