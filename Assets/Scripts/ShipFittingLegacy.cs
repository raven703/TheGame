using UnityEngine;

/// <summary>
/// LEGACY: старая IMGUI-версия фитинга. Отключена.
/// Заменена на FittingWindow (UGUI). Оставлена на случай отката.
/// </summary>
public class ShipFittingLegacy : MonoBehaviour
{
    private void Awake()
    {
        enabled = false;
    }
}