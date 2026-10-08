/// <summary>
/// Логические группы слотов корабля.
/// Используется в FittingItem, FittingSlot, FittingWindow, ShipLoadout.
/// </summary>
public enum SlotGroup
{
    /// <summary>1 Группа: Верхние слоты (Оружие).</summary>
    Top_Weapons,

    /// <summary>3 Группа: Центральные слоты (Защита / Щиты).</summary>
    Center_Shields,

    /// <summary>2 Группа: Нижние слоты (Двигатели / Активки).</summary>
    Bottom_Engines,

    /// <summary>Свободный инвентарь справа.</summary>
    Inventory
}