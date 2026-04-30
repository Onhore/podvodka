using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIButtonPressEvents : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    [SerializeField] private UnityEvent onPress;
    [SerializeField] private UnityEvent onRelease;

    private bool pressed;

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        onPress?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pressed) return;

        pressed = false;
        onRelease?.Invoke();
    }
}