﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.IO;

public class MenuManager : MonoBehaviour
{

    private ShowPanels panels;

    public static MenuManager ins;

    [HideInInspector] public string currentPanel = "MenuPanel";
    private string prevPanel;
    public AudioClip buttonPress;
    public AudioClip buttonPressCancel;
    public GameObject pausePanel;
    [HideInInspector] public string pausePanelName = "PausePanel";


    private AudioSource audio;

    public static bool gameRunning = false;



    public GameObject returnCurrentPanel()
    {
        return panels.returnPanel(currentPanel);
    }

	public void quitGame()
	{
        Invoke("exit", 1);
        audio.PlayOneShot(buttonPress);
        panels.hidePanel(currentPanel, true);
    }

    public void exit()
    {
        Application.Quit();
    }

    private void OnLevelWasLoaded(int level)
    {
        audio = GetComponent<AudioSource>();
        panels = gameObject.GetComponent<ShowPanels>();

        MenuManager.ins = this;

        switch (level)
        {
            case 0:
                panels.hidePanel("GamePanel");
                panels.showPanel("BG");
                break;

            case 1:
                panels.showPanel("GamePanel");
                panels.hidePanel("BG");
                break;
        }

    }

    public void changeMenu(string panel,AudioClip pressSound = null)
    {
        audio.Stop();
        if(pressSound == null)
        {
            pressSound = buttonPress;
        }

        audio.PlayOneShot(pressSound);
        prevPanel = currentPanel;
        
        panels.hidePanel(currentPanel,true);
        panels.showPanel(panel, true);
        currentPanel = panel;
    }

    public void goBack()
    {
        GameObject panel = returnCurrentPanel();

        PanelInfo pInfo = panel.GetComponent<PanelInfo>();

        string targetPanel = prevPanel;//just returns last menu game was on

        if(pInfo != null && pInfo.parentPanel != null) // check PanelInfo property, this allows hierachical menus.
        {
            targetPanel = pInfo.parentPanel.name;
        }

        if(targetPanel != null)
        {
            changeMenu(targetPanel,buttonPressCancel);
        }
        
    }
    
    public void Start()
    {
        audio = GetComponent<AudioSource>();
        MenuManager.ins = returnInstance();
        panels = gameObject.GetComponent<ShowPanels>();

        #if UNITY_ANDROID
        googlePlay = GameObject.Find("UI").GetComponent<gplayInteractor>();
#endif
        setVolume();

        currentPanel = "MenuPanel";
        if(pausePanel != null)
        {
            pausePanelName = pausePanel.name;
        }
    }

    

    public void setVolume()
    {
        if (PlayerPrefs.GetString("Muted", "no") != "yes")
        {
            AudioListener.volume = 1;
        }
        else
        {
            AudioListener.volume = 0;
        }
    }

    public void processPrefs()
    {
        playSound(buttonPress);
        setVolume();
    }

    public void playSound(AudioClip clip)
    {
        audio.Stop();
        audio.PlayOneShot(clip);
    }
    

    public void toggleUIelement(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    public static void startGame(string x)
    {
        ins.startGame();
    }

    public void startGame()
    {
        audio.PlayOneShot(buttonPress);
        panels.hidePanel(currentPanel, true);
        Invoke("doStart", 1);
    }

    public void doStart()
    {
        SceneManager.LoadScene(1);
        MenuManager.gameRunning = true;
    }

    public void StopGameplay()
    {
        //UnPause();
        gameRunning = false;
        SceneManager.LoadScene(0);
        panels.showPanel("MenuPanel", true);
    }

    public void openWeb(string address)
    {
        audio.Stop();
        audio.PlayOneShot(buttonPress);
        Application.OpenURL(address);
    }

    public void Restart()
    {
        UnPause();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    public static MenuManager returnInstance()
    {
        MenuManager instance = GameObject.FindGameObjectWithTag("Menu").GetComponent<MenuManager>(); // i know this is an extra line but unity freaked out when removed.
        ins = instance;
        return instance;
    }


    void Update()
    {

        //Check if the Cancel button in Input Manager is down this frame (default is Escape key) and that game is not paused, and that we're not in main menu
        if (Input.GetButtonDown("Pause") && !GameManager.isPaused && MenuManager.gameRunning) // fix this
        {
            //Call the DoPause function to pause the game
            DoPause(true);
        }
        //If the button is pressed and the game is paused and not in main menu
        else if (Input.GetButtonDown("Pause") && GameManager.isPaused)
        {
            //Call the UnPause function to unpause the game
            UnPause();
        }

    }


    public void DoPause(bool showPauseMenu = false)
    {
        GameManager.isPaused = true;
        //Set time.timescale to 0, this will cause animations and physics to stop updating
        Time.timeScale = 0;
        if (showPauseMenu)
        {
            panels.showPanel(pausePanelName);
        }
    }

    public void UnPause()
    {
        GameManager.isPaused = false;
        //Set time.timescale to 1, this will cause animations and physics to continue updating at regular speed
        Time.timeScale = 1;
        panels.hidePanel(pausePanelName);
    }

}