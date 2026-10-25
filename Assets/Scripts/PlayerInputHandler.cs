using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

    private CombatControls controls;
    private Camera mainCamera;

    private void Awake()
    {
        controls = new CombatControls();
        mainCamera = Camera.main;
        controls.Combat.Click.performed += OnClick;
    }

    private void OnEnable()
    {
        controls.Combat.Enable();
    }

    private void OnDisable()
    {
        controls.Combat.Disable();
    }

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Combat.Click.performed -= OnClick;
            controls.Dispose();
        }
    }

    public static bool IsPointerOverUI()
    {
        if (Mouse.current == null) return false;
        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 gui = new Vector2(screen.x, Screen.height - screen.y);
        if (CombatSession.Instance != null && CombatSession.Panel.Contains(gui)) return true;
        if (CombatStatusPanel.Panel.Contains(gui) && CombatSession.Instance != null) return true;
        foreach (var fitting in FindObjectsByType<ShipFitting>(FindObjectsSortMode.None))
            if (fitting.ContainsPointer(gui)) return true;
        foreach (var panel in FindObjectsByType<TacticalCommandPanel>(FindObjectsSortMode.None))
            if (panel.ContainsPointer(gui)) return true;
        if (EventSystem.current == null) return false;
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
        // Ignore physics raycasters: world colliders must remain selectable.
        return hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI()) return;
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

        var mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        var hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag(EnemyTag))
        {
            var enemyHealth = hit.collider.GetComponentInParent<ShipHealth>();
            if (enemyHealth != null)
            {
                // Клик по врагу: захватываем цель и включаем авто-маневрирование (орбита/кайтинг)
                playerWeapon.SetTarget(enemyHealth);
                playerWeapon.manualMoveOrder = false;
                Debug.Log($"[PlayerInputHandler] Захват цели и авто-маневрирование: {enemyHealth.name}");
            }
        }
        else
        {
            // Клик по пустому месту — НЕ сбрасываем захваченную цель!
            // Задаем точку движения и переводим корабль в режим ручного маневрирования.
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