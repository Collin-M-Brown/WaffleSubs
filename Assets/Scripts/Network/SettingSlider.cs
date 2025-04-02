using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public delegate void UpdateAction(float value);
public class SettingSlider: MonoBehaviour {
    private Slider slider;
    private TMP_InputField inputTMP;
    public float min = 0f;
    public float max = 3f;
    public int round = 2;
    private UpdateAction updateAction;

    public void Init(UpdateAction updateAction, float min, float max, int round, float initialValue) {
        this.min = min;
        this.max = max;
        this.round = round;
        this.updateAction = updateAction;
        slider = GetComponent<Slider>();
        inputTMP = GetComponentInChildren<TMP_InputField>();
        
        slider.wholeNumbers = (round == 0);
        slider.minValue = min;
        slider.maxValue = max;
        
        slider.onValueChanged.AddListener(OnSliderChange);
        inputTMP.onEndEdit.AddListener(OnInputEndEdit);
        
        SetValue(initialValue, false);
    }

    public void SetValue(float value, bool notify = true) {
        value = Mathf.Clamp(value, min, max);
        float roundedValue = RoundValue(value);
        slider.SetValueWithoutNotify(roundedValue);
        UpdateInputField(roundedValue);
        
        if (notify) {
            updateAction?.Invoke(roundedValue);
        }
    }
    
    public float GetValue() {
        return slider.value;
    }

    private void OnSliderChange(float value) {
        try {
            float roundedValue = RoundValue(value);
            UpdateInputField(roundedValue);
            updateAction?.Invoke(roundedValue);
        }
        catch (Exception ex) {
            Debug.Log("Error in OnSliderChange: " + ex.Message + $" value: {value}");
        }
    }

    private void OnInputEndEdit(string value) {
        if (float.TryParse(value, out float result)) {
            SetValue(result);
        }
        else {
            UpdateInputField(slider.value);
        }
    }

    private float RoundValue(float value) {
        return round == 0 ? Mathf.Round(value) : (float)Math.Round(value, round);
    }

    private void UpdateInputField(float value) {
        inputTMP.SetTextWithoutNotify(FormatValue(value));
    }

    private string FormatValue(float value) {
        return round == 0 ? value.ToString("F0") : value.ToString("F" + round);
    }

    
}