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
[DefaultExecutionOrder(-100)]
public class TargetSwitcher : MonoBehaviour
{
    [Tooltip("Weapon игрока.")]
    public ShipWeapon playerWeapon;

    [Tooltip("Клавиша переключения.")]
    public KeyCode switchKey = KeyCode.Tab;
    private bool restoreNavigation;
    private UnityEngine.EventSystems.EventSystem navigationSystem;
    private void LateUpdate(){if(restoreNavigation&&navigationSystem!=null)navigationSystem.sendNavigationEvents=true;restoreNavigation=false;}

    private void Update()
    {
        if (playerWeapon == null || FittingWindow.OpenWindow != null || TimeController.TextInputFocused) return;

#if ENABLE_INPUT_SYSTEM
        bool switchPressed = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        if(Keyboard.current!=null&&Keyboard.current.tabKey.isPressed)
        {
            navigationSystem=UnityEngine.EventSystems.EventSystem.current;
            if(navigationSystem!=null&&navigationSystem.sendNavigationEvents){navigationSystem.sendNavigationEvents=false;restoreNavigation=true;}
        }
#else
        bool switchPressed = Input.GetKeyDown(switchKey);
#endif

        var alive = EnemyRegistry.GetAliveEnemies();

        // === Tab — циклическое переключение ===
        if (switchPressed && alive.Count > 0)
        {
            alive.RemoveAll(enemy=>!ShipOrderController.Available(enemy));
            if(alive.Count==0)return;
            // Registration order is stable; keyboard UI navigation must not consume Tab too.
            if(UnityEngine.EventSystems.EventSystem.current!=null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
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