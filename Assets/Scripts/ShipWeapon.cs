using System;
using System.Collections;
using UnityEngine;

public enum CombatBehavior
{
    Orbit,          // Вращаться по орбите
    KeepDistance,   // Кайтить (держать дистанцию)
    Flank           // Заходить во фланг/корму
}

public class ShipWeapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    [Tooltip("Maximum firing distance in world units.")]
    public float range = 6f;

    [Tooltip("Seconds between shots.")]
    public float cooldown = 1.5f;

    [Tooltip("Damage per shot.")]
    public float damage = 15f;

    [Tooltip("Firing arc in degrees.")]
    public float firingArcAngle = 60f;

    [Header("Tactics & AI")]
    public CombatBehavior behavior = CombatBehavior.Orbit;
    public bool orbitClockwise = true;

    /// <summary>
    /// Если true — корабль выполняет ручной приказ движения от игрока и не меняет TargetPosition сам.
    /// </summary>
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
    private ShipMovement shipMovement;

    // Переменные для динамической волнистой орбиты
    private float orbitChangeTimer = 0f;
    private float randomRadiusOffset = 0f;

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

    private LineRenderer CreateLaserLine()
    {
        var beamObject = new GameObject("LaserBeam");
        beamObject.transform.SetParent(transform, false);

        var line = beamObject.AddComponent<LineRenderer>();
        line.startColor = new Color(1f, 0.92f, 0.25f, 1f);
        line.endColor = new Color(1f, 0.5f, 0.1f, 1f);
        line.material = CreateLaserMaterial();
        line.sortingOrder = 3;

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
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        var material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    public void SetTarget(ShipHealth target)
    {
        Target = target;
        manualMoveOrder = false; // При смене цели возвращаем авто-маневрирование
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

        // Если цели нет или она уничтожена
        if (Target == null || (Target.Data != null && Target.Data.IsDestroyed))
        {
            Target = null;
            return;
        }

        // Авто-маневрирование работает ТОЛЬКО если игрок не отдал ручной приказ движения в космос
        if (!manualMoveOrder)
        {
            UpdateCombatTactics();
        }

        // Стрельба идет ВСЕГДА, когда цель попадает в сектор и радиус, независимо от того, куда летит корабль
        var distance = Vector2.Distance(transform.position, Target.transform.position);
        bool isInFiringArc = IsTargetInFiringArc(Target.transform.position);

        if (distance <= range && isInFiringArc && currentCooldown <= 0f)
            Fire();
    }

    /// <summary>
    /// Автоматическое маневрирование с защитой от замкнутых орбит.
    /// </summary>
    private void UpdateCombatTactics()
    {
        if (shipMovement == null) return;

        Vector3 enemyPos = Target.transform.position;
        Vector3 myPos = transform.position;
        Vector3 dirToEnemy = (enemyPos - myPos).normalized;
        float currentDistance = Vector2.Distance(myPos, enemyPos);

        // Периодически сдвигаем радиус и меняем направление, чтобы избавиться от паттернов
        orbitChangeTimer -= Time.deltaTime;
        if (orbitChangeTimer <= 0f)
        {
            orbitChangeTimer = UnityEngine.Random.Range(3f, 6f);
            randomRadiusOffset = UnityEngine.Random.Range(-1.2f, 1.2f);

            // С шансом 30% меняем направление вращения по орбите
            if (UnityEngine.Random.value < 0.3f)
            {
                orbitClockwise = !orbitClockwise;
            }
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

            Target.TakeDamage(damage);
            Debug.Log($"[HIT] {gameObject.name} попал по {Target.name}!");
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