using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIRotary : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public RectTransform handle;

    public float minAngle = -180f;
    public float maxAngle = 180f;

    [Header("Rotation Sensitivity")]
    public float sensitivity = 1f;
    public bool invertRotation = false;
    [Range(0f, 1f)]
    public float smoothness = 0.5f;

    [Header("Physical Properties")]
    public float followSpeed = 8f;
    [Range(0.8f, 0.99f)]
    public float inertiaDamping = 0.95f;
    public float maxInertia = 15f;
    [Range(0f, 1f)]
    public float resistance = 0.2f;

    [Header("Rotation Limits Force")]
    public float boundarySpringForce = 2f;
    [Range(0f, 1f)]
    public float boundaryHardness = 0.5f;

    [Header("Return to Center (Smooth)")]
    public float returnToCenterForce = 100f;
    public float returnSpeed = 5f;
    [Range(0f, 1f)]
    public float returnSmoothing = 0.8f;
    public float returnDeadZone = 3f;
    [Range(0f, 1f)]
    public float returnDamping = 0.5f;

    [Header("Rusty Shake")]
    [Range(0f, 5f)]
    public float shakeIntensity = 1.5f;
    [Range(1f, 30f)]
    public float shakeFrequency = 15f;
    public bool shakeOnlyWhenMoving = true;
    public bool shakeDependsOnSpeed = true;
    [Range(0f, 1f)]
    public float maxShakeAtSpeed = 0.8f;
    [Range(0f, 1f)]
    public float randomJerkChance = 0.1f;
    [Range(0f, 3f)]
    public float jerkForce = 0.5f;

    [Header("Cursor Interaction")]
    public bool enableCursorAttachment = true;

    [Header("Sound Events")]
    public UnityEvent onRotationStart;
    public UnityEvent onRotation;
    public UnityEvent onRotationEnd;
    public UnityEvent<float> onRotationDelta;
    public UnityEvent<float> onRotationSpeed;
    public UnityEvent onDirectionChanged;
    public UnityEvent onShake;
    public UnityEvent<float> onValueChanged;

    public UnityEvent onMouseDown;

    private RectTransform rect;
    private Camera cam;

    private float currentAngle;
    private float targetAngle;
    private float inertiaVelocity;

    private Vector2 lastDragPosition;
    private bool dragging;
    private bool isRotating;
    private float lastFrameAngle;
    private float lastRotationSpeed;
    private float lastRotationDirection;
    
    // Кэшированные значения для оптимизации
    private float cachedDeltaTime;
    private float cachedReturnForce;
    private float cachedBoundaryForce;
    private float shakeOffset;
    private float shakeTimer;
    private float lastShakeTime;
    private float currentJerk;
    
    // Для оптимизации вызовов
    private bool isHovering;
    private float previousAngle;

    void Awake()
    {
        rect = handle != null ? handle : GetComponent<RectTransform>();
    }

    void Start()
    {
        currentAngle = rect.localEulerAngles.z;
        if (currentAngle > 180) currentAngle -= 360;
        targetAngle = currentAngle;
        lastFrameAngle = currentAngle;
        previousAngle = currentAngle;
    }

    void Update()
    {
        cachedDeltaTime = Time.deltaTime;
        
        float previousAngle = currentAngle;
        bool wasRotating = isRotating;

        if (dragging)
        {
            UpdateDragging();
        }
        else
        {
            UpdateIdle();
        }
        
        // Применяем дрожание (только если нужно)
        float finalAngle = ApplyShake(currentAngle);
        
        // Применяем поворот
        rect.localRotation = Quaternion.Euler(0, 0, finalAngle);
        
        // Обновляем события (только при реальном изменении)
        UpdateEvents(previousAngle, finalAngle, wasRotating);
        
        lastFrameAngle = finalAngle;
        previousAngle = finalAngle;
    }

    private void UpdateDragging()
    {
        float resistanceForce = 1f - (resistance * 0.5f);
        float smoothedTarget = Mathf.Lerp(targetAngle, currentAngle, smoothness);
        
        currentAngle = Mathf.Lerp(currentAngle, smoothedTarget, (followSpeed * resistanceForce) * cachedDeltaTime);
        
        // Границы (только если близко к ним)
        if (currentAngle < minAngle + 5f || currentAngle > maxAngle - 5f)
        {
            float boundaryForce = CalculateBoundaryForce(currentAngle);
            if (Mathf.Abs(boundaryForce) > 0.01f)
            {
                currentAngle += boundaryForce * boundarySpringForce * cachedDeltaTime;
            }
        }
        
        inertiaVelocity = (targetAngle - currentAngle) / cachedDeltaTime;
        inertiaVelocity = Mathf.Clamp(inertiaVelocity, -maxInertia, maxInertia);
    }

    private void UpdateIdle()
    {
        // Инерция
        if (Mathf.Abs(inertiaVelocity) > 0.05f)
        {
            currentAngle += inertiaVelocity * cachedDeltaTime;
            inertiaVelocity *= inertiaDamping * (1f - resistance * 0.3f);
            
            if (Mathf.Abs(inertiaVelocity) < 0.05f)
                inertiaVelocity = 0f;
        }
        
        // Возврат в центр (только если нужно)
        if (returnToCenterForce > 0 && Mathf.Abs(currentAngle) > returnDeadZone)
        {
            ApplyReturnToCenter();
        }
        
        currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
    }

    private void ApplyReturnToCenter()
    {
        float distanceToCenter = Mathf.Abs(currentAngle);
        float maxDistance = Mathf.Max(Mathf.Abs(minAngle), Mathf.Abs(maxAngle));
        float normalizedDistance = Mathf.Clamp01(distanceToCenter / maxDistance);
        
        // Упрощенный расчет силы
        float smoothFactor = Mathf.Pow(normalizedDistance, 1f + returnSmoothing * 3f);
        float returnForceMagnitude = returnToCenterForce * smoothFactor * cachedDeltaTime;
        float returnDirection = -Mathf.Sign(currentAngle);
        
        currentAngle += returnDirection * returnForceMagnitude;
        
        // Демпфирование только если очень близко
        if (distanceToCenter < returnDeadZone * 3f)
        {
            float dampingFactor = 1f - (distanceToCenter / (returnDeadZone * 3f)) * returnDamping;
            inertiaVelocity *= dampingFactor;
        }
        
        if (Mathf.Abs(currentAngle) < 0.5f)
        {
            currentAngle = 0f;
            inertiaVelocity = 0f;
        }
    }

    private float ApplyShake(float angle)
    {
        if (shakeIntensity <= 0) return angle;
        
        bool shouldShake = !shakeOnlyWhenMoving || isRotating;
        if (!shouldShake) return angle;
        
        float currentShakeIntensity = shakeIntensity;
        
        if (shakeDependsOnSpeed && isRotating)
        {
            float speedFactor = Mathf.Clamp01(lastRotationSpeed / 100f);
            currentShakeIntensity *= Mathf.Lerp(0.3f, maxShakeAtSpeed, speedFactor);
        }
        
        // Более производительный шум
        float noiseValue = Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) * 2f - 1f;
        
        // Случайные рывки (редко)
        if (randomJerkChance > 0 && isRotating && Time.time - lastShakeTime > 0.15f)
        {
            if (Random.value < randomJerkChance * cachedDeltaTime * 20f)
            {
                currentJerk = (Random.Range(-1f, 1f) * jerkForce);
                lastShakeTime = Time.time;
                onShake?.Invoke();
            }
            else
            {
                currentJerk = Mathf.Lerp(currentJerk, 0f, cachedDeltaTime * 10f);
            }
        }
        
        float appliedShake = noiseValue * currentShakeIntensity * cachedDeltaTime * 20f;
        return angle + appliedShake + currentJerk * cachedDeltaTime * 10f;
    }

    private void UpdateEvents(float previousAngle, float finalAngle, bool wasRotating)
    {
        float angleDelta = Mathf.Abs(finalAngle - previousAngle);
        isRotating = angleDelta > 0.01f;
        
        if (isRotating)
        {
            lastRotationSpeed = Mathf.Abs(finalAngle - lastFrameAngle) / cachedDeltaTime;
            
            float currentDirection = Mathf.Sign(finalAngle - previousAngle);
            if (lastRotationDirection != 0 && currentDirection != lastRotationDirection && currentDirection != 0)
            {
                onDirectionChanged?.Invoke();
            }
            lastRotationDirection = currentDirection;
            
            if (!wasRotating) onRotationStart?.Invoke();
            onRotation?.Invoke();
            onRotationDelta?.Invoke(angleDelta);
            onRotationSpeed?.Invoke(lastRotationSpeed);
            
            float t = Mathf.InverseLerp(minAngle, maxAngle, finalAngle);
            onValueChanged?.Invoke(t);
        }
        else if (wasRotating && !isRotating)
        {
            onRotationEnd?.Invoke();
        }
        else if (Mathf.Abs(finalAngle - lastFrameAngle) > 0.001f)
        {
            float t = Mathf.InverseLerp(minAngle, maxAngle, finalAngle);
            onValueChanged?.Invoke(t);
        }
    }

    private float CalculateBoundaryForce(float angle)
    {
        if (angle < minAngle)
        {
            return (minAngle - angle) * boundaryHardness;
        }
        if (angle > maxAngle)
        {
            return -(angle - maxAngle) * boundaryHardness;
        }
        return 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

        cam = eventData.pressEventCamera;
        dragging = true;
        inertiaVelocity = 0f;
        
        onMouseDown?.Invoke();
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect.parent as RectTransform,
            eventData.position,
            cam,
            out lastDragPosition
        );
        
        targetAngle = currentAngle;
        
        if (enableCursorAttachment)
        {
            //DynamicCursor.AttachToObject(gameObject, Vector2.zero);
            DynamicCursor.StartGrab();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentDragPosition;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect.parent as RectTransform,
            eventData.position,
            cam,
            out currentDragPosition
        );
        
        Vector2 fromVector = lastDragPosition - rect.anchoredPosition;
        Vector2 toVector = currentDragPosition - rect.anchoredPosition;
        
        float delta = Vector2.SignedAngle(fromVector, toVector);
        
        delta *= sensitivity;
        if (invertRotation) delta = -delta;
        
        lastDragPosition = currentDragPosition;
        targetAngle += delta;
        targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        DynamicCursor.EndGrab();
        
        if (Mathf.Abs(inertiaVelocity) < 0.5f)
            inertiaVelocity = 0f;
        
        if (enableCursorAttachment)
        {
            //DynamicCursor.Detach();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        /*if (!dragging && !DynamicCursor.IsAttached())
        {
            //DynamicCursor.SetCursor(CursorType.Hand);
        }*/
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!dragging)
        {
            //DynamicCursor.SetCursor(CursorType.Arrow);
        }
    }

    public void SetAngle(float angle, bool instant = true)
    {
        targetAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        
        if (instant)
        {
            currentAngle = targetAngle;
            inertiaVelocity = 0f;
            rect.localRotation = Quaternion.Euler(0, 0, currentAngle);
        }
        
        float t = Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
        onValueChanged?.Invoke(t);
    }

    public float GetCurrentSpeed() => lastRotationSpeed;
    public float GetCurrentAngle() => currentAngle;
    public bool IsRotating() => isRotating;
}