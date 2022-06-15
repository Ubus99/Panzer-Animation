using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    enum gamestates
    {
        Menu,
        Running
    };

    public GameObject UI;
    private UI_Handler UIhandler;
    private gamestates gamestate = gamestates.Running;

    private bool debugScreenActive;
    
    // Start is called before the first frame update
    void Start()
    {
        UIhandler = UI.GetComponent<UI_Handler>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        switch (gamestate)
        {
            case gamestates.Menu:
                MainMenu();
                break;
            case gamestates.Running:
                RunningBehavior();
                break;
        }

    }

    void MainMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) //escape from main menu
        {
            gamestate = gamestates.Running;
            if (debugScreenActive)
            {
                UIhandler.SetScreen("debug");
            }
            else
            {
                UIhandler.SetScreen("");
            }
        }
        Cursor.lockState = CursorLockMode.None;
    }

    void RunningBehavior()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) //open Main menu
        {
            gamestate = gamestates.Menu;
            UIhandler.SetScreen("MainMenu");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            if (debugScreenActive) //close debug
            {
                UIhandler.SetScreen("");
                debugScreenActive = false;
            }
            else //open debug
            {
                UIhandler.SetScreen("debug");
                debugScreenActive = true;
            }
        }
        Cursor.lockState = CursorLockMode.Locked;
    }
}
