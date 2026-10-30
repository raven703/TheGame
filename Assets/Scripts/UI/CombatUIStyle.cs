using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Shared Canvas-space layout and palette. Never changes item/weapon data.</summary>
public static class CombatUIStyle
{
    public static void ToggleVisual(Button button,bool active)
    {
        if(button==null)return;
        var image=button.targetGraphic as Image;
        if(image!=null)image.color=Color.white;
        Color fill=active?new Color(.06f,.48f,.6f):Card;
        var colors=button.colors;colors.normalColor=fill;colors.selectedColor=fill;
        colors.highlightedColor=Color.Lerp(fill,new Color(.3f,.65f,.75f),.25f);
        colors.pressedColor=Color.Lerp(fill,Color.white,.15f);colors.disabledColor=new Color(.12f,.15f,.18f,.6f);colors.colorMultiplier=1;
        button.colors=colors;
        var border=Ensure<Outline>(button.gameObject);border.effectDistance=Vector2.one;
        border.effectColor=active?new Color(.5f,.94f,1):new Color(.22f,.29f,.36f);
        foreach(var label in button.GetComponentsInChildren<TextMeshProUGUI>(true))label.color=button.interactable?Color.white:new Color(.5f,.56f,.6f);
    }
    public static T Ensure<T>(GameObject go) where T : Component
    {
        var component=go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
    public static readonly Color Surface = new Color(.055f, .075f, .105f, .98f);
    public static readonly Color Card = new Color(.105f, .14f, .19f, 1f);
    public static Color Category(SlotGroup group) => group == SlotGroup.Top_Weapons
        ? new Color(.95f, .59f, .27f) : group == SlotGroup.Center_Shields
        ? new Color(.30f, .72f, .95f) : new Color(.48f, .82f, .59f);

    public static RectTransform Rect(Transform t, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        var r = (RectTransform)t;
        r.anchorMin = min; r.anchorMax = max; r.pivot = new Vector2(.5f, .5f);
        r.offsetMin = offsetMin; r.offsetMax = offsetMax;
        r.localScale = Vector3.one;
        return r;
    }
    public static void Box(Transform t, float x, float y, float width, float height)
    {
        Rect(t, Vector2.up, Vector2.up, new Vector2(x, -y-height), new Vector2(x+width, -y));
    }
    public static GameObject Node(string name, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false); return go;
    }
    public static Image Panel(string name, Transform parent, Color color)
    {
        var go = Node(name, parent);
        var image = CombatUIStyle.Ensure<Image>(go);
        image.sprite = null; image.color = color; return image;
    }
    public static TextMeshProUGUI Text(string name, Transform parent, string text, float size = 16)
    {
        var go = Node(name, parent);
        var label = CombatUIStyle.Ensure<TextMeshProUGUI>(go);
        label.text = text; label.fontSize = size; label.color = new Color(.9f,.94f,1f);
        label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }
    public static Button Button(string name, Transform parent, string caption, UnityEngine.Events.UnityAction action)
    {
        var image = Panel(name, parent, Card);
        var button = Ensure<Button>(image.gameObject);
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners(); if (action != null) button.onClick.AddListener(action);
        var text = Text("Caption", image.transform, caption);
        text.alignment = TextAlignmentOptions.Center;
        Rect(text.transform, Vector2.zero, Vector2.one, new Vector2(6,3), new Vector2(-6,-3));
        return button;
    }
    public static void CardVisual(Image background, TextMeshProUGUI label, SlotGroup group, string name, bool highlight)
    {
        background.sprite = null; background.color = Card; background.raycastTarget = true;
        var stripe = Panel("CategoryStripe", background.transform, Category(group)); stripe.raycastTarget = false;
        Rect(stripe.transform, Vector2.zero, new Vector2(0,1), Vector2.zero, new Vector2(3,0));
        var outline = Ensure<Outline>(background.gameObject);
        outline.effectColor = highlight ? Category(group) : new Color(.22f,.28f,.36f);
        outline.effectDistance = Vector2.one;
        if (label != null)
        {
            label.text = name; label.fontSize = 14; label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            label.enableAutoSizing = true; label.fontSizeMin = 11; label.fontSizeMax = 14;
            Rect(label.transform, Vector2.zero, Vector2.one, new Vector2(8,7), new Vector2(-6,-7));
        }
    }
}