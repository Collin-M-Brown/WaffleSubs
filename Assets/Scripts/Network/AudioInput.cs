using System.Collections;
using UnityEngine;
using System;
#pragma warning disable CS0162

[RequireComponent(typeof(AudioSource))]
public class AudioInput : MonoBehaviour {
    public static AudioInput Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    private bool pushToTalkEnabled = false;
    private bool isPushToTalkActive = false;
    private AudioSource _audioSource;
    private int lastPosition, currentPosition;
    private bool isMicrophoneInitialized = false;
    private const int InitializationTimeout = 5;
    private float currentNoiseLevel;
    public float CurrentNoiseLevel => currentNoiseLevel;
    private string currentDevice;
    private string selectedDevice;
    private bool isEnabled = false;
    private float lastActivationTime = 0f;
    private float maxNoiseLevel = 0f;

    
    public void Start() {
        // EnableMicrophone();
    }
    
    private void Update() {
        if (!isEnabled || !isMicrophoneInitialized) return;
        
        currentPosition = Microphone.GetPosition(currentDevice);
        if (currentPosition > 0) {
            if (lastPosition > currentPosition)
                lastPosition = 0;
                
            if (currentPosition - lastPosition > 0) {
                ProcessAudioData();
                lastPosition = currentPosition;
            }
        }
        
        UpdateNoiseLevel();
        
        // Only update activation time in continuous mode
        if (!pushToTalkEnabled &&  currentNoiseLevel > NoiseLevelUI.Instance.GetUnscaledThreshold()) {
            lastActivationTime = Time.time;
        }
    }
    
    // Interface functions for push-to-talk
    public void TalkButtonHeld() {
        if (pushToTalkEnabled) {
            Debug.Log("Talk button held");
            isPushToTalkActive = true;
            TriggerActivation();
        }
        else {
            Debug.Log("Talk button not enabled");
        }
    }

    public void TalkButtonReleased() {
        if (pushToTalkEnabled) {
            Debug.Log("Talk button released");
            isPushToTalkActive = false;
        } 
    }

    public void TogglePushToTalk(bool enabled) {
        Debug.Log("Push to talk enabled: " + enabled);
        pushToTalkEnabled = enabled;
        isPushToTalkActive = false;  // Reset PTT state when switching modes
    }

    public void EnableMicrophone() {
        Debug.Log("enabling microphone");
        if (!isEnabled) {
            isEnabled = true;
            StartCoroutine(InitializeMicrophone(selectedDevice));
        }
    }

    public void DisableMicrophone() {
        if (isEnabled) {
            isEnabled = false;
            StopMicrophone();
        }
    }

    public bool IsEnabled() {
        return isEnabled;
    }

    public void SwitchMicrophone(string newDevice) {
        selectedDevice = newDevice;
        if (isEnabled) {
            StopMicrophone();
            StartCoroutine(InitializeMicrophone(newDevice));
        }
    }

    private IEnumerator InitializeMicrophone(string deviceToUse = null) {
        _audioSource = GetComponent<AudioSource>();
        if (Microphone.devices.Length > 0) {
            if (deviceToUse == null || !Array.Exists(Microphone.devices, device => device == deviceToUse)) {
                deviceToUse = Microphone.devices[0];
            }

            Debug.Log($"using device: {deviceToUse}");
            _audioSource.clip = Microphone.Start(deviceToUse, true, 10, AudioSettings.outputSampleRate);

            float elapsedTime = 0f;
            while (!(Microphone.GetPosition(deviceToUse) > 0) && elapsedTime < InitializationTimeout) {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (elapsedTime >= InitializationTimeout) {
                Debug.Log("Microphone initialization timed out. The device may be disconnected or inaccessible.");
                yield break;
            }

            currentDevice = deviceToUse;
            isMicrophoneInitialized = true;
        }
        else {
            Debug.Log("No microphone devices found!");
        }
    }
    
    private void StopMicrophone() {
        if (isMicrophoneInitialized) {
            Microphone.End(currentDevice);
            isMicrophoneInitialized = false;
        }
    }
    
    private void OnDisable() {
        DisableMicrophone();
    }
    
    private void ProcessAudioData() {
        int dataLength = currentPosition - lastPosition;
        float[] samples = new float[dataLength * _audioSource.clip.channels];
        _audioSource.clip.GetData(samples, lastPosition);

        short[] samplesAsShorts = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++) {
            samplesAsShorts[i] = f32_to_i16(samples[i]);
        }

        var samplesAsBytes = new byte[samplesAsShorts.Length * 2];
        Buffer.BlockCopy(samplesAsShorts, 0, samplesAsBytes, 0, samplesAsBytes.Length);
        
        if (UIMenu.Instance?.IsMicOn() == true) {
            AudioSender.Instance.ProcessAudio(samplesAsBytes);
        }
        
    }
    
    public float TimeSinceLastUserSpeech() {
        return Time.time - lastActivationTime;
    }
    
    public void TriggerActivation() {
        lastActivationTime = Time.time;
    }
    
    private void UpdateNoiseLevel() {
        if (!isMicrophoneInitialized) return;
        
        int micPosition = Microphone.GetPosition(currentDevice);
        int dataLength = 1024;

        int startPosition = micPosition - dataLength;
        if (startPosition < 0) {
            startPosition += _audioSource.clip.samples;
        }

        float[] samples = new float[dataLength];
        _audioSource.clip.GetData(samples, startPosition);

        float sum = 0f;
        for (int i = 0; i < dataLength; i++) {
            sum += Mathf.Abs(samples[i]);
        }

        currentNoiseLevel = sum / dataLength;
        if (currentNoiseLevel > maxNoiseLevel) {
            maxNoiseLevel = currentNoiseLevel;
        }
    }

    private short f32_to_i16(float sample) {
        sample *= 32768;
        return (short)Mathf.Clamp(sample, -32768, 32767);
    }
}