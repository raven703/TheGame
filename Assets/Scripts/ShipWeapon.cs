using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Тактические режимы поведения корабля в бою
/// </summary>
public enum CombatBehavior
{
    Orbit,          // Огибать цель по орбите
    KeepDistance,   // Кайтить (держать дистанцию)
    Flank           // Заходить во фланг/корму
}

public class ShipWeapon : MonoBehaviour
{
    [Header("Weapon & Projectile Setup")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    // Свойство для совместимости с автотестами (Step5Verify) и другими скриптами:
    public Transform weaponPoint
    {
        get => firePoint != null ? firePoint : transform;
        set => firePoint = value;
    }
    public float bulletSpeed = 15f;
    public float range = 8f;
    public float baseCooldown = 0.6f;
    public float firingArcAngle = 60f;

    [Header("Tactics & AI")]
    public CombatBehavior behavior = CombatBehavior.Orbit;
    public bool orbitClockwise = true;

    [Tooltip("Приоритетный модуль для выбивания (null = авто-распределение урона)")]
    public ModuleType? targetedModule = null;

    [HideInInspector] public bool manualMoveOrder = false;

    private List<FittingItem> activeWeapons = new List<FittingItem>();
    private float nextFireTime = 0f;

    private ShipHealth target;
    public ShipHealth Target
    {
        get => target;
        private set
        {
            if (target != value)
            {
                target = value;
                OnTargetChanged?.Invoke(target);
            }
        }
    }

    public event Action<ShipHealth> OnTargetChanged;

    private ShipHealth ownerHealth;
    public ShipHealth OwnerHealth => ownerHealth;
    private ShipMovement shipMovement;

    private float orbitChangeTimer = 0f;
    private float randomRadiusOffset = 0f;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<ShipHealth>();
        shipMovement = GetComponentInParent<ShipMovement>();
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();
    }

    public void SetBehavior(CombatBehavior newBehavior) => behavior = newBehavior;
    public void SetTargetModule(ModuleType? module) => targetedModule = module;
    public void SetTarget(ShipHealth newTarget) { Target = newTarget; manualMoveOrder = false; }
    public void ClearTarget() => Target = null;

    public void SetEquippedWeapons(List<FittingItem> weapons)
    {
        activeWeapons = weapons;
    }

    private void Update()
    {
        if (ownerHealth == null || ownerHealth.Data == null)
            return;

        var weaponModule = ownerHealth.Data.GetModule(ModuleType.Weapon);
        if (ownerHealth.Data.IsDestroyed || weaponModule == null || weaponModule.isDestroyed)
            return;

        // Сброс цели при её уничтожении
        if (Target != null && (Target.Data == null || Target.Data.IsDestroyed))
        {
            Target = null;
        }

        // Автоматический выбор маневра
        if (Target != null && !manualMoveOrder)
        {
            UpdateCombatTactics();
        }

        // Проверка ввода пользователя (автострельба по цели или ручная стрельба)
        bool isManualFiring = false;
#if ENABLE_INPUT_SYSTEM
        isManualFiring = (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                         (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
#else
        isManualFiring = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
#endif

        bool canAutoFire = Target != null && Vector2.Distance(transform.position, Target.transform.position) <= range && IsTargetInFiringArc(Target.transform.position);

        if ((isManualFiring || canAutoFire) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + baseCooldown;
        }
    }

    private void UpdateCombatTactics()
    {
        if (shipMovement == null || Target == null) return;

        Vector3 enemyPos = Target.transform.position;
        Vector3 myPos = transform.position;
        Vector3 dirToEnemy = (enemyPos - myPos).normalized;
        float currentDistance = Vector2.Distance(myPos, enemyPos);

        orbitChangeTimer -= Time.deltaTime;
        if (orbitChangeTimer <= 0f)
        {
            orbitChangeTimer = UnityEngine.Random.Range(3f, 6f);
            randomRadiusOffset = UnityEngine.Random.Range(-1.2f, 1.2f);

            if (UnityEngine.Random.value < 0.3f)
                orbitClockwise = !orbitClockwise;
        }

        float optimalDistance = Mathf.Clamp(range * 0.65f + randomRadiusOffset, 2f, range * 0.9f);

        switch (behavior)
        {
            case CombatBehavior.Orbit:
                Vector3 tangent = orbitClockwise
                    ? new Vector3(-dirToEnemy.y, dirToEnemy.x, 0f)
                    : new Vector3(dirToEnemy.y, -dirToEnemy.x, 0f);

                float radialCorrection = (currentDistance - optimalDistance);
                Vector3 orbitDestination = myPos + tangent * 4f + dirToEnemy * radialCorrection;

                shipMovement.SetTargetPosition(orbitDestination);
                break;

            case CombatBehavior.KeepDistance:
                if (currentDistance < optimalDistance)
                {
                    Vector3 retreatPoint = myPos - dirToEnemy * 4f;
                    shipMovement.SetTargetPosition(retreatPoint);
                }
                else
                {
                    shipMovement.SetTargetPosition(enemyPos);
                }
                break;

            case CombatBehavior.Flank:
                Vector3 enemyForward = Target.transform.right;
                var targetMovement = Target.GetComponent<ShipMovement>();
                if (targetMovement != null)
                {
                    enemyForward = targetMovement.GetForwardVector();
                }

                Vector3 rearPosition = enemyPos - enemyForward * optimalDistance;
                shipMovement.SetTargetPosition(rearPosition);
                break;
        }
    }

    private bool IsTargetInFiringArc(Vector3 targetPos)
    {
        Vector2 dirToTarget = (targetPos - transform.position).normalized;
        Vector2 forward = shipMovement != null ? shipMovement.GetForwardVector() : (Vector2)transform.up;

        float angle = Vector2.Angle(forward, dirToTarget);
        return angle <= (firingArcAngle * 0.5f);
    }

    private void Shoot()
    {
        Transform spawnPoint = firePoint != null ? firePoint : transform;

        // Сколько установленных стволов — столько и выстрелов за один залп
        int weaponCount = activeWeapons.Count > 0 ? activeWeapons.Count : 1;

        float spacing = 0.35f;
        float startOffset = -(weaponCount - 1) * spacing * 0.5f;

        for (int i = 0; i < weaponCount; i++)
        {
            FittingItem weaponModule = activeWeapons.Count > 0 ? activeWeapons[i] : null;
            float currentDamage = weaponModule != null ? weaponModule.damageBonus : 15f;
            Color bulletColor = weaponModule != null ? weaponModule.iconColor : Color.yellow;

            Vector3 offset = spawnPoint.right * (startOffset + i * spacing);
            Vector3 spawnPos = spawnPoint.position + offset;

            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnPoint.rotation);

                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = spawnPoint.up * bulletSpeed;
                }

                var renderer = bullet.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = bulletColor;
                }
            }
            else
            {
                // Если префаб снаряда не назначен, мгновенно передаем урон по выбранному модулю
                if (Target != null)
                {
                    Target.TakeDamage(currentDamage, targetedModule);
                }
                Debug.DrawRay(spawnPos, spawnPoint.up * 5f, bulletColor, 0.2f);
            }
        }
    }
}