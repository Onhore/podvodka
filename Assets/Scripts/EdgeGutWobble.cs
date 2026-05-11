using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
[RequireComponent(typeof(LineRenderer))]
public class EdgeGutWobble : MonoBehaviour
{
    [Header("Thickness")]

    [Tooltip("Анимировать толщину.")]
    public bool animateThickness = true;

    [Range(0f, 5f)]
    public float baseThickness = 0.5f;

    [Range(0f, 5f)]
    public float thicknessAmplitude = 0.15f;

    [Range(0f, 10f)]
    public float thicknessSpeed = 1.2f;

    [Range(0f, 1f)]
    public float thicknessNoise = 0.2f;

    [Tooltip("Множитель визуальной толщины LineRenderer.")]
    public float lineWidthMultiplier = 2f;

    [Header("Wobble")]
    [Tooltip("Включить движение точек.")]
    public bool animate = true;

    [Tooltip("Сила выпирания точек.")]
    [Range(0f, 2f)]
    public float amplitude = 0.25f;

    [Tooltip("Скорость булдыгания.")]
    [Range(0f, 10f)]
    public float speed = 1.5f;

    [Tooltip("Длина волны по точкам. Меньше = чаще волны.")]
    [Range(0.1f, 10f)]
    public float waveLength = 2.5f;

    [Tooltip("Насколько разные точки живут своей жизнью.")]
    [Range(0f, 2f)]
    public float noiseAmount = 0.35f;

    [Tooltip("Скорость шума.")]
    [Range(0f, 10f)]
    public float noiseSpeed = 0.8f;

    [Header("Shape")]
    [Tooltip("Фиксировать первую точку.")]
    public bool pinFirstPoint = false;

    [Tooltip("Фиксировать последнюю точку.")]
    public bool pinLastPoint = false;

    [Tooltip("Если контур замкнут — последняя точка повторяет первую.")]
    public bool closedLoop = false;

    [Header("Visual")]
    [Tooltip("Обновлять LineRenderer вместе с EdgeCollider.")]
    public bool updateLineRenderer = true;

    [Tooltip("Если true, LineRenderer.loop будет равен Closed Loop.")]
    public bool syncLineLoop = true;

    private EdgeCollider2D edge;
    private LineRenderer line;

    private Vector2[] basePoints;
    private Vector2[] animatedPoints;
    private Vector3[] linePositions;

    private float seed;

    private void Awake()
    {
        edge = GetComponent<EdgeCollider2D>();
        line = GetComponent<LineRenderer>();

        CachePoints();
    }

    private void OnEnable()
    {
        CachePoints();
    }

    private void Update()
    {
        if (!animate)
            return;

        if (basePoints == null || basePoints.Length < 2)
            CachePoints();

        AnimatePoints();
        ApplyPoints();
        AnimateThickness();
    }

    [ContextMenu("Cache Current Points As Base")]
    public void CachePoints()
    {
        if (edge == null)
            edge = GetComponent<EdgeCollider2D>();

        if (line == null)
            line = GetComponent<LineRenderer>();

        basePoints = edge.points;

        if (basePoints == null || basePoints.Length == 0)
            return;

        animatedPoints = new Vector2[basePoints.Length];
        linePositions = new Vector3[basePoints.Length];

        for (int i = 0; i < basePoints.Length; i++)
            animatedPoints[i] = basePoints[i];

        seed = Random.value * 1000f;

        ApplyPoints();
    }

    [ContextMenu("Reset To Base Points")]
    public void ResetToBase()
    {
        if (basePoints == null)
            return;

        for (int i = 0; i < basePoints.Length; i++)
            animatedPoints[i] = basePoints[i];

        ApplyPoints();
    }

    private void AnimatePoints()
    {
        float time = Time.time * speed;

        for (int i = 0; i < basePoints.Length; i++)
        {
            if (pinFirstPoint && i == 0)
            {
                animatedPoints[i] = basePoints[i];
                continue;
            }

            if (pinLastPoint && i == basePoints.Length - 1)
            {
                animatedPoints[i] = basePoints[i];
                continue;
            }

            if (closedLoop && i == basePoints.Length - 1)
            {
                animatedPoints[i] = animatedPoints[0];
                continue;
            }

            Vector2 normal = GetPointNormal(i);

            float wave =
                Mathf.Sin(time + i / waveLength);

            float noise =
                Mathf.PerlinNoise(
                    seed + i * 0.37f,
                    Time.time * noiseSpeed
                ) * 2f - 1f;

            float offset =
                wave * amplitude +
                noise * amplitude * noiseAmount;

            animatedPoints[i] = basePoints[i] + normal * offset;
        }
    }

    private Vector2 GetPointNormal(int index)
    {
        Vector2 prev;
        Vector2 next;

        if (index <= 0)
        {
            prev = closedLoop
                ? basePoints[basePoints.Length - 2]
                : basePoints[index];

            next = basePoints[index + 1];
        }
        else if (index >= basePoints.Length - 1)
        {
            prev = basePoints[index - 1];

            next = closedLoop
                ? basePoints[1]
                : basePoints[index];
        }
        else
        {
            prev = basePoints[index - 1];
            next = basePoints[index + 1];
        }

        Vector2 tangent = next - prev;

        if (tangent.sqrMagnitude < 0.000001f)
            return Vector2.up;

        tangent.Normalize();

        return new Vector2(-tangent.y, tangent.x);
    }

    private void ApplyPoints()
    {
        if (animatedPoints == null || animatedPoints.Length == 0)
            return;

        edge.points = animatedPoints;

        if (!updateLineRenderer || line == null)
            return;

        line.useWorldSpace = false;

        if (syncLineLoop)
            line.loop = closedLoop;
        
        int linePointCount = animatedPoints.Length;

        if (closedLoop)
        {
            Vector2 first = animatedPoints[0];
            Vector2 last = animatedPoints[animatedPoints.Length - 1];

            if (Vector2.Distance(first, last) < 0.0001f)
                linePointCount--;
        }

        line.positionCount = linePointCount;

        for (int i = 0; i < linePointCount; i++)
        {
            linePositions[i] = new Vector3(
                animatedPoints[i].x,
                animatedPoints[i].y,
                0f
            );
        }

        line.SetPositions(linePositions);
    }
    private void AnimateThickness()
    {
        if (!animateThickness)
            return;

        float time = Time.time * thicknessSpeed;

        float wave =
            Mathf.Sin(time) * thicknessAmplitude;

        float noise =
            (Mathf.PerlinNoise(
                seed + 999f,
                Time.time * 0.5f
            ) * 2f - 1f)
            * thicknessAmplitude
            * thicknessNoise;

        float thickness =
            Mathf.Max(
                0.001f,
                baseThickness + wave + noise
            );

        edge.edgeRadius = thickness;

        if (line != null)
        {
            float width = thickness * lineWidthMultiplier;

            line.startWidth = width;
            line.endWidth = width;
        }
    }
}