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

	private void SetupPanels()
	{
		VR_Panel[] panels = GetComponentsInChildren<VR_Panel>();

		foreach (VR_Panel panel in panels)
		{
			panel.Setup(this);
		}
		currentPanel.Show();
	}
}
