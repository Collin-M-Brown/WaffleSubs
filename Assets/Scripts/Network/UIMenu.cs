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

public class UIMenu : MonoBehaviour {
    public static UIMenu Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    public TMP_Dropdown microphoneDropdown;
    public Button micOnButton;
    public Button micOffButton;
    [SerializeField] private SettingSlider textSize;
    [SerializeField] private SettingSlider textWidth;
    [SerializeField] private SettingSlider textHeight;
    [SerializeField] private SettingSlider textDuration;
    [SerializeField] private TMP_InputField baseColorInputField;
    [SerializeField] private TMP_InputField tailColorInputField;
    [SerializeField] private TMP_InputField apiKeyInput;
    [SerializeField] private Button clearTextButton;
    [SerializeField] private Button toggleBackgroundButton;
    [SerializeField] private Button hideUIButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button testButton;
    [SerializeField] private Toggle alwaysOnTopToggle;
    [SerializeField] private Subtitles subtitles;

    void Start() {
        textSize.Init(OnTextSizeChanged, 0.01f, 5, 2, PlayerPrefs.GetFloat("textSize", 1));
        textWidth.Init(OnTextWidthChanged, 0.1f, 5f, 2, PlayerPrefs.GetFloat("textWidth", 1f));
        textHeight.Init(OnTextHeightChanged, 0.1f, 5f, 2, PlayerPrefs.GetFloat("textHeight", 1f));
        textDuration.Init(OnTextDurationChanged, 0, 30, 0, PlayerPrefs.GetFloat("textDuration", 0));
        baseColorInputField.onValueChanged.AddListener(OnBaseColorChanged);
        tailColorInputField.onValueChanged.AddListener(OnTailColorChanged);
        
        apiKeyInput.contentType = TMP_InputField.ContentType.Password;
        apiKeyInput.ForceLabelUpdate();
        apiKeyInput.onValueChanged.AddListener(OnApiKeyChanged);
        
        clearTextButton.onClick.AddListener(OnClearTextPushed);
        toggleBackgroundButton.onClick.AddListener(OnToggleBackgroundPushed);
        hideUIButton.onClick.AddListener(Deactivate);
        exitButton.onClick.AddListener(OnExitPushed);
        testButton.onClick.AddListener(OnTestPushed);
        alwaysOnTopToggle.onValueChanged.AddListener(OnAlwayOnTopToggleChanged);
        
        micOnButton.onClick.AddListener(PushMicConnectRequest);
        micOffButton.onClick.AddListener(PushMicDisconnectRequest);
        PopulateDropdown(microphoneDropdown); 
        microphoneDropdown.onValueChanged.AddListener(OnMicrophoneDropdownChange);
        AudioInput.Instance.DisableMicrophone();
        InitializeMicrophoneValue();
        PushMicDisconnectRequest();
        
        OnAlwayOnTopToggleChanged(PlayerPrefs.GetInt("AlwaysOnTop", 1) == 1 ? true : false);
    }
    
    private void PopulateDropdown(TMPro.TMP_Dropdown dropdown) {
        dropdown.ClearOptions();
        List<TMPro.TMP_Dropdown.OptionData> options = new List<TMPro.TMP_Dropdown.OptionData>();
        foreach (string device in Microphone.devices) {
            options.Add(new TMPro.TMP_Dropdown.OptionData(device));
        }
        dropdown.AddOptions(options);
    }
    
    public void PushMicConnectRequest() {
        PendingMicOn();
        AudioInput.Instance.TriggerActivation();
        AudioSender.Instance?.StartConnection();
    }
    
    void PendingMicOn() {
        SetPendingState(micOnButton, micOffButton);
    }
    
    public void PushMicDisconnectRequest() {
        SetDisconnectedState(micOnButton, micOffButton);
        if (AudioSender.Instance.IsConnected()){
            AudioSender.Instance?.StopConnection();
            MicOff();
            PendingMicOff();
        }
        
    }
    
    void PendingMicOff() {
        SetDisconnectedState(micOnButton, micOffButton);
        MicOff();
    }
    
    public void MicOn() {
        try  {
            DateTime startingTime = DateTime.Now;
            AudioInput.Instance.EnableMicrophone();
            SetConnectedState(micOnButton, micOffButton);   
            EventSystem.current?.SetSelectedGameObject(null);
            // naviLight.SetActive(true);
        } catch (Exception e)  {
            Debug.Log("Caught Error, probably null access or something dumb: " + e.Message);
        }
    }
    
    public void MicOff() {
        try {
            AudioInput.Instance.DisableMicrophone();
            SetDisconnectedState(micOnButton, micOffButton);
            EventSystem.current?.SetSelectedGameObject(null);
        }
        catch (Exception e) {
            Debug.Log("Caught Error, probably null access or something dumb: " + e.Message);
        }
    }

