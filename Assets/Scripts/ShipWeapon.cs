using System;
using System.Collections;
using UnityEngine;

public class ShipWeapon : MonoBehaviour
{
    [Tooltip("Maximum firing distance in world units.")]
    public float range = 6f;

    [Tooltip("Seconds between shots.")]
    public float cooldown = 1.5f;

    [Tooltip("Damage per shot.")]
    public float damage = 10f;

    [Tooltip("Muzzle marker the beam starts from (child WeaponPoint).")]
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

    // Событие смены цели для рамки UI
    public event Action<ShipHealth> OnTargetChanged;

    private const float LaserVisibleTime = 0.1f;
    private float currentCooldown = 0f;
    private ShipHealth ownerHealth;

    private void Awake()
    {
        ownerHealth = GetComponentInParent<ShipHealth>();

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

    public void SetTarget(ShipHealth target) => Target = target;
    public void ClearTarget() => Target = null;

    private void Update()
    {
        currentCooldown -= Time.deltaTime;

        if (ownerHealth == null || ownerHealth.Data == null)
            return;

        var weaponModule = ownerHealth.Data.GetModule(ModuleType.Weapon);
        if (ownerHealth.Data.IsDestroyed || weaponModule == null || weaponModule.isDestroyed)
            return;

        if (Target == null)
            return;

        if (Target.Data != null && Target.Data.IsDestroyed)
        {
            Target = null;
            return;
        }

        var distance = Vector2.Distance(transform.position, Target.transform.position);
        if (distance <= range && currentCooldown <= 0f)
            Fire();
    }

    private void Fire()
    {
        currentCooldown = cooldown;

        var muzzle = weaponPoint != null ? weaponPoint.position : transform.position;
        var targetPosition = Target.transform.position;

        Debug.DrawLine(muzzle, targetPosition, Color.yellow, 0.15f);

        if (laserLine != null)
            StartCoroutine(ShowLaser(muzzle, targetPosition));

        Target.TakeDamage(damage);
        Debug.Log($"{gameObject.name} выстрелил по {Target.name}!");
    }

    private IEnumerator ShowLaser(Vector3 start, Vector3 end)
    {
        laserLine.enabled = true;
        laserLine.SetPosition(0, start);
        laserLine.SetPosition(1, end);

        yield return new WaitForSeconds(LaserVisibleTime);

        laserLine.enabled = false;
    }
}