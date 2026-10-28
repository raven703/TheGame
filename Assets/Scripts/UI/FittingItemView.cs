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
public class FittingItemView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
    private int originalIndex;
    private GameObject placeholder;
    private bool dragging;
    private Vector3 pointerOffset;

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

        CombatUIStyle.CardVisual(background, nameLabel, item.allowedGroup, item.name, false);
        var layout=CombatUIStyle.Ensure<LayoutElement>(gameObject);layout.preferredHeight=64;layout.minHeight=64;layout.preferredWidth=184;
        if (descriptionLabel != null) { descriptionLabel.text = ""; descriptionLabel.raycastTarget=false; }
    }

    // ==================== DRAG ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null || window == null || !window.CanEdit) return;

        originalParent = transform.parent;
        originalIndex=transform.GetSiblingIndex(); dragging=true;
        originalAnchoredPosition = rectTransform.anchoredPosition;
        placeholder=new GameObject("DragPlaceholder",typeof(RectTransform),typeof(LayoutElement));
        placeholder.transform.SetParent(originalParent,false);placeholder.transform.SetSiblingIndex(originalIndex);
        var layout=placeholder.GetComponent<LayoutElement>();layout.preferredWidth=rectTransform.rect.width;layout.preferredHeight=rectTransform.rect.height;
        var size=rectTransform.rect.size;
        Vector3 pointer; RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)window.dragLayer,eventData.position,eventData.pressEventCamera,out pointer);
        pointerOffset=rectTransform.position-pointer;

        // Переносим в DragLayer, чтобы был поверх всего
        transform.SetParent(window.dragLayer, true);
        rectTransform.anchorMin=rectTransform.anchorMax=new Vector2(.5f,.5f);
        rectTransform.sizeDelta=new Vector2(170,64);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false; // чтобы drop прошёл под элементом
        canvasGroup.alpha = 0.85f;
        window.HighlightDestinations(item,null);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector3 pointer;RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)window.dragLayer,eventData.position,eventData.pressEventCamera,out pointer);
        rectTransform.position = pointer+pointerOffset;

        // Подсветка слота под курсором
        window.HighlightDestinations(item,null);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CancelDrag();
    }
    public void CancelDrag()
    {
        if(!dragging)return;dragging=false;
        if(window!=null)window.HighlightDestinations(null,null);
        if(placeholder!=null){Destroy(placeholder);placeholder=null;}

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
            transform.SetSiblingIndex(originalIndex);
            rectTransform.anchoredPosition = originalAnchoredPosition;
        }
    }
    private void OnDestroy(){if(placeholder!=null)Destroy(placeholder);}
    public void OnPointerEnter(PointerEventData eventData){if(window!=null)window.DescribeItem(item,rectTransform);}
    public void OnPointerExit(PointerEventData eventData){if(window!=null)window.HideSlotTooltip();}
    private void OnDisable(){CancelDrag();}

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
}
