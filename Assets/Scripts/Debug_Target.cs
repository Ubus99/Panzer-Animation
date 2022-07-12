using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_Target : MonoBehaviour
{
	private GameLogic gameLogic;

    public void Setup(GameLogic gameLogic)
	{
        this.gameLogic = gameLogic;
	}

    void hitByAA(RaycastHit Hit)
	{
        gameLogic.DestroyEnemy(this.gameObject);
	}

	private void OnDestroy()
	{
		
	}
}
