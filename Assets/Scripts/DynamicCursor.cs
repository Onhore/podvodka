using UnityEngine;
using UnityEngine.UI;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum CursorState
{
    Default,
    Click,
    Grab
}

public class DynamicCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    public CursorState defaultState = CursorState.Default;
    public bool useHardwareCursor = false;
    
    [Header("UI References")]
    public Image cursorImage;
    public RectTransform cursorTransform;
    public Animator cursorAnimator;
    
    [Header("Cursor Sprites (for hardware cursor fallback)")]
    public Sprite defaultSprite;
    public Sprite clickSprite;
    public Sprite grabSprite;
    public Vector2 defaultHotspot = Vector2.zero;
    public Vector2 clickHotspot = new Vector2(0.5f, 0.5f);
    public Vector2 grabHotspot = new Vector2(0.5f, 0.5f);
    
    private static DynamicCursor instance;
    private CursorState currentState;
    private bool isGrabbing;
    private Canvas cursorCanvas;
    
    // Events для внешнего отслеживания
    public static event Action<CursorState> OnStateChanged;
    public static event Action<bool> OnGrabStateChanged;
    public static event Action OnClickPerformed;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Настройка UI компонентов
        if (cursorImage == null)
        {
            cursorImage = GetComponent<Image>();
            if (cursorImage == null)
            {
                cursorImage = gameObject.AddComponent<Image>();
            }
        }
        
        if (cursorTransform == null)
        {
            cursorTransform = GetComponent<RectTransform>();
        }
        
        if (cursorAnimator == null)
        {
            cursorAnimator = GetComponent<Animator>();
        }
        
        cursorCanvas = GetComponentInParent<Canvas>();
        if (cursorCanvas != null)
        {
            cursorCanvas.sortingOrder = 1000;
        }
        
        // Отключаем raycast чтобы не блокировать клики
        cursorImage.raycastTarget = false;
        
        if (!useHardwareCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
        
        // Устанавливаем начальное состояние
        SetState(defaultState);
    }
    
    void Update()
    {
        if (!useHardwareCursor)
        {
            // Следуем за мышью
            Vector2 mousePos = GetMousePosition();
            
            if (cursorCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    cursorCanvas.transform as RectTransform,
                    mousePos,
                    cursorCanvas.worldCamera,
                    out Vector2 localPoint
                );
                cursorTransform.localPosition = localPoint;
            }
        }
    }
    
    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
        return Input.mousePosition;
    }
    
    /// <summary>
    /// Установить состояние курсора (Default, Click, Grab)
    /// </summary>
    public static void SetState(CursorState state)
    {
        if (instance == null)
        {
            Debug.LogError("DynamicCursor instance is null!");
            return;
        }
        
        instance.currentState = state;
        
        // Обновляем визуал через Animator или hardware cursor
        if (instance.useHardwareCursor)
        {
            SetHardwareCursor(state);
        }
        else
        {
            SetSoftwareCursor(state);
        }
        
        OnStateChanged?.Invoke(state);
    }
    
    /// <summary>
    /// Выполнить клик - анимация и возврат в дефолт
    /// </summary>
    public static void PerformClick()
    {
        if (instance == null) return;
        
        // Если сейчас в состоянии Grab, не прерываем его
        if (instance.isGrabbing) return;
        
        // Запускаем анимацию клика
        if (instance.cursorAnimator != null)
        {
            instance.cursorAnimator.SetTrigger("click");
        }
        
        // Временно меняем состояние если не используем Animator
        if (instance.useHardwareCursor)
        {
            SetState(CursorState.Click);
            // Возвращаемся в Default через короткое время
            instance.Invoke(nameof(ReturnToDefault), 0.1f);
        }
        
        OnClickPerformed?.Invoke();
    }
    
    /// <summary>
    /// Начать зажатие (удержание)
    /// </summary>
    public static void StartGrab()
    {
        if (instance == null) return;
        
        if (instance.isGrabbing) return;
        
        instance.isGrabbing = true;
        
        // Запускаем анимацию зажатия
        if (instance.cursorAnimator != null)
        {
            instance.cursorAnimator.SetBool("IsGrabbing", true);
            instance.cursorAnimator.SetTrigger("grab");
        }
        
        // Для hardware cursor
        if (instance.useHardwareCursor)
        {
            SetState(CursorState.Grab);
        }
        
        OnGrabStateChanged?.Invoke(true);
    }
    
    /// <summary>
    /// Закончить зажатие - возврат в дефолт
    /// </summary>
    public static void EndGrab()
    {
        if (instance == null) return;
        
        if (!instance.isGrabbing) return;
        
        instance.isGrabbing = false;
        
        // Останавливаем анимацию зажатия
        if (instance.cursorAnimator != null)
        {
            instance.cursorAnimator.SetBool("IsGrabbing", false);
        }
        
        // Возвращаемся в дефолтное состояние
        SetState(instance.defaultState);
        instance.cursorAnimator.SetTrigger("default");
        
        OnGrabStateChanged?.Invoke(false);
    }
    
    /// <summary>
    /// Проверить, находится ли курсор в состоянии зажатия
    /// </summary>
    public static bool IsGrabbing()
    {
        return instance != null && instance.isGrabbing;
    }
    
    /// <summary>
    /// Получить текущее состояние
    /// </summary>
    public static CursorState GetCurrentState()
    {
        return instance != null ? instance.currentState : CursorState.Default;
    }
    
    private static void SetSoftwareCursor(CursorState state)
    {
        if (instance.cursorAnimator != null && !instance.useHardwareCursor)
        {
            // Если есть Animator, используем его
            switch (state)
            {
                case CursorState.Default:
                    instance.cursorAnimator.SetBool("IsGrabbing", false);
                    break;
                case CursorState.Click:
                    instance.cursorAnimator.SetTrigger("click");
                    break;
                case CursorState.Grab:
                    instance.cursorAnimator.SetTrigger("grab");
                    instance.cursorAnimator.SetBool("IsGrabbing", true);
                    break;
            }
        }
        else if (instance.cursorImage != null)
        {
            // Fallback на смену спрайтов
            Sprite targetSprite = null;
            switch (state)
            {
                case CursorState.Default:
                    targetSprite = instance.defaultSprite;
                    break;
                case CursorState.Click:
                    targetSprite = instance.clickSprite;
                    break;
                case CursorState.Grab:
                    targetSprite = instance.grabSprite;
                    break;
            }
            
            if (targetSprite != null)
            {
                instance.cursorImage.sprite = targetSprite;
            }
        }
    }
    
    private static void SetHardwareCursor(CursorState state)
    {
        Sprite targetSprite = null;
        Vector2 hotspot = Vector2.zero;
        
        switch (state)
        {
            case CursorState.Default:
                targetSprite = instance.defaultSprite;
                hotspot = instance.defaultHotspot;
                break;
            case CursorState.Click:
                targetSprite = instance.clickSprite;
                hotspot = instance.clickHotspot;
                break;
            case CursorState.Grab:
                targetSprite = instance.grabSprite;
                hotspot = instance.grabHotspot;
                break;
        }
        
        if (targetSprite != null && targetSprite.texture != null)
        {
            Cursor.SetCursor(targetSprite.texture, hotspot, CursorMode.Auto);
        }
    }
    
    private void ReturnToDefault()
    {
        if (!isGrabbing)
        {
            SetState(defaultState);
        }
    }
    
    void OnDestroy()
    {
        if (!useHardwareCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}