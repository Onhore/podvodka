using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILeaverResistance : MonoBehaviour, 
    IPointerDownHandler, 
    IDragHandler, 
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    public RectTransform handle;
    public RectTransform background;
    
    [Header("Settings")]
    public Slider.Direction direction = Slider.Direction.LeftToRight;
    public float minValue = 0f;
    public float maxValue = 100f;
    public float currentValue = 50f;
    
    [Header("Resistance Settings")]
    public bool useResistance = true;
    [Range(0f, 5f)]
    public float dragResistance = 1f;
    
    [Header("Inertia & Damping")]
    public float inertia = 0.95f;
    public float damping = 0.5f;
    
    [Header("Movement Events")]
    public float movementThreshold = 0.5f; // Порог движения для срабатывания событий
    public UnityEngine.Events.UnityEvent onMovementStart;  // Начал двигать рычаг (один раз за перетаскивание)
    public UnityEngine.Events.UnityEvent onMovementStop;   // Остановил движение (но еще не отпустил)
    public UnityEngine.Events.UnityEvent onGrab;           // Начали тянуть (зажали)
    public UnityEngine.Events.UnityEvent onRelease;        // Отпустили
    
    [Header("Value Events")]
    public UnityEngine.Events.UnityEvent<float> onValueChanged;
    
    private bool isDragging;
    private bool isMoving;
    private float lastMovementValue;
    private float movementStopTimer;
    private float stopDelay = 0.1f; // Задержка для определения остановки
    
    private Camera cam;
    private RectTransform rect;
    private float targetValue;
    private float currentVelocity;
    private float dragVelocity;
    private float lastInvokedValue;
    
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
        targetValue = currentValue;
        lastInvokedValue = currentValue;
        lastMovementValue = currentValue;
        UpdateHandlePosition();
    }
    
    void Update()
    {
        if (!isDragging && useResistance)
        {
            float acceleration = (targetValue - currentValue) * damping;
            dragVelocity += acceleration * Time.deltaTime * 10f;
            dragVelocity *= inertia;
            
            currentValue += dragVelocity * Time.deltaTime * 5f;
            
            if (Mathf.Abs(dragVelocity) < 0.01f && Mathf.Abs(currentValue - targetValue) < 0.01f)
            {
                currentValue = targetValue;
                dragVelocity = 0f;
            }
            
            currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
            
            if (Mathf.Abs(currentValue - targetValue) > 0.01f || Mathf.Abs(dragVelocity) > 0.01f)
            {
                UpdateHandlePosition();
                
                if (Mathf.Abs(currentValue - lastInvokedValue) > 0.01f)
                {
                    onValueChanged?.Invoke(currentValue);
                    lastInvokedValue = currentValue;
                }
            }
        }
        
        // Логика отслеживания движения при перетаскивании
        if (isDragging)
        {
            float movement = Mathf.Abs(currentValue - lastMovementValue);
            
            if (movement > movementThreshold)
            {
                if (!isMoving)
                {
                    // ТОЛЬКО ЧТО НАЧАЛ ДВИГАТЬ
                    isMoving = true;
                    onMovementStart?.Invoke();
                }
                
                // Сбрасываем таймер остановки при движении
                movementStopTimer = 0f;
                lastMovementValue = currentValue;
            }
            else if (isMoving)
            {
                // Нет движения - увеличиваем таймер
                movementStopTimer += Time.deltaTime;
                
                if (movementStopTimer >= stopDelay)
                {
                    // ОСТАНОВИЛ ДВИЖЕНИЕ (но все еще держит)
                    isMoving = false;
                    onMovementStop?.Invoke();
                    movementStopTimer = 0f;
                }
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData) { }
    
    public void OnPointerExit(PointerEventData eventData) { }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        cam = eventData.pressEventCamera;
        isDragging = true;
        isMoving = false;
        dragVelocity = 0f;
        movementStopTimer = 0f;
        lastMovementValue = currentValue;
        
        DynamicCursor.StartGrab();
        UpdateValueFromPointer(eventData);
        
        // СОБЫТИЕ: Начали тянуть
        onGrab?.Invoke();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        UpdateValueFromPointer(eventData);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        
        // Если был в движении - вызываем остановку перед отпусканием
        if (isMoving)
        {
            onMovementStop?.Invoke();
            isMoving = false;
        }
        
        DynamicCursor.EndGrab();
        
        // СОБЫТИЕ: Отпустили
        onRelease?.Invoke();
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
        
        float newValue = Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalizedValue));
        
        if (useResistance)
        {
            float resistanceStrength = Mathf.Pow(dragResistance, 1.5f);
            float moveDelta = newValue - currentValue;
            float resistanceFactor = 1f / (1f + resistanceStrength * 3f);
            
            if (Mathf.Abs(moveDelta) > 0.05f && Mathf.Abs(dragVelocity) < 0.5f)
            {
                resistanceFactor *= 0.3f;
            }
            
            float step = Mathf.Clamp01(resistanceFactor * 0.5f);
            currentValue = Mathf.Lerp(currentValue, newValue, step);
            dragVelocity = (newValue - currentValue) / Time.deltaTime;
            
            float maxSpeed = Mathf.Lerp(100f, 15f, dragResistance / 5f);
            dragVelocity = Mathf.Clamp(dragVelocity, -maxSpeed, maxSpeed);
            
            targetValue = newValue;
        }
        else
        {
            currentValue = newValue;
            targetValue = newValue;
        }
        
        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
        UpdateHandlePosition();
        
        if (Mathf.Abs(currentValue - lastInvokedValue) > 0.01f)
        {
            onValueChanged?.Invoke(currentValue);
            lastInvokedValue = currentValue;
        }
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
        targetValue = Mathf.Clamp(value, minValue, maxValue);
        if (!useResistance)
        {
            currentValue = targetValue;
            UpdateHandlePosition();
        }
        
        if (invokeEvent)
        {
            onValueChanged?.Invoke(currentValue);
            lastInvokedValue = currentValue;
        }
    }
    
    public float GetValue() => currentValue;
}