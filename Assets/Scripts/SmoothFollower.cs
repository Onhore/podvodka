using UnityEngine;

public class SmoothFollower : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Цель, за которой следовать

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f; // Скорость сглаживания (рекомендуется 3-10)
    [SerializeField] private float rotationSmoothSpeed = 5f; // Скорость сглаживания поворота

    [Header("Position Offset")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // Смещение относительно цели

    [Header("Options")]
    [SerializeField] private bool followPosition = true; // Следовать за позицией
    [SerializeField] private bool followRotation = false; // Следовать за поворотом
    [SerializeField] private bool lookAtTarget = false; // Поворачиваться лицом к цели (заменяет followRotation)
    [SerializeField] private bool useFixedUpdate = false; // Использовать FixedUpdate (лучше для физики)

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError($"SmoothFollower on {gameObject.name}: Target не назначен!", this);
        }
    }

    private void Update()
    {
        if (!useFixedUpdate)
        {
            Follow();
        }
    }

    private void FixedUpdate()
    {
        if (useFixedUpdate)
        {
            Follow();
        }
    }

    private void Follow()
    {
        if (target == null) return;

        // Плавное движение к целевой позиции
        if (followPosition)
        {
            Vector3 targetPosition = target.position + positionOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }

        // Плавный поворот вслед за целью
        if (followRotation)
        {
            Quaternion targetRotation = target.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
        // Либо поворот лицом к цели
        else if (lookAtTarget)
        {
            Vector3 direction = target.position - transform.position;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
            }
        }
    }

    // Публичный метод для смены цели в рантайме
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Мгновенное перемещение к цели
    public void TeleportToTarget()
    {
        if (target == null) return;
        transform.position = target.position + positionOffset;
        if (followRotation)
        {
            transform.rotation = target.rotation;
        }
        else if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    }
}