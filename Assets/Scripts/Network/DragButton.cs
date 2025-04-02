using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Unity.VisualScripting;
using System;

#pragma warning disable CS0162 // Unreachable code detected
public class DraggablePressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler {
    private Button button;
    private bool isPressed = false;
    private bool isDragging = false;
    public RectTransform rectTransform = null;
    public RectTransform targetTransform = null;

    private Coroutine imageFadeRoutine;
    private Image image;
    private float fadeDuration = 0.50f;
    public float oAlph = .75f;
    public float tAlph = .25f;
    private Coroutine updateRoutine;
    public string objectLabel;
    [SerializeField] private AnimationCurve fadeCurve;
    private Transform originalParent;
    private Canvas parentCanvas;
    [SerializeField] private Camera mainCamera;

    void Awake() {
        button = GetComponent<Button>();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (targetTransform == null)
            targetTransform = GetComponent<RectTransform>();
        originalParent = targetTransform.parent;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    void Start() {
        if (fadeCurve == null || fadeCurve.keys.Length == 0) {
            fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
        
        // SetInitialPosition();
        image = GetComponent<Image>();
        mainCamera = MainCam.Instance.GetMainCam();
        
        // Ensure raycast detection is enabled on the image
        if (image != null) {
            image.raycastTarget = true;
        }
        
        // Force GraphicRaycaster on parent canvas to update
        if (parentCanvas != null) {
            GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) {
                // This will force it to rebuild its cache
                raycaster.enabled = false;
                raycaster.enabled = true;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (imageFadeRoutine != null)
            StopCoroutine(imageFadeRoutine);

        imageFadeRoutine = StartCoroutine(FadeImage(image.color.a, oAlph));
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (imageFadeRoutine != null)
            StopCoroutine(imageFadeRoutine);
        
        imageFadeRoutine = StartCoroutine(FadeImage(image.color.a, tAlph));
    }

    private IEnumerator FadeImage(float startAlpha, float endAlpha) {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration) {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            float curveValue = fadeCurve.Evaluate(t);
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, curveValue);
            SetTransparency(currentAlpha);
            yield return null;
        }

        SetTransparency(endAlpha);
    }

    public void SetTransparency(float alpha) {
        alpha = Mathf.Clamp01(alpha);
        Color color = image.color;
        if (!Mathf.Approximately(color.a, alpha)) {
            color.a = alpha;
            image.color = color;
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        isPressed = true;
        UpdateButtonState();
        if (updateRoutine != null)
            StopCoroutine(updateRoutine);
        updateRoutine = StartCoroutine(UpdateRoutine());
    }

    public void OnPointerUp(PointerEventData eventData) {
        isPressed = false;
        UpdateButtonState();
        if (updateRoutine != null)
            StopCoroutine(updateRoutine);
    }

    private Vector3 dragOffset;
    public void OnBeginDrag(PointerEventData eventData) {
        originalParent = rectTransform.parent;
        rectTransform.SetParent(CanvasFinder.Instance.GetUICanvasTransform());
        isDragging = true;
        
        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            CanvasFinder.Instance.GetUICanvasTransform() as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out mousePos
        );
        
        dragOffset = rectTransform.localPosition - new Vector3(mousePos.x, mousePos.y, 0);
    }

    public void OnDrag(PointerEventData eventData) {
        if (image.enabled && isDragging) {
            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                CanvasFinder.Instance.GetUICanvasTransform() as RectTransform,
                eventData.position,
                mainCamera,
                out mousePos
            );
            
            rectTransform.localPosition = new Vector3(mousePos.x, mousePos.y, rectTransform.localPosition.z) + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        isDragging = false;
        isPressed = false;
        UpdateButtonState();
        UpdatePosition();
        
        rectTransform.SetParent(originalParent);
        
        // Force GraphicRaycaster update after changing parent
        StartCoroutine(ForceRaycastUpdate());
    }
    
    private IEnumerator ForceRaycastUpdate() {
        yield return new WaitForEndOfFrame();
        
        // Force GraphicRaycaster on parent canvas to update
        if (parentCanvas != null) {
            GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) {
                // This will force it to rebuild its cache
                raycaster.enabled = false;
                raycaster.enabled = true;
            }
        }
    }

    private void UpdateButtonState() {
        if (button == null) return;

        if (isPressed) {
            button.OnPointerDown(new PointerEventData(EventSystem.current));
        }
        else {
            button.OnPointerUp(new PointerEventData(EventSystem.current));
            button.OnDeselect(new BaseEventData(EventSystem.current));
        }
    }

    IEnumerator UpdateRoutine() {
        while (true) {
            // Safety check: release the button if the mouse button is released anywhere
            if (isPressed && !Input.GetMouseButton(0)) {
                isPressed = false;
                isDragging = false;
                UpdateButtonState();
            }
            yield return null;
        }
    }

    
    public void OnEnable() {
        // SetInitialPosition();   
    }
    
    public void SetInitialPosition() {
        StartCoroutine(SetPosition());
    }

    IEnumerator SetPosition() {
        
        yield return new WaitForEndOfFrame();
        rectTransform.SetParent(CanvasFinder.Instance.GetUICanvasTransform());
        
        float x = rectTransform.localPosition.x;
        float y = rectTransform.localPosition.y;
        
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 elementSize = rectTransform.rect.size;
        float elementWidth = elementSize.x * rectTransform.localScale.x;
        float elementHeight = elementSize.y * rectTransform.localScale.y;
        x = Mathf.Clamp(x, elementWidth/2, screenSize.x - elementWidth/2);
        y = Mathf.Clamp(y, elementHeight/2, screenSize.y - elementHeight/2);
        Vector2 InitialPos = new Vector2(x, y);
        
        rectTransform.localPosition = InitialPos;
        rectTransform.SetParent(originalParent);
        
        // Force GraphicRaycaster update after changing parent
        StartCoroutine(ForceRaycastUpdate());
    }
    
    private void UpdatePosition() {
        PlayerPrefs.SetFloat(objectLabel + ".x", rectTransform.anchoredPosition.x);
        PlayerPrefs.SetFloat(objectLabel + ".y", rectTransform.anchoredPosition.y);
    }
}