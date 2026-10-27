using UnityEngine;

public class CombatStatusPanel : MonoBehaviour
{
    public static Rect Panel => new Rect(Screen.width - 275, Screen.height - 250, 260, 160);
    private void OnGUI()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var reactor = player.GetComponent<ShipReactor>();
        var abilities = player.GetComponent<ShipAbilities>();
        var weapon = player.GetComponent<ShipWeapon>();
        GUILayout.BeginArea(Panel, GUI.skin.box);
        if (reactor != null)
        {
            GUILayout.Label($"Энергия: {reactor.Energy:F0}/{reactor.maxEnergy:F0}");
            GUILayout.Label($"Нагрев: {reactor.Heat:F0}% {(reactor.Overheated ? "ПЕРЕГРЕВ" : "")}");
        }
        if (abilities != null)
        {
            GUILayout.Label(abilities.isAfterburnerActive ? $"Форсаж: {abilities.AfterburnerRemaining:F1} сек." : $"Форсаж: КД {Mathf.Max(0, abilities.afterburnerCooldown):F1}");
            GUILayout.Label($"Щит+: {Mathf.Max(0, abilities.shieldBoostCooldown):F1}  РЭБ: {Mathf.Max(0, abilities.emPulseCooldown):F1}");
        }
        if (weapon != null)
        {
            if (weapon.IsTargetingDisrupted) GUILayout.Label($"РЭБ: {weapon.TargetingDisruptionRemaining:F1} сек.");
            if (weapon.Target != null)
            {
                var enemy = weapon.Target.GetComponent<ShipWeapon>();
                if (enemy != null && enemy.IsTargetingDisrupted)
                    GUILayout.Label($"РЭБ цели: {enemy.TargetingDisruptionRemaining:F1} сек.");
            }
        }
        GUILayout.EndArea();
    }
}
