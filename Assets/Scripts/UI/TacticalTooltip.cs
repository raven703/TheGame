using UnityEngine;
using UnityEngine.EventSystems;

public class TacticalTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TacticalCommandPanel panel;
    public string description;
    public void OnPointerEnter(PointerEventData eventData) { if(panel!=null)panel.ShowTooltip(description, (RectTransform)transform); }
    public void OnPointerExit(PointerEventData eventData) { if(panel!=null)panel.ShowTooltip(null); }
    private void OnDisable() { if(panel!=null)panel.ShowTooltip(null); }
}