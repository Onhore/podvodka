using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onPointerDown;
    public UnityEngine.Events.UnityEvent onPointerUp;
    public UnityEngine.Events.UnityEvent onDragStart;  // Первый кадр перетаскивания
    public UnityEngine.Events.UnityEvent onDrag;       // Каждый кадр перетаскивания
    public UnityEngine.Events.UnityEvent onDragEnd;    // Когда закончили перетаскивание
    
    private bool isDragging;
    private bool hasInvokedDragStart;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        hasInvokedDragStart = false;
        onPointerDown?.Invoke();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        if (!hasInvokedDragStart)
        {
            hasInvokedDragStart = true;
            onDragStart?.Invoke();
        }
        
        onDrag?.Invoke();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDragging && hasInvokedDragStart)
        {
            onDragEnd?.Invoke();
        }
        
        isDragging = false;
        hasInvokedDragStart = false;
        onPointerUp?.Invoke();
    }
}