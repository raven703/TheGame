using UnityEngine;

public class CombatStatusPanel : MonoBehaviour
{
    public static Rect Panel => new Rect(Screen.width - 275, Screen.height - 250, 260, 160);
    private TMPro.TextMeshProUGUI label;
    private void Update()
    {
        if (label == null)
        {
            var canvas = FindFirstObjectByType<CombatInterfaceLayout>(); if (canvas == null) return;
            var panel = CombatUIStyle.Panel("CombatResources",canvas.transform,CombatUIStyle.Surface);
            CombatUIStyle.Rect(panel.transform,Vector2.right,Vector2.right,new Vector2(-278,100),new Vector2(-16,220));
            label=CombatUIStyle.Text("Resources",panel.transform,"",15);
            CombatUIStyle.Rect(label.transform,Vector2.zero,Vector2.one,new Vector2(12,10),new Vector2(-12,-10));
        }
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var reactor = player.GetComponent<ShipReactor>();
        var abilities = player.GetComponent<ShipAbilities>();
        var weapon = player.GetComponent<ShipWeapon>();
        label.text = "РЕАКТОР";
        if (reactor != null)
        {
            label.text += $"\nЭнергия: {reactor.Energy:F0}/{reactor.maxEnergy:F0}";
            label.text += $"   Нагрев: {reactor.Heat:F0}% {(reactor.Overheated ? "ПЕРЕГРЕВ" : "")}";
        }
        if (abilities != null)
        {
            label.text += "\n" + (abilities.isAfterburnerActive ? $"Форсаж: {abilities.AfterburnerRemaining:F1} сек." : $"Форсаж: КД {Mathf.Max(0, abilities.afterburnerCooldown):F1}");
            label.text += $"\nЩит+: {Mathf.Max(0, abilities.shieldBoostCooldown):F1}  РЭБ: {Mathf.Max(0, abilities.emPulseCooldown):F1}";
        }
        if (weapon != null)
        {
            if (weapon.IsTargetingDisrupted) label.text += $"\nРЭБ: {weapon.TargetingDisruptionRemaining:F1} сек.";
            if (weapon.Target != null)
            {
                var enemy = weapon.Target.GetComponent<ShipWeapon>();
                if (enemy != null && enemy.IsTargetingDisrupted)
                    label.text += $"\nРЭБ цели: {enemy.TargetingDisruptionRemaining:F1} сек.";
            }
        }
    }
}
