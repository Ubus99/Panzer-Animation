using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine : MonoBehaviour
{
	private float lastDistance;

	public void Hover(Rigidbody body, float springStrength, float hoverDistance, float dampening)
	{

		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, hoverDistance))//todo make ray align to engine angle
		{
			Vector3 engineVector = (Vector3.up * springStrength * HooksLawDampen(hoverDistance, hit.distance, dampening));// / Mathf.Pow(hit.distance, EngineFalloff);

			Debug.DrawRay(transform.position, transform.localPosition + (Vector3.down * hoverDistance), Color.red);
			body.AddForceAtPosition(engineVector, transform.position, ForceMode.Force);
		}
		else
		{
			Debug.DrawRay(transform.position, transform.localPosition + (Vector3.down * hoverDistance), Color.green);
		}
	}

	private float HooksLawDampen(float hoverDistance, float distance, float dampening)
	{
		float forceAmount = Mathf.Max(0f, hoverDistance - distance + (dampening * (lastDistance - distance)));
		lastDistance = distance;

		return forceAmount;
	}
}
