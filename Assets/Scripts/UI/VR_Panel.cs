using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VR_Panel : MonoBehaviour
{

	private Canvas canvas = null;
	private MenuManager menuManager = null;
	// Start is called before the first frame update
	void Awake()
	{
		canvas = GetComponent<Canvas>();
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	public void Setup(MenuManager menuManager)
	{
		this.menuManager = menuManager;
		Hide();
	}

	public void Show()
	{
		canvas.enabled = true;
	}

	public void Hide()
	{
		canvas.enabled = true;
	}
}
