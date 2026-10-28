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
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
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
        item = newItem != null && !string.IsNullOrEmpty(newItem.id) ? newItem : null;
        isHighlighted = false;   // сброс подсветки при любом изменении
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();

        string title=item==null?"":item.name;
        CombatUIStyle.CardVisual(background,label,group,title,isHighlighted);
        if(window!=null)
        {
            var iconTransform=transform.Find("MountIcon");
            Image icon;
            if(iconTransform==null)
            {
                var go=new GameObject("MountIcon",typeof(RectTransform),typeof(Image));
                go.transform.SetParent(transform,false);icon=go.GetComponent<Image>();
            }
            else icon=iconTransform.GetComponent<Image>();
            icon.sprite=Resources.Load<Sprite>("FittingIcons/"+window.SlotIcon(this));
            icon.raycastTarget=false;icon.preserveAspect=true;
            CombatUIStyle.Rect(icon.transform,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(7,-15),new Vector2(37,15));
            if(label!=null)
            {
                CombatUIStyle.Rect(label.transform,Vector2.zero,Vector2.one,new Vector2(43,6),new Vector2(-5,-6));
                label.overflowMode=TextOverflowModes.Overflow;label.textWrappingMode=TextWrappingModes.Normal;
                var layout=CombatUIStyle.Ensure<LayoutElement>(label.gameObject);layout.ignoreLayout=true;
            }
            var number=CombatUIStyle.Text("SlotNumber",transform,(index+1).ToString(),11);
            CombatUIStyle.Rect(number.transform,Vector2.zero,Vector2.one,new Vector2(7,1),new Vector2(-7,-48));
            number.alignment=TextAlignmentOptions.BottomLeft;number.raycastTarget=false;
            CombatUIStyle.Ensure<LayoutElement>(number.gameObject).ignoreLayout=true;
            CombatUIStyle.Ensure<LayoutElement>(icon.gameObject).ignoreLayout=true;
        }
        if (isHighlighted)
        {
            background.color = new Color(.30f,.48f,.32f,1f);
            var outline = GetComponent<Outline>();
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.48f,.95f,.59f,1f);
            outline.effectDistance = new Vector2(2f,-2f);
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
            rect.sizeDelta=new Vector2(170,64);
            Vector3 pointer;RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)window.dragLayer,eventData.position,eventData.pressEventCamera,out pointer);
            dragOffset=transform.position-pointer;
            dragVisual.transform.position=pointer+dragOffset;
        }

        if (label != null) label.color = new Color(1,1,1,.35f);

        canvasGroup.blocksRaycasts = false;
        isDragging = true;
        window.HighlightDestinations(item,this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            Vector3 pointer;RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)window.dragLayer,eventData.position,eventData.pressEventCamera,out pointer);
            dragVisual.transform.position=pointer+dragOffset;
        }

        if(isDragging)window.HighlightDestinations(item,this);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        CancelDrag();
    }
    public void CancelDrag()
    {
        if (!isDragging) return;
        if(window!=null)window.HighlightDestinations(null,null);

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
    public void OnPointerEnter(PointerEventData eventData){if(window!=null)window.DescribeSlot(this);}
    public void OnPointerExit(PointerEventData eventData){if(window!=null)window.HideSlotTooltip();}

}
