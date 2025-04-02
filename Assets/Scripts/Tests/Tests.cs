using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;
using System.Linq;

public class WaffleTests : MonoBehaviour
{
    public static WaffleTests Instance { get; private set; }
    void Awake() { 
        if (Instance != null && Instance != this) 
            Destroy(this.gameObject); 
        else 
            Instance = this; 
    }
    
    [SerializeField] Subtitles subtitles;
    
    public void AddTextFinal(string text) {
        subtitles.AddText(text, true);
    }
    
    public void AddText(string text) {
        subtitles.AddText(text, false);
    }

}