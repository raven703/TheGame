using UnityEngine;

/// <summary>
/// Профиль игрока: текущий корабль, деньги, репутация.
/// Синглтон — один на сцену, живёт на [Managers].
/// </summary>
public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile Instance { get; private set; }

    [Header("Current Ship")]
    [Tooltip("shipId текущего корабля игрока. Должен совпадать с ShipDefinition.shipId.")]
    public string currentShipId = "cobra_mk3";

    [Header("Future")]
    public int credits = 1000;
    public int reputation = 0;

    private void Awake()
    {
        // Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Сменить текущий корабль игрока. Загружает loadout нового корабля.
    /// </summary>
    public void SetCurrentShip(string shipId)
    {
        if (string.IsNullOrEmpty(shipId))
            return;

        currentShipId = shipId;
        PlayerPrefs.SetString("player_current_ship_id", shipId);
        PlayerPrefs.Save();

        Debug.Log($"[PlayerProfile] Текущий корабль: {shipId}");
    }

    private void Start()
    {
        // Загружаем сохранённый корабль
        if (PlayerPrefs.HasKey("player_current_ship_id"))
        {
            currentShipId = PlayerPrefs.GetString("player_current_ship_id");
        }
    }
}