using UnityEngine;

/// <summary>
/// Управляет движением подводной лодки в мировом пространстве.
/// Получает входные данные от UIRotary (руль) и UILeaverResistance (рычаг скорости).
/// </summary>
public class SubmarineController : MonoBehaviour
{
    [Header("References")]
    public UIRotary steeringWheel;
    public UILeaverResistance speedLever;

    [Header("Movement Settings")]
    [Tooltip("Максимальная скорость в юнитах/сек")]
    public float maxSpeed = 5f;

    [Tooltip("Плавность набора/сброса скорости (меньше = плавнее)")]
    public float speedSmoothTime = 1.5f;

    [Tooltip("Множитель поворота от руля (градусов/сек на единицу угла руля)")]
    public float steerStrength = 60f;

    [Tooltip("Плавность поворота (меньше = плавнее)")]
    public float steerSmoothTime = 0.3f;

    // Текущие значения
    private float currentSpeed;
    private float targetSpeed;
    private float speedVelocity;

    private float currentTurnRate;
    private float turnRateVelocity;

    // Текущий угол движения в мировом пространстве (0 = вверх/север)
    private float headingAngle;

    // Публичные свойства для других систем
    public float CurrentSpeed => currentSpeed;
    public float HeadingAngle => headingAngle;
    public Vector2 ForwardDirection => new Vector2(
        Mathf.Sin(headingAngle * Mathf.Deg2Rad),
        Mathf.Cos(headingAngle * Mathf.Deg2Rad)
    );

    void Start()
    {
        headingAngle = transform.eulerAngles.z;
    }

    void Update()
    {
        UpdateTargetSpeed();
        UpdateTurnRate();
        ApplyMovement();
    }

    private void UpdateTargetSpeed()
    {
        if (speedLever == null) return;

        // Рычаг: 0-100, где 100 = максимальная скорость
        float leverNormalized = speedLever.GetValue() / 100f;
        targetSpeed = leverNormalized * maxSpeed;

        // Плавный набор скорости
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);
    }

    private void UpdateTurnRate()
    {
        if (steeringWheel == null) return;

        // Руль: -120..+120 градусов → скорость поворота
        float wheelAngle = steeringWheel.GetCurrentAngle(); // -120 до +120
        float normalizedWheel = wheelAngle / 120f;          // -1 до +1

        // Поворот зависит от скорости — на нулевой скорости не поворачиваем
        float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
        float targetTurnRate = -normalizedWheel * steerStrength * speedFactor;

        currentTurnRate = Mathf.SmoothDamp(currentTurnRate, targetTurnRate, ref turnRateVelocity, steerSmoothTime);
    }

    private void ApplyMovement()
    {
        // Поворачиваем курс
        headingAngle += currentTurnRate * Time.deltaTime;

        // Двигаем субмарину вперёд по текущему курсу
        Vector2 movement = ForwardDirection * currentSpeed * Time.deltaTime;
        transform.position += new Vector3(movement.x, movement.y, 0f);

        // Визуально поворачиваем спрайт субмарины по курсу
        transform.rotation = Quaternion.Euler(0f, 0f, -headingAngle);
    }

    /// <summary>
    /// Вызывается извне (например, при столкновении) для нанесения импульса
    /// </summary>
    public void ApplyImpulse(Vector2 force)
    {
        transform.position += new Vector3(force.x, force.y, 0f);
    }

    /// <summary>
    /// Мгновенно установить курс (используется при загрузке уровня)
    /// </summary>
    public void SetHeading(float angle)
    {
        headingAngle = angle;
        transform.rotation = Quaternion.Euler(0f, 0f, -headingAngle);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Показываем направление движения в редакторе
        Gizmos.color = Color.cyan;
        Vector2 fwd = new Vector2(
            Mathf.Sin(headingAngle * Mathf.Deg2Rad),
            Mathf.Cos(headingAngle * Mathf.Deg2Rad)
        );
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(fwd.x, fwd.y, 0) * 2f);
    }
#endif
}
