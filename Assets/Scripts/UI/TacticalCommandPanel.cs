using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TacticalCommandPanel : MonoBehaviour
{
    [Header("Ship References")]
    public ShipWeapon playerWeapon;
    public ShipAbilities playerAbilities;

    [Header("Behavior Buttons")]
    public Button orbitButton;
    public Button keepDistanceButton;
    public Button flankButton;

    [Header("Target Priority Buttons")]
    public Button autoTargetButton;
    public Button weaponTargetButton;
    public Button shieldTargetButton;
    public Button engineTargetButton;

    [Header("Ability Buttons")]
    public Button afterburnerButton;
    public Button shieldBoostButton;
    public Button empButton;

    [Header("Ability Labels (TMP on each button)")]
    public TextMeshProUGUI afterburnerLabel;
    public TextMeshProUGUI shieldBoostLabel;
    public TextMeshProUGUI empLabel;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(1f, 0.85f, 0.2f, 1f);
    private TextMeshProUGUI tooltip;
    private GameObject tooltipPopup;
    private Button stopButton;
    private Button engageButton;
    private readonly Button[] speedButtons = new Button[4];
    private TextMeshProUGUI movementStatus;
    public bool PointerInsideTooltip(Vector2 screen)
    {
        var canvas=GetComponentInParent<Canvas>();
        return tooltipPopup!=null && tooltipPopup.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)tooltipPopup.transform,screen,canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null);
    }
    public void ShowTooltip(string text, RectTransform source = null)
    {
        if(tooltip==null || tooltipPopup==null)return;
        bool visible=!string.IsNullOrEmpty(text) && FittingWindow.OpenWindow==null;
        tooltipPopup.SetActive(visible);
        if(!visible)return;
        tooltip.text=text;
        var canvas=GetComponentInParent<Canvas>().rootCanvas;
        var root=(RectTransform)canvas.transform;
        var popup=(RectTransform)tooltipPopup.transform;
        popup.SetParent(root,false);
        var overlay=CombatUIStyle.Ensure<Canvas>(tooltipPopup);overlay.overrideSorting=true;overlay.sortingOrder=200;
        var group=CombatUIStyle.Ensure<CanvasGroup>(tooltipPopup);group.blocksRaycasts=false;group.interactable=false;
        foreach(var graphic in tooltipPopup.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
        const float gap=12;
        float width=Mathf.Min(340,root.rect.width-gap*2);
        tooltip.fontSize=14;tooltip.enableAutoSizing=false;
        float height=Mathf.Min(root.rect.height-gap*2,tooltip.GetPreferredValues(text,width-20,0).y+20);
        var corners=new Vector3[4];((RectTransform)transform).GetWorldCorners(corners);
        Vector2 low=root.InverseTransformPoint(corners[0]),high=root.InverseTransformPoint(corners[2]);
        // Include shield distribution in the control envelope, independent of resolution.
        RectTransform shields=null;
        foreach(var rect in root.GetComponentsInChildren<RectTransform>(true))if(rect.name=="ShieldDistribution"){shields=rect;break;}
        if(shields!=null){shields.GetWorldCorners(corners);low=Vector2.Min(low,root.InverseTransformPoint(corners[0]));high=Vector2.Max(high,root.InverseTransformPoint(corners[2]));}
        float x=high.x-width*.5f,y=high.y+gap+height*.5f;
        if(y+height*.5f>root.rect.yMax-gap){x=low.x-gap-width*.5f;y=high.y-height*.5f;}
        x=Mathf.Clamp(x,root.rect.xMin+gap+width*.5f,root.rect.xMax-gap-width*.5f);
        y=Mathf.Clamp(y,root.rect.yMin+gap+height*.5f,root.rect.yMax-gap-height*.5f);
        popup.anchorMin=popup.anchorMax=new Vector2(.5f,.5f);popup.pivot=new Vector2(.5f,.5f);
        popup.sizeDelta=new Vector2(width,height);popup.position=root.TransformPoint(new Vector3(x,y,0));popup.SetAsLastSibling();
    }
    private void OnDisable() { ShowTooltip(null); }
    private void OnDestroy(){if(tooltipPopup!=null)Destroy(tooltipPopup);}
    public void ConfigureLayout()
    {
        CombatUIStyle.Rect(transform,Vector2.one,Vector2.one,new Vector2(-288,-714),new Vector2(-16,-394));
        var background=CombatUIStyle.Ensure<Image>(gameObject);background.sprite=null;background.color=CombatUIStyle.Surface;background.raycastTarget=true;
        foreach(var layout in GetComponentsInChildren<LayoutGroup>(true))layout.enabled=false;
        foreach(var fitter in GetComponentsInChildren<ContentSizeFitter>(true))fitter.enabled=false;
        foreach(var text in GetComponentsInChildren<TextMeshProUGUI>(true))text.gameObject.SetActive(false);
        Group("BehaviorHeading","ПРИКАЗ ДВИЖЕНИЯ",8);
        var engage=CombatUIStyle.Button("EngageOrder",transform,"Вступить в бой",()=> {
            var selected=playerWeapon!=null?(playerWeapon.PendingTarget!=null?playerWeapon.PendingTarget:playerWeapon.Target):null;
            if(ShipOrderController.Available(selected))PlayerOrders()?.Tactical(ShipOrderMode.EngageTarget,selected);
        });
        CombatUIStyle.Box(engage.transform,134,8,130,18);
        engageButton=engage;
        var engageTip=CombatUIStyle.Ensure<TacticalTooltip>(engage.gameObject);engageTip.panel=this;
        Group("PriorityHeading","ПРИОРИТЕТ ЦЕЛИ",78);
        Group("AbilitiesHeading","СПОСОБНОСТИ",148);
        Setup(orbitButton,"orbit","Орбита","Орбита — движение вокруг захваченной цели.",0,28);
        Setup(keepDistanceButton,"distance","Дистанция","Дистанция — удерживать дистанцию до цели.",1,28);
        Setup(flankButton,"flank","Корма","Заход с кормы — отдельный приказ относительно выбранной цели.",2,28);
        stopButton=CombatUIStyle.Button("StopOrder",transform,"Стоп",()=>PlayerOrders()?.Stop());
        CombatUIStyle.Box(stopButton.transform,206,28,58,48);
        var stopCaption=stopButton.GetComponentInChildren<TextMeshProUGUI>(true);stopCaption.gameObject.SetActive(true);stopCaption.fontSize=12;
        var stopTip=CombatUIStyle.Ensure<TacticalTooltip>(stopButton.gameObject);stopTip.panel=this;stopTip.description="Стоп [X] — очистить маршрут и активно затормозить. При выбитом двигателе остаётся только пассивное замедление.";
        for(int i=0;i<4;i++)
        {
            float limit=(i+1)*.25f;
            var button=CombatUIStyle.Button("SpeedLimit"+(i+1),transform,((i+1)*25)+"%",()=>PlayerOrders()?.SetSpeedLimit(limit));
            speedButtons[i]=button;CombatUIStyle.Box(button.transform,8+i*65,222,61,25);
            var caption=button.GetComponentInChildren<TextMeshProUGUI>(true);caption.gameObject.SetActive(true);caption.fontSize=12;
            var tip=CombatUIStyle.Ensure<TacticalTooltip>(button.gameObject);tip.panel=this;tip.description="Предел желаемой скорости "+((i+1)*25)+"%. Не меняет оборудование и скорость игры; снижение вызывает плавное торможение.";
        }
        movementStatus=CombatUIStyle.Text("MovementStatus",transform,"",12);movementStatus.gameObject.SetActive(true);
        CombatUIStyle.Box(movementStatus.transform,8,252,256,48);
        Setup(autoTargetButton,"auto","Авто","Авто — автоматически распределять попадания по модулям.",0,98);
        Setup(weaponTargetButton,"weapon","Оружие","Оружие — приоритет поражения оружейного модуля.",1,98);
        Setup(shieldTargetButton,"shield","Щит","Щит — приоритет поражения модуля щита.",2,98);
        Setup(engineTargetButton,"engine","Двигатель","Двигатель — приоритет поражения двигателя.",3,98);
        afterburnerLabel=Setup(afterburnerButton,"boost","Форсаж","Форсаж — скорость ×2 на 2 с. Энергия 25, нагрев 20, КД 10 с.",0,168);
        shieldBoostLabel=Setup(shieldBoostButton,"shieldboost","Щит+","Щит+ — +30 щита, −10 корпуса. Энергия 30, нагрев 15, КД 8 с.",1,168);
        empLabel=Setup(empButton,"emp","РЭБ","РЭБ — подавить захват цели на 3 с. Нужна цель. Энергия 35, нагрев 25, КД 12 с.",2,168);
        var oldTooltip=transform.Find("CommandTooltip");
        if(oldTooltip!=null)oldTooltip.gameObject.SetActive(false);
        var popup=CombatUIStyle.Panel("CommandTooltipPopup",transform,CombatUIStyle.Surface);
        popup.raycastTarget=false;tooltipPopup=popup.gameObject;
        var border=CombatUIStyle.Ensure<Outline>(tooltipPopup);border.effectColor=new Color(.3f,.45f,.6f);border.effectDistance=Vector2.one;
        tooltip=CombatUIStyle.Text("Text",popup.transform,"",13);
        tooltip.gameObject.SetActive(true);
        CombatUIStyle.Rect(tooltip.transform,Vector2.zero,Vector2.one,new Vector2(10,8),new Vector2(-10,-8));
        tooltipPopup.transform.SetAsLastSibling();tooltipPopup.SetActive(false);
    }
    private void Group(string name,string title,float y)
    {
        var label=CombatUIStyle.Text(name,transform,title,12);label.gameObject.SetActive(true);
        CombatUIStyle.Box(label.transform,10,y,252,18);
    }
    private TextMeshProUGUI Setup(Button button,string icon,string title,string hint,int column,float y)
    {
        if(button==null)return null;
        button.transform.SetParent(transform,false);button.gameObject.SetActive(true);
        CombatUIStyle.Box(button.transform,10+column*64,y,60,46);
        var image=CombatUIStyle.Ensure<Image>(button.gameObject);image.sprite=null;image.color=CombatUIStyle.Card;image.raycastTarget=true;button.targetGraphic=image;
        var border=CombatUIStyle.Ensure<Outline>(button.gameObject);border.effectDistance=new Vector2(1.5f,1.5f);
        var picture=CombatUIStyle.Panel("CommandIcon",button.transform,Color.white);picture.raycastTarget=false;
        picture.sprite=Resources.Load<Sprite>("TacticalIcons/"+icon);picture.preserveAspect=true;
        CombatUIStyle.Box(picture.transform,19,3,22,22);
        var label=CombatUIStyle.Text("CommandLabel",button.transform,title,10);label.gameObject.SetActive(true);
        label.alignment=TextAlignmentOptions.Center;CombatUIStyle.Box(label.transform,1,26,58,18);
        var hover=CombatUIStyle.Ensure<TacticalTooltip>(button.gameObject);hover.panel=this;hover.description=hint;
        return label;
    }

    private void Start()
    {
        if (playerWeapon == null)
        {
            Debug.LogWarning("[TacticalCommandPanel] playerWeapon не назначен.");
            return;
        }

        // Behavior
        orbitButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.Orbit));
        keepDistanceButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.KeepDistance));
        flankButton.onClick.AddListener(() => playerWeapon.SetBehavior(CombatBehavior.Flank));

        // Target priority
        autoTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(null));
        weaponTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Weapon));
        shieldTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Shield));
        engineTargetButton.onClick.AddListener(() => playerWeapon.SetTargetModule(ModuleType.Engine));

        // Abilities
        afterburnerButton.onClick.AddListener(() => TimeController.Instance?.RequestAbility(Game.Core.AbilityCommandKind.Afterburner));
        shieldBoostButton.onClick.AddListener(() => TimeController.Instance?.RequestAbility(Game.Core.AbilityCommandKind.ShieldBoost));
        empButton.onClick.AddListener(() => TimeController.Instance?.RequestAbility(Game.Core.AbilityCommandKind.Emp));
    }

    private Button courseButton, targetFacingButton;
    private void EnsureOrientationButtons()
    {
        if (courseButton != null) return;
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            var panel = CombatUIStyle.Panel(i == 0 ? "FaceCourse" : "FaceOrderTarget", transform, CombatUIStyle.Card);
            CombatUIStyle.Box(panel.transform, 10 + i * 128, 290, 124, 25);
            var button = CombatUIStyle.Ensure<Button>(panel.gameObject);
            button.targetGraphic = panel;
            var label = CombatUIStyle.Text("Label", panel.transform, i == 0 ? "По курсу" : "На цель", 11);
            label.gameObject.SetActive(true);
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            CombatUIStyle.Rect(label.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            button.onClick.AddListener(() => {
                if(index==0)PlayerOrders()?.SetOrientation(HullOrientation.Course);
                else PlayerOrders()?.FaceSelectedTarget(playerWeapon!=null?(playerWeapon.PendingTarget!=null?playerWeapon.PendingTarget:playerWeapon.Target):null);
            });
            var tip = CombatUIStyle.Ensure<TacticalTooltip>(panel.gameObject);
            tip.panel = this;
            tip.description = i == 0 ? "Нос по траектории. Торможение имеет приоритет." : "Нос на цель текущего приказа, не выбранную оружейную цель. Боковая сила ограничена.";
            if (i == 0) courseButton = button; else targetFacingButton = button;
        }
    }
    private void Update()
    {
        if(FittingWindow.OpenWindow!=null)ShowTooltip(null);
        var player=GameObject.FindGameObjectWithTag("Player");
        playerWeapon=player!=null?player.GetComponent<ShipWeapon>():null;
        playerAbilities=player!=null?player.GetComponent<ShipAbilities>():null;
        if (playerWeapon == null) return;
        EnsureOrientationButtons();
        var orders=PlayerOrders();
        if(orders!=null)
        {
            SetButtonColor(stopButton,orders.Mode==ShipOrderMode.HoldPosition?activeColor:normalColor);
            orders.ValidateFacingTarget();
            targetFacingButton.interactable = ShipOrderController.Available(playerWeapon.PendingTarget!=null?playerWeapon.PendingTarget:playerWeapon.Target);
            var facingTip=CombatUIStyle.Ensure<TacticalTooltip>(targetFacingButton.gameObject);facingTip.panel=this;
            facingTip.description=targetFacingButton.interactable?"Держать нос на выбранном противнике, сохраняя маршрут":"Выберите противника ЛКМ";
            if(engageButton!=null)
            {
                engageButton.interactable=ShipOrderController.Available(playerWeapon.PendingTarget!=null?playerWeapon.PendingTarget:playerWeapon.Target);
                engageButton.GetComponent<TacticalTooltip>().description=engageButton.interactable?"Вступить в бой с выбранной целью.":"Нужна выбранная цель.";
                SetButtonColor(engageButton,orders.Mode==ShipOrderMode.EngageTarget?activeColor:normalColor);
            }
            SetButtonColor(courseButton, orders.Orientation == HullOrientation.Course ? activeColor : normalColor);
            SetButtonColor(targetFacingButton, orders.Orientation == HullOrientation.OrderTarget ? activeColor : normalColor);
            var movement=orders.GetComponent<ShipMovement>();
            if(movementStatus!=null)movementStatus.text=$"{orders.Status}\nСкорость {movement.Rb.linearVelocity.magnitude:F1} / {movement.maxSpeed*movement.SpeedMultiplier*orders.SpeedLimit:F1} ({orders.SpeedLimit:P0})"+(movement.EngineAvailable?"":"\nДвигатель выбит: активное торможение недоступно");
            for(int i=0;i<4;i++)if(speedButtons[i]!=null)SetButtonColor(speedButtons[i],Mathf.Abs(orders.SpeedLimit-(i+1)*.25f)<.01f?activeColor:normalColor);
        }

        HighlightBehavior(orbitButton, CombatBehavior.Orbit);
        HighlightBehavior(keepDistanceButton, CombatBehavior.KeepDistance);
        HighlightBehavior(flankButton, CombatBehavior.Flank);

        HighlightTarget(autoTargetButton, null);
        HighlightTarget(weaponTargetButton, ModuleType.Weapon);
        HighlightTarget(shieldTargetButton, ModuleType.Shield);
        HighlightTarget(engineTargetButton, ModuleType.Engine);

        UpdateAbilityButton(afterburnerButton, afterburnerLabel, "Форсаж", playerAbilities ? playerAbilities.afterburnerCooldown : 0f);
        UpdateAbilityButton(shieldBoostButton, shieldBoostLabel, "Щит+", playerAbilities ? playerAbilities.shieldBoostCooldown : 0f);
        UpdateAbilityButton(empButton, empLabel, "РЭБ", playerAbilities ? playerAbilities.emPulseCooldown : 0f);
    }

    private void HighlightBehavior(Button btn, CombatBehavior behavior)
    {
        if (btn == null) return;
        var orders=PlayerOrders();
        var selected=playerWeapon.PendingTarget!=null?playerWeapon.PendingTarget:playerWeapon.Target;
        btn.interactable=ShipOrderController.Available(selected);
        var tip=btn.GetComponent<TacticalTooltip>();
        if(tip!=null)tip.description=(behavior==CombatBehavior.KeepDistance?"Держать дистанцию до выбранной цели.":behavior==CombatBehavior.Orbit?"Орбита вокруг выбранной цели.":"Заход с кормы выбранной цели.")+(btn.interactable?"":" Нужна выбранная цель.");
        var mode=behavior==CombatBehavior.Orbit?ShipOrderMode.Orbit:behavior==CombatBehavior.KeepDistance?ShipOrderMode.KeepDistance:ShipOrderMode.Flank;
        bool isActive = orders!=null && orders.Mode==mode;
        SetButtonColor(btn, isActive ? activeColor : normalColor);
    }

    private void HighlightTarget(Button btn, ModuleType? module)
    {
        if (btn == null) return;
        bool isActive = playerWeapon.targetedModule == module;
        SetButtonColor(btn, isActive ? activeColor : normalColor);
    }
    private ShipOrderController PlayerOrders()
    {
        var movement=playerWeapon!=null?playerWeapon.GetComponentInParent<ShipMovement>():null;
        return movement!=null?movement.Orders:null;
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if(btn==null)return;
        var cb = btn.colors;
        cb.normalColor = color;
        cb.selectedColor = color;
        cb.highlightedColor = color * 1.1f;
        btn.colors = cb;
        var border=btn.GetComponent<Outline>();
        if(border!=null)border.effectColor=color==activeColor?activeColor:new Color(.22f,.29f,.36f);
    }

    private void UpdateAbilityButton(Button btn, TextMeshProUGUI label, string baseLabel, float cooldown)
    {
        if (btn == null) return;

        var reactor=playerAbilities!=null?playerAbilities.GetComponent<ShipReactor>():null;
        float cost=btn==afterburnerButton?25f:btn==shieldBoostButton?30f:35f;
        string state=playerAbilities==null||reactor==null?"Нет корабля":CombatSession.IsPreparing?"Подготовка":
            cooldown>0f?$"{Mathf.CeilToInt(cooldown)} с":reactor.Overheated?"Перегрев":reactor.Energy<cost?"Нет энергии":
            btn==empButton&&(playerWeapon==null||playerWeapon.Target==null)?"Нет цели":"";
        btn.interactable=TimeController.IsPaused || state=="";
        var abilityTip=btn.GetComponent<TacticalTooltip>();
        if(abilityTip!=null)abilityTip.description=(btn==afterburnerButton?"Форсаж: скорость ×2 на 2 с.":btn==shieldBoostButton?"Щит+: +30 щита, −10 корпуса.":"РЭБ: подавление захвата на 3 с.")+
            (state==""?"":" "+state+".")+(TimeController.IsPaused?" Нажатие добавляет или снимает с плана.":"");
        if(TimeController.IsPaused && TimeController.Instance!=null)
        {
            var kind=btn==afterburnerButton?Game.Core.AbilityCommandKind.Afterburner:btn==shieldBoostButton?Game.Core.AbilityCommandKind.ShieldBoost:Game.Core.AbilityCommandKind.Emp;
            int number=0;var queue=TimeController.Instance.Plan.Commands;
            for(int i=0;i<queue.Count;i++)if(queue[i].Kind==kind)number=i+1;
            if(number>0)state="#"+number+" "+(state==""?baseLabel:state);
        }
        if(label!=null){label.text=state==""?baseLabel:state;label.fontSize=state.Length>9?8:10;}
        var icon=btn.transform.Find("CommandIcon");
        if(icon!=null)icon.GetComponent<Image>().color=state==""?Color.white:new Color(.42f,.48f,.54f);
        var border=btn.GetComponent<Outline>();
        if(border!=null)border.effectColor=state=="Перегрев"?new Color(.9f,.22f,.12f):state=="Нет энергии"?new Color(.85f,.62f,.2f):new Color(.22f,.29f,.36f);
    }
}