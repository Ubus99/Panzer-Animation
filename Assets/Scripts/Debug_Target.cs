using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_Target : MonoBehaviour
{
	public int leben;
	public float killDelay;

	private GameLogic gameLogic;

	public void Setup(GameLogic gameLogic)
	{
		this.gameLogic = gameLogic;
	}

	void FixedUpdate()
	{
		if (leben <= 0)
		{
			gameLogic.DestroyEnemy(this.gameObject, killDelay);
		}
	}

	public void hitByAA(RaycastHit Hit)
	{
		leben--;
	}

	private void OnDestroy()
	{

	}
}
