using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Translates player input into ship orders: left click on an enemy captures it as a target
/// and enables automatic approach/orbiting, left click on empty space keeps the active target
/// but forces a manual movement order (allowing strafe runs and retreat while firing).
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
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        // Клик по UI (UGUI) — игнорируем.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!IsPointerOverUI()) HandleClick();
    }

    public static bool IsPointerOverUI()
    {
        if (Mouse.current == null) return false;
        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 gui = new Vector2(screen.x, Screen.height - screen.y);
        if (CombatSession.Instance != null && (CombatSession.Panel.Contains(gui) || CombatStatusPanel.Panel.Contains(gui))) return true;
        if (FittingWindow.PresetsPanel.Contains(gui) && FindFirstObjectByType<FittingWindow>() != null) return true;
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

        var hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag(EnemyTag))
        {
            var enemyHealth = hit.collider.GetComponentInParent<ShipHealth>();
            if (enemyHealth != null)
            {
                playerWeapon.SetTarget(enemyHealth);
                playerWeapon.manualMoveOrder = false;
                Debug.Log($"[PlayerInputHandler] Захват цели и авто-маневрирование: {enemyHealth.name}");
            }
        }
        else
        {
            playerWeapon.manualMoveOrder = true;
            playerMovement.SetTargetPosition(worldPosition);

            if (playerWeapon.Target != null)
            {
                Debug.Log($"[PlayerInputHandler] Ручной приказ в {worldPosition}. Ведётся прикрывающий огонь по: {playerWeapon.Target.name}");
            }
            else
            {
                Debug.Log($"[PlayerInputHandler] Приказ двигаться в {worldPosition} (цель не захвачена)");
            }
        }
    }
}