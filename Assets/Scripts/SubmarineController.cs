using UnityEngine;

/// <summary>
/// Движение подводной лодки в мировом пространстве.
/// Читает UIRotary (руль) и UILeaverResistance (рычаг скорости).
/// </summary>
public class SubmarineController : MonoBehaviour
{
    [Header("References")]
    public UIRotary           steeringWheel;
    public UILeaverResistance speedLever;

    [Header("Movement")]
    [Tooltip("Максимальная скорость (units/sec)")]
    public float maxSpeed        = 1.5f;
    [Tooltip("Время плавного набора скорости")]
    public float speedSmoothTime = 1.5f;
    [Tooltip("Время торможения (меньше = резче останавливается)")]
    public float brakeSmoothTime = 0.3f;
    [Tooltip("Макс. угловая скорость поворота (deg/sec) при полном руле")]
    public float maxTurnRate     = 90f;
    [Tooltip("Плавность отклика руля")]
    public float steerSmoothTime = 0.15f;

    // Внутреннее состояние
    float _speed, _speedV;
    float _turnRate, _turnV;
    float _heading; // градусы, 0 = вверх (север)

    // Публичные свойства для других систем
    public float   CurrentSpeed => _speed;
    public float   HeadingAngle => _heading;
    public Vector2 Forward      => new Vector2(
        Mathf.Sin(_heading * Mathf.Deg2Rad),
        Mathf.Cos(_heading * Mathf.Deg2Rad));

    void Start()
    {
        _heading = -transform.eulerAngles.z;
    }

    void Update()
    {
        UpdateSpeed();
        UpdateTurn();
        ApplyMotion();
    }

    void UpdateSpeed()
    {
        float target = 0f;
        if (speedLever != null)
            target = (speedLever.currentValue / speedLever.maxValue) * maxSpeed;

        // Разные времена для разгона и торможения
        float smoothTime = (target < _speed) ? brakeSmoothTime : speedSmoothTime;
        _speed = Mathf.SmoothDamp(_speed, target, ref _speedV, smoothTime);
    }

    void UpdateTurn()
    {
        float wheelNorm = 0f;
        if (steeringWheel != null)
        {
            float angle = steeringWheel.GetCurrentAngle();
            float maxA  = Mathf.Max(Mathf.Abs(steeringWheel.minAngle),
                                    Mathf.Abs(steeringWheel.maxAngle));
            wheelNorm = Mathf.Clamp(angle / maxA, -1f, 1f);
        }

        // На месте поворот чуть слабее (0.4), на полной скорости — полный (1.0)
        float speedFactor = Mathf.Lerp(1f, 1f, Mathf.Clamp01(Mathf.Abs(_speed) / maxSpeed));
        float targetTurn  = -wheelNorm * maxTurnRate * speedFactor;;

        _turnRate = Mathf.SmoothDamp(_turnRate, targetTurn, ref _turnV, steerSmoothTime);
    }

    void ApplyMotion()
    {
        _heading += _turnRate * Time.deltaTime;

        Vector2 move = Forward * _speed * Time.deltaTime;
        transform.position += new Vector3(move.x, move.y, 0f);

        transform.rotation = Quaternion.Euler(0f, 0f, -_heading);
    }

    /// <summary>Телепортировать субмарину и задать курс (при загрузке уровня)</summary>
    public void Warp(Vector2 position, float headingDeg)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        _heading  = headingDeg;
        _speed    = 0f;
        _speedV   = 0f;
        _turnRate = 0f;
        _turnV    = 0f;
        transform.rotation = Quaternion.Euler(0f, 0f, -_heading);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float h = Application.isPlaying ? _heading : -transform.eulerAngles.z;
        var fwd = new Vector3(Mathf.Sin(h * Mathf.Deg2Rad), Mathf.Cos(h * Mathf.Deg2Rad), 0f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + fwd * 2f);
    }
#endif
}
