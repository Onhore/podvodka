using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    [Header("References")]
    public UIRotary steeringWheel;
    public UILeaverResistance speedLever;
    public LevelBoundary[] levelBoundaries;

    public Transform submarineVisual;
    public Transform worldPivot;
    public Transform worldRotator;
    public Transform worldContent;

    [Header("Speed")]
    public float maxSpeed = 1.5f;
    public float acceleration = 1.5f;
    public float braking = 0.25f;

    [Header("Rotation")]
    public float maxTurnRate = 60f;
    public float turnAcceleration = 6f;

    [Header("Options")]
    public bool invertSteering = false;

    float _currentSpeed;
    float _speedVelocity;
    float _currentTurnRate;

    float _logicalHeading;
    Vector2 _logicalPosition;

    Vector3 _pivotStartLocalPosition;
    Vector3 _submarineStartLocalPosition;

    public float CurrentSpeed => _currentSpeed;
    public float LogicalHeading => _logicalHeading;
    public Vector2 LogicalPosition => _logicalPosition;

    void Start()
    {
        if (worldPivot != null)
            _pivotStartLocalPosition = worldPivot.localPosition;

        if (submarineVisual != null)
            _submarineStartLocalPosition = submarineVisual.localPosition;
    }

    void Update()
    {
        UpdateHeading();
        UpdateSpeed();
        UpdateLogicalPosition();
        ApplyVisuals();
    }

    void UpdateHeading()
    {
        float wheelNorm = 0f;

        if (steeringWheel != null)
        {
            float angle = steeringWheel.GetCurrentAngle();

            float maxAngle = Mathf.Max(
                Mathf.Abs(steeringWheel.minAngle),
                Mathf.Abs(steeringWheel.maxAngle)
            );

            if (maxAngle > 0.001f)
                wheelNorm = Mathf.Clamp(angle / maxAngle, -1f, 1f);
        }

        if (invertSteering)
            wheelNorm *= -1f;

        float targetTurnRate = wheelNorm * maxTurnRate;

        _currentTurnRate = Mathf.MoveTowards(
            _currentTurnRate,
            targetTurnRate,
            turnAcceleration * maxTurnRate * Time.deltaTime
        );

        _logicalHeading += _currentTurnRate * Time.deltaTime;
    }

    void UpdateSpeed()
    {
        float targetSpeed = 0f;

        if (speedLever != null && Mathf.Abs(speedLever.maxValue) > 0.001f)
            targetSpeed = Mathf.Clamp01(speedLever.currentValue / speedLever.maxValue) * maxSpeed;

        if (targetSpeed < 0.01f)
            targetSpeed = 0f;

        float smoothTime = targetSpeed > _currentSpeed ? acceleration : braking;

        _currentSpeed = Mathf.SmoothDamp(
            _currentSpeed,
            targetSpeed,
            ref _speedVelocity,
            smoothTime
        );

        if (targetSpeed == 0f && Mathf.Abs(_currentSpeed) < 0.01f)
        {
            _currentSpeed = 0f;
            _speedVelocity = 0f;
        }
    }

    void UpdateLogicalPosition()
    {
        if (Mathf.Abs(_currentSpeed) < 0.0001f)
            return;

        float rad = _logicalHeading * Mathf.Deg2Rad;

        Vector2 forward = new Vector2(
            -Mathf.Sin(rad),
             Mathf.Cos(rad)
        );

        Vector2 movement = forward * _currentSpeed * Time.deltaTime;
        Vector2 nextPosition = _logicalPosition + movement;

        if (!IsInsideAnyBoundary(nextPosition))
        {
            // Мягкий удар о стену:
            // позицию не меняем, скорость режем, управление оставляем.
            _currentSpeed *= 0.25f;
            _speedVelocity = 0f;
            return;
        }

        _logicalPosition = nextPosition;
    }
    bool IsInsideAnyBoundary(Vector2 position)
    {
        if (levelBoundaries == null || levelBoundaries.Length == 0)
            return true;

        for (int i = 0; i < levelBoundaries.Length; i++)
        {
            if (levelBoundaries[i] == null)
                continue;

            if (levelBoundaries[i].IsInsideBoundary(position))
                return true;
        }

        return false;
    }
    void ApplyVisuals()
    {
        if (worldPivot != null)
        {
            worldPivot.localPosition = _pivotStartLocalPosition;
            worldPivot.localRotation = Quaternion.identity;
        }

        if (submarineVisual != null)
        {
            submarineVisual.localPosition = _submarineStartLocalPosition;
            submarineVisual.localRotation = Quaternion.identity;
        }

        if (worldRotator != null)
        {
            worldRotator.localPosition = Vector3.zero;
            worldRotator.localRotation = Quaternion.Euler(0f, 0f, -_logicalHeading);
        }

        if (worldContent != null)
        {
            worldContent.localPosition = new Vector3(
                -_logicalPosition.x,
                -_logicalPosition.y,
                0f
            );

            worldContent.localRotation = Quaternion.identity;
        }
    }

    public void ResetWorld()
    {
        _currentSpeed = 0f;
        _speedVelocity = 0f;
        _currentTurnRate = 0f;

        _logicalHeading = 0f;
        _logicalPosition = Vector2.zero;

        if (worldPivot != null)
        {
            worldPivot.localPosition = _pivotStartLocalPosition;
            worldPivot.localRotation = Quaternion.identity;
        }

        if (worldRotator != null)
        {
            worldRotator.localPosition = Vector3.zero;
            worldRotator.localRotation = Quaternion.identity;
        }

        if (worldContent != null)
        {
            worldContent.localPosition = Vector3.zero;
            worldContent.localRotation = Quaternion.identity;
        }

        if (submarineVisual != null)
        {
            submarineVisual.localPosition = _submarineStartLocalPosition;
            submarineVisual.localRotation = Quaternion.identity;
        }
    }
}