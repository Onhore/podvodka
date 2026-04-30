using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class FullscreenImage : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Image targetImage;
    public CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    public float fadeDuration = 0.3f;
    public float scaleDuration = 0.4f;
    public float openDelay = 0.1f;
    public float closeDelay = 0.1f;
    public Ease fadeEase = Ease.OutQuad;
    public Ease scaleEase = Ease.OutBack;
    public float startScale = 0.8f;
    
    [Header("Background")]
    public Image backgroundOverlay;
    public float backgroundFadeDuration = 0.3f;
    public Color backgroundColor = new Color(0, 0, 0, 0.85f);
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent onOpen;   // Событие при открытии
    public UnityEngine.Events.UnityEvent onClose;  // Событие при закрытии
    
    [Header("Settings")]
    public bool closeOnBackgroundClick = true;
    public bool blockRaycastsWhenOpen = true;
    
    private RectTransform imageRect;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Transform originalParent;
    private bool isOpen = false;
    private Canvas parentCanvas;
    private CanvasGroup blockerCanvasGroup;
    private GameObject blockerObject;
    
    void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        
        imageRect = targetImage.GetComponent<RectTransform>();
        
        if (canvasGroup == null)
        {
            canvasGroup = targetImage.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
        }
        
        targetImage.gameObject.SetActive(false);
        
        originalParent = targetImage.transform.parent;
        originalPosition = imageRect.anchoredPosition;
        originalScale = targetImage.transform.localScale;
        
        if (backgroundOverlay == null)
            CreateBackgroundOverlay();
        else
            backgroundOverlay.gameObject.SetActive(false);
        
        if (blockRaycastsWhenOpen)
            CreateBlocker();
    }
    
    void CreateBlocker()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            GameObject canvasObj = new GameObject("FullscreenCanvas");
            parentCanvas = canvasObj.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        blockerObject = new GameObject("RaycastBlocker");
        blockerObject.transform.SetParent(parentCanvas.transform, false);
        
        Image blockerImage = blockerObject.AddComponent<Image>();
        blockerImage.color = new Color(0, 0, 0, 0);
        
        blockerCanvasGroup = blockerObject.AddComponent<CanvasGroup>();
        blockerCanvasGroup.alpha = 0;
        blockerCanvasGroup.interactable = false;
        blockerCanvasGroup.blocksRaycasts = false;
        
        RectTransform blockerRect = blockerObject.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        
        blockerObject.SetActive(false);
    }
    
    void CreateBackgroundOverlay()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            GameObject canvasObj = new GameObject("FullscreenCanvas");
            parentCanvas = canvasObj.AddComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        GameObject bgObj = new GameObject("BackgroundOverlay");
        bgObj.transform.SetParent(parentCanvas.transform, false);
        
        backgroundOverlay = bgObj.AddComponent<Image>();
        backgroundOverlay.color = new Color(0, 0, 0, 0);
        
        RectTransform bgRect = backgroundOverlay.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        if (closeOnBackgroundClick)
        {
            Button bgButton = bgObj.AddComponent<Button>();
            bgButton.onClick.AddListener(Close);
            bgButton.targetGraphic = backgroundOverlay;
        }
        
        bgObj.SetActive(false);
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isOpen)
            Close();
        else
            Open();
    }
    
    public void Open()
    {
        if (isOpen) return;
        
        if (blockRaycastsWhenOpen && blockerObject != null)
        {
            blockerObject.SetActive(true);
            blockerCanvasGroup.interactable = true;
            blockerCanvasGroup.blocksRaycasts = true;
        }
        
        DOVirtual.DelayedCall(openDelay, () =>
        {
            targetImage.gameObject.SetActive(true);
            
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                GameObject canvasObj = new GameObject("TempCanvas");
                parentCanvas = canvasObj.AddComponent<Canvas>();
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            
            targetImage.transform.SetParent(parentCanvas.transform, true);
            targetImage.transform.SetAsLastSibling();
            
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            
            canvasGroup.alpha = 0;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            targetImage.transform.localScale = Vector3.one * startScale;
            
            if (backgroundOverlay != null)
            {
                backgroundOverlay.gameObject.SetActive(true);
                backgroundOverlay.DOFade(backgroundColor.a, backgroundFadeDuration);
            }
            
            Sequence sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));
            sequence.Join(targetImage.transform.DOScale(1f, scaleDuration).SetEase(scaleEase));
            sequence.Play();
            
            isOpen = true;
            
            // ВЫЗЫВАЕМ СОБЫТИЕ ОТКРЫТИЯ
            onOpen?.Invoke();
        });
    }
    
    public void Close()
    {
        if (!isOpen) return;
        
        DOVirtual.DelayedCall(closeDelay, () =>
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase));
            sequence.Join(targetImage.transform.DOScale(startScale, scaleDuration).SetEase(Ease.InBack));
            
            if (backgroundOverlay != null)
                sequence.Join(backgroundOverlay.DOFade(0f, backgroundFadeDuration));
            
            sequence.OnComplete(() =>
            {
                targetImage.gameObject.SetActive(false);
                
                targetImage.transform.SetParent(originalParent, true);
                targetImage.transform.localScale = originalScale;
                imageRect.anchoredPosition = originalPosition;
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                
                if (backgroundOverlay != null)
                    backgroundOverlay.gameObject.SetActive(false);
                
                if (blockRaycastsWhenOpen && blockerObject != null)
                {
                    blockerCanvasGroup.interactable = false;
                    blockerCanvasGroup.blocksRaycasts = false;
                    blockerObject.SetActive(false);
                }
            });
            
            sequence.Play();
            isOpen = false;
            
            // ВЫЗЫВАЕМ СОБЫТИЕ ЗАКРЫТИЯ
            onClose?.Invoke();
        });
    }
    
    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }
    
    public bool IsOpen => isOpen;
}