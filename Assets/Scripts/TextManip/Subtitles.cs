using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using UnityEngine.UI;

public class Subtitles : MonoBehaviour {
    [SerializeField] private TMPro.TextMeshProUGUI textTmp;
    [SerializeField] private GameObject textMaskObject;
    [SerializeField] private GameObject backgroundBoxObject;
    [SerializeField] string baseColor = "FFFFFF";
    [SerializeField] string tailColor = "FFFFFF";
    [SerializeField] DraggablePressHoldButton backgroundDragScript;
    static readonly string prefix = "<color=#";
    static readonly string suffix = "</color>";
    
    private class CharacterData {
        private Subtitles parent;
        public float alpha = 1.0f;
        char character;
        
        public CharacterData(Subtitles parent, char character) {
            this.parent = parent;
            this.character = character;
        }
        
        public string GetCharacter() {
            string alphaHex = Mathf.RoundToInt(alpha * 255).ToString("X2");
            return prefix + parent.baseColor + alphaHex + ">" + character + suffix;
        }
    }
    
    private class TimedSentence {
        private CharacterData[] Text;
        private float TimeAdded;
        public bool isFading = false;
        public bool faded = false;
        
        public TimedSentence(Subtitles subtitles, string text) {
            Text = new CharacterData[text.Length];
            for (int i = 0; i < text.Length; i++) {
                Text[i] = new CharacterData(subtitles, text[i]);
            }
            
            TimeAdded = Time.time;
        }
        
        public string GetText() {
            StringBuilder sb = new();
            foreach (var c in Text) {
                sb.Append(c.GetCharacter());
            }
            return sb.ToString();
        }
        
        public int GetCharCount() {
            return Text.Length;
        }
        
        public float GetTimeAdded() {
            return TimeAdded;
        }
        
        public CharacterData CharAt(int index) {
            if (index < 0 || index >= Text.Length) {
                Debug.LogError($"invalid character index {index}");
                return null;
            }
            return Text[index];
        }
    }
    
    private List<TimedSentence> sentenceList = new();
    public string tempText = "";
    private int maxLength = 500;
    private float charDuration;
    private float fadeDuration = 1f;
    private float fadeIncrement = 0.05f;
    
    private Transformation textTransform;
    private Transformation visualBoxTransform;
    private Transformation textMaskTransform;
    private bool started = false;
    private bool textChanged = false;
    private Image backgroundImage;
    
    public void Start() {
        textTmp = GetComponent<TMPro.TextMeshProUGUI>();
        textTmp.text = "";
        StartCoroutine(WaitForSettings());
    }
    
    private IEnumerator WaitForSettings() {
        while (UIMenu.Instance == null) {
            yield return null;
        }
        
        textTransform = new Transformation(gameObject.GetComponent<RectTransform>());
        visualBoxTransform = new Transformation(backgroundBoxObject.GetComponent<RectTransform>());
        textMaskTransform = new Transformation(textMaskObject.GetComponent<RectTransform>());
        backgroundImage = backgroundBoxObject.GetComponent<Image>();
        // backgroundDragScript = backgroundBoxObject.GetComponent<DraggablePressHoldButton>();
        
        SetSize(UIMenu.Instance.GetTextSize());
        SetMaxLength(1000);
        SetDuration(UIMenu.Instance.GetTextDuration());
        SetWidth(UIMenu.Instance.GetTextWidth());
        SetHeight(UIMenu.Instance.GetTextHeight());
        SetBaseColor(UIMenu.Instance.GetBaseTextColor());
        SetTailColor(UIMenu.Instance.GetTailTextColor());
        SetBackgroundColor(UIMenu.Instance.GetTextBackgroundColor());
        // SetColor(UIMenu.Instance.GetTextColor());
        
        StartCoroutine(CheckCharacterDuration());
        started = true;
    }
    
    public void Update() {
        if (!started) {
            return;
        }
        
        BuildText();
        if (IsTextOverflowing() && textTmp.alignment != TMPro.TextAlignmentOptions.BottomLeft) {
            textTmp.alignment = TMPro.TextAlignmentOptions.BottomLeft;
        }
        else if (sentenceList.Count == 0) {
            textTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
        }
    }
    
