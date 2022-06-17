using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
	private Rigidbody body;
	private Rigidbody turret;
	private Rigidbody barrel_1;
	private Rigidbody barrel_2;
	public float verticalAcceleration, horizontalAcceleration, rotationalAcceleration = 1;
	public float maxSpeed, maxRotation = 1;

	// Start is called before the first frame update
	void Start()
	{
		body = GetComponent<Rigidbody>();
		turret = GetComponent<Rigidbody>(); //todo
		barrel_1 = GetComponent<Rigidbody>(); //todo
		barrel_2 = GetComponent<Rigidbody>(); //todo
}

	// Update is called once per frame
	void Update()
	{
		//Debug.Log("Current inputs: " + Input.GetAxis("Vertical") + " | " + Input.GetAxis("Horizontal") + "|" + Input.GetAxis("Rotate"));
		if (body.velocity.magnitude >= maxSpeed)
		{
			body.velocity = body.velocity.normalized * maxSpeed;
		}
		else
		{
			body.AddRelativeForce(Vector3.forward * Input.GetAxis("Vertical") * verticalAcceleration, ForceMode.Force);
			body.AddRelativeForce(Vector3.right * Input.GetAxis("Horizontal") * horizontalAcceleration, ForceMode.Force);
		}

		if (body.angularVelocity.magnitude >= maxRotation)
		{
			body.angularVelocity = body.angularVelocity.normalized * maxRotation;
		}
        else {
			body.AddRelativeTorque(Vector3.up * Input.GetAxis("Rotate") * rotationalAcceleration, ForceMode.Force);
		}


	}
}
