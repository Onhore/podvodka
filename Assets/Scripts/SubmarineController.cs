using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    [Header("Auto Find")]
    [SerializeField] private bool autoFindSceneObjectsOnAwake = true;
    [SerializeField] private bool includeInactiveObjects = true;

    [Header("Mirror Objects")]
    [SerializeField] private MirrorObjectMover[] mirrorObjects;

    [Header("Pushback")]
    [SerializeField] private bool useBoundaryPushback = true;

    [Header("Rotation Lock")]
    [SerializeField] private bool lockRotationOnCollision = true;
    [SerializeField]
    private float rotationLockContactDelay = 0.15f;

    private float collisionContactTimer;
    [SerializeField]
    private float rotationLockDuration = 0.25f;

    private float rotationLockTimer;

    [SerializeField]
    private int pushbackIterations = 4;

    [SerializeField]
    private float pushbackStrength = 1.05f;

    [SerializeField]
    private float pushbackStep = 0.05f;

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

    [Header("Collision Check")]
    [SerializeField] private int collisionSweepSteps = 8;
    [SerializeField] private bool checkCollisionEvenWhenStopped = true;

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

    void Awake()
    {
        if (autoFindSceneObjectsOnAwake)
            FindSceneObjects();
    }

    void Start()
    {
        if (worldPivot != null)
            _pivotStartLocalPosition = worldPivot.localPosition;

        if (submarineVisual != null)
            _submarineStartLocalPosition = submarineVisual.localPosition;

        UpdateMirrorObjects();
    }
    private bool rotationWasLockedByCollision;
    void Update()
    {
        UpdateHeading();
        UpdateSpeed();

        // Сначала обновляем визуал мира
        ApplyVisuals();

        // Потом обновляем динамичные mirror/boundary объекты
        UpdateMirrorObjects();

        // Проверяем:
        // не въехала ли динамичная стена в игрока
        if (!IsInsideAnyBoundary(_logicalPosition))
        {
            if (useBoundaryPushback)
            {
                ResolveBoundaryPenetration();
            }

            _currentSpeed = 0f;
            _speedVelocity = 0f;

            collisionContactTimer += Time.deltaTime;

            if (
                lockRotationOnCollision &&
                !rotationWasLockedByCollision &&
                collisionContactTimer >= rotationLockContactDelay
            )
            {
                rotationLockTimer = rotationLockDuration;
                rotationWasLockedByCollision = true;
            }

            if (IsInsideAnyBoundary(_logicalPosition))
            {
                collisionContactTimer = 0f;
            }
        }

        // Теперь двигаем игрока
        UpdateLogicalPosition();

        // После движения ещё раз обновляем мир
        ApplyVisuals();

        // И ещё раз обновляем mirror объекты
        UpdateMirrorObjects();
        if (useBoundaryPushback)
            ResolveBoundaryPushback();
        // Финальная проверка после всех движений
        if (!IsInsideAnyBoundary(_logicalPosition))
        {
            _currentSpeed = 0f;
            _speedVelocity = 0f;
        }
    }

    void FindSceneObjects()
    {
        if (includeInactiveObjects)
        {
            mirrorObjects = FindObjectsOfType<MirrorObjectMover>(true);
            levelBoundaries = FindObjectsOfType<LevelBoundary>(true);
        }
        else
        {
            mirrorObjects = FindObjectsOfType<MirrorObjectMover>();
            levelBoundaries = FindObjectsOfType<LevelBoundary>();
        }
    }

    void UpdateHeading()
    {
        if (rotationLockTimer > 0f)
        {
            rotationLockTimer -= Time.deltaTime;

            _currentTurnRate = 0f;

            if (rotationLockTimer <= 0f)
                rotationWasLockedByCollision = false;

            return;
        }


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
        {
            if (checkCollisionEvenWhenStopped && !IsInsideAnyBoundary(_logicalPosition))
            {
                _currentSpeed = 0f;
                _speedVelocity = 0f;
            }

            return;
        }

        float rad = _logicalHeading * Mathf.Deg2Rad;

        Vector2 forward = new Vector2(
            -Mathf.Sin(rad),
             Mathf.Cos(rad)
        );

        Vector2 movement = forward * _currentSpeed * Time.deltaTime;
        Vector2 nextPosition = _logicalPosition + movement;

        if (!IsPathClear(_logicalPosition, nextPosition))
        {
            _currentSpeed = 0f;
            _speedVelocity = 0f;
            return;
        }

        _logicalPosition = nextPosition;
    }

    bool IsPathClear(Vector2 from, Vector2 to)
    {
        int steps = Mathf.Max(1, collisionSweepSteps);

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 checkPosition = Vector2.Lerp(from, to, t);

            if (!IsInsideAnyBoundary(checkPosition))
                return false;
        }

        return true;
    }

    bool IsInsideAnyBoundary(Vector2 position)
    {
        if (levelBoundaries == null || levelBoundaries.Length == 0)
            return true;

        for (int i = 0; i < levelBoundaries.Length; i++)
        {
            LevelBoundary boundary = levelBoundaries[i];

            if (boundary == null)
                continue;

            if (!boundary.gameObject.activeInHierarchy)
                continue;

            if (!boundary.enabled)
                continue;

            if (!boundary.IsInsideBoundary(position))
                return false;
        }

        return true;
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

    void UpdateMirrorObjects()
    {
        if (mirrorObjects == null)
            return;

        float worldTurnRate = -_currentTurnRate;

        for (int i = 0; i < mirrorObjects.Length; i++)
        {
            if (mirrorObjects[i] == null)
                continue;

            mirrorObjects[i].SetRadarState(
                _logicalPosition,
                _logicalHeading,
                worldTurnRate,
                Mathf.Abs(_currentSpeed)
            );
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

        ResetMirrorObjects();
        UpdateMirrorObjects();
    }

    void ResetMirrorObjects()
    {
        if (mirrorObjects == null)
            return;

        for (int i = 0; i < mirrorObjects.Length; i++)
        {
            if (mirrorObjects[i] == null)
                continue;

            mirrorObjects[i].ResetMirror();
        }
    }

    void ResolveBoundaryPenetration()
    {
        Vector2 original = _logicalPosition;

        for (int i = 0; i < pushbackIterations; i++)
        {
            if (IsInsideAnyBoundary(_logicalPosition))
                return;

            Vector2 bestDirection = Vector2.zero;
            float bestDistance = float.MaxValue;

            Vector2[] directions =
            {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

            for (int d = 0; d < directions.Length; d++)
            {
                Vector2 test =
                    _logicalPosition +
                    directions[d] * pushbackStep;

                if (IsInsideAnyBoundary(test))
                {
                    float dist = Vector2.Distance(original, test);

                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestDirection = directions[d];
                    }
                }
            }

            if (bestDirection == Vector2.zero)
                return;

            _logicalPosition += bestDirection * pushbackStep;
        }
    }
    void ResolveBoundaryPushback()
    {
        if (levelBoundaries == null)
            return;

        for (int iteration = 0; iteration < pushbackIterations; iteration++)
        {
            Vector2 totalPush = Vector2.zero;

            for (int i = 0; i < levelBoundaries.Length; i++)
            {
                LevelBoundary boundary = levelBoundaries[i];

                if (boundary == null)
                    continue;

                if (!boundary.gameObject.activeInHierarchy)
                    continue;

                if (!boundary.enabled)
                    continue;

                if (boundary.TryGetPushOut(_logicalPosition, out Vector2 push))
                    totalPush += push;
            }

            if (totalPush.sqrMagnitude < 0.000001f)
                return;

            _logicalPosition += totalPush * pushbackStrength;
        }
    }
}