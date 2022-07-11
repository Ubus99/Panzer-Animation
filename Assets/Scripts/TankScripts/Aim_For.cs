using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aim_For : MonoBehaviour
{
	public enum Axis
	{
		X_Axis,
		Y_Axis,
		Z_Axis
	};
	public Axis rotationAxis;

	public Transform origin;
	public float distance;

	public Transform target;
	public float followSpeed;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per tick
	void FixedUpdate()
	{
		if (origin == null)
		{
			rotate();
		}
		else
		{
			rotatePoint();
		}
	}

	/// <summary>
	/// rotates around own pivot
	/// </summary>
	private void rotate()
	{
		Vector3 tempRotationAxis = new Vector3();
		switch (rotationAxis)
		{
			case Axis.X_Axis:
				tempRotationAxis = transform.right;
				break;
			case Axis.Y_Axis:
				tempRotationAxis = transform.up;
				break;
			case Axis.Z_Axis:
				tempRotationAxis = transform.forward;
				break;
		}

		Vector3 toTarget = target.position - transform.position;
		Vector3 toTargetProjected = Vector3.ProjectOnPlane(toTarget, tempRotationAxis).normalized;
		Debug.DrawRay(transform.position, toTargetProjected * 10, Color.red);

		float angle = transform.localRotation.eulerAngles.y + Vector3.SignedAngle(transform.forward, toTargetProjected, tempRotationAxis); //todo?
		float delta = Mathf.Abs(Vector3.Dot(transform.right, toTarget));

		Quaternion q = Quaternion.Euler(0, angle, 0);
		transform.localRotation = Quaternion.RotateTowards(transform.localRotation, q, followSpeed);
	}

	/// <summary>
	/// rotates around origin at a distance todo dynamic angles dont work jet, fuck this
	/// </summary>
	private void rotatePoint()
	{
		Vector3 tempRotationAxis = new Vector3();
		switch (rotationAxis)
		{
			case Axis.X_Axis:
				tempRotationAxis = origin.right;
				break;
			case Axis.Y_Axis:
				tempRotationAxis = origin.up;
				break;
			case Axis.Z_Axis:
				tempRotationAxis = origin.forward;
				break;
		}

		Vector3 toTarget = target.position - origin.position;
		Vector3 toTargetProjected = Vector3.ProjectOnPlane(toTarget, tempRotationAxis).normalized;
		Debug.DrawRay(origin.position, toTargetProjected * 10, Color.red);

		transform.position = origin.position + toTargetProjected * distance;

		float angle = transform.localRotation.eulerAngles.x + Vector3.SignedAngle(transform.forward, toTargetProjected, tempRotationAxis); //todo?
		float delta = Mathf.Abs(Vector3.Dot(transform.right, toTarget));

		Quaternion q = Quaternion.Euler(angle, 0, 0);
		transform.localRotation = Quaternion.RotateTowards(transform.localRotation, q, followSpeed);
	}
}
