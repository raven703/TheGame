using UnityEngine;

/// <summary>
/// Visualizes the ship weapon's firing arc in 2D world space using a LineRenderer.
/// Attach this component directly to the ship GameObject.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class FiringArcVisualizer : MonoBehaviour
{
    [Header("Arc Settings")]
    [Tooltip("Number of segments used to draw the smooth arc boundary.")]
    public int segments = 24;

    [Tooltip("Color of the firing arc visualization.")]
    public Color arcColor = new Color(0.2f, 0.8f, 1f, 0.25f);

    [Tooltip("Line width for the arc outline.")]
    public float lineWidth = 0.05f;

    private LineRenderer lineRenderer;
    private ShipWeapon shipWeapon;
    private ShipMovement shipMovement;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        shipWeapon = GetComponent<ShipWeapon>();
        shipMovement = GetComponent<ShipMovement>();

        ConfigureLineRenderer();
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments + 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Material flatMat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = flatMat;
        lineRenderer.startColor = arcColor;
        lineRenderer.endColor = arcColor;
    }

    private void LateUpdate()
    {
        if (shipWeapon == null)
            shipWeapon = GetComponent<ShipWeapon>();

        if (shipWeapon == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        // Если модуль оружия выбит — скрываем сектор
        if (shipWeapon.OwnerHealth != null && shipWeapon.OwnerHealth.Data != null)
        {
            var weaponModule = shipWeapon.OwnerHealth.Data.GetModule(ModuleType.Weapon);
            if (weaponModule != null && weaponModule.isDestroyed)
            {
                lineRenderer.enabled = false;
                return;
            }
        }

        lineRenderer.enabled = true;
        DrawArc();
    }

    private void DrawArc()
    {
        Vector3 origin = transform.position;
        float weaponRange = shipWeapon.range;
        float halfArc = shipWeapon.firingArcAngle * 0.5f;

        Vector2 forward = shipMovement != null ? shipMovement.GetForwardVector() : (Vector2)transform.up;
        float baseAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        lineRenderer.positionCount = segments + 2;
        lineRenderer.SetPosition(0, origin);

        float startAngle = baseAngle - halfArc;
        float endAngle = baseAngle + halfArc;
        float step = (endAngle - startAngle) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = (startAngle + step * i) * Mathf.Deg2Rad;
            Vector3 point = origin + new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle), 0f) * weaponRange;
            lineRenderer.SetPosition(i + 1, point);
        }
    }
}