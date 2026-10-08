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

    /// <summary>Какая группа слотов (Weapon/Shield/Engine).</summary>
    public SlotGroup group;

    /// <summary>Индекс слота внутри группы.</summary>
    public int index;

    /// <summary>Ссылка на окно фитинга.</summary>
    public FittingWindow window;

    private FittingItem item;
    public FittingItem Item => item;

    // Drag
    private CanvasGroup canvasGroup;
    private GameObject dragVisual;
    private bool isDragging = false;

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
            background.color = emptyColor;
            if (label != null) label.text = $"[Слот {index + 1}]";
        }
    }

    // ==================== DROP ====================

    public void OnDrop(PointerEventData eventData)
    {
        if (window == null) return;

        // Источник — FittingItemView (инвентарь)
        var itemView = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingItemView>()
            : null;

        if (itemView != null && itemView.item != null && itemView.item.allowedGroup == group)
        {
            window.HandleDropFromInventory(itemView, this);
            return;
        }

        // Источник — другой FittingSlot
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
        // ПКМ = снять предмет из слота обратно в инвентарь (для мыши).
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (window != null && item != null)
                window.HandleUnequip(this);
        }
    }

    // ==================== DRAG ====================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;           // пустой слот не тащим
        if (window == null) return;

        // Создаём временный визуал в DragLayer
        if (window.itemViewPrefab != null && window.dragLayer != null)
        {
            var view = Instantiate(window.itemViewPrefab, window.dragLayer);
            view.window = window;
            view.SetItem(item);

            // Отключаем компонент, чтобы сам view не пытался начать свой drag.
            view.enabled = false;

            dragVisual = view.gameObject;
            dragVisual.transform.position = eventData.position;
        }

        // Скрываем визуал слота (фон + текст), но сам слот остаётся
        if (background != null) background.enabled = false;
        if (label != null) label.enabled = false;

        canvasGroup.blocksRaycasts = false;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
            dragVisual.transform.position = eventData.position;
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

        // Если drop не был обработан слотом или зоной инвентаря — слот остаётся как был.
    }
}