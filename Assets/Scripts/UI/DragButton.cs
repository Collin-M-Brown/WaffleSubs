using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Unity.VisualScripting;
using System;

#pragma warning disable CS0162 // Unreachable code detected
public class DraggablePressHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler {
    private Button button;
    private bool isPressed = false;
    private bool isDragging = false;
    public RectTransform rectTransform = null;
    public RectTransform targetTransform = null;

    private Coroutine imageFadeRoutine;
    private Image image;
    
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
        
        image = GetComponent<Image>();
        mainCamera = MainCam.Instance.GetMainCam();
        
        if (image != null) {
            image.raycastTarget = true;
        }
        
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
        StartCoroutine(ForceRaycastUpdate());
    }
    
    private IEnumerator ForceRaycastUpdate() {
        yield return new WaitForEndOfFrame();
        
        if (parentCanvas != null) {
            GraphicRaycaster raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null) {
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
            if (isPressed && !Input.GetMouseButton(0)) {
                isPressed = false;
                isDragging = false;
                UpdateButtonState();
            }
            yield return null;
        }
    }
    
    private void UpdatePosition() {
        PlayerPrefs.SetFloat(objectLabel + ".x", rectTransform.anchoredPosition.x);
        PlayerPrefs.SetFloat(objectLabel + ".y", rectTransform.anchoredPosition.y);
    }
}