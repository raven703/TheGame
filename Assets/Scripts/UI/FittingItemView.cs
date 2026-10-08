using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-элемент инвентаря. Можно перетащить в FittingSlot.
/// </summary>
[RequireComponent(typeof(Image))]
public class FittingItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image background;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI descriptionLabel;

    /// <summary>Данные модуля.</summary>
    public FittingItem item;

    /// <summary>Ссылка на окно фитинга.</summary>
    public FittingWindow window;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>
    /// Устанавливает данные предмета и обновляет UI.
    /// </summary>
    public void SetItem(FittingItem newItem)
    {
        item = newItem;
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (item == null) return;

        if (background != null) background.color = item.iconColor;
        if (nameLabel != null) nameLabel.text = item.name;
        if (descriptionLabel != null) descriptionLabel.text = item.description;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // Временно переносим в корень Canvas, чтобы элемент был поверх всего
        transform.SetParent(window != null ? window.dragLayer : originalParent, false);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false; // чтобы drop прошёл под элементом
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (item == null) return;
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (item == null) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Если drop не был обработан слотом — возвращаем на место
        if (transform.parent == (window != null ? window.dragLayer : null))
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Двойной клик по элементу инвентаря — можно сделать автоустановку в первый свободный слот
        if (eventData.clickCount == 2 && window != null)
        {
            window.HandleAutoEquip(this);
        }
    }
}