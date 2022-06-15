using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UI_Handler : MonoBehaviour
{
    public Rigidbody player;
    public VisualTreeAsset debugScreen;
    public VisualTreeAsset MainMenue;

    private UIDocument UIDoc;
    //debug refs
    private Label UISpeedRef;
    private Label UITorqueRef;
    //Main menu refs
    private Button UIContinue;
    private Button UIQuit;

    private enum UIstate
    {
        MainMenu,
        debugscreen,
        hidden
    }

    private UIstate state;

    // Start is called before the first frame update
    void Start()
    {
        UIDoc = GetComponent<UIDocument>();
        UIContinue.RegisterValueChangedCallback((evt) => { });
        UIQuit.RegisterValueChangedCallback((evt) => { Application.Quit(); });
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case UIstate.MainMenu:

                break;
            case UIstate.debugscreen:

                UISpeedRef.text = "Velocity (x,y,z): " + player.velocity.ToString("0.00");
                UITorqueRef.text = "Rotation (x,y,z): " + player.rotation.ToString("0.00");
                break;
        }
    }

    public void SetScreen(string screen)
    {
        switch (screen)
        {
            case "MainMenu":
                UIDoc.visualTreeAsset = MainMenue;

                UIQuit = UIDoc.rootVisualElement.Q<Button>("ButtonQuit");
                UIContinue = UIDoc.rootVisualElement.Q<Button>("ButtonContinue");

                state = UIstate.MainMenu;
                break;

            case "debug":
                UIDoc.visualTreeAsset = debugScreen;

                UISpeedRef = UIDoc.rootVisualElement.Q<Label>("speedIndicator");
                UITorqueRef = UIDoc.rootVisualElement.Q<Label>("torqueIndicator");

                state = UIstate.debugscreen;
                break;

            default:        //fallback, hide all menues
                UIDoc.visualTreeAsset = null;
                state = UIstate.hidden;
                break;
        }
    }
}
