using UnityEngine;

public class DelayedFollower : MonoBehaviour
{
    [Header("References")]
    public UIRotary wheel; // Ссылка на руль
    public float followSpeed = 5f; // Скорость следования (чем меньше, тем больше лаг)
    public float maxRotationSpeed = 360f; // Максимальная скорость поворота
    
    private float currentAngle;
    private float targetAngle;
    private float velocity;
    
    void Start()
    {
        if (wheel == null)
            wheel = FindObjectOfType<UIRotary>();
            
        currentAngle = wheel.GetCurrentAngle();
        targetAngle = currentAngle;
    }
    
    void Update()
    {
        if (wheel == null) return;
        
        // Целевой угол = текущий угол руля
        targetAngle = wheel.GetCurrentAngle();
        
        // Плавное следование с лагом
        currentAngle = Mathf.SmoothDamp(
            currentAngle, 
            targetAngle, 
            ref velocity, 
            1f / followSpeed, // Время сглаживания
            maxRotationSpeed
        );
        
        // Применяем поворот к объекту
        transform.rotation = Quaternion.Euler(0, 0, currentAngle);
    }
}