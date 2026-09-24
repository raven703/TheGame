using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Translates player input into ship orders: left click on an enemy captures it as a target
/// and flies towards it, left click on empty space issues a plain move order.
/// </summary>
/// <remarks>
/// Lives on the [Managers] object and drives the components of the player ship.
/// </remarks>
public class PlayerInputHandler : MonoBehaviour
{
    /// <summary>Tag that marks a clickable hostile collider.</summary>
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

    private void OnClick(InputAction.CallbackContext context)
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

        var mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        var worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        var hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null && hit.collider.CompareTag(EnemyTag))
        {
            var enemyHealth = hit.collider.GetComponentInParent<ShipHealth>();
            if (enemyHealth != null)
            {
                playerWeapon.SetTarget(enemyHealth);
                playerMovement.SetTargetPosition(hit.transform.position);
                Debug.Log($"Игрок взял в захват цель: {enemyHealth.name}");
            }
        }
        else
        {
            playerMovement.SetTargetPosition(worldPosition);
            Debug.Log($"Игрок отдал приказ двигаться в {worldPosition}");
        }
    }
}
