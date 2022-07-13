using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
	[HeaderAttribute("Components")]
	public GameObject turret;
	public List<Barrel> barrels = new List<Barrel>();
	public GameObject Aim_Point;
	public List<Engine> engines;

	[HeaderAttribute("Movement")]
	public float verticalAcceleration = 1;
	public float horizontalAcceleration = 1;
	public float rotationalAcceleration = 1;
	public float maxSpeed, maxRotation = 1;

	[HeaderAttribute("Turret")]
	public float verticalSensitivity = 0.25f;
	public float horizontalSensitivity = 0.25f;

	[HeaderAttribute("Hover")]
	public float hoverDistance = 1;
	public float springStrength = 1;
	public float dampening = 1;

	private Rigidbody body;
	private InputHandler inp;
	private RaycastHit Hit = new RaycastHit();

	private float turretAcc = 0;
	private float aimAngle;

	// Start is called before the first frame update
	void Start()
	{
		body = GetComponent<Rigidbody>();
		inp = GetComponent<InputHandler>();
	}

	// Update is called once per physics calc
	void FixedUpdate()
	{
		BodyPhysics();
		if (!StaticData.inMenu)
		{
			moveBody();
			moveTurret();
			CastRays();
			combat();
		}
	}

	private void BodyPhysics()
	{
		foreach (Engine e in engines)
		{
			e.Hover(body, springStrength, hoverDistance, dampening);
		}
	}

	private void moveBody()
	{
		if (body.velocity.magnitude >= maxSpeed)
		{
			body.velocity = body.velocity.normalized * maxSpeed;
		}
		else
		{
			body.AddRelativeForce(Vector3.forward * inp.GetAxis("Vertical") * verticalAcceleration * body.mass, ForceMode.Force);
			body.AddRelativeForce(Vector3.right * inp.GetAxis("Horizontal") * horizontalAcceleration * body.mass, ForceMode.Force);
		}

		if (body.angularVelocity.magnitude >= maxRotation)
		{
			body.angularVelocity = body.angularVelocity.normalized * maxRotation;
		}
		else
		{
			body.AddRelativeTorque(Vector3.up * inp.GetAxis("Rotate") * rotationalAcceleration * body.mass, ForceMode.Force);
		}
	}

	private void moveTurret()
	{
		float magnitude = Mathf.Pow(inp.GetAxis("Mouse X"), 2.0f) + Mathf.Pow(inp.GetAxis("Mouse Y"), 2.0f);
		magnitude = Mathf.Sqrt(magnitude);

		if (magnitude > 0.01f)
		{
			turretAcc += inp.GetAxis("Mouse X") * horizontalSensitivity;
			turretAcc = Mathf.Clamp(turretAcc, -2, 2);

			aimAngle += inp.GetAxis("Mouse Y") * verticalSensitivity;
			aimAngle = Mathf.Clamp(aimAngle, 0.0f, 45.0f);
		}

		turret.transform.Rotate(turretAcc * Vector3.back, Space.Self);
	}

	private void CastRays()
	{
		Quaternion aimDirection = Quaternion.AngleAxis(aimAngle, turret.transform.up);
		//Quaternion radialSpread = Quaternion.AngleAxis(Random.value * 360, turret.transform.right);
		//Quaternion axialSpread = Quaternion.AngleAxis(Random.value * gunSpread, turret.transform.forward);

		Vector3 rayDirection = aimDirection * /*radialSpread * axialSpread * */-turret.transform.right;
		Debug.DrawRay(turret.transform.position, rayDirection * 10, Color.red);
		Aim_Point.transform.position = Physics.Raycast(turret.transform.position, rayDirection, out Hit) && !Hit.transform.CompareTag("Player")
			? Hit.point
			: turret.transform.position + (rayDirection * 50);
	}

	private void combat()
	{
		if (Input.GetAxis("Fire1") > 0) //todo vr_ready
		{
			foreach (Barrel b in barrels)
			{
				b.startFire();
			}
			//effects
			if (Hit.transform != null)
			{
				Hit.transform.SendMessage("hitByAA", Hit);
				Debug.Log("hit " + Hit.transform.name);
			}
		}
		else
		{
			foreach (Barrel b in barrels)
			{
				b.stopFire();
			}
		}
	}
}
