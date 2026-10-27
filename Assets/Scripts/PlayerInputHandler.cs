using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Selection and movement orders are independent. UI raycasts are resolved before world input.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private const string EnemyTag = "Enemy";

    [Tooltip("Movement component of the player ship.")]
    public ShipMovement playerMovement;

    [Tooltip("Weapon component of the player ship.")]
    public ShipWeapon playerWeapon;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // LateUpdate — чтобы EventSystem уже обновил состояние.
    // Если обрабатывать клик в Update (или через InputAction callback) — IsPointerOverGameObject()
    // вернёт состояние прошлого кадра, и клик по UI «провалится» в игровое поле.
    private void LateUpdate()
    {
        if (FittingWindow.OpenWindow != null) return;
        if (Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame && !IsPointerOverUI())
            playerMovement?.Stop();
        if (Mouse.current == null || (!Mouse.current.leftButton.wasPressedThisFrame && !Mouse.current.rightButton.wasPressedThisFrame))
            return;

        // Клик по UI (UGUI) — игнорируем.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!IsPointerOverUI()) HandleClick();
    }

    public static bool IsPointerOverUI()
    {
        if (FittingWindow.OpenWindow != null) return true;
        if (Mouse.current == null) return false;
        Vector2 screen = Mouse.current.position.ReadValue();
        return IsScreenPointOverUI(screen);
    }

    public static bool IsScreenPointOverUI(Vector2 screen)
    {
        if(FittingWindow.OpenWindow!=null)return true;
        foreach(var panel in Object.FindObjectsByType<TacticalCommandPanel>(FindObjectsSortMode.None))
            if(panel.PointerInsideTooltip(screen))return true;
        if (EventSystem.current == null) return false;
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
        return hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
    }

    private void HandleClick()
    {
        if (playerMovement == null || playerWeapon == null)
        {
            Debug.LogWarning("[PlayerInputHandler] playerMovement / playerWeapon links are not assigned.");
            return;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogWarning("[PlayerInputHandler] No Main Camera found (tag the camera as MainCamera).");
            return;
        }

        var mousePosition = Mouse.current.position.ReadValue();
        var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        ShipHealth enemy = null;
        foreach (var hit in Physics2D.OverlapPointAll(worldPosition))
        {
            var health = hit.GetComponentInParent<ShipHealth>();
            if (health != null && health.CompareTag(EnemyTag)) { enemy = health; break; }
        }
        bool append = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        IssueClick(Mouse.current.leftButton.wasPressedThisFrame, worldPosition, enemy, append, false);
    }

    public void IssueClick(bool left, Vector2 position, ShipHealth enemy, bool append, bool overUI)
    {
        if (overUI || FittingWindow.OpenWindow != null || playerMovement == null || playerWeapon == null) return;
        if (left) { if (enemy != null) playerWeapon.SetTarget(enemy); return; }
        var selected = playerWeapon.PendingTarget != null ? playerWeapon.PendingTarget : playerWeapon.Target;
        if (!append && enemy != null && selected == enemy)
            playerMovement.Orders.Tactical(ShipOrderMode.EngageTarget, enemy);
        else playerMovement.Orders.MoveTo(enemy != null ? (Vector2)enemy.transform.position : position, append);
    }
}