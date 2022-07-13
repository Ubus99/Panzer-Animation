using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
	public VR_Panel currentPanel = null;

	private List<VR_Panel> panelHistory = new List<VR_Panel>();

	private void Start()
	{
		SetupPanels();
	}

	private void Update()
	{
		if (Input.GetButtonDown("Cancel"))
		{
			Debug.Log("menu button pressed");
			if (currentPanel.isHidden()) //first open
			{
				currentPanel.Show();
				Cursor.lockState = StaticData.menuMode;
				StaticData.inMenu = true;
			}
			else //first open
			{
				ToPrevious();
			}
		}
	}

	private void SetupPanels()
	{
		VR_Panel[] panels = GetComponentsInChildren<VR_Panel>();

		foreach (VR_Panel panel in panels)
		{
			panel.Setup(this);
		}
		currentPanel.Hide();
	}

	public void ToPrevious()
	{
		if (panelHistory.Count == 0)
		{
			currentPanel.Hide();
			Cursor.lockState = CursorLockMode.Locked;
			StaticData.inMenu = false;
		}
		else
		{
			int lastIndex = panelHistory.Count - 1;
			SetCurrent(panelHistory[lastIndex]);
			panelHistory.RemoveAt(lastIndex);
		}
	}

	public void SetCurrentWithHistory(VR_Panel newPanel)
	{
		panelHistory.Add(currentPanel);
		SetCurrent(newPanel);
	}

	public void SetCurrent(VR_Panel newPanel)
	{
		currentPanel.Hide();
		currentPanel = newPanel;
		currentPanel.Show();
	}
}
