using System.Collections;
using System.IO;
using UnityEngine;
#pragma warning disable CS0162 // Unreachable code detected lmao

public class ExitGame : MonoBehaviour {
    public static ExitGame Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }
    
    private bool exiting = false;
    public void Exit() {
        if (exiting) return;
        
        exiting = true;
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    
}