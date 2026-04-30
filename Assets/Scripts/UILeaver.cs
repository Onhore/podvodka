using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILeaver : MonoBehaviour, 
    IPointerDownHandler, 
    IDragHandler, 
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    public RectTransform handle; // Ручка которую перетаскиваем
    public RectTransform background; // Фон (опционально, для расчета границ)
    
    [Header("Settings")]
    public Slider.Direction direction = Slider.Direction.LeftToRight;
    public float minValue = 0f;
    public float maxValue = 100f;
    public float currentValue = 50f;
    
    [Header("Cursor Settings")]
    public bool enableCursorAttachment = true;
    
    public UnityEngine.Events.UnityEvent<float> onValueChanged;
    
    private bool isDragging;
    private Camera cam;
    private RectTransform rect;
    private Rect bounds;
    
    void Awake()
    {
        rect = GetComponent<RectTransform>();
        
        if (handle == null)
        {
            handle = GetComponentInChildren<Image>()?.GetComponent<RectTransform>();
            if (handle == null)
            {
                Debug.LogError("UILeaver: Handle not assigned!");
            }
        }
        
        if (background == null)
        {
            // Ищем фон среди детей
            foreach (Transform child in transform)
            {
                if (child != handle && child.GetComponent<Image>() != null)
                {
                    background = child as RectTransform;
                    break;
                }
            }
        }
    }
    
    void Start()
    {
        // Инициализируем позицию ручки
        UpdateHandlePosition();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        cam = eventData.pressEventCamera;
        isDragging = true;
        
        DynamicCursor.StartGrab();
        
        // Сразу обновляем позицию по клику
        UpdateValueFromPointer(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        UpdateValueFromPointer(eventData);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        DynamicCursor.EndGrab();
    }
    
    private void UpdateValueFromPointer(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform parentRect = background != null ? background : rect;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect.parent as RectTransform,
            eventData.position,
            cam,
            out localPoint
        );
        
        // Получаем границы движения
        Rect bounds = parentRect.rect;
        
        float normalizedValue = 0f;
        
        switch (direction)
        {
            case Slider.Direction.LeftToRight:
                normalizedValue = (localPoint.x - bounds.xMin) / bounds.width;
                break;
            case Slider.Direction.RightToLeft:
                normalizedValue = 1f - (localPoint.x - bounds.xMin) / bounds.width;
                break;
            case Slider.Direction.TopToBottom:
                normalizedValue = 1f - (localPoint.y - bounds.yMin) / bounds.height;
                break;
            case Slider.Direction.BottomToTop:
                normalizedValue = (localPoint.y - bounds.yMin) / bounds.height;
                break;
        }
        
        currentValue = Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue));
        UpdateHandlePosition();
        onValueChanged?.Invoke(currentValue);
    }
    
    private void UpdateHandlePosition()
    {
        if (handle == null) return;
        
        float t = Mathf.InverseLerp(minValue, maxValue, currentValue);
        RectTransform parentRect = background != null ? background : rect;
        Rect bounds = parentRect.rect;
        
        Vector2 anchoredPos = handle.anchoredPosition;
        
        switch (direction)
        {
            case Slider.Direction.LeftToRight:
                anchoredPos.x = Mathf.Lerp(bounds.xMin, bounds.xMax, t);
                break;
            case Slider.Direction.RightToLeft:
                anchoredPos.x = Mathf.Lerp(bounds.xMax, bounds.xMin, t);
                break;
            case Slider.Direction.TopToBottom:
                anchoredPos.y = Mathf.Lerp(bounds.yMax, bounds.yMin, t);
                break;
            case Slider.Direction.BottomToTop:
                anchoredPos.y = Mathf.Lerp(bounds.yMin, bounds.yMax, t);
                break;
        }
        
        handle.anchoredPosition = anchoredPos;
    }
    
    public void SetValue(float value, bool invokeEvent = true)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        UpdateHandlePosition();
        
        if (invokeEvent)
        {
            onValueChanged?.Invoke(currentValue);
        }
    }
    
    public float GetValue() => currentValue;
}