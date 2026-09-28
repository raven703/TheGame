using System.Collections;
using UnityEngine;

public class ShipAbilities : MonoBehaviour
{
    private ShipMovement shipMovement;
    private ShipHealth shipHealth;
    private ShipWeapon shipWeapon;

    // Таймеры перезарядки
    public float afterburnerCooldown { get; private set; }
    public float shieldBoostCooldown { get; private set; }
    public float emPulseCooldown { get; private set; }

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
        if (afterburnerCooldown > 0f || isAfterburnerActive || shipMovement == null)
            return false;

        StartCoroutine(AfterburnerRoutine());
        afterburnerCooldown = 10f;
        return true;
    }

    private IEnumerator AfterburnerRoutine()
    {
        isAfterburnerActive = true;
        float originalSpeed = shipMovement.maxSpeed;
        float originalAccel = shipMovement.acceleration;

        shipMovement.maxSpeed *= 2f;
        shipMovement.acceleration *= 2f;

        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} включил Форсаж!");

        yield return new WaitForSeconds(2f);

        shipMovement.maxSpeed = originalSpeed;
        shipMovement.acceleration = originalAccel;
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

        shipHealth.Data.currentShieldHP = Mathf.Min(shipHealth.Data.maxShieldHP, shipHealth.Data.currentShieldHP + 30f);
        shipHealth.Data.TakeHullDamage(10f);

        shieldBoostCooldown = 8f;
        Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} перегрузил щиты! (+30 Щит, -10 Корпус)");
        return true;
    }

    /// <summary> Импульс помех (2 группа): Сбивает захват цели у врага на 3 сек, КД 12 сек </summary>
    public bool UseEMPulse()
    {
        if (emPulseCooldown > 0f || shipWeapon == null || shipWeapon.Target == null)
            return false;

        var enemyWeapon = shipWeapon.Target.GetComponent<ShipWeapon>();
        if (enemyWeapon != null)
        {
            enemyWeapon.ClearTarget();
            enemyWeapon.manualMoveOrder = true; // Сбиваем автопилот
            Debug.Log($"[АКТИВАЦИЯ] {gameObject.name} запустил Импульс Помех! Захват цели у {shipWeapon.Target.name} сбит!");
        }

        emPulseCooldown = 12f;
        return true;
    }
}