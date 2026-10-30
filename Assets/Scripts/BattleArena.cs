using UnityEngine;
using Game.Core;

/// <summary>Scene-local observation/display adapter for the immutable Core battle circle.</summary>
public class BattleArena : MonoBehaviour
{
    [Min(1)] public float operationRadius=90f;
    public BattleArea Area {get;private set;}
    public float InitialRange {get;private set;}
    private LineRenderer line;
    private Material material;
    private UnityEngine.UI.Button returnButton;
    private TMPro.TextMeshProUGUI warning;
    private bool outsideSupport;
    private float outsideSeconds;
    public float OutsideSeconds => outsideSeconds;
    public bool OutsideSupport => outsideSupport;
    public static BattleArena Current {get;private set;}
    public static BattleArena Ensure()
    {
        if(Current!=null)return Current;
        var existing=FindAnyObjectByType<BattleArena>();
        if(existing!=null){existing.Initialize();return existing;}
        var arena=new GameObject("Battle area (runtime)").AddComponent<BattleArena>();arena.Initialize();return arena;
    }
    private void Initialize()
    {
        if(Area!=null)return;
        Current=this;
        var player=GameObject.FindGameObjectWithTag("Player");
        var weapon=player!=null?player.GetComponent<ShipWeapon>():null;
        InitialRange=weapon!=null?Mathf.Max(1,weapon.range):8;
        Vector2 center=player!=null?(Vector2)player.transform.position:Vector2.zero;
        Area=new BattleArea(new AimPoint(center.x,center.y),operationRadius);
        line=gameObject.AddComponent<LineRenderer>();line.useWorldSpace=true;line.loop=true;line.positionCount=192;line.widthMultiplier=.045f;
        material=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=material;
        for(int i=0;i<192;i++){float a=i*Mathf.PI*2/192;line.SetPosition(i,new Vector3(center.x+Mathf.Cos(a)*Area.Radius,center.y+Mathf.Sin(a)*Area.Radius,0));}
        var layout=FindAnyObjectByType<CombatInterfaceLayout>();
        if(layout!=null)
        {
            warning=CombatUIStyle.Text("ArenaWarning",layout.transform,"",16);
            warning.raycastTarget=false;
            warning.alignment=TMPro.TextAlignmentOptions.Center;
            CombatUIStyle.Rect(warning.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-240,-190),new Vector2(240,-136));
            returnButton=CombatUIStyle.Button("ArenaReturn",layout.transform,"Вернуться",ReturnToSupport);
            CombatUIStyle.Rect(returnButton.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-60,-224),new Vector2(60,-194));
            warning.gameObject.SetActive(false);returnButton.gameObject.SetActive(false);
        }
        Debug.Log("[BattleArena] radius="+Area.Radius+" center="+center);
    }
    public Vector2 SafePoint(Vector2 desired,float margin)
    {
        Vector2 result;
        return TrySafePoint(desired,margin,out result)?result:desired;
    }
    public bool TrySafePoint(Vector2 desired,float margin,out Vector2 result)
    {
        var p=Area.Clamp(new AimPoint(desired.x,desired.y),margin);
        Vector2 center=new Vector2(Area.Center.X,Area.Center.Y),candidate=new Vector2(p.X,p.Y);
        for(int i=0;i<48;i++)
        {
            bool blocked=false;
            foreach(var c in Physics2D.OverlapCircleAll(candidate,Mathf.Max(.1f,margin-2)))if(c.GetComponentInParent<Asteroid>()!=null){blocked=true;break;}
            if(!blocked){result=candidate;return true;}
            float a=i*2.399963f;
            float radius=Mathf.Max(0,Area.Radius-margin)*Mathf.Sqrt((i+.5f)/48f);
            candidate=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
        }
        result=desired;return false;
    }
    public void ReturnToSupport()
    {
        var player=GameObject.FindGameObjectWithTag("Player");
        if(player==null)return;
        var movement=player.GetComponent<ShipMovement>();Vector2 destination;
        if(movement!=null&&TrySafePoint(player.transform.position,movement.ShipRadius+4,out destination))movement.Orders.MoveTo(destination);
        else CombatSession.Log("Безопасная точка возврата не найдена");
    }
    public void TickSupport(Vector2 position,float gameDelta,bool active,CombatManager manager)
    {
        if(!active){outsideSupport=false;outsideSeconds=0;return;}
        var point=new AimPoint(position.x,position.y);
        if(!outsideSupport&&!Area.Contains(point))outsideSupport=true;
        else if(outsideSupport&&Area.Contains(point,2)){outsideSupport=false;outsideSeconds=0;}
        if(outsideSupport)
        {
            outsideSeconds+=Mathf.Max(0,gameDelta);
            if(outsideSeconds>=10&&manager!=null)manager.FailOutsideSupport();
        }
    }
    private void Update()
    {
        if(Area==null)Initialize();
        var player=GameObject.FindGameObjectWithTag("Player");
        var manager=FindAnyObjectByType<CombatManager>();
        bool active=player!=null&&manager!=null&&!CombatSession.IsPreparing&&!manager.IsCombatOver;
        TickSupport(player!=null?(Vector2)player.transform.position:Vector2.zero,Time.deltaTime,active,manager);
        bool outside=outsideSupport&&active&&!manager.IsCombatOver;
        if(warning!=null)
        {
            warning.gameObject.SetActive(outside);
            warning.text="Вы покидаете зону тактической поддержки\nВернитесь: "+Mathf.CeilToInt(Mathf.Max(0,10-outsideSeconds))+" с";
            warning.alpha=Time.timeScale==0?1:.8f+.2f*Mathf.Sin(Time.unscaledTime*2);
        }
        if(returnButton!=null)returnButton.gameObject.SetActive(outside);
        float proximity=player!=null?Vector2.Distance(player.transform.position,new Vector2(Area.Center.X,Area.Center.Y))/Area.Radius:0;
        line.startColor=line.endColor=new Color(.2f,.8f,.85f,Mathf.Lerp(.12f,.55f,Mathf.InverseLerp(.7f,1,proximity)));
    }
    private void OnDestroy(){if(Current==this)Current=null;if(material!=null)Destroy(material);if(warning!=null)Destroy(warning.gameObject);if(returnButton!=null)Destroy(returnButton.gameObject);}
}
