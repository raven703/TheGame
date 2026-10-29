using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Переключение цели:
/// - Tab — циклически по живым врагам.
/// - No implicit target selection: only an explicit Tab changes weapon selection.
/// </summary>
public class TargetSwitcher : MonoBehaviour
{
    [Tooltip("Weapon игрока.")]
    public ShipWeapon playerWeapon;

    [Tooltip("Клавиша переключения.")]
    public KeyCode switchKey = KeyCode.Tab;

    private void Update()
    {
        if (playerWeapon == null || FittingWindow.OpenWindow != null || PlayerInputHandler.IsPointerOverUI()) return;

#if ENABLE_INPUT_SYSTEM
        bool switchPressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
        bool switchPressed = Input.GetKeyDown(switchKey);
#endif

        var alive = EnemyRegistry.GetAliveEnemies();

        // === Tab — циклическое переключение ===
        if (switchPressed && alive.Count > 0)
        {
            playerWeapon.SetTarget(GetNextEnemy(alive));
        }
    }

    private ShipHealth GetNearestEnemy(List<ShipHealth> alive)
    {
        ShipHealth best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = playerWeapon.transform.position;

        foreach (var e in alive)
        {
            if (e == null) continue;
            float d = Vector3.Distance(myPos, e.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    private ShipHealth GetNextEnemy(List<ShipHealth> alive)
    {
        if (alive.Count == 0) return null;

        // Если сейчас идёт захват — от него и считаем
        var current = playerWeapon.PendingTarget != null
            ? playerWeapon.PendingTarget
            : playerWeapon.Target;

        if (current == null) return alive[0];

        int currentIndex = alive.IndexOf(current);
        if (currentIndex < 0)
            return alive[0];

        int nextIndex = (currentIndex + 1) % alive.Count;
        return alive[nextIndex];
    }
}