#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using UnityEngine;

/// <summary>Editor-only physical experiment. Does not exist in a player build and never uses player stock.</summary>
public sealed class CombatExperiment : MonoBehaviour
{
    [Serializable] public class FighterMetrics
    {
        public string role;
        public int shots,hits,misses,coverHits,boosts,shieldBoosts,emps,navigationErrors;
        public float hullDamage,shieldDamage,moduleDamage,noGeometrySeconds,coverSeconds,noEnergySeconds,overheatSeconds,noLockSeconds;
    }
    [Serializable] public class Result
    {
        public int parameterVersion=CombatArchetypeCatalog.Version,seed;
        public string outcome,error;
        public float seconds,distanceFactor,fixedDeltaTime;
        public bool cover,swapped;
        public Vector2 initialA,initialB;
        public FighterMetrics a=new FighterMetrics(),b=new FighterMetrics();
    }
    [Serializable] private class Results { public List<Result> fights=new List<Result>(); }
    private sealed class SeededRandom:ICombatRandom
    {
        private readonly System.Random source;
        public SeededRandom(int seed){source=new System.Random(seed);}
        public float Value()=>(float)source.NextDouble();
        public int Index(int maximum)=>source.Next(maximum);
    }
    private readonly List<GameObject> actors=new List<GameObject>();
    private readonly Results results=new Results();
    private ShipHealth a,b;
    private Result current;
    public bool Finished {get;private set;}
    public string ReportDirectory {get;private set;}
    public bool SmokeOnly=true;
    public bool PreviewOnly;
    public bool Interactive=true;
    private int selectedA,selectedB=2;
    private bool withCover,playerControls,running;
    private float began;
    private string status="Подготовка — выберите сборки";
    private object oldLedger;
    private Action oldGuard;
    private Func<string,string> oldPresetRead;
    private Action<string,string> oldPresetWrite;
    private bool isolated;
    private IEnumerator Start()
    {
        // Installed before creating any ship/UI/profile component capable of accessing storage.
        var manager=typeof(ShipLoadoutManager);
        var field=manager.GetField("ledger",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        oldLedger=field.GetValue(null);oldGuard=ShipLoadoutManager.BeforeProductionAccess;
        field.SetValue(null,new PlayerEquipment("{\"formatVersion\":4,\"starterInventoryIssued\":true,\"inventory\":[],\"loadouts\":[]}",_=>{}));
        ShipLoadoutManager.BeforeProductionAccess=()=>{throw new InvalidOperationException("Experiment attempted production storage");};
        oldPresetRead=LoadoutPresets.Read;oldPresetWrite=LoadoutPresets.Write;
        LoadoutPresets.Read=_=>"";LoadoutPresets.Write=(_,__)=>{};isolated=true;
        ReportDirectory=Path.GetFullPath("Tools/CombatEvidence/Demo/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(ReportDirectory);
        if(Interactive)yield break;
        if(PreviewOnly)
        {
            Spawn(CombatDemoLoadouts.Role.Interceptor,CombatDemoLoadouts.Role.Escort,.9f,false,false,101);
            yield break;
        }
        int count=SmokeOnly?3:72;
        for(int index=0;index<count;index++)
        {
            int pair=SmokeOnly?index:index%6;
            int[] left={0,0,0,1,1,2},right={0,1,2,1,2,2};
            float factor=SmokeOnly?.9f:new[]{.5f,.9f,1.3f}[(index/6)%3];
            bool cover=!SmokeOnly&&(index/18)%2==1,swap=!SmokeOnly&&index/36==1;
            int seed=new[]{101,307,911}[index%3];
            Spawn((CombatDemoLoadouts.Role)left[pair],(CombatDemoLoadouts.Role)right[pair],factor,cover,swap,seed);
            float began=Time.time;
            while(Time.time-began<90f && a!=null&&b!=null&&!a.Data.IsDestroyed&&!b.Data.IsDestroyed)
            {
                yield return new WaitForFixedUpdate();
                Measure(a,b,current.a);Measure(b,a,current.b);
            }
            current.seconds=Time.time-began;
            bool deadA=a==null||a.Data.IsDestroyed,deadB=b==null||b.Data.IsDestroyed;
            current.outcome=deadA&&deadB?"mutual":deadA?"B":deadB?"A":"timeout";
            results.fights.Add(current);WriteResults();CleanupActors();yield return null;
        }
        Finished=true;
    }
    private void Spawn(CombatDemoLoadouts.Role left,CombatDemoLoadouts.Role right,float distance,bool cover,bool swap,int seed)
    {
        current=new Result{seed=seed,distanceFactor=distance,cover=cover,swapped=swap,fixedDeltaTime=Time.fixedDeltaTime};
        current.a.role=left.ToString();current.b.role=right.ToString();
        float half=8f*distance*.5f;current.initialA=new Vector2(swap?half:-half,0);current.initialB=-current.initialA;
        a=Fighter(left,current.initialA,swap?90:-90,"A",new SeededRandom(seed),current.a);
        b=Fighter(right,current.initialB,swap?-90:90,"B",new SeededRandom(seed+1),current.b);
        a.GetComponent<EnemyAI>().SetOpponent(b);b.GetComponent<EnemyAI>().SetOpponent(a);
        if(cover)
        {
            foreach(var position in new[]{new Vector2(0,0),new Vector2(0,3)})
            {
                var rock=new GameObject("Fixed experiment cover");actors.Add(rock);rock.transform.position=position;
                var rb=rock.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Static;rock.AddComponent<CircleCollider2D>().radius=.65f;
                rock.AddComponent<Asteroid>();
            }
        }
    }
    private ShipHealth Fighter(CombatDemoLoadouts.Role role,Vector2 position,float rotation,string label,ICombatRandom random,FighterMetrics metrics)
    {
        var definition=UnityEditor.AssetDatabase.LoadAssetAtPath<ShipDefinition>(role==CombatDemoLoadouts.Role.Interceptor?"Assets/Ships/Sidewinder.asset":"Assets/Ships/CobraMK3.asset");
        var ship=new GameObject("ISOLATED "+label+" "+role);ship.SetActive(false);actors.Add(ship);
        ship.transform.position=position;ship.transform.rotation=Quaternion.Euler(0,0,rotation);
        ship.AddComponent<Rigidbody2D>();ship.AddComponent<CircleCollider2D>().radius=.3f;
        var health=ship.AddComponent<ShipHealth>();health.shipDefinition=definition;
        ship.AddComponent<ShipMovement>();var weapon=ship.AddComponent<ShipWeapon>();weapon.CombatRandom=random;
        var abilities=ship.AddComponent<ShipAbilities>();var ai=ship.AddComponent<EnemyAI>();ai.useCombatRole=true;ai.combatRole=(CombatRole)(int)role;
        var sprite=ship.AddComponent<SpriteRenderer>();sprite.sprite=definition.hullSprite;sprite.color=label=="A"?Color.cyan:Color.red;
        ship.SetActive(true);
        ShipStatsCalculator.ApplyExperiment(health,definition,CombatDemoLoadouts.Create(role,definition,label));
        if(weapon.EquippedSlotCount==0)throw new InvalidOperationException("Experiment loadout contains no weapons");
        weapon.OnShotResolved+=(shot,hull,shield,module)=>
        {
            if(!shot.Fired)return;metrics.shots++;if(shot.Outcome==ShotOutcome.Ship)metrics.hits++;
            if(shot.Outcome==ShotOutcome.Miss)metrics.misses++;if(shot.Outcome==ShotOutcome.Cover)metrics.coverHits++;
            metrics.hullDamage+=hull;metrics.shieldDamage+=shield;metrics.moduleDamage+=module;
        };
        return health;
    }
    private readonly Dictionary<ShipHealth,string> lastNavigationError=new Dictionary<ShipHealth,string>();
    private readonly Dictionary<ShipHealth,Vector3> lastCooldowns=new Dictionary<ShipHealth,Vector3>();
    private void Measure(ShipHealth own,ShipHealth opponent,FighterMetrics metrics)
    {
        float dt=Time.fixedDeltaTime;var weapon=own.GetComponent<ShipWeapon>();var reactor=own.GetComponent<ShipReactor>();
        bool geometry=false;
        for(int i=0;i<weapon.EquippedSlotCount;i++)
        {
            var item=weapon.EquippedWeapon(i);if(item==null)continue;
            Vector2 delta=opponent.transform.position-weapon.MountOrigin(i);
            bool sector=Vector2.Angle(weapon.MountAxis(i),delta)<=weapon.EffectiveSector(i,item)*.5f;
            if(delta.magnitude<=weapon.range&&sector)geometry=true;
        }
        if(!geometry)metrics.noGeometrySeconds+=dt;
        if(Asteroid.FindCover(own.transform.position,opponent.transform.position,out _)!=null)metrics.coverSeconds+=dt;
        if(weapon.IsLocking||weapon.IsTargetingDisrupted)metrics.noLockSeconds+=dt;
        float cheapest=float.MaxValue;for(int i=0;i<weapon.EquippedSlotCount;i++){var item=weapon.EquippedWeapon(i);if(item!=null)cheapest=Mathf.Min(cheapest,WeaponStatsCalculator.Calculate(item,weapon.baseCooldown).energy);}
        if(reactor.Energy<cheapest)metrics.noEnergySeconds+=dt;if(reactor.Overheated)metrics.overheatSeconds+=dt;
        var abilities=own.GetComponent<ShipAbilities>();var cooldowns=new Vector3(abilities.afterburnerCooldown,abilities.shieldBoostCooldown,abilities.emPulseCooldown);
        lastCooldowns.TryGetValue(own,out var previous);
        if(cooldowns.x>previous.x+.1f)metrics.boosts++;if(cooldowns.y>previous.y+.1f)metrics.shieldBoosts++;if(cooldowns.z>previous.z+.1f)metrics.emps++;
        lastCooldowns[own]=cooldowns;string error=own.GetComponent<ShipMovement>().Orders.Error;
        lastNavigationError.TryGetValue(own,out var prior);if(!string.IsNullOrEmpty(error)&&error!=prior)metrics.navigationErrors++;lastNavigationError[own]=error;
    }
    private void WriteResults()
    {
        File.WriteAllText(Path.Combine(ReportDirectory,"results.json"),JsonUtility.ToJson(results,true));
        var lines=new List<string>{"roleA,roleB,seed,distance,cover,swapped,outcome,seconds,shotsA,hitsA,shotsB,hitsB"};
        foreach(var fight in results.fights)lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",fight.a.role,fight.b.role,fight.seed,fight.distanceFactor,fight.cover,fight.swapped,fight.outcome,fight.seconds,fight.a.shots,fight.a.hits,fight.b.shots,fight.b.hits));
        File.WriteAllLines(Path.Combine(ReportDirectory,"results.csv"),lines);
    }
    private void CleanupActors(){foreach(var actor in actors)if(actor!=null)Destroy(actor);actors.Clear();lastCooldowns.Clear();lastNavigationError.Clear();a=null;b=null;}
    public void StartSelectedFight()
    {
        if(!isolated)return;
        CleanupActors();
        Spawn((CombatDemoLoadouts.Role)selectedA,(CombatDemoLoadouts.Role)selectedB,.9f,withCover,false,101);
        if(playerControls)a.GetComponent<EnemyAI>().enabled=false;
        a.GetComponent<ShipWeapon>().SetTarget(b);
        began=Time.time;running=true;status="Бой";
    }
    private void Update()
    {
        if(!Interactive||!running)return;
        bool deadA=a==null||a.Data.IsDestroyed,deadB=b==null||b.Data.IsDestroyed;
        if(deadA||deadB||Time.time-began>=90f)
        {
            current.seconds=Time.time-began;current.outcome=deadA&&deadB?"mutual":deadA?"B":deadB?"A":"timeout";
            status=current.outcome=="timeout"?"Лимит 90 с — ничья":current.outcome=="mutual"?"Одновременное уничтожение":"Победитель: "+current.outcome;
            results.fights.Add(current);WriteResults();running=false;
            foreach(var ship in new[]{a,b})if(ship!=null){ship.GetComponent<EnemyAI>().enabled=false;ship.GetComponent<ShipWeapon>().AIFireAllowed=false;ship.GetComponent<ShipMovement>().Stop();}
        }
        if(playerControls&&a!=null&&!deadA)
        {
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(mouse!=null&&mouse.rightButton.wasPressedThisFrame&&mouse.position.ReadValue().y<Screen.height-260)
            {
                var position=Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
                a.GetComponent<ShipMovement>().Orders.MoveTo(position);
            }
        }
    }
    private void FixedUpdate()
    {
        if(Interactive&&running&&a!=null&&b!=null){Measure(a,b,current.a);Measure(b,a,current.b);}
    }
    private string Describe(ShipHealth ship)
    {
        if(ship==null)return "—";
        var reactor=ship.GetComponent<ShipReactor>();var move=ship.GetComponent<ShipMovement>();
        return $"{ship.name}: корпус {ship.Data.currentHullHP:F0}/{ship.Data.maxHullHP:F0}, щит {ship.Data.currentShieldHP:F0}; энергия {reactor.Energy:F0}; скорость {move.maxSpeed:F1}, тяга {move.acceleration:F1}; {ship.GetComponent<EnemyAI>().CurrentManeuver}";
    }
    private void OnDestroy()
    {
        CleanupActors();if(!isolated)return;
        typeof(ShipLoadoutManager).GetField("ledger",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,oldLedger);
        ShipLoadoutManager.BeforeProductionAccess=oldGuard;LoadoutPresets.Read=oldPresetRead;LoadoutPresets.Write=oldPresetWrite;
    }
    private void OnGUI()
    {
        if(Interactive)
        {
            GUI.Box(new Rect(8,8,Mathf.Min(Screen.width-16,1140),246),"");
            GUILayout.BeginArea(new Rect(20,15,Mathf.Min(Screen.width-40,1100),235));
            GUILayout.Label("Демонстрация — прогресс не сохраняется | ИЗОЛИРОВАННЫЙ ТЕСТОВЫЙ ПРОФИЛЬ");
            string[] names={"Перехватчик / Sidewinder","Артиллерист / Cobra","Эскорт / Cobra"};
            GUILayout.BeginHorizontal();GUILayout.Label("A",GUILayout.Width(20));selectedA=GUILayout.SelectionGrid(selectedA,names,3);GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();GUILayout.Label("B",GUILayout.Width(20));selectedB=GUILayout.SelectionGrid(selectedB,names,3);GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();withCover=GUILayout.Toggle(withCover,"Фиксированные астероиды");playerControls=GUILayout.Toggle(playerControls,"Игрок управляет A (иначе AI)");
            if(GUILayout.Button("Начать / перезапустить"))StartSelectedFight();GUILayout.EndHorizontal();
            GUILayout.Label(status+" | Время: "+(running?Time.time-began:current!=null?current.seconds:0).ToString("F1")+" с");
            GUILayout.Label(Describe(a));GUILayout.Label(Describe(b));
            GUILayout.Label("Игрок A: ПКМ по полю — курс; огонь по B автоматический. Способности:");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Форсаж")&&playerControls&&running&&a!=null)a.GetComponent<ShipAbilities>().UseAfterburner();
            if(GUILayout.Button("Усилить щит")&&playerControls&&running&&a!=null)a.GetComponent<ShipAbilities>().UseShieldBoost();
            if(GUILayout.Button("ЭМИ")&&playerControls&&running&&a!=null)a.GetComponent<ShipAbilities>().UseEMPulse();
            GUILayout.EndHorizontal();GUILayout.EndArea();return;
        }
        GUI.color=Color.cyan;GUI.Label(new Rect(15,15,1000,30),"ИЗОЛИРОВАННЫЙ ПРОФИЛЬ — прогресс игрока не используется. Combat v1, реальная Unity-физика.");
        GUI.color=Color.white;GUI.Label(new Rect(15,45,1000,30),$"Бои: {results.fights.Count}; {(Finished?"завершено":PreviewOnly?"демонстрация":"испытание")}; отчёты: {ReportDirectory}");
    }
}
#endif