using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;
using UnityEngine.Events;
#pragma warning disable CS0162 // Unreachable code detected

/*
TODO: 
color
delay timings
Height
*/

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }
    
    public void Update() {
        bool ctrl = Input.GetKey(KeyCode.LeftControl);
        bool IKey = Input.GetKey(KeyCode.I);
        if (ctrl && IKey) {
            OnShowUI();
        }
    }
    
    public void OnHideUI() {
        UIMenu.Instance.Deactivate();
    }
    
    public void OnShowUI() {
        UIMenu.Instance.Activate();
    }    
    
}