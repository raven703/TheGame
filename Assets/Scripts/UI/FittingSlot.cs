using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-слот фитинга. Принимает drop от FittingItemView и от других FittingSlot.
/// Сам может быть источником drag (для тачскрина, где ПКМ недоступен).
/// </summary>
[RequireComponent(typeof(Image))]
public class FittingSlot : MonoBehaviour, IDropHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler
{
    [Header("UI References")]
    public Image background;
    public TextMeshProUGUI label;

    [Header("Colors")]
    public Color emptyColor = new Color(0.15f, 0.18f, 0.25f, 0.9f);
    public Color filledColor = new Color(0.2f, 0.22f, 0.3f, 1f);
    public Color validDropColor = new Color(0.2f, 0.5f, 0.2f, 1f);

    public SlotGroup group;
    public int index;
    public FittingWindow window;

    private FittingItem item;
    public FittingItem Item => item;

    private CanvasGroup canvasGroup;
    private GameObject dragVisual;
    private bool isDragging = false;
    private bool isHighlighted = false;
    private FittingSlot lastHighlightedSlot;
    private Vector3 dragOffset;

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void SetItem(FittingItem newItem)
    {
        item = newItem;
        isHighlighted = false;   // сброс подсветки при любом изменении
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();

        CombatUIStyle.CardVisual(background,label,group,item==null?"Пустой слот "+(index+1):item.name,isHighlighted);
    }

    // ==================== DROP ====================

    public void OnDrop(PointerEventData eventData)
    {
        if (window == null) return;

        var itemView = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingItemView>()
            : null;

        if (itemView != null && itemView.item != null && itemView.item.allowedGroup == group)
        {
            window.HandleDropFromInventory(itemView, this);
            return;
        }

        var sourceSlot = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingSlot>()
            : null;

        if (sourceSlot != null && sourceSlot != this && sourceSlot.Item != null)
        {
            window.HandleDropFromSlot(sourceSlot, this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (window != null && item != null)
                window.HandleUnequip(this);
        }
    }

    // ==================== DRAG ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null || window == null || !window.CanEdit) return;
        if (window == null) return;

        if (window.itemViewPrefab != null && window.dragLayer != null)
        {
            var view = Instantiate(window.itemViewPrefab, window.dragLayer);
            view.window = window;
            view.SetItem(item);
            view.enabled = false;

            var cg = view.GetComponent<CanvasGroup>();
            if (cg == null) cg = view.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            dragVisual = view.gameObject;
            var rect=(RectTransform)dragVisual.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.sizeDelta=((RectTransform)transform).rect.size;
            Vector3 pointer;RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)window.dragLayer,eventData.position,eventData.pressEventCamera,out pointer);
            dragOffset=transform.position-pointer;
            dragVisual.transform.position=pointer+dragOffset;
        }

        if (label != null) label.color = new Color(1,1,1,.35f);

        canvasGroup.blocksRaycasts = false;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            Vector3 pointer;RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)window.dragLayer,eventData.position,eventData.pressEventCamera,out pointer);
            dragVisual.transform.position=pointer+dragOffset;
        }

        HighlightSlotUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CancelDrag();
    }
    public void CancelDrag()
    {
        if (!isDragging) return;

        canvasGroup.blocksRaycasts = true;
        isDragging = false;

        if (background != null) background.enabled = true;
        if (label != null) label.enabled = true;
        RefreshVisual();

        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }

        if (lastHighlightedSlot != null)
        {
            lastHighlightedSlot.SetHighlight(false);
            lastHighlightedSlot = null;
        }
    }

    // ==================== HIGHLIGHT ====================

    public void SetHighlight(bool on)
    {
        isHighlighted = on;

        if (background == null) background = GetComponent<Image>();

        RefreshVisual();
    }
    private void OnDisable(){CancelDrag();}
    public void OnPointerEnter(PointerEventData eventData){if(window!=null)window.ShowDescription(item);}

    private void HighlightSlotUnderPointer(PointerEventData eventData)
    {
        if (EventSystem.current == null) return;

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

        if (lastHighlightedSlot != null && lastHighlightedSlot != slotUnderPointer)
        {
            lastHighlightedSlot.SetHighlight(false);
        }

        if (slotUnderPointer != null && slotUnderPointer != this
            && item != null && slotUnderPointer.group == item.allowedGroup)
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