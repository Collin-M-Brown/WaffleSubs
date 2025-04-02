using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(WaffleTests))]
public class TestButtons : Editor {
    private float fogDuration = 20;
    private string testText = "this is a test sentence with a bunch of stuff in it.";
    
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        WaffleTests script = (WaffleTests)target;
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);
        AddInputButton("AddTextFinal", ref testText, script.AddTextFinal, false);
        AddInputButton("AddText", ref testText, script.AddText, false);
    }

    private void AddButton(string name, Action action) {
        if(GUILayout.Button(name)) {
            action();
        }
    }
    
    private void AddInputButton(string name, ref float value, Action<float> action) {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.FloatField(name, value);
        if(GUILayout.Button(name, GUILayout.Width(200))) {
            action(value);
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void AddInputButton(string name, ref string value, Action<string> action, bool sameLine = true) {
        if (sameLine) {
            EditorGUILayout.BeginHorizontal();
        }
        
        value = EditorGUILayout.TextField(name, value);
        if(GUILayout.Button(name, GUILayout.Width(200))) {
            action(value);
        }
        
        if (sameLine) {
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void AddInputButton(string name, ref int value, Action<int> action) {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.IntField(name, value);
        if(GUILayout.Button(name, GUILayout.Width(300))) {
            action(value);
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void AddTextButton(string buttonName, string label, ref string value, Action<string> action) {
        value = EditorGUILayout.TextField(label, value);
        if(GUILayout.Button(buttonName, GUILayout.Width(150))) {
            action(value);
        }
    }
    
    private void AddTextButton(string buttonName, string label1, ref string value1, string label2, ref string value2, Action<string, string> action) {
        value1 = EditorGUILayout.TextField(label1, value1);
        value2 = EditorGUILayout.TextField(label2, value2);
        if(GUILayout.Button(buttonName, GUILayout.Width(150))) {
            action(value1, value2);
        }
    }
    
    private void AddTextButton(string buttonName, string label1, ref int value1, string label2, ref float value2, Action<int, float> action) {
        value1 = EditorGUILayout.IntField(label1, value1);
        value2 = EditorGUILayout.FloatField(label2, value2);
        if(GUILayout.Button(buttonName, GUILayout.Width(150))) {
            action(value1, value2);
        }
    }
    
    private void AddTextButton(string buttonName, string label1, ref string value1, string label2, ref float value2, Action<string, float> action) {
        value1 = EditorGUILayout.TextField(label1, value1);
        value2 = EditorGUILayout.FloatField(label2, value2);
        if(GUILayout.Button(buttonName, GUILayout.Width(150))) {
            action(value1, value2);
        }
    }
}