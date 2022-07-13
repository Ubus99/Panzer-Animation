using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
	private float lastDistance;

	public void Hover(Rigidbody body, float springStrength, float hoverDistance, float dampening)
	{

		if (Physics.Raycast(transform.position, -transform.forward, out RaycastHit hit, hoverDistance))//todo make ray align to engine angle
		{
			Vector3 engineVector = HooksLawDampen(hoverDistance, hit.distance, dampening) * springStrength * transform.forward;// / Mathf.Pow(hit.distance, EngineFalloff);

			Debug.DrawRay(transform.position, transform.localPosition - (transform.forward * hoverDistance), Color.red);
			body.AddForceAtPosition(engineVector, transform.position, ForceMode.Force);
		}
		else
		{
			Debug.DrawRay(transform.position, transform.localPosition - (transform.forward * hoverDistance), Color.green);
		}
	}

	private float HooksLawDampen(float hoverDistance, float distance, float dampening)
	{
		float forceAmount = Mathf.Max(0f, hoverDistance - distance + (dampening * (lastDistance - distance)));
		lastDistance = distance;

		return forceAmount;
	}
}
