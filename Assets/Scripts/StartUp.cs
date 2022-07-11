using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUp : MonoBehaviour
{
	// Start is called before the first frame update
	void Awake()
	{
		StaticData.CheckHMD();
		SceneManager.LoadScene("GameScene");
	}
}
