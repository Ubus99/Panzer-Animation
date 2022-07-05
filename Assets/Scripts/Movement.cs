using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
	public float verticalAcceleration, horizontalAcceleration, rotationalAcceleration = 1;
	public float maxSpeed, maxRotation = 1;
	public float hoverDistance = 1;
	public float springStrength = 1;
	public float dampening = 1;
	public List<Engine> engines;

	private Rigidbody body;
	private Rigidbody turret;
	private Rigidbody barrel_1;
	private Rigidbody barrel_2;

	// Start is called before the first frame update
	void Start()
	{
		body = GetComponent<Rigidbody>();
		turret = GetComponent<Rigidbody>(); //todo
		barrel_1 = GetComponent<Rigidbody>(); //todo
		barrel_2 = GetComponent<Rigidbody>(); //todo
	}

	// Update is called once per physics calc
	void FixedUpdate()
	{
		//Debug.Log("Current inputs: " + Input.GetAxis("Vertical") + " | " + Input.GetAxis("Horizontal") + "|" + Input.GetAxis("Rotate"));
		foreach (Engine e in engines)
		{
			e.Hover(body, springStrength, hoverDistance, dampening);
		}

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
		else
		{
			body.AddRelativeTorque(Vector3.up * Input.GetAxis("Rotate") * rotationalAcceleration, ForceMode.Force);
		}
	}
}
