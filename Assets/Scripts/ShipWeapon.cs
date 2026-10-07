using System;
using System.Collections;
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

    [Header("Fallback Weapon (если фитинг не задан)")]
    [Tooltip("Если true и фитинг не установил ни одного оружия — стреляем fallback-оружием (для врагов без ShipFitting).")]
    public bool useFallbackWeapon = false;
    public float fallbackDamage = 0.5f;
    public Color fallbackColor = new Color(1f, 0.3f, 0.3f);
    public WeaponVisualType fallbackVisual = WeaponVisualType.Laser;

    [Header("Shot Visuals")]
    [Tooltip("Длительность отрисовки следа выстрела в секундах.")]
    public float laserVisualDuration = 0.1f;
    public float plasmaVisualDuration = 0.15f;
    [Tooltip("Ширина лазерного луча.")]
    public float laserWidth = 0.05f;
    [Tooltip("Ширина плазменного луча.")]
    public float plasmaWidth = 0.25f;

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

    // Материал для временных LineRenderer'ов (создаётся один раз)
    private Material lineMaterial;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<ShipHealth>();
        shipMovement = GetComponentInParent<ShipMovement>();
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();

        // Fallback: если Fire Point не назначен — ищем дочерний объект "WeaponPoint"
        if (firePoint == null)
        {
            var found = transform.Find("WeaponPoint");
            if (found != null)
                firePoint = found;
        }

        // Общий материал для LineRenderer'ов выстрелов.
        // В URP 2D "Sprites/Default" может быть вырезан — пробуем несколько вариантов.
        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                  ?? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Legacy Shaders/Diffuse");

        if (shader != null)
            lineMaterial = new Material(shader);
        else
            Debug.LogWarning("[ShipWeapon] Не найден ни один шейдер для LineRenderer, линия выстрела может быть невидимой.");
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

        // При паузе оружие не стреляет: Time.time не растёт, следующий выстрел не перезаряжается
        if (Time.timeScale <= 0f)
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
        isManualFiring = Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        isManualFiring = Input.GetMouseButton(0);
#endif

        if ((isManualFiring || Target != null) && Time.time >= nextFireTime)
        {
            if (Target != null)
            {
                // Если цель есть — стреляем ТОЛЬКО когда она в радиусе и в конусе прицела.
                float dist = Vector2.Distance(transform.position, Target.transform.position);
                bool inRange = dist <= range;
                bool inArc = IsTargetInFiringArc(Target.transform.position);

                if (inRange && inArc)
                {
                    Shoot();
                    nextFireTime = Time.time + baseCooldown;
                }
                // Иначе — не стреляем вообще (даже при зажатой ЛКМ).
            }
            else if (isManualFiring)
            {
                // Цели нет — ручной выстрел «вперёд в пустоту» (только визуал, урона нет).
                Shoot();
                nextFireTime = Time.time + baseCooldown;
            }
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
        // Защитная проверка конуса/радиуса: если есть цель, но она вне — не стреляем.
        // (На случай, если Shoot вызовут из другого места: AI, тесты, будущие скрипты.)
        if (Target != null)
        {
            float dist = Vector2.Distance(transform.position, Target.transform.position);
            if (dist > range || !IsTargetInFiringArc(Target.transform.position))
                return;
        }

        Transform spawnPoint = firePoint != null ? firePoint : transform;

        // Собираем список выстрелов: либо из установленного фитинга, либо fallback
        bool useFitting = activeWeapons != null && activeWeapons.Count > 0;

        if (!useFitting && !useFallbackWeapon)
            return;

        int weaponCount = useFitting ? activeWeapons.Count : 1;

        // Разнос стволов перпендикулярно направлению стрельбы (по "right" точки спавна)
        float spacing = 0.35f;
        float startOffset = -(weaponCount - 1) * spacing * 0.5f;

        Vector2 forward = shipMovement != null
            ? shipMovement.GetForwardVector()
            : (Vector2)spawnPoint.up;

        for (int i = 0; i < weaponCount; i++)
        {
            FittingItem weaponModule = useFitting ? activeWeapons[i] : null;

            float currentDamage = weaponModule != null ? weaponModule.damageBonus : fallbackDamage;
            Color shotColor = weaponModule != null ? weaponModule.iconColor : fallbackColor;
            WeaponVisualType visual = weaponModule != null ? weaponModule.visualType : fallbackVisual;

            Vector3 offset = spawnPoint.right * (startOffset + i * spacing);
            Vector3 spawnPos = spawnPoint.position + offset;

            // Если есть цель — линия идёт до цели; иначе — на длину range по forward
            Vector3 endPos;
            if (Target != null)
            {
                endPos = Target.transform.position;
            }
            else
            {
                endPos = spawnPos + (Vector3)(forward * range);
            }

            // Спавн снаряда (если назначен) или мгновенный урон + визуал
            if (bulletPrefab != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnPoint.rotation);

                var rb = bullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = forward * bulletSpeed;
                }

                var renderer = bullet.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = shotColor;
                }
            }
            else
            {
                // Мгновенный урон, если есть цель (уже проверено выше, что цель в конусе)
                if (Target != null)
                {
                    Target.TakeDamage(currentDamage, targetedModule);
                }

                // Визуальный след
                DrawShot(spawnPos, endPos, shotColor, visual);
            }
        }
    }

    /// <summary>
    /// Рисует временный след выстрела через LineRenderer.
    /// Laser — тонкий луч, Plasma — толстый луч-капля.
    /// </summary>
    private void DrawShot(Vector3 from, Vector3 to, Color color, WeaponVisualType visual)
    {
        GameObject go = new GameObject("ShotVisual");
        var lr = go.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        if (lineMaterial != null)
            lr.material = lineMaterial;

        lr.sortingOrder = 5;

        float width;
        float duration;
        int capVertices;

        if (visual == WeaponVisualType.Plasma)
        {
            width = plasmaWidth;
            duration = plasmaVisualDuration;
            capVertices = 4; // скруглённые концы — эффект "капли"
        }
        else // Laser
        {
            width = laserWidth;
            duration = laserVisualDuration;
            capVertices = 0;
        }

        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = capVertices;
        lr.startColor = color;
        lr.endColor = color;

        Destroy(go, duration);
    }
}