using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

#pragma warning disable CS0162 // Unreachable code detected

public class NoiseLevelUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public static NoiseLevelUI Instance { get; private set; }
    void Awake() {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;
    }

    [SerializeField] private RectTransform mainBar;
    [SerializeField] private RectTransform thresholdIndicator;
    [SerializeField] private RectTransform currentLevelIndicator;
    [SerializeField] private float noiseMultiplier;
    
    private float minThreshold = 0f;
    private float maxThreshold = 1f;
    [SerializeField] private float visualScale;
    private float threshold;
    private float mainBarWidth;
    private Vector2 dragStartPosition;
    private float dragStartThreshold;

    private void Start() {
        threshold = (minThreshold + maxThreshold) / 2f;
        threshold = PlayerPrefs.GetFloat("NoiseThreshold", threshold);
        mainBarWidth = mainBar.rect.width;
        
        SetupRectTransforms();
        UpdateThresholdPosition();
        
        Debug.Log("noise indicator started succesfully");
    }

    private void SetupRectTransforms() {
        mainBar.anchorMin = new Vector2(0, 0.5f);
        mainBar.anchorMax = new Vector2(1, 0.5f);
        mainBar.anchoredPosition = Vector2.zero;
        mainBar.sizeDelta = new Vector2(0, mainBar.rect.height);

        currentLevelIndicator.anchorMin = new Vector2(0, 0);
        currentLevelIndicator.anchorMax = new Vector2(0, 1);
        currentLevelIndicator.pivot = new Vector2(0, 0.5f);
        currentLevelIndicator.anchoredPosition = Vector2.zero;
        currentLevelIndicator.sizeDelta = new Vector2(0, 0);

        thresholdIndicator.anchorMin = new Vector2(0, 0);
        thresholdIndicator.anchorMax = new Vector2(0, 1);
        thresholdIndicator.sizeDelta = new Vector2(thresholdIndicator.rect.width, 0);
    }

    private void Update() {
        float currentNoiseLevel = AudioInput.Instance.CurrentNoiseLevel * noiseMultiplier * visualScale;
        UpdateCurrentLevelIndicator(currentNoiseLevel);
    }

    private void UpdateCurrentLevelIndicator(float noiseLevel) {
        float normalizedLevel = Mathf.Clamp01(noiseLevel);
        float width = normalizedLevel * mainBarWidth;
        currentLevelIndicator.sizeDelta = new Vector2(width, 0);
        if (normalizedLevel > threshold) {
            currentLevelIndicator.GetComponent<Image>().color = Color.green;
        }
        else {
            currentLevelIndicator.GetComponent<Image>().color = Color.red;
        }
    }

    private void UpdateThresholdPosition() {
        float xPosition = Mathf.Clamp(threshold * mainBarWidth, 0, mainBarWidth);
        thresholdIndicator.anchoredPosition = new Vector2(xPosition, 0);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mainBar, eventData.position, eventData.pressEventCamera, out dragStartPosition);
        dragStartThreshold = threshold;
    }

    public void OnDrag(PointerEventData eventData) {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainBar, eventData.position, eventData.pressEventCamera, out localPoint)) {
            float dragDelta = (localPoint.x - dragStartPosition.x) / mainBarWidth;
            float newThreshold = Mathf.Clamp(dragStartThreshold + dragDelta, minThreshold, maxThreshold);
            
            if (newThreshold != threshold) {
                threshold = newThreshold;
                UpdateThresholdPosition();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        PlayerPrefs.SetFloat("NoiseThreshold", threshold);
    }

    public float GetThreshold() {
        return threshold;
    }

    public float GetUnscaledThreshold() {
        return threshold / noiseMultiplier / visualScale;
    }

    private string FormatValue(float value) {
        return value.ToString("F" + 2);
    }
}