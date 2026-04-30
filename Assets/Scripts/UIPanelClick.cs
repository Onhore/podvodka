using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIPanelClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Click Event")]
    public UnityEvent onClick;
    
    [Header("Optional")]
    public bool interactable = true;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
        {
            onClick?.Invoke();
        }
    }
    
    public void SetInteractable(bool value)
    {
        interactable = value;
    }
}