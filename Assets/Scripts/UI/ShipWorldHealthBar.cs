using UnityEngine;

/// <summary>
/// Deprecated world space health bar. Left empty to avoid duplicate UI over ship sprites.
/// Status UI is now managed by CombatHUDManager on screen sides.
/// </summary>
public class ShipWorldHealthBar : MonoBehaviour
{
    private void Awake()
    {
        // Отключаем компонент, если он висит на объекте
        enabled = false;
    }
}