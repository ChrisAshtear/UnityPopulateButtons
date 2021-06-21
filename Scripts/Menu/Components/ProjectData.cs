﻿using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
[System.Serializable]


/// <summary>
/// This is an asset which contains all the data for a theme.
/// As an asset it live in the project folder, and get built into an asset bundle.
/// </summary>
[CreateAssetMenu(fileName = "projData", menuName = "UI/ProjectData")]
public class ProjectData : ScriptableObject
{
    [Header("Project Data")]
    public string gameName = "CarDerby";
    public string versionNum;
    //public MenuManager menu;
    [TextArea(6, 20)]
    public string credits = "";
    public GameObject defaultButton;
    [Header("Sound")]
    public List<AudioClip> audioList;
    private Dictionary<string, AudioClip> audio;
    public List<AudioMixer> audioMixers;
    private Dictionary<string, AudioClip> mixers;
    public AudioSource player;
    public AudioClip menuMusic;
    //each map should have its own music property.


    public void PlaySound(string sound)
    {
        audio.TryGetValue(sound, out AudioClip clip);
        if(clip!=null)
        {
            player.PlayOneShot(clip);
        }
    }
}