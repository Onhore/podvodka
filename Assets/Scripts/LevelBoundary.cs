using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class LevelBoundary : MonoBehaviour
{
    public enum BoundaryMode
    {
        [InspectorName("Level Boundary | внутри можно")]
        LevelBoundary = 0,

        [InspectorName("Solid Obstacle | внутри нельзя")]
        SolidObstacle = 1
    }
    [Header("Data")]
    public LevelData levelData;

    [Header("Override")]
    public Vector2[] points = new Vector2[]
    {
        new Vector2(-5f,  5f),
        new Vector2( 5f,  5f),
        new Vector2( 5f, -5f),
        new Vector2(-5f, -5f),
    };

    [Header("Shape")]
    [Tooltip("Замыкать контур. ON = область, OFF = открытая стена.")]
    public bool closedLoop = true;

    [Header("Collision Logic")]
    public BoundaryMode boundaryMode =
    BoundaryMode.LevelBoundary;
    [Header("Collision")]
    [Tooltip("Корень логических координат. Обычно WorldContent. Если пусто — берётся parent.")]
    public Transform logicalRoot;

    [Tooltip("Толщина столкновения для открытого Edge.")]
    public float openEdgeCollisionRadius = 0.25f;

    [Header("Visual")]
    public Color color = new Color(1f, 0.2f, 0.2f, 1f);
    public float lineWidth = 0.05f;

    [Header("Curve Tool")]
    [Range(1, 16)]
    public int curveSteps = 5;

    public float curveBendAmount = 0.75f;

    LineRenderer _line;
    EdgeCollider2D _edge;

    void Awake()
    {
        Build();
    }

    void OnValidate()
    {
        Build();
    }

    public void Build()
    {
        _line = GetComponent<LineRenderer>();
        _edge = GetComponent<EdgeCollider2D>();

        Vector2[] pts = GetPoints();

        if (pts == null || pts.Length < 2)
            return;

        _line.useWorldSpace = false;
        _line.loop = closedLoop;
        _line.positionCount = pts.Length;
        _line.startWidth = GetWidth();
        _line.endWidth = GetWidth();
        _line.startColor = GetColor();
        _line.endColor = GetColor();

        if (_line.sharedMaterial == null)
            _line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        _line.sharedMaterial.color = GetColor();

        for (int i = 0; i < pts.Length; i++)
        {
            _line.SetPosition(i, new Vector3(pts[i].x, pts[i].y, 0f));
        }

        if (closedLoop)
        {
            Vector2[] edgePts = new Vector2[pts.Length + 1];

            for (int i = 0; i < pts.Length; i++)
                edgePts[i] = pts[i];

            edgePts[pts.Length] = pts[0];

            _edge.points = edgePts;
        }
        else
        {
            _edge.points = pts;
        }

        _edge.isTrigger = false;
        _edge.edgeRadius = openEdgeCollisionRadius;
    }

    public bool IsInsideBoundary(Vector2 logicalPosition)
    {
        Vector2[] pts = GetPoints();

        if (pts == null || pts.Length < 2)
            return true;

        if (closedLoop)
        {
            if (pts.Length < 3)
                return true;

            bool inside =
                IsPointInsidePolygon(
                    logicalPosition,
                    pts
                );

            switch (boundaryMode)
            {
                case BoundaryMode.LevelBoundary:
                    return inside;

                case BoundaryMode.SolidObstacle:
                    return !inside;

                default:
                    return inside;
            }
        }

        return !IsPointTouchingOpenEdge(logicalPosition, pts);
    }

    bool IsPointTouchingOpenEdge(Vector2 logicalPoint, Vector2[] edgePoints)
    {
        for (int i = 0; i < edgePoints.Length - 1; i++)
        {
            Vector2 a = BoundaryLocalPointToLogical(edgePoints[i]);
            Vector2 b = BoundaryLocalPointToLogical(edgePoints[i + 1]);

            float distance = DistancePointToSegment(logicalPoint, a, b);

            if (distance <= openEdgeCollisionRadius)
                return true;
        }

        return false;
    }

    bool IsPointInsidePolygon(Vector2 logicalPoint, Vector2[] polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 pi = BoundaryLocalPointToLogical(polygon[i]);
            Vector2 pj = BoundaryLocalPointToLogical(polygon[j]);

            bool intersect =
                ((pi.y > logicalPoint.y) != (pj.y > logicalPoint.y)) &&
                (logicalPoint.x < (pj.x - pi.x) *
                (logicalPoint.y - pi.y) /
                (pj.y - pi.y) + pi.x);

            if (intersect)
                inside = !inside;
        }

        return inside;
    }

    Vector2 BoundaryLocalPointToLogical(Vector2 localPoint)
    {
        Vector3 worldPoint = transform.TransformPoint(
            new Vector3(localPoint.x, localPoint.y, 0f)
        );

        Transform root = logicalRoot;

        if (root == null)
            root = transform.parent;

        if (root != null)
        {
            Vector3 logicalPoint = root.InverseTransformPoint(worldPoint);
            return new Vector2(logicalPoint.x, logicalPoint.y);
        }

        return new Vector2(worldPoint.x, worldPoint.y);
    }

    float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLengthSqr = ab.sqrMagnitude;

        if (abLengthSqr < 0.000001f)
            return Vector2.Distance(point, a);

        float t = Vector2.Dot(point - a, ab) / abLengthSqr;
        t = Mathf.Clamp01(t);

        Vector2 closestPoint = a + ab * t;

        return Vector2.Distance(point, closestPoint);
    }

    public Vector2[] GetPoints()
    {
        if (levelData != null && levelData.boundaryPoints != null)
            return levelData.boundaryPoints;

        return points;
    }

    public void SetPoints(Vector2[] newPoints)
    {
        if (levelData != null)
            levelData.boundaryPoints = newPoints;
        else
            points = newPoints;

        Build();
    }

    public void DetachFromLevelData()
    {
        if (levelData == null)
            return;

        points = (Vector2[])levelData.boundaryPoints.Clone();
        color = levelData.boundaryColor;
        lineWidth = levelData.lineWidth;
        levelData = null;

        Build();
    }

    public Color GetColor()
    {
        return levelData != null ? levelData.boundaryColor : color;
    }

    public float GetWidth()
    {
        return levelData != null ? levelData.lineWidth : lineWidth;
    }
    public bool TryGetPushOut(Vector2 logicalPosition, out Vector2 push)
    {
        push = Vector2.zero;

        if (closedLoop)
            return false;

        Vector2[] pts = GetPoints();

        if (pts == null || pts.Length < 2)
            return false;

        float bestPenetration = 0f;
        Vector2 bestPush = Vector2.zero;

        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 a = BoundaryLocalPointToLogical(pts[i]);
            Vector2 b = BoundaryLocalPointToLogical(pts[i + 1]);

            Vector2 closest = ClosestPointOnSegment(logicalPosition, a, b);
            Vector2 fromLineToPlayer = logicalPosition - closest;

            float distance = fromLineToPlayer.magnitude;
            float penetration = openEdgeCollisionRadius - distance;

            if (penetration > bestPenetration)
            {
                Vector2 normal;

                if (distance > 0.0001f)
                {
                    normal = fromLineToPlayer.normalized;
                }
                else
                {
                    Vector2 segment = (b - a).normalized;
                    normal = new Vector2(-segment.y, segment.x);
                }

                bestPenetration = penetration;
                bestPush = normal * penetration;
            }
        }

        if (bestPenetration > 0f)
        {
            push = bestPush;
            return true;
        }

        return false;
    }
    Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float abLengthSqr = ab.sqrMagnitude;

        if (abLengthSqr < 0.000001f)
            return a;

        float t = Vector2.Dot(point - a, ab) / abLengthSqr;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }
}