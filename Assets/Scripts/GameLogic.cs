using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
	public bool autoVR;
	public List<GameObject> Enemies;
	public List<Transform> SpawnPoints;
	public int maxEnemies;
	public TMPro.TextMeshProUGUI Scoreboard;
	public float score;

	private Dictionary<Vector3, GameObject> InstancedEnemies = new Dictionary<Vector3, GameObject>();
	private float timeToSpawn;

	// Start is called before the first frame update
	void Start()
	{
		if (autoVR)
		{
			if (StaticData.evaluated == false)
			{
				StaticData.CheckHMD();
			}
		}
		if (!StaticData.isVR)
		{
			Cursor.lockState = CursorLockMode.Locked;
		}
		SpawnEnemy();
	}

	// Update is called once per frame
	void FixedUpdate()
	{
		Scoreboard.text = score.ToString();
		combatHandler();
	}

	private void combatHandler()
	{
		if (InstancedEnemies.Count < maxEnemies && SpawnPoints.Count > 0)
		{
			if (timeToSpawn <= 0)
			{
				timeToSpawn = Random.Range(10.0f, 15.0f);
				SpawnEnemy();
			}
			else
			{
				timeToSpawn -= Time.deltaTime;
			}

		}
	}

	public void DestroyEnemy(GameObject gameObject, float killDelay)
	{
		InstancedEnemies.Remove(gameObject.transform.position);
		Destroy(gameObject, killDelay);
		score++;
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

	private void SpawnEnemy()
	{
		Vector3 position;
		do {
			position = SpawnPoints[Random.Range(0, SpawnPoints.Count - 1)].position;
		} while (InstancedEnemies.ContainsKey(position));
		InstancedEnemies.Add(position, Instantiate(Enemies[Random.Range(0, Enemies.Count)],
			position, Quaternion.identity));
			InstancedEnemies[position].GetComponent<Debug_Target>().Setup(this);
	}
}
