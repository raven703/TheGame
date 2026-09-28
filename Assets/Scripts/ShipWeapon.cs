using System;
using System.Collections;
using UnityEngine;

public enum CombatBehavior
{
    Orbit,          // Вращаться по орбите
    KeepDistance,   // Кайтить (держать дистанцию на пределе дальности)
    Flank           // Заходить во фланг/корму
}

public class ShipWeapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    public float range = 6f;
    public float cooldown = 1.5f;
    public float damage = 15f;
    public float firingArcAngle = 60f;

    [Header("Tactics & AI")]
    public CombatBehavior behavior = CombatBehavior.Orbit;
    public bool orbitClockwise = true;

    [Tooltip("Приоритетный модуль для выбивания (null = авто-распределение урона)")]
    public ModuleType? targetedModule = null;

    [HideInInspector] public bool manualMoveOrder = false;

    [Header("Visuals")]
    public Transform weaponPoint;
    [SerializeField] private LineRenderer laserLine;

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

    private const float LaserVisibleTime = 0.1f;
    private float currentCooldown = 0f;
    private ShipHealth ownerHealth;
    public ShipHealth OwnerHealth => ownerHealth;

    private ShipMovement shipMovement;
    private float orbitChangeTimer = 0f;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<ShipHealth>();
        shipMovement = GetComponentInParent<ShipMovement>();
        if (shipMovement == null)
            shipMovement = GetComponent<ShipMovement>();

        if (laserLine == null)
            laserLine = CreateLaserLine();
        else
            ApplyLaserGeometry(laserLine);

        laserLine.enabled = false;
    }

    public void SetBehavior(CombatBehavior newBehavior) => behavior = newBehavior;
    public void SetTargetModule(ModuleType? module) => targetedModule = module;

    private LineRenderer CreateLaserLine()
    {
        var beamObject = new GameObject("LaserBeam");
        beamObject.transform.SetParent(transform, false);

        var line = beamObject.AddComponent<LineRenderer>();
        line.startColor = new Color(1f, 0.92f, 0.25f, 1f);
        line.endColor = new Color(1f, 0.5f, 0.1f, 1f);
        line.material = CreateLaserMaterial();

        line.sortingLayerName = "Default";
        line.sortingOrder = 10;

        ApplyLaserGeometry(line);
        return line;
    }

    private static void ApplyLaserGeometry(LineRenderer line)
    {
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = 0.08f;
        line.endWidth = 0.04f;
    }

    private static Material CreateLaserMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        var material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    public void SetTarget(ShipHealth target)
    {
        Target = target;
        manualMoveOrder = false;
    }

    public void ClearTarget() => Target = null;

    private void Update()
    {
        currentCooldown -= Time.deltaTime;

        if (ownerHealth == null || ownerHealth.Data == null)
            return;

        var weaponModule = ownerHealth.Data.GetModule(ModuleType.Weapon);
        if (ownerHealth.Data.IsDestroyed || weaponModule == null || weaponModule.isDestroyed)
            return;

        if (Target == null || (Target.Data != null && Target.Data.IsDestroyed))
        {
            Target = null;
            return;
        }

        if (!manualMoveOrder)
        {
            UpdateCombatTactics();
        }

        var distance = Vector2.Distance(transform.position, Target.transform.position);
        bool isInFiringArc = IsTargetInFiringArc(Target.transform.position);

        if (distance <= range && isInFiringArc && currentCooldown <= 0f)
            Fire();
    }

    private void UpdateCombatTactics()
    {
        if (shipMovement == null || Target == null) return;

        Vector3 enemyPos = Target.transform.position;
        Vector3 myPos = transform.position;

        orbitChangeTimer -= Time.deltaTime;
        if (orbitChangeTimer <= 0f)
        {
            orbitChangeTimer = UnityEngine.Random.Range(6f, 10f);
            if (UnityEngine.Random.value < 0.25f)
                orbitClockwise = !orbitClockwise;
        }

        switch (behavior)
        {
            case CombatBehavior.Orbit:
                ExecuteOrbitBehavior(myPos, enemyPos);
                break;

            case CombatBehavior.KeepDistance:
                ExecuteKiteBehavior(myPos, enemyPos);
                break;

            case CombatBehavior.Flank:
                ExecuteFlankBehavior(myPos, enemyPos);
                break;
        }
    }

    /// <summary>
    /// Орбита: вычисляем фиксированную точку на окружности вокруг врага
    /// </summary>
    private void ExecuteOrbitBehavior(Vector3 myPos, Vector3 enemyPos)
    {
        float targetRadius = range * 0.70f;
        Vector3 dirFromEnemy = (myPos - enemyPos).normalized;

        if (dirFromEnemy == Vector3.zero)
            dirFromEnemy = Vector3.up;

        // Поворачиваем вектор от врага на 40 градусов по или против часовой стрелки
        float angleOffset = orbitClockwise ? -40f : 40f;
        Vector3 rotatedDir = Quaternion.Euler(0, 0, angleOffset) * dirFromEnemy;

        // Целевая точка строго привязана к позиции врага!
        Vector3 orbitTargetPoint = enemyPos + rotatedDir * targetRadius;
        shipMovement.SetTargetPosition(orbitTargetPoint);
    }

    /// <summary>
    /// Кайт: удерживаем дистанцию на краю зоны поражения (85% от range)
    /// </summary>
    private void ExecuteKiteBehavior(Vector3 myPos, Vector3 enemyPos)
    {
        float currentDist = Vector2.Distance(myPos, enemyPos);
        float optimalKiteDist = range * 0.85f;
        Vector3 dirFromEnemy = (myPos - enemyPos).normalized;

        if (dirFromEnemy == Vector3.zero)
            dirFromEnemy = Vector3.up;

        if (currentDist < range * 0.75f)
        {
            // Враг близко — отступаем в точку на дистанции 85% от врага с небольшим боковым смещением
            float sideAngle = orbitClockwise ? -25f : 25f;
            Vector3 retreatDir = Quaternion.Euler(0, 0, sideAngle) * dirFromEnemy;
            Vector3 kiteTargetPoint = enemyPos + retreatDir * optimalKiteDist;
            shipMovement.SetTargetPosition(kiteTargetPoint);
        }
        else if (currentDist > range * 0.95f)
        {
            // Враг далеко — сближаемся к нему на 80% дальности
            Vector3 approachTargetPoint = enemyPos + dirFromEnemy * (range * 0.80f);
            shipMovement.SetTargetPosition(approachTargetPoint);
        }
        else
        {
            // В идеальной зоне — совершаем плавную орбиту по краю дальности
            ExecuteOrbitBehavior(myPos, enemyPos);
        }
    }

    /// <summary>
    /// Фланк: заходим в корму противника
    /// </summary>
    private void ExecuteFlankBehavior(Vector3 myPos, Vector3 enemyPos)
    {
        float optimalDist = range * 0.70f;
        Vector3 enemyForward = Target.transform.right;

        if (Target.TryGetComponent<ShipMovement>(out var targetMovement))
        {
            enemyForward = targetMovement.GetForwardVector();
        }

        Vector3 flankTargetPoint = enemyPos - enemyForward * optimalDist;
        shipMovement.SetTargetPosition(flankTargetPoint);
    }

    private bool IsTargetInFiringArc(Vector3 targetPos)
    {
        Vector2 dirToTarget = (targetPos - transform.position).normalized;
        Vector2 forward = shipMovement != null ? shipMovement.GetForwardVector() : (Vector2)transform.up;

        float angle = Vector2.Angle(forward, dirToTarget);
        return angle <= (firingArcAngle * 0.5f);
    }

    private void Fire()
    {
        var weaponMod = ownerHealth.Data.GetModule(ModuleType.Weapon);
        float actualCooldown = cooldown;
        if (weaponMod != null && weaponMod.currentHP < weaponMod.maxHP)
        {
            actualCooldown *= 1.5f;
        }
        currentCooldown = actualCooldown;

        var muzzle = weaponPoint != null ? weaponPoint.position : transform.position;
        var targetPosition = Target.transform.position;

        float targetSpeed = 0f;
        if (Target.TryGetComponent<Rigidbody2D>(out var targetRb))
        {
            targetSpeed = targetRb.linearVelocity.magnitude;
        }

        float targetEvasion = Target.Data.GetTotalEvasion(targetSpeed);
        float hitChance = ownerHealth.Data.accuracy - targetEvasion;

        bool isHit = UnityEngine.Random.value <= hitChance;

        if (isHit)
        {
            if (laserLine != null)
                StartCoroutine(ShowLaser(muzzle, targetPosition, Color.yellow));

            Target.TakeDamage(damage, targetedModule);
            Debug.Log($"[HIT] {gameObject.name} попал по {Target.name}! (Приоритет модуля: {(targetedModule.HasValue ? targetedModule.Value.ToString() : "Авто")})");
        }
        else
        {
            Vector3 missOffset = new Vector3(UnityEngine.Random.Range(-1.2f, 1.2f), UnityEngine.Random.Range(-1.2f, 1.2f), 0f);
            if (laserLine != null)
                StartCoroutine(ShowLaser(muzzle, targetPosition + missOffset, Color.gray));

            Debug.Log($"[MISS] {gameObject.name} промахнулся по {Target.name}!");
        }
    }

    private IEnumerator ShowLaser(Vector3 start, Vector3 end, Color laserColor)
    {
        laserLine.startColor = laserColor;
        laserLine.enabled = true;
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);

        yield return new WaitForSeconds(LaserVisibleTime);

        laserLine.enabled = false;
    }
}