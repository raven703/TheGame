using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Кнопка "ФИТИНГ (I)". Открывает/закрывает FittingWindow по клику и по клавише I.
/// </summary>
public class FittingButton : MonoBehaviour
{
    [Header("References")]
    public FittingWindow fittingWindow;
    public Button button;

    [Header("Hotkey")]
    public bool useHotkey = true;
    public KeyCode toggleKey = KeyCode.I;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        if (!useHotkey) return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            OnClick();
#else
        if (Input.GetKeyDown(toggleKey))
            OnClick();
#endif
    }

    private void OnClick()
    {
        // Блокировка в бою
        if (GameState.Instance != null && GameState.Instance.isCombatStarted)
        {
            Debug.Log("[FittingButton] Фитинг недоступен во время боя!");
            return;
        }
        if (fittingWindow == null) return;
        fittingWindow.Toggle();
    }
}