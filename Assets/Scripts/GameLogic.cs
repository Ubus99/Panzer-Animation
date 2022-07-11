using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
	public bool autoVR;
	public List<GameObject> Enemies;
	public List<Vector3> SpawnPoints;
	public GameObject VRPlayer;
	public GameObject nonVRCam;
	public MenuManager menu;

	private List<GameObject> InstancedEnemies = new List<GameObject>();

	// Start is called before the first frame update
	void Start()
	{
		if (autoVR)
		{
			if (StaticData.evaluated == false)
			{
				StaticData.CheckHMD();
			}
			VRPlayer.SetActive(StaticData.isVR);
			nonVRCam.SetActive(!StaticData.isVR);
		}
		if (!StaticData.isVR)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}
	}

	// Update is called once per frame
	void Update()
	{
		combatHandler();
	}

	void combatHandler()
	{
		if (InstancedEnemies.Count == 0 && Random.Range(0.0f, 1.0f) > 0.5f && SpawnPoints.Count > 0)
		{
			InstancedEnemies.Add(Enemies[Random.Range(0, Enemies.Count)]);
			Instantiate(InstancedEnemies[^1], SpawnPoints[Random.Range(0, SpawnPoints.Count - 1)], Quaternion.identity);
		}
	}

	public void CloseGame()
	{
		//savegame

		#if UNITY_EDITOR
		// Application.Quit() does not work in the editor so
		// UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
		UnityEditor.EditorApplication.isPlaying = false;
		#else
			Application.Quit();
		#endif
	}
}
