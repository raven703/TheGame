using UnityEngine;

/// <summary>
/// Отвечает за смену визуала корабля (спрайта) при смене ShipDefinition.
/// Вешается на тот же GameObject, где SpriteRenderer корабля.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShipVisuals : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Применить визуал из ShipDefinition. Если hullSprite не задан — оставить как есть.
    /// </summary>
    public void Apply(ShipDefinition def)
    {
        if (def == null || def.hullSprite == null)
        {
            Debug.LogWarning($"[ShipVisuals] hullSprite не задан для {def?.displayName ?? "null"}, спрайт не изменён.");
            return;
        }

        spriteRenderer.sprite = def.hullSprite;
        Debug.Log($"[ShipVisuals] Спрайт корабля обновлён: {def.hullSprite.name} (для {def.displayName})");
    }
}