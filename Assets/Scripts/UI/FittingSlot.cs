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
    IBeginDragHandler, IDragHandler, IEndDragHandler
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

        if (item != null)
        {
            background.color = item.iconColor;
            if (label != null) label.text = item.name;
        }
        else
        {
            background.color = isHighlighted ? validDropColor : emptyColor;
            if (label != null) label.text = $"[Слот {index + 1}]";
        }
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
        if (item == null) return;
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
            dragVisual.transform.position = eventData.position;
        }

        if (background != null) background.enabled = false;
        if (label != null) label.enabled = false;

        canvasGroup.blocksRaycasts = false;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
            dragVisual.transform.position = eventData.position;

        HighlightSlotUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        canvasGroup.blocksRaycasts = true;
        isDragging = false;

        if (background != null) background.enabled = true;
        if (label != null) label.enabled = true;

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

        if (item == null)
        {
            background.color = on ? validDropColor : emptyColor;
        }
    }

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