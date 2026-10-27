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
    Orbit,
    KeepDistance,
    Flank
}

public class ShipWeapon : MonoBehaviour
{
    [Header("Weapon & Projectile Setup")]
    public GameObject bulletPrefab;
    [Tooltip("Точки вылета снарядов (по индексу = слот оружия). Если пусто — центр корабля.")]
    public Transform[] firePoints;

     public float bulletSpeed = 15f;
    public float range = 8f;
    [UnityEngine.Serialization.FormerlySerializedAs("cooldown")]
    public float baseCooldown = 0.6f;
    public float firingArcAngle = 60f;

    [Header("Fallback Weapon (если фитинг не задан)")]
    public bool useFallbackWeapon = false;
    [UnityEngine.Serialization.FormerlySerializedAs("damage")]
    public float fallbackDamage = 0.5f;
    public Color fallbackColor = new Color(1f, 0.3f, 0.3f, 1f);
    public WeaponVisualType fallbackVisual = WeaponVisualType.Laser;

    [Header("Shot Visuals")]
    public float laserVisualDuration = 0.1f;
    public float plasmaVisualDuration = 0.15f;
    public float laserWidth = 0.05f;
    public float plasmaWidth = 0.25f;

    [Header("Tactics & AI")]
    public CombatBehavior behavior = CombatBehavior.Orbit;
    public bool orbitClockwise = true;

    [Tooltip("Приоритетный модуль для выбивания (null = авто-распределение урона)")]
    [System.NonSerialized]
    public ModuleType? targetedModule = null;

    [HideInInspector] public bool manualMoveOrder = false;

    [Header("Target Lock (fallback)")]
    [Tooltip("Fallback время захвата, если ShipData недоступен. Обычно берётся из ShipData.lockTime.")]
    public float fallbackLockTime = 0.5f;

    // ============ TARGET STATE ============
    private readonly Dictionary<string, float> weaponReadyAt = new Dictionary<string, float>();
    public float TargetingDisruptionRemaining => Mathf.Max(0f, targetingDisabledUntil - Time.time);

    private float targetingDisabledUntil;
    public bool IsTargetingDisrupted => Time.time < targetingDisabledUntil;

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

    private ShipHealth pendingTarget;
    public ShipHealth PendingTarget => pendingTarget;

    private float lockProgress = 0f;
    public float LockProgress => lockProgress;

    public bool IsLocking => pendingTarget != null;

    // ============ EVENTS ============

    public event Action<ShipHealth> OnTargetChanged;
    public event Action<ShipHealth> OnLockStarted;
    public event Action<ShipHealth> OnLockCancelled;
    public event Action<ShipHealth> OnLockCompleted;

    // ============ RUNTIME ============

    private List<FittingItem> activeWeapons = new List<FittingItem>();
    private float nextFireTime = 0f;

    private ShipHealth ownerHealth;
    public ShipHealth OwnerHealth => ownerHealth;
    private ShipMovement shipMovement;



    private Material lineMaterial;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<ShipHealth>();

        shipMovement = GetComponentInParent<ShipMovement>();
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();



        var shader = Shader.Find("Sprites/Default")
                  ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                  ?? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Legacy Shaders/Diffuse");

