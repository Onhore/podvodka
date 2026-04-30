using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class TelegraphButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visualizer")]
    public SimpleMorseDrawer morseDrawer;  // Перетащите объект с SimpleMorseDrawer
    
    [Header("Events")]
    public UnityEvent onButtonDown;
    public UnityEvent onButtonUp;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (morseDrawer != null)
            morseDrawer.StartDrawing();
            
        onButtonDown?.Invoke();
        
        // Меняем курсор
        DynamicCursor.PerformClick();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (morseDrawer != null)
            morseDrawer.StopDrawing();
            
        onButtonUp?.Invoke();
        
        // Возвращаем курсор
        //DynamicCursor.SetCursor(CursorType.Arrow);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        //DynamicCursor.SetCursor(CursorType.Arrow);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        //DynamicCursor.SetCursor(CursorType.Arrow);
    }
}