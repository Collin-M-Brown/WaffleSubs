using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutoCompileBeforePlay
{
    static AutoCompileBeforePlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.Log("Waiting for compilation to complete before entering Play mode...");
                EditorApplication.isPlaying = false;
                
                // Create a delay to wait for compilation
                EditorApplication.delayCall += () =>
                {
                    // Check again after delay to ensure compilation is complete
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                    {
                        Debug.Log("Compilation complete, entering Play mode...");
                        EditorApplication.isPlaying = true;
                    }
                };
            }
            else
            {
                // Force a recompile by touching a script asset
                AssetDatabase.Refresh();
                
                if (EditorApplication.isCompiling)
                {
                    Debug.Log("Initiated recompile before entering Play mode...");
                    EditorApplication.isPlaying = false;
                    
                    EditorApplication.delayCall += () =>
                    {
                        if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                        {
                            Debug.Log("Recompile complete, entering Play mode...");
                            EditorApplication.isPlaying = true;
                        }
                    };
                }
            }
        }
    }
} 
