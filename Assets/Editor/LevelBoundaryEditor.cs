using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelBoundary))]
public class LevelBoundaryEditor : Editor
{
    int selectedPoint = -1;

    readonly Color addButtonColor = new Color(0.45f, 0.85f, 0.45f);
    readonly Color removeButtonColor = new Color(0.95f, 0.45f, 0.45f);
    readonly Color normalButtonColor = Color.white;

    void OnSceneGUI()
    {
        var lb = (LevelBoundary)target;
        var pts = lb.GetPoints();

        if (pts == null || pts.Length == 0)
            return;

        var t = lb.transform;
        bool changed = false;

        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 worldPos = t.TransformPoint(new Vector3(pts[i].x, pts[i].y, 0f));

            Handles.color = i == selectedPoint ? Color.green : Color.yellow;

            float size = HandleUtility.GetHandleSize(worldPos) * 0.12f;

            if (Handles.Button(
                worldPos,
                Quaternion.identity,
                size * 0.7f,
                size * 0.7f,
                Handles.SphereHandleCap))
            {
                selectedPoint = i;
                Repaint();
            }

            EditorGUI.BeginChangeCheck();

            Vector3 newWorld = Handles.FreeMoveHandle(
                worldPos,
                size,
                Vector3.zero,
                Handles.SphereHandleCap
            );

            if (EditorGUI.EndChangeCheck())
            {
                RecordUndo(lb, "Move Boundary Point");

                Vector3 local = t.InverseTransformPoint(newWorld);
                pts[i] = new Vector2(local.x, local.y);

                lb.SetPoints(pts);

                selectedPoint = i;
                changed = true;
            }

            Handles.Label(worldPos + Vector3.up * 0.1f, i.ToString());
        }

        Handles.color = lb.GetColor();

        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 a = t.TransformPoint(new Vector3(pts[i].x, pts[i].y, 0f));
            Vector3 b = t.TransformPoint(new Vector3(
                pts[(i + 1) % pts.Length].x,
                pts[(i + 1) % pts.Length].y,
                0f
            ));

            Handles.DrawLine(a, b);
        }

        if (changed)
            RebuildAndRefresh(lb);
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
            RebuildAndRefresh((LevelBoundary)target);

        GUILayout.Space(8);

        EditorGUILayout.LabelField("Boundary Tools", EditorStyles.boldLabel);

        if (selectedPoint >= 0)
            EditorGUILayout.LabelField("Selected Point", selectedPoint.ToString());
        else
            EditorGUILayout.LabelField("Selected Point", "None");

        GUILayout.Space(6);

        DrawDefaultButton("Rebuild", () =>
        {
            RebuildAndRefresh((LevelBoundary)target);
        });

        GUILayout.Space(4);

        DrawColoredButton("Add Point", addButtonColor, () =>
        {
            AddPointAfterSelected((LevelBoundary)target);
        });

        DrawDefaultButton("Smooth", () =>
        {
            BendSegmentAfterSelected((LevelBoundary)target);
        });

        DrawColoredButton("Remove Point", removeButtonColor, () =>
        {
            RemoveSelectedPoint((LevelBoundary)target);
        });
    }

    void DrawDefaultButton(string label, System.Action action)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = normalButtonColor;

        if (GUILayout.Button(label))
            action?.Invoke();

        GUI.backgroundColor = old;
    }

    void DrawColoredButton(string label, Color color, System.Action action)
    {
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = color;

        if (GUILayout.Button(label))
            action?.Invoke();

        GUI.backgroundColor = old;
    }

    void AddPointAfterSelected(LevelBoundary lb)
    {
        var src = lb.GetPoints();

        if (src == null || src.Length == 0)
            return;

        if (selectedPoint < 0 || selectedPoint >= src.Length)
            selectedPoint = src.Length - 1;

        RecordUndo(lb, "Add Point After Selected");

        var n = new Vector2[src.Length + 1];

        for (int i = 0; i <= selectedPoint; i++)
            n[i] = src[i];

        Vector2 current = src[selectedPoint];
        Vector2 next = src[(selectedPoint + 1) % src.Length];

        n[selectedPoint + 1] = Vector2.Lerp(current, next, 0.5f);

        for (int i = selectedPoint + 1; i < src.Length; i++)
            n[i + 1] = src[i];

        selectedPoint++;

        lb.SetPoints(n);
        RebuildAndRefresh(lb);
    }

    void BendSegmentAfterSelected(LevelBoundary lb)
    {
        var src = lb.GetPoints();

        if (src == null || src.Length < 2)
            return;

        if (selectedPoint < 0 || selectedPoint >= src.Length)
            return;

        int startIndex = selectedPoint;
        int endIndex = (selectedPoint + 1) % src.Length;

        Vector2 start = src[startIndex];
        Vector2 end = src[endIndex];

        Vector2 segment = end - start;

        if (segment.sqrMagnitude < 0.0001f)
            return;

        RecordUndo(lb, "Bend Segment After Selected");

        int steps = Mathf.Max(1, lb.curveSteps);

        Vector2 mid = (start + end) * 0.5f;

        Vector2 normal = new Vector2(-segment.y, segment.x).normalized;

        Vector2 control = mid + normal * lb.curveBendAmount;

        Vector2[] curvePoints = new Vector2[steps];

        for (int i = 0; i < steps; i++)
        {
            float t = (i + 1f) / (steps + 1f);
            curvePoints[i] = QuadraticBezier(start, control, end, t);
        }

        Vector2[] result = new Vector2[src.Length + steps];

        int write = 0;

        for (int i = 0; i < src.Length; i++)
        {
            result[write] = src[i];
            write++;

            if (i == startIndex)
            {
                for (int c = 0; c < curvePoints.Length; c++)
                {
                    result[write] = curvePoints[c];
                    write++;
                }
            }
        }

        selectedPoint = startIndex;

        lb.SetPoints(result);
        RebuildAndRefresh(lb);
    }

    Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;

        return
            u * u * a +
            2f * u * t * b +
            t * t * c;
    }

    void RemoveSelectedPoint(LevelBoundary lb)
    {
        var src = lb.GetPoints();

        if (src == null || src.Length <= 3)
            return;

        if (selectedPoint < 0 || selectedPoint >= src.Length)
            return;

        RecordUndo(lb, "Remove Selected Point");

        var n = new Vector2[src.Length - 1];

        int index = 0;

        for (int i = 0; i < src.Length; i++)
        {
            if (i == selectedPoint)
                continue;

            n[index] = src[i];
            index++;
        }

        selectedPoint = Mathf.Clamp(selectedPoint, 0, n.Length - 1);

        lb.SetPoints(n);
        RebuildAndRefresh(lb);
    }

    void RecordUndo(LevelBoundary lb, string name)
    {
        if (lb.levelData != null)
            Undo.RecordObject(lb.levelData, name);

        Undo.RecordObject(lb, name);
    }

    static void RebuildAndRefresh(LevelBoundary lb)
    {
        lb.Build();

        var edge = lb.GetComponent<EdgeCollider2D>();
        var line = lb.GetComponent<LineRenderer>();

        if (edge != null)
            EditorUtility.SetDirty(edge);

        if (line != null)
            EditorUtility.SetDirty(line);

        if (lb.levelData != null)
            EditorUtility.SetDirty(lb.levelData);

        EditorUtility.SetDirty(lb);

        Physics2D.SyncTransforms();
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }
}