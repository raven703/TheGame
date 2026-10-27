using UnityEngine;

// Effects are rendered in the unified player HUD, not a duplicate resource panel.
public class CombatStatusPanel : MonoBehaviour
{
    public static Rect Panel => new Rect(0, 0, 0, 0);
    public static string Effects(GameObject player)
    {
        if (player == null) return "Корабль недоступен";
        string text = "";
        var a = player.GetComponent<ShipAbilities>();
        var w = player.GetComponent<ShipWeapon>();
        if (a != null)
            text = (a.isAfterburnerActive ? $"Форсаж: {a.AfterburnerRemaining:F1} с" : $"Форсаж КД: {Mathf.Max(0,a.afterburnerCooldown):F1} с") +
                $"\nЩит+ КД: {Mathf.Max(0,a.shieldBoostCooldown):F1} с; РЭБ КД: {Mathf.Max(0,a.emPulseCooldown):F1} с";
        if (w != null && w.IsTargetingDisrupted) text += $"\nПодавление захвата: {w.TargetingDisruptionRemaining:F1} с";
        var enemy = w != null && w.Target != null ? w.Target.GetComponent<ShipWeapon>() : null;
        if (enemy != null && enemy.IsTargetingDisrupted) text += $"\nРЭБ цели: {enemy.TargetingDisruptionRemaining:F1} с";
        return text;
    }
}