    private IEnumerator CheckCharacterDuration() {
        while (true) {
            float currentTime = Time.time;
            
            int i = 0;
            while (i < sentenceList.Count && (currentTime - sentenceList[i].GetTimeAdded() > charDuration)) {
                if (!sentenceList[i].isFading) {
                    sentenceList[i].isFading = true;
                    StartCoroutine(FadeSentence(sentenceList[i]));
                    break;
                }
                else if (!sentenceList[i].faded) {
                    break;
                }
                i++;
            }
            
            if (i >= sentenceList.Count && tempText.Length == 0) {
                sentenceList.Clear();
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    public void AddText(string text, bool isFinal = false) {
        if (text.Length > maxLength) {
            text = text.Substring(0, maxLength);
        }
        
        if (isFinal) {
            sentenceList.Add(new TimedSentence(this, text + " "));
            tempText = "";
        }
        else {
            tempText = text;
        }
        textChanged = true;
    }
    
    private void BuildText() {
        if (!textChanged) {
            return;
        }
        
        TrimText();
        string text = string.Join("", sentenceList.Select(x => x.GetText()));
        if (tempText.Length > 0) {
            text += prefix + tailColor + tempText + suffix;
        }
        
        // Debug.Log($"currentText: {text}");
        textTmp.text = text;
    }
    
    public void TrimText() {
        int charCount = GetCharCount();
        // Debug.Log($"textLen: {charCount}");
        while (charCount > maxLength) {
            charCount -= sentenceList[0].GetCharCount();
            sentenceList.RemoveAt(0);
        }
    }
    
    private IEnumerator FadeSentence(TimedSentence sentence) {
        for (var i = 0; i < sentence.GetCharCount(); i++) {
            StartCoroutine(FadeCharacter(sentence.CharAt(i)));
            textChanged = true;
            yield return new WaitForSeconds(fadeIncrement);
        }
        yield return new WaitForSeconds(fadeDuration);
        sentence.faded = true;
    }
    
    private IEnumerator FadeCharacter(CharacterData character) {
        float startAlpha = character.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration) {
            elapsedTime += fadeIncrement;
            float t = elapsedTime / fadeDuration;
            character.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return new WaitForSeconds(fadeIncrement);
        }
        
        character.alpha = 0f;
    }
    
    public void SetMaxLength(int newLength) {
        // maxLength = newLength;
    }
    
    public void SetSize(float newSize) {
        // textTransform.ApplyScaleMod(newSize);
        visualBoxTransform.ApplyScaleMod(newSize);
    }
    
    public void SetWidth(float newWidth) {
        textTransform.ScaleWidth(newWidth);
        visualBoxTransform.ScaleWidth(newWidth);
        textMaskTransform.ScaleWidth(newWidth);
    }
    
    public void SetHeight(float newHeight) {
        textTransform.ScaleHeight(newHeight);
        visualBoxTransform.ScaleHeight(newHeight);
        textMaskTransform.ScaleHeight(newHeight);
    }
    
    public void SetDuration(float newDuration) {
        charDuration = newDuration;
    }
    
    public int GetCharCount() {
        return sentenceList.Sum(s => s.GetCharCount()) + tempText.Length;
    }
    
    public void SetBaseColor(string hexColor) {
        baseColor = hexColor;
    }
    
    public void SetTailColor(string hexColor) {
        tailColor = hexColor + ">";
    }
    
    public void ClearText() {
        sentenceList.Clear();
        tempText = "";
    }
    
    public void ToggleBackGroundImage(bool enabled) {
        if (started) 
            backgroundImage.enabled = enabled;
    }
    
    public void SetBackgroundColor(Color color) {
        Debug.Log($"setting background image color {color.ToString()}");
        if (backgroundImage != null && backgroundImage.enabled)
            backgroundImage.color = color;
    }

    public void SetDraggable(bool enabled) {
        backgroundDragScript.enabled = enabled;
    }
    
    private bool IsTextOverflowing() {
        float textHeight = textTmp.textBounds.size.y;
        float rectHeight = textTmp.rectTransform.rect.height;
        return textHeight > rectHeight;
    }
}