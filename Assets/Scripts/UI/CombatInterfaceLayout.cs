using UnityEngine;
using UnityEngine.UI;

/// <summary>Responsive layout of existing scene objects; safe to apply outside Play Mode.</summary>
public class CombatInterfaceLayout : MonoBehaviour
{
    private void Awake() { Apply(); }
    public void Apply()
    {
        var canvas = GetComponent<Canvas>();
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null) { scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f; }
        var prep = GetComponentInChildren<PreCombatPanel>(true);
        if (prep != null)
        {
            CombatUIStyle.Rect(prep.transform, Vector2.zero, Vector2.zero, new Vector2(16,100), new Vector2(386,216));
            CombatUIStyle.Rect(prep.panelRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            // This Image is the old grey square, not a ship icon. Retain panelRoot and its children.
            var image = prep.panelRoot.GetComponent<Image>();
            if (image != null) { image.color = Color.clear; image.raycastTarget = false; }
            CombatUIStyle.Box(prep.statusLabel.transform, 0,0,360,38); prep.statusLabel.fontSize = 20;
            CombatUIStyle.Box(prep.startCombatButton.transform,0,50,172,40);
            CombatUIStyle.Box(prep.openFittingButton.transform,184,50,172,40);
        }
        var fittingButton = GetComponentInChildren<FittingButton>(true);
        if (fittingButton != null) CombatUIStyle.Rect(fittingButton.transform, Vector2.zero,Vector2.zero,new Vector2(16,76),new Vector2(174,100));
        var tactical = GetComponentInChildren<TacticalCommandPanel>(true);
        if (tactical != null) CombatUIStyle.Rect(tactical.transform,Vector2.zero,Vector2.right,new Vector2(12,8),new Vector2(-12,72));
        var radar = transform.Find("Radar");
        if (radar != null) CombatUIStyle.Rect(radar, Vector2.one,Vector2.one,new Vector2(-224,-382),new Vector2(-16,-174));
        var window = GetComponentInChildren<FittingWindow>(true);
        if (window != null) window.ConfigureLayout();
    }
}