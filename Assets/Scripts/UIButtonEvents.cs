using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public UnityEngine.Events.UnityEvent onPointerDown;
    public UnityEngine.Events.UnityEvent onPointerUp;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp?.Invoke();
    }
}