        if (shader != null)
            lineMaterial = new Material(shader);
    }

    // ============ LOCK TIME ============

    /// <summary>
    /// Итоговое время захвата: из ShipData, иначе fallback.
    /// </summary>
    private float GetLockTime()
    {
        if (ownerHealth != null && ownerHealth.Data != null)
            return Mathf.Max(0.05f, ownerHealth.Data.lockTime);

        return Mathf.Max(0.05f, fallbackLockTime);
    }

    // ============ PUBLIC API ============

    public void SetBehavior(CombatBehavior newBehavior)
    {
        behavior = newBehavior;
        var selected = PendingTarget != null ? PendingTarget : Target;
        if (shipMovement != null && shipMovement.Orders != null)
            shipMovement.Orders.Tactical(newBehavior == CombatBehavior.Orbit ? ShipOrderMode.Orbit : newBehavior == CombatBehavior.KeepDistance ? ShipOrderMode.KeepDistance : ShipOrderMode.Flank, selected);
    }

    public void SetTargetModule(ModuleType? module) => targetedModule = module;

    public void SetTarget(ShipHealth newTarget)
    {
        if (newTarget == null)
        {
            CancelLock();
            Target = null;
            return;
        }

        if (newTarget == pendingTarget || (newTarget == Target && pendingTarget == null))
            return;

        pendingTarget = newTarget;
        lockProgress = 0f;

        OnLockStarted?.Invoke(newTarget);

        Debug.Log($"[ShipWeapon] Начат захват: {newTarget.name} ({GetLockTime():F2} сек)");
    }

    public void SetTargetImmediate(ShipHealth newTarget)
    {
        pendingTarget = null;
        lockProgress = 0f;
        Target = newTarget;
    }

    public void ClearTarget()
    {
        CancelLock();
        Target = null;
    }

    // Retain the selected target: reacquisition is automatic when suppression expires.
    public void DisruptTargeting(float duration)
    {
        targetingDisabledUntil = Mathf.Max(targetingDisabledUntil, Time.time + Mathf.Max(0f, duration));
    }

    public void SetEquippedWeapons(List<FittingItem> weapons)
    {
        activeWeapons = weapons;
    }

    private void CancelLock()
    {
        if (pendingTarget != null)
        {
            OnLockCancelled?.Invoke(pendingTarget);
            pendingTarget = null;
            lockProgress = 0f;
        }
    }

    // ============ UPDATE ============

    private void Update()
    {
        if (ownerHealth == null || ownerHealth.Data == null) return;
        var weaponModule = ownerHealth.Data.GetModule(ModuleType.Weapon);
        if (ownerHealth.Data.IsDestroyed || weaponModule == null || weaponModule.isDestroyed) return;
        if (CombatSession.IsPreparing || Time.timeScale <= 0f || IsTargetingDisrupted) return;
        UpdateLock();
        if (Target != null && !ShipOrderController.Available(Target)) Target = null;
        if (pendingTarget != null || Target == null || Time.time < nextFireTime) return;
        if (Vector2.Distance(transform.position, Target.transform.position) <= range && IsTargetInFiringArc(Target.transform.position))
        { Shoot(); nextFireTime = Time.time + .1f; }
    }

    private void UpdateLock()
    {
        if (pendingTarget == null)
            return;

        if (pendingTarget.Data == null || pendingTarget.Data.IsDestroyed
            || !pendingTarget.gameObject.activeInHierarchy)
        {
            Debug.Log($"[ShipWeapon] Захват отменён: цель недоступна.");
            CancelLock();
            return;
        }

        float lockTime = GetLockTime();
        lockProgress += Time.deltaTime / lockTime;

        if (lockProgress >= 1f)
        {
            Target = pendingTarget;
            pendingTarget = null;
            lockProgress = 0f;


            Debug.Log($"[ShipWeapon] Захват завершён: {Target.name}");
            OnLockCompleted?.Invoke(Target);
        }
    }

    // ============ COMBAT TACTICS ============



    private bool IsTargetInFiringArc(Vector3 targetPos)
    {
        Vector2 dirToTarget = (targetPos - transform.position).normalized;
        Vector2 forward = shipMovement != null ? shipMovement.GetForwardVector() : (Vector2)transform.up;

        float angle = Vector2.Angle(forward, dirToTarget);
        return angle <= (firingArcAngle * 0.5f);
    }

    // ============ SHOOT ============

    private void Shoot()
    {
        if (Target != null)
        {
            float dist = Vector2.Distance(transform.position, Target.transform.position);
            if (dist > range || !IsTargetInFiringArc(Target.transform.position))
                return;
        }

        bool useFitting = activeWeapons != null && activeWeapons.Count > 0;
        if (!useFitting && !useFallbackWeapon)
            return;

        int weaponCount = useFitting ? activeWeapons.Count : 1;

        Vector2 forward = shipMovement != null
            ? shipMovement.GetForwardVector()
            : (Vector2)transform.up;

        for (int i = 0; i < weaponCount; i++)
        {
            // Используем firePoints[i], если он есть и валиден. Иначе центр корабля.
            Transform spawnPoint = (firePoints != null && i < firePoints.Length && firePoints[i] != null)
                ? firePoints[i]
                : transform;

            FittingItem weaponModule = useFitting ? activeWeapons[i] : null;

            var stats = WeaponStatsCalculator.Calculate(weaponModule, baseCooldown, fallbackDamage);
            float currentDamage = stats.damage;
            Color shotColor = weaponModule != null ? weaponModule.iconColor : fallbackColor;
            WeaponVisualType visual = weaponModule != null ? weaponModule.visualType : fallbackVisual;

            Vector3 spawnPos = spawnPoint.position;

            Vector3 endPos;
            if (Target != null)
                endPos = Target.transform.position;
            else
                endPos = spawnPos + (Vector3)(forward * range);

            // All equipped weapons use the same cover/resource path, including legacy prefabs.
            WeaponKind kind = weaponModule != null ? weaponModule.EffectiveWeaponKind : WeaponKind.Laser;
            string key = i.ToString();
            if (weaponReadyAt.TryGetValue(key, out float ready) && Time.time < ready) continue;
            var reactor = ownerHealth.GetComponent<ShipReactor>();
            if (reactor == null || !reactor.TrySpend(stats.energy, stats.heat)) continue;
            weaponReadyAt[key] = Time.time + stats.cooldown;

            Asteroid cover = Asteroid.FindCover(spawnPos, endPos, out Vector2 impact);
            if (cover != null)
            {
                endPos = impact;
                cover.TakeDamage(currentDamage);
                CombatSession.Log($"{name}: выстрел в укрытие");
            }
            else if (Target != null)
            {
                var targetMovement = Target.GetComponent<ShipMovement>();
                float speed = targetMovement != null && targetMovement.Rb != null
                    ? targetMovement.Rb.linearVelocity.magnitude : 0f;
                float evasion = Target.Data != null ? Target.Data.GetTotalEvasion(speed) : 0f;
                float accuracyBonus = kind == WeaponKind.Laser ? .1f : kind == WeaponKind.Plasma ? -.1f : 0f;
                if (UnityEngine.Random.value < Mathf.Clamp01(ownerHealth.Data.accuracy + accuracyBonus - evasion))
                {
                    Target.TakeWeaponDamage(currentDamage, kind, targetedModule);
                }
                else CombatSession.Log($"{name}: промах ({kind})");
            }
            DrawShot(spawnPos, endPos, shotColor, visual);
        }
    }

    /// <summary>
    /// Рисует временный след выстрела через LineRenderer.
    /// Laser — тонкий луч, Plasma — толстый луч-капля.
    /// </summary>
    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            if (Application.isPlaying) Destroy(lineMaterial);
            else DestroyImmediate(lineMaterial);
        }
    }

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
            capVertices = 4;
        }
        else
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