using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-элемент инвентаря. Можно перетащить в FittingSlot.
/// При перетаскивании подсвечивает слоты нужной группы.
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

    // Текущий подсвеченный слот (чтобы снять подсветку при перемещении)
    private FittingSlot lastHighlightedSlot;

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

    // ==================== DRAG ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;

        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        // Переносим в DragLayer, чтобы был поверх всего
        transform.SetParent(window != null ? window.dragLayer : originalParent, false);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false; // чтобы drop прошёл под элементом
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (item == null) return;

        rectTransform.position = eventData.position;

        // Подсветка слота под курсором
        HighlightSlotUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (item == null) return;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Снимаем подсветку с последнего слота
        if (lastHighlightedSlot != null)
        {
            lastHighlightedSlot.SetHighlight(false);
            lastHighlightedSlot = null;
        }

        // Если drop не был обработан слотом — возвращаем на место
        if (transform.parent == (window != null ? window.dragLayer : null))
        {
            transform.SetParent(originalParent, false);
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Двойной клик — автоустановка в первый свободный слот нужной группы
        if (eventData.clickCount == 2 && window != null)
        {
            window.HandleAutoEquip(this);
        }
    }

    // ==================== HIGHLIGHT ====================

    /// <summary>
    /// Подсвечивает слот под курсором, если группа слота совпадает с группой модуля.
    /// </summary>
    private void HighlightSlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return;

        // Raycast по всем UI-элементам под курсором
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        FittingSlot slotUnderPointer = null;
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponentInParent<FittingSlot>();
            if (slot != null)
            {
                slotUnderPointer = slot;
                break;
            }
        }

        // Снимаем подсветку с прошлого
        if (lastHighlightedSlot != null && lastHighlightedSlot != slotUnderPointer)
        {
            lastHighlightedSlot.SetHighlight(false);
        }

        // Подсвечиваем новый, если группа совпадает
        if (slotUnderPointer != null && item != null && slotUnderPointer.group == item.allowedGroup)
        {
            slotUnderPointer.SetHighlight(true);
            lastHighlightedSlot = slotUnderPointer;
        }
        else
        {
            lastHighlightedSlot = null;
        }
    }
}