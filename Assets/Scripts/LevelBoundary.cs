using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class LevelBoundary : MonoBehaviour
{
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

    [Header("Visual")]
    public Color color = new Color(1f, 0.2f, 0.2f, 1f);
    public float lineWidth = 0.05f;

    [Header("Curve Tool")]
    [Tooltip("How many points will be inserted between selected point and next point.")]
    [Range(1, 16)]
    public int curveSteps = 5;

    [Tooltip("How strongly the segment bends.")]
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

        if (pts == null || pts.Length < 3)
            return;

        _line.useWorldSpace = false;
        _line.loop = true;
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

        Vector2[] edgePts = new Vector2[pts.Length + 1];

        for (int i = 0; i < pts.Length; i++)
            edgePts[i] = pts[i];

        edgePts[pts.Length] = pts[0];

        _edge.points = edgePts;
        _edge.isTrigger = false;
    }

    public bool IsInsideBoundary(Vector2 logicalPosition)
    {
        Vector2[] pts = GetPoints();

        if (pts == null || pts.Length < 3)
            return true;

        return IsPointInsidePolygon(logicalPosition, pts);
    }

    bool IsPointInsidePolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;

        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            bool intersect =
                ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                (point.x < (polygon[j].x - polygon[i].x) *
                (point.y - polygon[i].y) /
                (polygon[j].y - polygon[i].y) + polygon[i].x);

            if (intersect)
                inside = !inside;
        }

        return inside;
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


}