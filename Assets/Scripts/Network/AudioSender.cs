using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

using NativeWebSocket;

[System.Serializable]
public class DeepgramResponse {
    public int[] channel_index;
    public bool is_final;
    public Channel channel;
    public string type;
}

[System.Serializable]
public class Channel {
    public Alternative[] alternatives;
}

[System.Serializable]
public class Alternative {
    public string transcript;
}

public class AudioSender : MonoBehaviour {
    public static AudioSender Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
        apiToken = PlayerPrefs.GetString("ApiKey", "");
    }
    
    WebSocket websocket;
    private bool isConnected = false;
    private string apiToken = "";
    [SerializeField] Subtitles subtitles;
    
    public async void StartConnection() {
        if (isConnected) {
            Debug.Log("WebSocket is already connected!");
            return;
        }
        
        if (apiToken.Length == 0) {
            // TODO: some warning or something
        }

        var headers = new Dictionary<string, string> {
            { "Authorization", "Token " + apiToken }
        };
        
        Debug.Log($"output sample rate in settings: {AudioSettings.outputSampleRate.ToString()}");
        var addons = "punctuate=true&interim_results=true&vad_events=true&filler_words=true";
        websocket = new WebSocket($"wss://api.deepgram.com/v1/listen?{addons}&encoding=linear16&sample_rate=" + AudioSettings.outputSampleRate.ToString(), headers);

        websocket.OnOpen += () => {
            Debug.Log("Connected to Deepgram!");
            isConnected = true;
            UIMenu.Instance.MicOn();
        };

        websocket.OnError += (e) => {
            Debug.Log("Error: " + e);
        };

        websocket.OnClose += (e) => {
            Debug.Log("Connection closed!");
            isConnected = false;
            UIMenu.Instance.MicOff();
        };

        websocket.OnMessage += (bytes) => {
            var message = System.Text.Encoding.UTF8.GetString(bytes);

            // DeepgramResponse deepgramResponse = new DeepgramResponse();
            // object boxedDeepgramResponse = deepgramResponse;
            // EditorJsonUtility.FromJsonOverwrite(message, boxedDeepgramResponse);
            // deepgramResponse = (DeepgramResponse) boxedDeepgramResponse;
            DeepgramResponse deepgramResponse = JsonUtility.FromJson<DeepgramResponse>(message);
            
            if (deepgramResponse.type == "Results") {
                var transcript = deepgramResponse.channel.alternatives[0].transcript.Trim();
                if (transcript.Length > 0) {
                    if (deepgramResponse.is_final) {
                        subtitles.AddText(transcript, true);
                    }
                    else {
                        subtitles.AddText(transcript);
                    }
                }
            }
            
        };

        await websocket.Connect();
    }
    
    public async void StopConnection() {
        if (!isConnected || websocket == null) {
            Debug.Log("WebSocket is not connected!");
            return;
        }
        
        await websocket.Close();
        isConnected = false;
        Debug.Log("WebSocket connection stopped!");
    }
    
    void Update() {
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null) {
            websocket.DispatchMessageQueue();
        }
        #endif
    }

    private async void OnApplicationQuit() {
        if (websocket != null && isConnected) {
            await websocket.Close();
        }
    }

    public async void ProcessAudio(byte[] audio) {
        if (websocket != null && websocket.State == WebSocketState.Open) {
            await websocket.Send(audio);
        } else {
            // Debug.LogWarning("Cannot process audio: WebSocket is not connected!");
        }
    }
    
    public bool IsConnected() {
        return isConnected && websocket != null && websocket.State == WebSocketState.Open;
    }
    
    public void SetApiToken(string apiKey) {
        apiToken = apiKey;
        PlayerPrefs.SetString("ApiKey", apiKey);
    }
}