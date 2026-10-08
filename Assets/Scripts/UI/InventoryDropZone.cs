using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop-зона инвентаря. Принимает drop из FittingSlot — снимает предмет со слота
/// и возвращает его в инвентарь (создаёт FittingItemView в контейнере).
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    [Tooltip("Ссылка на окно фитинга.")]
    public FittingWindow window;

    public void OnDrop(PointerEventData eventData)
    {
        if (window == null) return;

        // Источник — FittingSlot
        var sourceSlot = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<FittingSlot>()
            : null;

        if (sourceSlot != null && sourceSlot.Item != null)
        {
            window.HandleUnequip(sourceSlot);
        }
    }
}