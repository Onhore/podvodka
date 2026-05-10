using UnityEngine;

/// <summary>
/// ScriptableObject хранящий данные одного уровня.
/// Создать: Assets → Create → Podvodka → Level Data
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "Podvodka/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Boundary")]
    [Tooltip("Точки контура уровня (замкнутый полигон)")]
    public Vector2[] boundaryPoints = new Vector2[]
    {
        new Vector2(-5f,  5f),
        new Vector2( 5f,  5f),
        new Vector2( 5f, -5f),
        new Vector2(-5f, -5f),
    };

    [Header("Navigation")]
    [Tooltip("Стартовая точка A (позиция субмарины в начале уровня)")]
    public Vector2 pointA = new Vector2(0f, -4f);

    [Tooltip("Финишная точка B (цель уровня)")]
    public Vector2 pointB = new Vector2(0f, 4f);

    [Header("Display")]
    [Tooltip("Цвет границы уровня")]
    public Color boundaryColor = new Color(1f, 0.2f, 0.2f, 1f); // красный

    [Tooltip("Толщина линии границы")]
    public float lineWidth = 0.05f;
}
