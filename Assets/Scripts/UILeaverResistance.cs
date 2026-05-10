using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Рычаг скорости.
/// Логика:
/// - При зажатой ЛКМ рычаг стремится к позиции курсора по вертикали.
/// - Минимальная скорость движения к цели — всегда, даже если мышь стоит.
/// - Максимальная скорость — когда мышь активно двигается к цели.
/// - Если мышь двигается против направления — рычаг всё равно едет к цели, но с минимальной скоростью.
/// - При первом клике — НЕ телепортируется, начинает ехать к позиции клика.
/// </summary>
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
    public Slider.Direction direction = Slider.Direction.RightToLeft;
    public float minValue = 0f;
    public float maxValue = 100f;
    public float currentValue = 50f;

    [Header("Speed")]
    [Tooltip("Минимальная скорость движения к цели (units/sec) — рычаг всегда едет к курсору")]
    public float minSpeed = 5f;
    [Tooltip("Максимальная скорость когда мышь активно двигается к цели")]
    public float maxSpeed = 80f;
    [Tooltip("Насколько быстро скорость нарастает от мин до макс при движении мыши")]
    public float acceleration = 120f;

    [Header("Movement Events")]
    public float movementThreshold = 0.5f;
    public UnityEngine.Events.UnityEvent onMovementStart;
    public UnityEngine.Events.UnityEvent onMovementStop;
    public UnityEngine.Events.UnityEvent onGrab;
    public UnityEngine.Events.UnityEvent onRelease;

    [Header("Value Events")]
    public UnityEngine.Events.UnityEvent<float> onValueChanged;

    // Runtime
    private bool isDragging;
    private bool isMoving;
    private float lastMovementValue;
    private float movementStopTimer;
    private float stopDelay = 0.1f;

    private Camera cam;
    private RectTransform rect;
    private float targetValue;      // куда едет рычаг (позиция курсора в value-пространстве)
    private float currentSpeed;     // текущая скорость движения к цели
    private float lastInvokedValue;

    void Awake()
    {
        rect = GetComponent<RectTransform>();

        if (handle == null)
        {
            handle = GetComponentInChildren<Image>()?.GetComponent<RectTransform>();
            if (handle == null)
                Debug.LogError("UILeaverResistance: Handle not assigned!");
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
        if (isDragging)
        {
            MoveTowardTarget();
            TrackMovement();
        }
    }

    // ── Движение к цели ─────────────────────────────────────

    private void MoveTowardTarget()
    {
        float diff = targetValue - currentValue;
        if (Mathf.Abs(diff) < 0.01f)
        {
            currentSpeed = minSpeed;
            return;
        }

        // Направление к цели
        float direction = Mathf.Sign(diff);

        // Скорость нарастает до maxSpeed, не падает ниже minSpeed
        currentSpeed = Mathf.Clamp(currentSpeed + acceleration * Time.deltaTime, minSpeed, maxSpeed);

        float step = currentSpeed * Time.deltaTime;
        currentValue = Mathf.MoveTowards(currentValue, targetValue, step);
        currentValue = Mathf.Clamp(currentValue, minValue, maxValue);

        UpdateHandlePosition();
        FireValueChanged();
    }

    // ── Pointer Events ────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }

    public void OnPointerDown(PointerEventData eventData)
    {
        cam = eventData.pressEventCamera;
        isDragging = true;
        isMoving = false;
        currentSpeed = minSpeed;
        movementStopTimer = 0f;
        lastMovementValue = currentValue;

        // Устанавливаем цель по позиции клика — но НЕ телепортируемся
        targetValue = GetValueFromPointer(eventData);

        DynamicCursor.StartGrab();
        onGrab?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float newTarget = GetValueFromPointer(eventData);
        float mouseDelta = GetMouseDelta(eventData.delta);

        // Если мышь двигается к цели — ускоряемся
        // Если против — сбрасываем скорость до минимума
        float dirToTarget = Mathf.Sign(newTarget - currentValue);
        float mouseDir = Mathf.Sign(mouseDelta);

        if (Mathf.Abs(mouseDelta) > 0.1f && dirToTarget != 0f && mouseDir == dirToTarget)
        {
            // Мышь движется к цели — разгоняемся
            currentSpeed = Mathf.Clamp(currentSpeed + acceleration * Time.deltaTime, minSpeed, maxSpeed);
        }
        else
        {
            // Мышь стоит или движется против — минимальная скорость
            currentSpeed = minSpeed;
        }

        targetValue = newTarget;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        currentSpeed = minSpeed;

        if (isMoving)
        {
            onMovementStop?.Invoke();
            isMoving = false;
        }

        DynamicCursor.EndGrab();
        onRelease?.Invoke();
    }

    // ── Вспомогательные ──────────────────────────────────────

    /// <summary>Читает позицию курсора и возвращает value в диапазоне min..max</summary>
    private float GetValueFromPointer(PointerEventData eventData)
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

        // SpeedLeaver повёрнут на 270° — X в локальных = вертикаль на экране
        // Движение вверх = больше скорости → нормализуем от xMax к xMin (инверсия)
        float normalized = Mathf.Clamp01((localPoint.x - bounds.xMin) / bounds.width);
        // Инвертируем: верх = max
        normalized = 1f - normalized;

        return Mathf.Lerp(minValue, maxValue, normalized);
    }

    /// <summary>Читает вертикальное смещение мыши с учётом поворота объекта</summary>
    private float GetMouseDelta(Vector2 screenDelta)
    {
        // SpeedLeaver повёрнут 270° — вертикальное движение мыши = Y
        return screenDelta.y;
    }

    private void TrackMovement()
    {
        float movement = Mathf.Abs(currentValue - lastMovementValue);
        if (movement > movementThreshold)
        {
            if (!isMoving)
            {
                isMoving = true;
                onMovementStart?.Invoke();
            }
            movementStopTimer = 0f;
            lastMovementValue = currentValue;
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
    }

    private void FireValueChanged()
    {
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

        // SpeedLeaver повёрнут 270° — двигаем по X (= вертикаль на экране)
        // maxValue = xMin (верх), minValue = xMax (низ) — инверсия
        anchoredPos.x = Mathf.Lerp(bounds.xMax, bounds.xMin, t);

        handle.anchoredPosition = anchoredPos;
    }

    public void SetValue(float value, bool invokeEvent = true)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        targetValue = currentValue;
        UpdateHandlePosition();
        if (invokeEvent) FireValueChanged();
    }

    public float GetValue() => currentValue;
}
