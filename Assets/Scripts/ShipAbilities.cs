using System.Collections;
using UnityEngine;

public class ShipAbilities : MonoBehaviour
{
    private ShipMovement shipMovement;
    private ShipHealth shipHealth;
    private ShipWeapon shipWeapon;
    public float AfterburnerHeatMultiplier { get; set; } = 1f;
    private bool Spend(float energy, float heat)
    {
        var reactor = GetComponent<ShipReactor>();
        return Time.timeScale>0f && !CombatSession.IsPreparing && ShipOrderController.Available(shipHealth) && reactor != null && reactor.TrySpend(energy, heat);
    }

    // Таймеры перезарядки
    public float afterburnerCooldown { get; private set; }
    public float shieldBoostCooldown { get; private set; }
    public float emPulseCooldown { get; private set; }

    public float AfterburnerRemaining => isAfterburnerActive ? Mathf.Max(0f, afterburnerCooldown - 8f) : 0f;
    public bool isAfterburnerActive { get; private set; }

    private void Awake()
    {
        shipMovement = GetComponent<ShipMovement>();
        shipHealth = GetComponent<ShipHealth>();
        shipWeapon = GetComponent<ShipWeapon>();
    }

    private void Update()
    {
        if (afterburnerCooldown > 0f) afterburnerCooldown -= Time.deltaTime;
        if (shieldBoostCooldown > 0f) shieldBoostCooldown -= Time.deltaTime;
        if (emPulseCooldown > 0f) emPulseCooldown -= Time.deltaTime;
    }

    /// <summary> Форсаж (2 группа): +100% к скорости на 2 секунды, КД 10 сек </summary>
    public bool UseAfterburner()
    {
        if(shipHealth==null || shipHealth.Data==null || shipHealth.Data.GetModule(ModuleType.Engine).isDestroyed)return false;
        if (afterburnerCooldown > 0f || isAfterburnerActive || shipMovement == null)
            return false;

        if (!Spend(25f, 20f * AfterburnerHeatMultiplier)) return false;
        CombatSession.Log($"{name}: форсаж на 2 сек.");
        StartCoroutine(AfterburnerRoutine());
        afterburnerCooldown = 10f;
        return true;
    }

    private IEnumerator AfterburnerRoutine()
    {
        isAfterburnerActive = true;
        shipMovement.SpeedMultiplier = 2f;

        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} включил Форсаж!");

        yield return new WaitForSeconds(2f);

        shipMovement.SpeedMultiplier = 1f;
        isAfterburnerActive = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (shipMovement != null) shipMovement.SpeedMultiplier = 1f;
        isAfterburnerActive = false;
    }

    /// <summary> Перегрузка щита (2 группа): +30 HP щита, -10 HP корпуса, КД 8 сек </summary>
    public bool UseShieldBoost()
    {
        if (shieldBoostCooldown > 0f || shipHealth == null || shipHealth.Data == null)
            return false;

        var shieldMod = shipHealth.Data.GetModule(ModuleType.Shield);
        if (shieldMod != null && shieldMod.isDestroyed)
            return false;

        if (!Spend(30f, 15f)) return false;
        CombatSession.Log($"{name}: перегрузка щита");
        shipHealth.Data.Damage.BoostShield(30f, 10f);

        shieldBoostCooldown = 8f;
        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} перегрузил щиты! (+30 Щит, -10 Корпус)");
        return true;
    }

    /// <summary> Импульс помех (2 группа): Сбивает захват цели у врага на 3 сек, КД 12 сек </summary>
    public bool UseEMPulse()
    {
        return UseEMPulseOn(shipWeapon!=null?shipWeapon.Target:null);
    }
    public bool UseEMPulseOn(ShipHealth target)
    {
        if (emPulseCooldown > 0f || shipWeapon == null || !ShipOrderController.Available(target) ||
            shipHealth==null || shipHealth.Data.GetModule(ModuleType.Weapon).isDestroyed)
            return false;

        var enemyWeapon = target.GetComponent<ShipWeapon>();
        if (enemyWeapon == null)
            return false;

        if (!Spend(35f, 25f)) return false;
        CombatSession.Log($"{name}: РЭБ на 3 сек.");
        enemyWeapon.DisruptTargeting(3f);
        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name}: захват цели противника подавлен на 3 секунды.");

        emPulseCooldown = 12f;
        return true;
    }
}