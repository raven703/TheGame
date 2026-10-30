using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI-панель боевого HUD: HP игрока слева, HP цели справа.
/// Полосы корпуса/щита, индикаторы модулей W/S/E.
/// </summary>
public class CombatHUDManager : MonoBehaviour
{
    [Header("Ship References")]
    public ShipHealth playerHealth;
    public ShipWeapon playerWeapon;

    [Header("Fallback Enemy")]
    [Tooltip("Используется, если цель не захвачена playerWeapon.Target == null.")]
    public ShipHealth defaultEnemyHealth;

    [Header("Player HUD — Labels")]
    public TextMeshProUGUI playerTitleLabel;
    public TextMeshProUGUI playerHullText;
    public TextMeshProUGUI playerShieldText;

    [Header("Player HUD — Bars")]
    public Image playerHullBarFill;
    public Image playerShieldBarFill;
    private Image energyFill, heatFill;
    private TextMeshProUGUI energyText, heatText, effectsText, shieldDistributionText;
    public void ConfigurePlayerPanel()
    {
        if (playerTitleLabel == null) return;
        var panel = playerTitleLabel.transform.parent;
        CombatUIStyle.Rect(panel, Vector2.up, Vector2.up, new Vector2(16,-320), new Vector2(336,-16));
        // Keep all original module indicators, only reposition the existing resource rows.
        CombatUIStyle.Box(playerTitleLabel.transform,12,8,296,22);
        CombatUIStyle.Box(playerHullText.transform,12,30,296,20);
        CombatUIStyle.Box(playerHullBarFill.transform.parent,12,51,296,8);
        CombatUIStyle.Box(playerShieldText.transform,12,62,174,20);
        CombatUIStyle.Box(playerShieldBarFill.transform.parent,12,83,296,8);
        shieldDistributionText=CombatUIStyle.Text("ShieldDistributionText",panel,"Перед 50% / Корма 50%",14);
        shieldDistributionText.raycastTarget=false;
        shieldDistributionText.textWrappingMode=TextWrappingModes.NoWrap;
        CombatUIStyle.Box(shieldDistributionText.transform,12,94,296,20);
        energyText=CombatUIStyle.Text("EnergyText",panel,"Энергия: —",15);
        heatText=CombatUIStyle.Text("HeatText",panel,"Нагрев: —",15);
        CombatUIStyle.Box(energyText.transform,12,122,296,20);
        CombatUIStyle.Box(heatText.transform,12,154,296,20);
        energyFill=ResourceBar(panel,"Energy",143);
        heatFill=ResourceBar(panel,"Heat",175);
        var moduleRow=panel.Find("PlayerModulesRow");
        if(moduleRow!=null)CombatUIStyle.Box(moduleRow,12,202,120,20);
        var moduleLabel=panel.Find("PlayerModulesLabel");
        if(moduleLabel!=null)CombatUIStyle.Box(moduleLabel,140,202,140,20);
        if(playerWeaponBoxImage!=null) CombatUIStyle.Box(playerWeaponBoxImage.transform,12,190,24,20);
        if(playerShieldBoxImage!=null) CombatUIStyle.Box(playerShieldBoxImage.transform,42,190,24,20);
        if(playerEngineBoxImage!=null) CombatUIStyle.Box(playerEngineBoxImage.transform,72,190,24,20);
        effectsText=CombatUIStyle.Text("Effects",panel,"",13);
        CombatUIStyle.Box(effectsText.transform,12,238,296,64);
    }
    private Image ResourceBar(Transform parent,string name,float y)
    {
        var track=CombatUIStyle.Panel(name+"Track",parent,new Color(.18f,.22f,.27f));
        track.raycastTarget=false;CombatUIStyle.Box(track.transform,12,y,296,8);
        var fill=CombatUIStyle.Panel("Fill",track.transform,Color.cyan);fill.raycastTarget=false;
        CombatUIStyle.Rect(fill.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        return fill;
    }
    private void UpdateResources()
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        if(player!=null){playerHealth=player.GetComponent<ShipHealth>();playerWeapon=player.GetComponent<ShipWeapon>();}
        else {playerHealth=null;playerWeapon=null;}
        if(energyText==null)ConfigurePlayerPanel();
        if(energyText==null)return;
        var reactor=player!=null?player.GetComponent<ShipReactor>():null;
        float energy=reactor!=null?Mathf.Clamp01(reactor.Energy/Mathf.Max(.001f,reactor.maxEnergy)):0;
        float heat=reactor!=null?Mathf.Clamp01(reactor.Heat/ShipReactor.OverheatThreshold):0;
        energyText.text=reactor!=null?$"Энергия: {reactor.Energy:0.#} / {reactor.maxEnergy:0.#}":"Энергия: —";
        heatText.text=reactor!=null?$"Нагрев: {heat*100:0}%"+(reactor.Overheated?" — ПЕРЕГРЕВ":""):"Нагрев: —";
        ((RectTransform)energyFill.transform).anchorMax=new Vector2(energy,1);
        ((RectTransform)heatFill.transform).anchorMax=new Vector2(heat,1);
        energyFill.color=new Color(.3f,.8f,.9f);
        heatFill.color=Color.Lerp(new Color(.3f,.75f,.65f),new Color(1,.15f,.08f),heat);
        effectsText.text=CombatStatusPanel.Effects(player);
        if(playerHealth==null){if(playerHullText!=null)playerHullText.text="Корпус: —";if(playerShieldText!=null)playerShieldText.text="Щит: —";}
    }

    [Header("Player HUD — Modules")]
    public Image playerWeaponBoxImage;
    public Image playerShieldBoxImage;
    public Image playerEngineBoxImage;

