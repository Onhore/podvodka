using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MirrorObjectMover : MonoBehaviour
{
    public enum MirrorResponse
    {
        [InspectorName("-1 | обратно")]
        Backward = -1,

        [InspectorName("0 | не двигать")]
        None = 0,

        [InspectorName("1 | прямо")]
        Forward = 1
    }

    public enum EffectSource
    {
        [InspectorName("Выключено")]
        Off = 0,

        [InspectorName("От движения")]
        Movement = 1,

        [InspectorName("От вращения")]
        Rotation = 2,

        [InspectorName("От движения + вращения")]
        MovementAndRotation = 3
    }

    [Header("Player Blocking")]
    public bool stopMovementWhenTouchingPlayer = true;
    public bool autoFindBlockingBoundaries = true;
    [SerializeField] private LevelBoundary[] ownBoundaries;

    [Header("Crush Escape")]
    [Tooltip("Если стена долго упирается в игрока, она временно уходит назад.")]
    public bool enableCrushEscape = true;

    [Tooltip("Сколько секунд стена может давить игрока перед forced return.")]
    [Range(0.1f, 10f)]
    public float crushTimeToForceReturn = 1.5f;

    [Tooltip("Сколько секунд длится forced return. Если стена успеет вернуться раньше — режим закончится раньше.")]
    [Range(0.05f, 10f)]
    public float forcedReturnDuration = 0.75f;

    [Tooltip("Скорость forced return позиции.")]
    [Range(0.1f, 100f)]
    public float forcedReturnMoveSpeed = 15f;

    [Tooltip("Скорость forced return вращения.")]
    [Range(1f, 720f)]
    public float forcedReturnRotationSpeed = 240f;

    [Header("Hierarchy")]
    public Transform visual;
    public Transform roomAnchor;

    [Header("Source")]
    [SerializeField] private Vector2 logicalPosition;
    [SerializeField] private float logicalHeading;
    [SerializeField] private float worldTurnRate;
    [SerializeField] private float playerSpeed;

    [Header("Movement Speed")]
    [Range(-5f, 5f)]
    public float moveSpeedMultiplier = 1f;

    [Header("Mirror Response X")]
    public MirrorResponse worldLeft_ObjectX = MirrorResponse.Forward;
    public MirrorResponse worldRight_ObjectX = MirrorResponse.Backward;

    [Header("Mirror Response Y")]
    public MirrorResponse worldUp_ObjectY = MirrorResponse.Backward;
    public MirrorResponse worldDown_ObjectY = MirrorResponse.Forward;

    [Header("Cross Axis Optional")]
    public MirrorResponse worldLeft_ObjectY = MirrorResponse.None;
    public MirrorResponse worldRight_ObjectY = MirrorResponse.None;
    public MirrorResponse worldUp_ObjectX = MirrorResponse.None;
    public MirrorResponse worldDown_ObjectX = MirrorResponse.None;

    [Header("Rotation Speed")]
    [Range(-1f, 5f)]
    public float rotationSpeedMultiplier = 0f;

    [Header("Mirror Rotation")]
    public MirrorResponse clockwiseRotation = MirrorResponse.Forward;
    public MirrorResponse counterClockwiseRotation = MirrorResponse.Forward;

    [Header("Scale Effect")]
    public EffectSource scaleSource = EffectSource.Off;

    [Range(-5f, 5f)]
    public float scaleMultiplier = 0f;

    [Range(0.1f, 5f)]
    public float minScale = 0.5f;

    [Range(0.1f, 5f)]
    public float maxScale = 1.5f;

    [Tooltip("Плавность изменения размера. Чем больше — тем быстрее.")]
    [Range(0.1f, 30f)]
    public float scaleSmoothSpeed = 8f;

    [Header("Color Effect")]
    public EffectSource colorSource = EffectSource.Off;

    public Color idleColor = Color.white;
    public Color activeColor = Color.cyan;

    [Range(0f, 10f)]
    public float colorMultiplier = 1f;

    [Header("Circular Room")]
    [Range(0f, 100f)]
    public float roomRadius = 80f;

    [Header("Activation Zone")]
    [Range(0f, 500f)]
    public float activationRadius = 150f;

    public bool returnToStartOutsideZone = true;

    [Range(0f, 20f)]
    public float returnSpeedMultiplier = 5f;

    [Range(0f, 200f)]
    public float returnRotationSpeedMultiplier = 60f;

    [Header("Editor View")]
    public bool drawRoom = true;
    public bool drawActivationZone = true;

    [Range(0.02f, 0.5f)]
    public float roomFillAlpha = 0.15f;

    [Range(0.1f, 1f)]
    public float roomBorderAlpha = 0.8f;

    [Range(0.02f, 0.5f)]
    public float activationFillAlpha = 0.12f;

    [Range(0.1f, 1f)]
    public float activationBorderAlpha = 0.9f;

    private Vector3 startRootLocalPosition;
    private Quaternion startVisualLocalRotation;
    private Vector3 startVisualLocalScale;

    private Vector2 lastLogicalPosition;
    private bool hasRadarSample;

    private Vector2 mirrorOffset;
    private float mirrorRotationOffset;

    private float crushTimer;
    private bool forcedReturning;
    private float forcedReturnTimer;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (visual == null)
            visual = transform.childCount > 0 ? transform.GetChild(0) : transform;

        startRootLocalPosition = transform.localPosition;
        startVisualLocalRotation = visual.localRotation;
        startVisualLocalScale = visual.localScale;

        spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (autoFindBlockingBoundaries)
            ownBoundaries = GetComponentsInChildren<LevelBoundary>(true);

        lastLogicalPosition = logicalPosition;
        hasRadarSample = false;
    }

    public void SetRadarState(
        Vector2 position,
        float heading,
        float currentWorldTurnRate,
        float currentPlayerSpeed
    )
    {
        logicalPosition = position;
        logicalHeading = heading;
        worldTurnRate = currentWorldTurnRate;
        playerSpeed = currentPlayerSpeed;

        if (!hasRadarSample)
        {
            lastLogicalPosition = logicalPosition;
            hasRadarSample = true;
            return;
        }

        ApplyMirror();
    }

    public void ResetMirror()
    {
        mirrorOffset = Vector2.zero;
        mirrorRotationOffset = 0f;

        crushTimer = 0f;
        forcedReturnTimer = 0f;
        forcedReturning = false;

        hasRadarSample = false;
        lastLogicalPosition = logicalPosition;

        transform.localPosition = GetRoomCenterLocal();

        if (visual != null)
        {
            visual.localRotation = startVisualLocalRotation;
            visual.localScale = startVisualLocalScale;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = idleColor;
    }

    private void ApplyMirror()
    {
        Vector2 logicalDelta = logicalPosition - lastLogicalPosition;
        lastLogicalPosition = logicalPosition;

        if (forcedReturning)
        {
            TickForcedReturn();
            return;
        }

        Vector2 radarDelta = GetRadarMovementDelta(logicalDelta, logicalHeading);

        bool insideActivationZone = IsInsideActivationZone();

        if (!insideActivationZone && returnToStartOutsideZone)
        {
            crushTimer = 0f;
            ReturnToStart();
            return;
        }

        bool movementWasBlocked = ApplyRootMovement(radarDelta);

        if (movementWasBlocked)
        {
            HandleCrushTimer();
            return;
        }

        crushTimer = 0f;

        ApplyVisualRotation();
        ApplyVisualScaleAndColor(radarDelta);
    }

    private void HandleCrushTimer()
    {
        if (!enableCrushEscape)
            return;

        crushTimer += Time.deltaTime;

        if (crushTimer >= crushTimeToForceReturn)
            BeginForcedReturn();
    }

    private void BeginForcedReturn()
    {
        forcedReturning = true;
        forcedReturnTimer = 0f;
        crushTimer = 0f;
    }

    private void TickForcedReturn()
    {
        forcedReturnTimer += Time.deltaTime;

        StepReturnToStart(
            forcedReturnMoveSpeed,
            forcedReturnRotationSpeed
        );

        bool reachedStart = IsReturnedToStart();
        bool timeExpired = forcedReturnTimer >= forcedReturnDuration;

        if (reachedStart || timeExpired)
        {
            forcedReturning = false;
            forcedReturnTimer = 0f;
            crushTimer = 0f;
        }
    }

    private bool IsReturnedToStart()
    {
        bool positionDone = mirrorOffset.sqrMagnitude < 0.0001f;
        bool rotationDone = Mathf.Abs(mirrorRotationOffset) < 0.1f;

        bool scaleDone = true;

        if (visual != null)
            scaleDone = Vector3.Distance(visual.localScale, startVisualLocalScale) < 0.001f;

        return positionDone && rotationDone && scaleDone;
    }

    private Vector2 GetRadarMovementDelta(Vector2 logicalDelta, float heading)
    {
        float amount = logicalDelta.magnitude;

        if (amount < 0.000001f)
            return Vector2.zero;

        float rad = heading * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Sin(rad),
            Mathf.Cos(rad)
        ) * amount;
    }

    private bool ApplyRootMovement(Vector2 radarDelta)
    {
        Vector2 movementReaction = Vector2.zero;

        if (radarDelta.x < -0.000001f)
        {
            float amount = -radarDelta.x;

            movementReaction += Vector2.left * amount * GetResponseValue(worldLeft_ObjectX);
            movementReaction += Vector2.up * amount * GetResponseValue(worldLeft_ObjectY);
        }
        else if (radarDelta.x > 0.000001f)
        {
            float amount = radarDelta.x;

            movementReaction += Vector2.right * amount * GetResponseValue(worldRight_ObjectX);
            movementReaction += Vector2.up * amount * GetResponseValue(worldRight_ObjectY);
        }

        if (radarDelta.y > 0.000001f)
        {
            float amount = radarDelta.y;

            movementReaction += Vector2.right * amount * GetResponseValue(worldUp_ObjectX);
            movementReaction += Vector2.up * amount * GetResponseValue(worldUp_ObjectY);
        }
        else if (radarDelta.y < -0.000001f)
        {
            float amount = -radarDelta.y;

            movementReaction += Vector2.right * amount * GetResponseValue(worldDown_ObjectX);
            movementReaction += Vector2.down * amount * GetResponseValue(worldDown_ObjectY);
        }

        Vector2 oldOffset = mirrorOffset;
        Vector3 oldLocalPosition = transform.localPosition;

        mirrorOffset += movementReaction * moveSpeedMultiplier;

        if (mirrorOffset.magnitude > roomRadius)
            mirrorOffset = mirrorOffset.normalized * roomRadius;

        Vector3 roomCenter = GetRoomCenterLocal();

        transform.localPosition = roomCenter + new Vector3(
            mirrorOffset.x,
            mirrorOffset.y,
            0f
        );

        if (stopMovementWhenTouchingPlayer && IsTouchingPlayer())
        {
            mirrorOffset = oldOffset;
            transform.localPosition = oldLocalPosition;
            return true;
        }

        return false;
    }

    private void ApplyVisualRotation()
    {
        if (visual == null)
            return;

        if (Mathf.Abs(worldTurnRate) < 0.0001f)
            return;

        float speedFactor = Mathf.Max(0f, 1f + rotationSpeedMultiplier);

        float response = 0f;

        if (worldTurnRate < 0f)
            response = GetResponseValue(clockwiseRotation);
        else if (worldTurnRate > 0f)
            response = GetResponseValue(counterClockwiseRotation);

        float rotationDelta =
            worldTurnRate *
            response *
            speedFactor *
            Time.deltaTime;

        mirrorRotationOffset += rotationDelta;

        visual.localRotation =
            startVisualLocalRotation *
            Quaternion.Euler(0f, 0f, mirrorRotationOffset);
    }

    private void ApplyVisualScaleAndColor(Vector2 radarDelta)
    {
        if (visual == null)
            return;

        float scalePower = GetEffectPower(scaleSource, radarDelta);
        float colorPower = GetEffectPower(colorSource, radarDelta);

        if (scaleSource != EffectSource.Off)
        {
            float targetScaleFactor = 1f + scalePower * scaleMultiplier;

            targetScaleFactor = Mathf.Clamp(
                targetScaleFactor,
                minScale,
                maxScale
            );

            Vector3 targetScale = startVisualLocalScale * targetScaleFactor;

            visual.localScale = Vector3.Lerp(
                visual.localScale,
                targetScale,
                1f - Mathf.Exp(-scaleSmoothSpeed * Time.deltaTime)
            );
        }

        if (colorSource != EffectSource.Off && spriteRenderer != null)
        {
            float t = Mathf.Clamp01(colorPower * colorMultiplier);

            spriteRenderer.color = Color.Lerp(
                idleColor,
                activeColor,
                t
            );
        }
    }

    private bool IsTouchingPlayer()
    {
        if (ownBoundaries == null || ownBoundaries.Length == 0)
            return false;

        for (int i = 0; i < ownBoundaries.Length; i++)
        {
            LevelBoundary boundary = ownBoundaries[i];

            if (boundary == null)
                continue;

            if (!boundary.gameObject.activeInHierarchy)
                continue;

            if (!boundary.enabled)
                continue;

            if (!boundary.IsInsideBoundary(logicalPosition))
                return true;
        }

        return false;
    }

    private bool IsInsideActivationZone()
    {
        if (activationRadius <= 0f)
            return true;

        Vector2 activationCenter = GetActivationCenterLogical();

        float distance = Vector2.Distance(
            logicalPosition,
            activationCenter
        );

        return distance <= activationRadius;
    }

    private Vector2 GetActivationCenterLogical()
    {
        if (roomAnchor != null)
        {
            return new Vector2(
                roomAnchor.localPosition.x,
                roomAnchor.localPosition.y
            );
        }

        return new Vector2(
            startRootLocalPosition.x,
            startRootLocalPosition.y
        );
    }

    private void ReturnToStart()
    {
        if (returnSpeedMultiplier <= 0f && returnRotationSpeedMultiplier <= 0f)
            return;

        float moveReturnSpeed =
            playerSpeed *
            Mathf.Abs(moveSpeedMultiplier) *
            returnSpeedMultiplier;

        float rotationReturnSpeed =
            playerSpeed *
            returnRotationSpeedMultiplier;

        StepReturnToStart(moveReturnSpeed, rotationReturnSpeed);
    }

    private void StepReturnToStart(float moveSpeed, float rotationSpeed)
    {
        Vector3 roomCenter = GetRoomCenterLocal();

        if (moveSpeed > 0f)
        {
            mirrorOffset = Vector2.MoveTowards(
                mirrorOffset,
                Vector2.zero,
                moveSpeed * Time.deltaTime
            );

            transform.localPosition = roomCenter + new Vector3(
                mirrorOffset.x,
                mirrorOffset.y,
                0f
            );

            if (visual != null)
            {
                visual.localScale = Vector3.MoveTowards(
                    visual.localScale,
                    startVisualLocalScale,
                    moveSpeed * Time.deltaTime
                );
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(
                    spriteRenderer.color,
                    idleColor,
                    Mathf.Clamp01(moveSpeed * Time.deltaTime)
                );
            }
        }

        if (rotationSpeed > 0f)
        {
            mirrorRotationOffset = Mathf.MoveTowards(
                mirrorRotationOffset,
                0f,
                rotationSpeed * Time.deltaTime
            );

            if (visual != null)
            {
                visual.localRotation =
                    startVisualLocalRotation *
                    Quaternion.Euler(0f, 0f, mirrorRotationOffset);
            }
        }
    }

    private float GetEffectPower(EffectSource source, Vector2 radarDelta)
    {
        float movementPower = 0f;

        if (Mathf.Abs(radarDelta.x) > Mathf.Abs(radarDelta.y))
            movementPower = radarDelta.x;
        else
            movementPower = radarDelta.y;

        float rotationPower =
            worldTurnRate * Time.deltaTime;

        switch (source)
        {
            case EffectSource.Off:
                return 0f;

            case EffectSource.Movement:
                return movementPower;

            case EffectSource.Rotation:
                return rotationPower;

            case EffectSource.MovementAndRotation:
                return movementPower + rotationPower;

            default:
                return 0f;
        }
    }

    private Vector3 GetRoomCenterLocal()
    {
        if (roomAnchor != null)
        {
            Transform parent = transform.parent;

            if (parent != null)
                return parent.InverseTransformPoint(roomAnchor.position);

            return roomAnchor.position;
        }

        return startRootLocalPosition;
    }

    private float GetResponseValue(MirrorResponse response)
    {
        switch (response)
        {
            case MirrorResponse.Backward:
                return -1f;

            case MirrorResponse.None:
                return 0f;

            case MirrorResponse.Forward:
                return 1f;

            default:
                return 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!drawRoom && !drawActivationZone)
            return;

        Vector3 roomCenterLocal = GetEditorRoomCenterLocal();

        if (drawActivationZone)
            DrawActivationDiscLocal(
                roomCenterLocal,
                activationRadius
            );

        if (drawRoom)
            DrawRoomDiscLocal(
                roomCenterLocal,
                roomRadius
            );
    }

    private Vector3 GetEditorRoomCenterLocal()
    {
        if (roomAnchor != null)
        {
            Transform parent = transform.parent;

            if (parent != null)
                return parent.InverseTransformPoint(roomAnchor.position);

            return roomAnchor.position;
        }

        return transform.localPosition;
    }

    private void DrawRoomDiscLocal(Vector3 localCenter, float radius)
    {
        if (radius <= 0f)
            return;

        Matrix4x4 oldMatrix = Handles.matrix;

        if (transform.parent != null)
            Handles.matrix = transform.parent.localToWorldMatrix;
        else
            Handles.matrix = Matrix4x4.identity;

        Handles.zTest =
            UnityEngine.Rendering.CompareFunction.Always;

        Color fillColor =
            new Color(0f, 0.8f, 1f, roomFillAlpha);

        Color borderColor =
            new Color(0f, 0.9f, 1f, roomBorderAlpha);

        Handles.color = fillColor;
        Handles.DrawSolidDisc(
            localCenter,
            Vector3.forward,
            radius
        );

        Handles.color = borderColor;
        Handles.DrawWireDisc(
            localCenter,
            Vector3.forward,
            radius
        );

        Handles.matrix = oldMatrix;
    }

    private void DrawActivationDiscLocal(Vector3 localCenter, float radius)
    {
        if (radius <= 0f)
            return;

        Matrix4x4 oldMatrix = Handles.matrix;

        if (transform.parent != null)
            Handles.matrix = transform.parent.localToWorldMatrix;
        else
            Handles.matrix = Matrix4x4.identity;

        Handles.zTest =
            UnityEngine.Rendering.CompareFunction.Always;

        Color fillColor =
            new Color(1f, 0.45f, 0f, activationFillAlpha);

        Color borderColor =
            new Color(1f, 0.5f, 0f, activationBorderAlpha);

        Handles.color = fillColor;
        Handles.DrawSolidDisc(
            localCenter,
            Vector3.forward,
            radius
        );

        Handles.color = borderColor;
        Handles.DrawWireDisc(
            localCenter,
            Vector3.forward,
            radius
        );

        Handles.matrix = oldMatrix;
    }
#endif
}