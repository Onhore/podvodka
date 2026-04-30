using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimpleMorseDrawer : MonoBehaviour
{
    [Header("References")]
    public RectTransform lineContainer;
    public GameObject linePrefab;
    
    [Header("Drawing Settings")]
    public float lineSpeed = 100f;
    public float lineWidth = 10f;
    public Color lineColor = Color.green;
    public int poolSize = 50;
    public float drawInterval = 0.05f;    // Интервал рисования
    
    [Header("Effects")]
    public bool useGradient = false;
    public Gradient colorGradient;
    public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
    
    private bool isDrawing;
    private Queue<GameObject> pool;
    private List<PooledLine> activeLines = new List<PooledLine>();
    private float containerWidth;
    private float lastDrawTime;
    private float currentDrawTime;
    
    private class PooledLine
    {
        public GameObject obj;
        public RectTransform rect;
        public Image image;
        public float creationTime;
        
        public PooledLine(GameObject go)
        {
            obj = go;
            rect = go.GetComponent<RectTransform>();
            image = go.GetComponent<Image>();
            creationTime = Time.time;
        }
    }
    
    void Start()
    {
        if (lineContainer != null)
            containerWidth = lineContainer.rect.width;
            
        if (linePrefab == null)
            CreateLinePrefab();
            
        InitializePool();
    }
    
    void InitializePool()
    {
        pool = new Queue<GameObject>();
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject line = Instantiate(linePrefab, lineContainer);
            line.SetActive(false);
            pool.Enqueue(line);
        }
    }
    
    void Update()
    {
        if (isDrawing && Time.time - lastDrawTime > drawInterval)
        {
            DrawNewLine();
            lastDrawTime = Time.time;
        }
        
        MoveLinesRight();
        UpdateLineEffects();
        RemoveOffscreenLines();
    }
    
    void DrawNewLine()
    {
        if (pool.Count == 0) return;
        
        GameObject newLine = pool.Dequeue();
        newLine.SetActive(true);
        
        PooledLine pooledLine = new PooledLine(newLine);
        
        float startX = -containerWidth / 2;
        pooledLine.rect.anchoredPosition = new Vector2(startX, 0);
        pooledLine.rect.sizeDelta = new Vector2(lineWidth, lineContainer.rect.height);
        
        if (useGradient && colorGradient != null)
            pooledLine.image.color = colorGradient.Evaluate(0);
        else
            pooledLine.image.color = lineColor;
            
        activeLines.Add(pooledLine);
    }
    
    void MoveLinesRight()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i].obj != null)
            {
                Vector2 pos = activeLines[i].rect.anchoredPosition;
                pos.x += lineSpeed * Time.deltaTime;
                activeLines[i].rect.anchoredPosition = pos;
            }
        }
    }
    
    void UpdateLineEffects()
    {
        float rightBound = containerWidth / 2;
        
        for (int i = 0; i < activeLines.Count; i++)
        {
            PooledLine line = activeLines[i];
            if (line.obj == null) continue;
            
            // Прогресс линии (0 = только появилась, 1 = у края)
            float xPos = line.rect.anchoredPosition.x;
            float progress = (xPos + containerWidth / 2) / containerWidth;
            progress = Mathf.Clamp01(progress);
            
            // Применяем эффекты
            if (useGradient && colorGradient != null)
            {
                line.image.color = colorGradient.Evaluate(progress);
            }
            
            // Изменяем ширину по кривой
            float widthMultiplier = widthCurve.Evaluate(1 - progress);
            Vector2 size = line.rect.sizeDelta;
            size.x = lineWidth * widthMultiplier;
            line.rect.sizeDelta = size;
        }
    }
    
    void RemoveOffscreenLines()
    {
        float rightBound = containerWidth / 2;
        
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            if (activeLines[i].obj == null)
            {
                activeLines.RemoveAt(i);
            }
            else if (activeLines[i].rect.anchoredPosition.x - lineWidth > rightBound)
            {
                activeLines[i].obj.SetActive(false);
                pool.Enqueue(activeLines[i].obj);
                activeLines.RemoveAt(i);
            }
        }
    }
    
    void CreateLinePrefab()
    {
        linePrefab = new GameObject("LineSegment");
        linePrefab.AddComponent<Image>();
        linePrefab.AddComponent<RectTransform>();
        linePrefab.SetActive(false);
    }
    
    public void StartDrawing()
    {
        isDrawing = true;
        lastDrawTime = Time.time;
    }
    
    public void StopDrawing()
    {
        isDrawing = false;
    }
    
    public void ClearScreen()
    {
        foreach (var line in activeLines)
        {
            if (line.obj != null)
            {
                line.obj.SetActive(false);
                pool.Enqueue(line.obj);
            }
        }
        activeLines.Clear();
    }
    
    public void SetLineColor(Color color)
    {
        lineColor = color;
        useGradient = false;
    }
    
    public void SetGradient(Gradient gradient)
    {
        colorGradient = gradient;
        useGradient = true;
    }
}