    public void SetConnectedState(Button onButton, Button offButton) {
        onButton.GetComponent<Shadow>().enabled = false;
        offButton.GetComponent<Shadow>().enabled = true;
        onButton.GetComponent<Image>().color = colors.OnBright();
        offButton.GetComponent<Image>().color = colors.OffDim();
    }

    public void SetPendingState(Button onButton, Button offButton) {
        onButton.GetComponent<Shadow>().enabled = false;
        offButton.GetComponent<Shadow>().enabled = true;
        onButton.GetComponent<Image>().color = colors.OnDim();
        offButton.GetComponent<Image>().color = colors.OffDim();
    }

    public void SetDisconnectedState(Button onButton, Button offButton) {
        onButton.GetComponent<Shadow>().enabled = true;
        offButton.GetComponent<Shadow>().enabled = false;
        onButton.GetComponent<Image>().color = colors.OnDim();
        offButton.GetComponent<Image>().color = colors.OffBright();
    }

    private void OnMicrophoneDropdownChange(int value) {
        string selectedDevice = Microphone.devices[value];
        AudioInput.Instance.SwitchMicrophone(selectedDevice);
        PlayerPrefs.SetString("microphone", selectedDevice);
    }
    
    public void UpdateMicrophoneDropdown() {
        PopulateDropdown(microphoneDropdown);
    }

    public void InitializeMicrophoneValue() {
        string value = PlayerPrefs.GetString("microphone");
        for (int i = 0; i < microphoneDropdown.options.Count; i++) {
            if (microphoneDropdown.options[i].text == value) {
                microphoneDropdown.value = i;
                break;
            }
        }
    }
    
    public float GetTextSize() {
        return textSize.GetValue();
    }
    
    public int GetTextDuration() {
        return (int) textDuration.GetValue();
    }
    
    public float GetTextWidth() {
        return textWidth.GetValue();
    }
    
    public float GetTextHeight() {
        return textHeight.GetValue();
    }
    
    public string GetBaseTextColor() {
        return PlayerPrefs.GetString("BaseTextColor", "FFFFFF");
    }
    
    public string GetTailTextColor() {
        return PlayerPrefs.GetString("TailTextColor", "1dde2d>");
    }
    
    private void OnTextSizeChanged(float value) {
        subtitles.SetSize(value);
        PlayerPrefs.SetFloat("textSize", value);
    }
    
    private void OnTextWidthChanged(float value) {
        subtitles.SetWidth(value);
        PlayerPrefs.SetFloat("textWidth", value);
    }
    
    private void OnTextHeightChanged(float value) {
        subtitles.SetHeight(value);
        PlayerPrefs.SetFloat("textHeight", value);
    }
    
    private void OnTextDurationChanged(float value) {
        subtitles.SetDuration((int) value);
        PlayerPrefs.SetFloat("textDuration", value);
    }
    
    private void OnBaseColorChanged(string value) {
        if (value.Length > 6) {
            value = value.Substring(0, 6);
            baseColorInputField.text = value;
            return;
        }
        if (value.Length < 6) {
            int missingChars = 6 - value.Length;
            value = value + new string('F', missingChars);
            baseColorInputField.text = value;
            return;
        }
        PlayerPrefs.SetString("BaseTextColor", value);
        subtitles.SetBaseColor(value);
    }
    
    private void OnTailColorChanged(string value) {
        if (value.Length > 6) {
            value = value.Substring(0, 6);
            baseColorInputField.text = value;
            return;
        }
        if (value.Length < 6) {
            int missingChars = 6 - value.Length;
            value = value + new string('F', missingChars);
            baseColorInputField.text = value;
            return;
        }
        PlayerPrefs.SetString("TailTextColor", value + ">");
        subtitles.SetTailColor(value + ">");
    }

    private void OnApiKeyChanged(string value) {
        AudioSender.Instance.SetApiToken(value);
    }

    private void OnClearTextPushed() {
        subtitles.ClearText();
    }
    
    private void OnToggleBackgroundPushed() {
        // subtitles.ToggleBackGroundImage();
    }
    
    private void OnExitPushed() {
        ExitGame.Instance.Exit();
    }
    
    private void OnTestPushed() {
        subtitles.AddText("this is some test text this is some test text", true);
    }
    
    private void OnAlwayOnTopToggleChanged(bool value) {
        WaffleTransparency.Instance.SetAlwaysOnTop(value);
        PlayerPrefs.SetInt("AlwaysOnTop", value ? 1 : 0);
    }
    
    public void Activate() {
        WaffleTransparency.Instance.SetClickthrough(false);
        subtitles.ToggleBackGroundImage(true);
        gameObject.SetActive(true);
    }
    
    public void Deactivate() {
        WaffleTransparency.Instance.SetClickthrough(true);
        subtitles.ToggleBackGroundImage(false);
        gameObject.SetActive(false);
    }
    
    public bool IsMicOn() {
        return micOnButton.GetComponent<Shadow>().enabled == false;
    }
}
