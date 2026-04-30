using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UILeaverReload : MonoBehaviour, 
    IPointerDownHandler, 
    IDragHandler, 
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    public RectTransform handle;
    public RectTransform track;
    
    [Header("Reload Settings")]
    public float pullDirection = 1f;
    public float maxPullDistance = 100f;
    public float returnSpeed = 8f;
    public float resistanceForce = 0.8f;
    
    [Header("States")]
    public bool isInRestPosition = true;
    public float restThreshold = 5f;
    
    [Header("Movement Events")]
    public float movementThreshold = 0.1f;
    public float endThreshold = 0.95f;
    
    public UnityEngine.Events.UnityEvent onPull;
    public UnityEngine.Events.UnityEvent onReload;
    public UnityEngine.Events.UnityEvent onReachedEnd;
    public UnityEngine.Events.UnityEvent onLeftEnd;
    public UnityEngine.Events.UnityEvent onReachedRest;    // НОВОЕ: достиг исходного положения
    public UnityEngine.Events.UnityEvent onMovementStart;
    public UnityEngine.Events.UnityEvent onMovementStop;
    public UnityEngine.Events.UnityEvent onGrab;
    public UnityEngine.Events.UnityEvent onRelease;
    public UnityEngine.Events.UnityEvent<float> onPullProgress;
    
    private bool isDragging;
    private bool isMoving;
    private bool wasAtEnd;
    private bool wasAtRest;      // Был ли в исходном положении в прошлом кадре
    private float movementStopTimer;
    private float stopDelay = 0.15f;
    
    private Camera cam;
    private float currentPullDistance;
    private float targetPullDistance;
    private float restPosition;
    private float lastInvokedProgress;
    private float lastMovementValue;
    
    void Start()
    {
        if (handle == null) handle = GetComponentInChildren<Image>()?.GetComponent<RectTransform>();
        if (track == null) FindTrack();
        
        restPosition = handle.anchoredPosition.x;
        currentPullDistance = 0;
        targetPullDistance = 0;
        lastMovementValue = 0;
        lastInvokedProgress = 0;
        wasAtEnd = false;
        wasAtRest = true;
    }
    
    void FindTrack()
    {
        foreach (Transform child in transform)
        {
            if (child != handle && child.GetComponent<Image>() != null)
            {
                track = child as RectTransform;
                break;
            }
        }
    }
    
    void Update()
    {
        // Плавное движение к цели
        if (isDragging)
        {
            currentPullDistance = Mathf.Lerp(currentPullDistance, targetPullDistance, 0.3f);
            UpdateHandlePosition();
            
            float progress = Mathf.Clamp01(currentPullDistance / maxPullDistance);
            bool isAtEnd = progress >= endThreshold;
            
            // ПРОВЕРКА ДОСТИЖЕНИЯ КОНЦА
            if (isAtEnd && !wasAtEnd)
            {
                onReachedEnd?.Invoke();
                wasAtEnd = true;
            }
            else if (!isAtEnd && wasAtEnd)
            {
                onLeftEnd?.Invoke();
                wasAtEnd = false;
            }
            
            // ПРОВЕРКА ДВИЖЕНИЯ
            float movement = Mathf.Abs(currentPullDistance - lastMovementValue);
            
            if (movement > movementThreshold)
            {
                if (!isMoving)
                {
                    isMoving = true;
                    onMovementStart?.Invoke();
                }
                movementStopTimer = 0f;
                lastMovementValue = currentPullDistance;
            }
            else if (isMoving)
            {
                movementStopTimer += Time.deltaTime;
                if (movementStopTimer >= stopDelay)
                {
                    isMoving = false;
                    onMovementStop?.Invoke();
                    movementStopTimer = 0f;
                }
            }
            
            // Прогресс
            if (Mathf.Abs(progress - lastInvokedProgress) > 0.01f)
            {
                onPullProgress?.Invoke(progress);
                lastInvokedProgress = progress;
            }
        }
        
        // Возврат в исходное положение
        if (!isDragging && Mathf.Abs(currentPullDistance) > restThreshold)
        {
            float returnStep = returnSpeed * Time.deltaTime;
            currentPullDistance = Mathf.MoveTowards(currentPullDistance, 0, returnStep);
            targetPullDistance = currentPullDistance;
            UpdateHandlePosition();
            
            float progress = Mathf.Clamp01(Mathf.Abs(currentPullDistance) / maxPullDistance);
            if (Mathf.Abs(progress - lastInvokedProgress) > 0.01f)
            {
                onPullProgress?.Invoke(progress);
                lastInvokedProgress = progress;
            }
        }
        
        // ПРОВЕРКА ДОСТИЖЕНИЯ ИСХОДНОГО ПОЛОЖЕНИЯ (для возврата)
        if (!isDragging)
        {
            bool isAtRest = Mathf.Abs(currentPullDistance) <= restThreshold;
            
            if (isAtRest && !wasAtRest)
            {
                // ТОЛЬКО ЧТО ДОСТИГ ИСХОДНОГО ПОЛОЖЕНИЯ
                Debug.Log("Reached REST position");
                onReachedRest?.Invoke();
                isInRestPosition = true;
                wasAtRest = true;
            }
            else if (!isAtRest && wasAtRest)
            {
                // УШЕЛ С ИСХОДНОГО ПОЛОЖЕНИЯ (начал тянуть)
                isInRestPosition = false;
                wasAtRest = false;
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData) { }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
            DynamicCursor.EndGrab();
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        cam = eventData.pressEventCamera;
        isDragging = true;
        isMoving = false;
        wasAtEnd = false;
        movementStopTimer = 0f;
        lastMovementValue = currentPullDistance;
        
        DynamicCursor.StartGrab();
        
        onGrab?.Invoke();
        
        if (isInRestPosition)
        {
            onPull?.Invoke();
        }
        
        UpdateValueFromPointer(eventData);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        UpdateValueFromPointer(eventData);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        
        if (isMoving)
        {
            onMovementStop?.Invoke();
            isMoving = false;
        }
        
        wasAtEnd = false;
        
        DynamicCursor.EndGrab();
        
        onRelease?.Invoke();
        
        if (currentPullDistance > maxPullDistance * 0.7f)
        {
            onReload?.Invoke();
        }
    }
    
    private void UpdateValueFromPointer(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform parentForCalc = track != null ? track : GetComponent<RectTransform>();
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentForCalc,
            eventData.position,
            cam,
            out localPoint
        );
        
        float targetDistance = localPoint.x - restPosition;
        targetDistance *= pullDirection;
        targetDistance = Mathf.Clamp(targetDistance, 0, maxPullDistance);
        
        float resistance = 1f - (targetDistance / maxPullDistance) * resistanceForce;
        targetPullDistance = Mathf.Lerp(targetPullDistance, targetDistance, resistance);
    }
    
    void UpdateHandlePosition()
    {
        if (handle == null) return;
        
        float newX = restPosition + (currentPullDistance * pullDirection);
        handle.anchoredPosition = new Vector2(newX, handle.anchoredPosition.y);
    }
    
    public bool IsInRestPosition() => isInRestPosition;
    public bool IsAtEnd() => Mathf.Clamp01(currentPullDistance / maxPullDistance) >= endThreshold;
    public float GetPullProgress() => Mathf.Clamp01(currentPullDistance / maxPullDistance);
    public bool IsMoving() => isMoving;
    public bool IsDragging() => isDragging;
    
    public void ForceReturn()
    {
        currentPullDistance = 0;
        targetPullDistance = 0;
        UpdateHandlePosition();
        isInRestPosition = true;
        wasAtRest = true;
        isMoving = false;
        wasAtEnd = false;
        movementStopTimer = 0f;
        lastInvokedProgress = 0;
        lastMovementValue = 0;
    }
}