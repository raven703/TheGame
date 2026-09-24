using UnityEngine;

/// <summary>
/// Рисует рамку захвата цели вокруг выделенного корабля.
/// </summary>
public class TargetSelectionFrame : MonoBehaviour
{
    public ShipWeapon playerWeapon;
    public Color frameColor = Color.red;

    private LineRenderer frameLine;

    private void Awake()
    {
        CreateFrameLine();
    }

    private void OnEnable()
    {
        if (playerWeapon != null)
            playerWeapon.OnTargetChanged += OnTargetChanged;
    }

    private void OnDisable()
    {
        if (playerWeapon != null)
            playerWeapon.OnTargetChanged -= OnTargetChanged;
    }

    private void OnTargetChanged(ShipHealth target)
    {
        if (frameLine != null)
            frameLine.enabled = target != null;
    }

    private void LateUpdate()
    {
        if (playerWeapon == null || playerWeapon.Target == null)
        {
            if (frameLine != null)
                frameLine.enabled = false;
            return;
        }

        var targetPos = playerWeapon.Target.transform.position;
        DrawSquare(targetPos, 1.2f);
    }

    private void DrawSquare(Vector3 center, float size)
    {
        float half = size * 0.5f;
        frameLine.enabled = true;
        frameLine.SetPosition(0, new Vector3(center.x - half, center.y + half, 0));
        frameLine.SetPosition(1, new Vector3(center.x + half, center.y + half, 0));
        frameLine.SetPosition(2, new Vector3(center.x + half, center.y - half, 0));
        frameLine.SetPosition(3, new Vector3(center.x - half, center.y - half, 0));
    }

    private void CreateFrameLine()
    {
        var frameGO = new GameObject("TargetFrameLine");
        frameGO.transform.SetParent(transform, false);

        frameLine = frameGO.AddComponent<LineRenderer>();
        frameLine.positionCount = 4;
        frameLine.loop = true;
        frameLine.useWorldSpace = true;
        frameLine.startWidth = 0.05f;
        frameLine.endWidth = 0.05f;
        frameLine.startColor = frameColor;
        frameLine.endColor = frameColor;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            frameLine.material = new Material(shader);

        frameLine.sortingOrder = 5;
        frameLine.enabled = false;
    }
}