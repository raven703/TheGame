using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TacticalCommandPanel : MonoBehaviour
{
    [Header("Ship References")]
    public ShipWeapon playerWeapon;
    public ShipAbilities playerAbilities;

    [Header("Behavior Buttons")]
    public Button orbitButton;
    public Button keepDistanceButton;
    public Button flankButton;

    [Header("Target Priority Buttons")]
    public Button autoTargetButton;
    public Button weaponTargetButton;
    public Button shieldTargetButton;
    public Button engineTargetButton;

    [Header("Ability Buttons")]
    public Button afterburnerButton;
    public Button shieldBoostButton;
    public Button empButton;

    [Header("Ability Labels (TMP on each button)")]
    public TextMeshProUGUI afterburnerLabel;
    public TextMeshProUGUI shieldBoostLabel;
    public TextMeshProUGUI empLabel;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(1f, 0.85f, 0.2f, 1f);

    private void Start()
    {
        if (playerWeapon == null)
        {
            Debug.LogWarning("[TacticalCommandPanel] playerWeapon не назначен.");
            return;
        }

        // Behavior
        orbitButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.Orbit));
        keepDistanceButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.KeepDistance));
        flankButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.Flank));

        // Target priority
        autoTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(null));
        weaponTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Weapon));
        shieldTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Shield));
        engineTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Engine));

        // Abilities
        afterburnerButton.onClick.AddListener(() => playerAbilities?.UseAfterburner());
        shieldBoostButton.onClick.AddListener(() => playerAbilities?.UseShieldBoost());
        empButton.onClick.AddListener(() => playerAbilities?.UseEMPulse());
    }

    private void Update()
    {
        if (playerWeapon == null) return;

        HighlightBehavior(orbitButton, CombatBehavior.Orbit);
        HighlightBehavior(keepDistanceButton, CombatBehavior.KeepDistance);
        HighlightBehavior(flankButton, CombatBehavior.Flank);

        HighlightTarget(autoTargetButton, null);
        HighlightTarget(weaponTargetButton, ModuleType.Weapon);
        HighlightTarget(shieldTargetButton, ModuleType.Shield);
        HighlightTarget(engineTargetButton, ModuleType.Engine);

        UpdateAbilityButton(afterburnerButton, afterburnerLabel, "Форсаж", playerAbilities ? playerAbilities.afterburnerCooldown : 0f);
        UpdateAbilityButton(shieldBoostButton, shieldBoostLabel, "Щит+", playerAbilities ? playerAbilities.shieldBoostCooldown : 0f);
        UpdateAbilityButton(empButton, empLabel, "РЭБ", playerAbilities ? playerAbilities.emPulseCooldown : 0f);
    }

    private void HighlightBehavior(Button btn, CombatBehavior behavior)
    {
        if (btn == null) return;
        bool isActive = playerWeapon.behavior == behavior;
        SetButtonColor(btn, isActive ? activeColor : normalColor);
    }

    private void HighlightTarget(Button btn, ModuleType? module)
    {
        if (btn == null) return;
        bool isActive = playerWeapon.targetedModule == module;
        SetButtonColor(btn, isActive ? activeColor : normalColor);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var cb = btn.colors;
        cb.normalColor = color;
        cb.selectedColor = color;
        cb.highlightedColor = color * 1.1f;
        btn.colors = cb;
    }

    private void UpdateAbilityButton(Button btn, TextMeshProUGUI label, string baseLabel, float cooldown)
    {
        if (btn == null) return;

        bool ready = cooldown <= 0f;
        btn.interactable = ready;

        if (label != null)
            label.text = ready ? baseLabel : $"{baseLabel}\n({Mathf.CeilToInt(cooldown)}s)";
    }
}