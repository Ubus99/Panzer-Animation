using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_Target : MonoBehaviour
{
	public int leben;
	public float killDelay;
	public GameObject VFXKill;
	public AudioClip AFXKill;

	private GameLogic gameLogic;
	private GameObject VFXKillInt;
	private float killTimer;
	private bool kill = false;

	public void Setup(GameLogic gameLogic)
	{
		this.gameLogic = gameLogic;
	}

	void FixedUpdate()
	{
		if (leben <= 0 && !kill)
		{
			VFXKillInt = Instantiate(VFXKill);
			AudioSource.PlayClipAtPoint(AFXKill, transform.position);
			kill = true;
			killTimer = Time.time;
		}
		if (Time.time - killTimer > 10 && kill)
		{
			Destroy(VFXKillInt);
			gameLogic.DestroyEnemy(gameObject, 0);
		}
	}

	public void hitByAA(RaycastHit Hit)
	{
		leben--;
	}
}