    [Header("Target HUD — Labels")]
    public TextMeshProUGUI targetTitleLabel;
    public TextMeshProUGUI targetHullText;
    public TextMeshProUGUI targetShieldText;

    [Header("Target HUD — Bars")]
    public Image targetHullBarFill;
    public Image targetShieldBarFill;

    [Header("Target HUD — Modules")]
    public Image targetWeaponBoxImage;
    public Image targetShieldBoxImage;
    public Image targetEngineBoxImage;

    [Header("Colors")]
    public Color hullColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color shieldColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color moduleOkColor = new Color(0.15f, 0.75f, 0.15f, 1f);
    public Color moduleDamagedColor = new Color(0.9f, 0.7f, 0.1f, 1f);
    public Color moduleDestroyedColor = new Color(0.8f, 0.2f, 0.2f, 1f);

    private void Update()
    {
        UpdateResources();
        UpdatePlayerHUD();
        UpdateTargetHUD();
    }

    private void UpdatePlayerHUD()
    {
        if (playerHealth == null || playerHealth.Data == null) return;

        // Заголовок
        if (playerTitleLabel != null)
            playerTitleLabel.text = "ИГРОК";

        // Корпус
        float hullPct = Mathf.Clamp01(playerHealth.Data.currentHullHP / Mathf.Max(1f, playerHealth.Data.maxHullHP));
        if (playerHullBarFill != null)
        {
            playerHullBarFill.fillAmount = hullPct;
            playerHullBarFill.color = hullColor;
        }
        if (playerHullText != null)
            playerHullText.text = $"Корпус: {playerHealth.Data.currentHullHP:F0}/{playerHealth.Data.maxHullHP:F0}";

        // Щит
        float shieldPct = playerHealth.Data.maxShieldHP > 0f
            ? Mathf.Clamp01(playerHealth.Data.currentShieldHP / playerHealth.Data.maxShieldHP)
            : 0f;
        if (playerShieldBarFill != null)
        {
            playerShieldBarFill.fillAmount = shieldPct;
            playerShieldBarFill.color = shieldColor;
        }
        if (playerShieldText != null)
        {
            var shield=playerHealth.Data.Damage.Shields;
            float fraction=TimeController.IsPaused?shield.Desired:shield.Fraction;
            playerShieldText.text=$"Щит: {playerHealth.Data.currentShieldHP:0} / {playerHealth.Data.maxShieldHP:0}";
            if(shieldDistributionText!=null)shieldDistributionText.text=$"Перед {fraction:P0} / Корма {1-fraction:P0}";
            playerShieldText.fontSize=12;
        }

        // Модули
        UpdateModuleBox(playerWeaponBoxImage, playerHealth.Data.GetModule(ModuleType.Weapon));
        UpdateModuleBox(playerShieldBoxImage, playerHealth.Data.GetModule(ModuleType.Shield));
        UpdateModuleBox(playerEngineBoxImage, playerHealth.Data.GetModule(ModuleType.Engine));
    }

    private void UpdateTargetHUD()
    {
        ShipHealth target = playerWeapon!=null
            ? (playerWeapon.PendingTarget!=null?playerWeapon.PendingTarget:playerWeapon.Target)
            : null;

        if (!ShipOrderController.Available(target))
        {
            if(targetTitleLabel!=null)targetTitleLabel.text="ЦЕЛЬ — не выбрана";
            if(targetHullText!=null)targetHullText.text="";
            if(targetShieldText!=null)targetShieldText.text="";
            if(targetHullBarFill!=null)targetHullBarFill.fillAmount=0;
            if(targetShieldBarFill!=null)targetShieldBarFill.fillAmount=0;
            return;
        }

        if (targetTitleLabel != null)
            targetTitleLabel.text = "ЦЕЛЬ";

        float hullPct = Mathf.Clamp01(target.Data.currentHullHP / Mathf.Max(1f, target.Data.maxHullHP));
        if (targetHullBarFill != null)
        {
            targetHullBarFill.fillAmount = hullPct;
            targetHullBarFill.color = hullColor;
        }
        if (targetHullText != null)
            targetHullText.text = $"Корпус: {target.Data.currentHullHP:F0}/{target.Data.maxHullHP:F0}";

        float shieldPct = target.Data.maxShieldHP > 0f
            ? Mathf.Clamp01(target.Data.currentShieldHP / target.Data.maxShieldHP)
            : 0f;
        if (targetShieldBarFill != null)
        {
            targetShieldBarFill.fillAmount = shieldPct;
            targetShieldBarFill.color = shieldColor;
        }
        if (targetShieldText != null)
            targetShieldText.text = $"Щит: {target.Data.currentShieldHP:F0}/{target.Data.maxShieldHP:F0}";

        UpdateModuleBox(targetWeaponBoxImage, target.Data.GetModule(ModuleType.Weapon));
        UpdateModuleBox(targetShieldBoxImage, target.Data.GetModule(ModuleType.Shield));
        UpdateModuleBox(targetEngineBoxImage, target.Data.GetModule(ModuleType.Engine));
    }

    private void UpdateModuleBox(Image boxImage, ShipModule module)
    {
        if (boxImage == null) return;

        if (module == null)
        {
            boxImage.color = moduleDestroyedColor;
            return;
        }

        if (module.isDestroyed)
        {
            boxImage.color = moduleDestroyedColor;
            return;
        }

        float pct = module.maxHP > 0f ? module.currentHP / module.maxHP : 0f;

        if (pct >= 0.99f)
            boxImage.color = moduleOkColor;
        else
            boxImage.color = moduleDamagedColor;
    }
}