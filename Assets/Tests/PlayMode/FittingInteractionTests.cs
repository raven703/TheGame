using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>Real UGUI drag handlers against an isolated, in-memory-owned ledger. Never writes player saves.</summary>
public class FittingInteractionTests
{
    private static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
    private static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name).Invoke(value,args);
    private static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);
    private static void Set(object value,string name,object data)=>value.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(value,data);
    private static object Invoke(object value,string name,params object[] args)=>value.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(value,args);
    private static IList Slots(object window)=>(IList)Field(window,"weaponSlots");
    private static string LedgerJson(object ledger)=>(string)ledger.GetType().GetProperty("Json").GetValue(ledger);
    private static object NewNode(string name,Transform parent,Type type)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go.AddComponent(type);}

    [UnityTest] public IEnumerator EquipmentCommandsRefreshRealUIAndStatsOnlyAfterCommit_IsolatedFile()
    {
        var randomState=UnityEngine.Random.state;float previousScale=Time.timeScale;
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("isolated equipment integration");SceneManager.SetActiveScene(scene);
        var ledgerField=T("ShipLoadoutManager").GetField("ledger",BindingFlags.NonPublic|BindingFlags.Static);
        object oldLedger=ledgerField.GetValue(null),oldSession=T("CombatSession").GetProperty("Instance").GetValue(null);
        object game=T("GameState").GetProperty("Instance").GetValue(null),profile=T("PlayerProfile").GetProperty("Instance").GetValue(null);
        bool started=game!=null&&(bool)Field(game,"isCombatStarted");
        var disabled=new List<GameObject>();foreach(var root in previous.GetRootGameObjects())if(root.activeSelf&&root.name!="Code-based tests runner"){disabled.Add(root);root.SetActive(false);}
        var first=ScriptableObject.CreateInstance(T("ShipDefinition"));var second=ScriptableObject.CreateInstance(T("ShipDefinition"));
        string id="isolated-"+Guid.NewGuid().ToString("N"),key="TheGame.Loadout.v2."+id+".1";
        string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),id);System.IO.Directory.CreateDirectory(directory);
        string path=System.IO.Path.Combine(directory,"ship_loadouts.json");bool fail=false;
        try
        {
            T("PlayerProfile").GetProperty("Instance").SetValue(null,null);
            var session=new GameObject("session").AddComponent(T("CombatSession"));T("CombatSession").GetProperty("Instance").SetValue(null,session);if(game!=null)Set(game,"isCombatStarted",false);
            foreach(var def in new[]{first,second}){Set(def,"shipId",def==first?id:id+"-two");Set(def,"weaponSlots",2);Set(def,"shieldSlots",0);Set(def,"engineSlots",0);}
            var store=Activator.CreateInstance(T("EquipmentFileStore"),path,null);
            string json="{\"formatVersion\":4,\"starterInventoryIssued\":true,\"inventory\":[{\"id\":\"laser\",\"instanceId\":\"a\",\"name\":\"Laser A\",\"speedBonus\":3},{\"id\":\"laser\",\"instanceId\":\"b\",\"name\":\"Laser B\",\"speedBonus\":3}],\"loadouts\":[{\"shipId\":\""+id+"\",\"weaponSlots\":[null,null]}]}";
            Call(store,"Write",json);
            var ledger=Activator.CreateInstance(T("PlayerEquipment"),json,(Action<string>)(text=>{if(fail)throw new System.IO.IOException("injected write failure");Call(store,"Write",text);}));ledgerField.SetValue(null,ledger);
            var image=Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true);var textType=Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro",true);
            var canvas=new GameObject("canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var go=new GameObject("fitting",typeof(RectTransform));go.transform.SetParent(canvas.transform,false);go.SetActive(false);go.AddComponent(image);
            var window=go.AddComponent(T("FittingWindow"));Set(window,"shipDefinition",first);
            foreach(string name in new[]{"weaponSlotsContainer","shieldSlotsContainer","engineSlotsContainer","inventoryContainer","shipSelectorContainer"})
            {var node=new GameObject(name,typeof(RectTransform));node.transform.SetParent(go.transform,false);Set(window,name,node.transform);}
            Set(window,"titleLabel",NewNode("title",go.transform,textType));
            Set(window,"closeButton",NewNode("close",go.transform,typeof(UnityEngine.UI.Button)));
            var slotTemplate=new GameObject("slot template",typeof(RectTransform));slotTemplate.transform.SetParent(canvas.transform,false);
            var slotPrefab=slotTemplate.AddComponent(T("FittingSlot"));Set(slotPrefab,"label",NewNode("label",slotTemplate.transform,textType));Set(window,"slotPrefab",slotPrefab);
            var viewTemplate=new GameObject("view template",typeof(RectTransform));viewTemplate.transform.SetParent(canvas.transform,false);
            var viewPrefab=viewTemplate.AddComponent(T("FittingItemView"));Set(window,"itemViewPrefab",viewPrefab);
            var ship=new GameObject("ship");ship.SetActive(false);var health=ship.AddComponent(T("ShipHealth"));Set(health,"shipDefinition",first);ship.AddComponent(T("ShipMovement"));ship.AddComponent(T("ShipWeapon"));ship.SetActive(true);
            Set(window,"targetShipHealth",health);Call(window,"Open");
            var views=(IList)Field(window,"itemViews");var view=views[0];var item=Field(view,"item");
            Call(window,"HighlightDestinations",item,null);Assert.True((bool)Field(Slots(window)[0],"isHighlighted"));
            string before=LedgerJson(ledger);Call(window,"Close");Assert.AreEqual(before,LedgerJson(ledger));Call(window,"Open");
            float baseSpeed=(float)Field(first,"baseMaxSpeed");var movement=ship.GetComponent(T("ShipMovement"));
            Call(window,"HandleDropFromInventory",((IList)Field(window,"itemViews"))[0],Slots(window)[0]);
            Assert.AreEqual(baseSpeed+3,(float)Field(movement,"maxSpeed"));
            var installed=Slots(window)[0];var stats=Field(window,"statsLabel");string label=(string)stats.GetType().GetProperty("text").GetValue(stats);
            before=LedgerJson(ledger);Call(window,"HandleDropFromInventory",((IList)Field(window,"itemViews"))[0],installed);
            Assert.AreEqual(before,LedgerJson(ledger));Assert.AreSame(installed,Slots(window)[0]);Assert.AreEqual(label,stats.GetType().GetProperty("text").GetValue(stats));
            fail=true;Call(window,"HandleDropFromInventory",((IList)Field(window,"itemViews"))[0],Slots(window)[1]);
            Assert.AreEqual(before,LedgerJson(ledger));Assert.AreSame(installed,Slots(window)[0]);Assert.AreEqual(baseSpeed+3,(float)Field(movement,"maxSpeed"));fail=false;
            var owned=T("ShipLoadoutManager").GetMethod("GetLoadout").Invoke(null,new object[]{id});T("LoadoutPresets").GetMethod("Save").Invoke(null,new[]{owned,(object)1});
            Call(window,"HandleUnequip",Slots(window)[0]);Assert.AreEqual(baseSpeed,(float)Field(movement,"maxSpeed"));
            Invoke(window,"LoadPreset",1);Assert.AreEqual(baseSpeed+3,(float)Field(movement,"maxSpeed"));
            Assert.AreEqual("b",Field(Slots(window)[0].GetType().GetProperty("Item").GetValue(Slots(window)[0]),"instanceId"));
            Call(window,"HandleUnequip",Slots(window)[0]);Invoke(window,"OnShipSelected",second);
            Call(window,"HandleAutoEquip",((IList)Field(window,"itemViews"))[0]);
            Assert.AreEqual("a",Field(Slots(window)[0].GetType().GetProperty("Item").GetValue(Slots(window)[0]),"instanceId"));
            var reloaded=Activator.CreateInstance(T("PlayerEquipment"),System.IO.File.ReadAllText(path),(Action<string>)(_=>{}));Assert.AreEqual(LedgerJson(ledger),LedgerJson(reloaded));
            var firstSaved=T("ShipLoadoutManager").GetMethod("GetLoadout").Invoke(null,new object[]{id});Assert.IsNull(((IList)Field(firstSaved,"weaponSlots"))[0]);
            Assert.AreEqual(baseSpeed+3,(float)Field(movement,"maxSpeed"));
            yield return null;
        }
        finally
        {
            ledgerField.SetValue(null,oldLedger);T("CombatSession").GetProperty("Instance").SetValue(null,oldSession);T("PlayerProfile").GetProperty("Instance").SetValue(null,profile);if(game!=null)Set(game,"isCombatStarted",started);
            PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);
            UnityEngine.Random.state=randomState;Time.timeScale=previousScale;
            SceneManager.SetActiveScene(previous);foreach(var root in disabled)if(root!=null)root.SetActive(true);System.IO.Directory.Delete(directory,true);
        }
        yield return SceneManager.UnloadSceneAsync(scene);
    }
    [UnityTest] public IEnumerator DragRulesCancelTooltipAndReadableNames_UseRealHandlers()
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.CreateScene("fitting interaction test");
        SceneManager.SetActiveScene(scene);
        object oldLedger=T("ShipLoadoutManager").GetField("ledger",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        object oldSession=T("CombatSession").GetProperty("Instance").GetValue(null);
        var game=T("GameState").GetProperty("Instance").GetValue(null);
        bool started=game!=null&&(bool)Field(game,"isCombatStarted");
        var disabled=new List<GameObject>();
        foreach(var root in previous.GetRootGameObjects())if(root.activeSelf && root.name!="Code-based tests runner")
        {disabled.Add(root);root.SetActive(false);}
        var session=new GameObject("session").AddComponent(T("CombatSession"));
        T("CombatSession").GetProperty("Instance").SetValue(null,session);
        if(game!=null)Set(game,"isCombatStarted",false);
        var definition=ScriptableObject.CreateInstance(T("ShipDefinition"));Set(definition,"shipId","ui-test");Set(definition,"weaponSlots",2);
        var points=Array.CreateInstance(T("WeaponHardpoint"),2);
        for(int i=0;i<2;i++){var p=Activator.CreateInstance(T("WeaponHardpoint"));Set(p,"slotIndex",i);Set(p,"compatibility",Enum.ToObject(T("MountCompatibility"),i));points.SetValue(p,i);}
        Set(definition,"hardpoints",points);
        string json="{\"formatVersion\":4,\"starterInventoryIssued\":true,\"inventory\":[{\"id\":\"laser\",\"instanceId\":\"free\",\"name\":\"Лазер T1\"}],\"loadouts\":[{\"shipId\":\"ui-test\",\"weaponSlots\":[null,null]}]}";
        var ledger=Activator.CreateInstance(T("PlayerEquipment"),json,(Action<string>)(_=>{}));
        T("ShipLoadoutManager").GetField("ledger",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,ledger);
        var canvas=new GameObject("canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var windowGo=new GameObject("window",typeof(RectTransform));windowGo.transform.SetParent(canvas.transform,false);windowGo.SetActive(false);
        var window=windowGo.AddComponent(T("FittingWindow"));Set(window,"shipDefinition",definition);
        windowGo.SetActive(true);
        var layer=new GameObject("drag",typeof(RectTransform));layer.transform.SetParent(windowGo.transform,false);Set(window,"dragLayer",layer.transform);
        var eventGo=new GameObject("events",typeof(EventSystem));var ev=new PointerEventData(eventGo.GetComponent<EventSystem>()){position=new Vector2(100,100)};
        var slots=new Component[2];
        for(int i=0;i<2;i++)
        {
            var go=new GameObject("slot"+i,typeof(RectTransform));go.transform.SetParent(windowGo.transform,false);
            ((RectTransform)go.transform).sizeDelta=new Vector2(130,70);slots[i]=go.AddComponent(T("FittingSlot"));Set(slots[i],"window",window);Set(slots[i],"index",i);
            var label=new GameObject("label",typeof(RectTransform));label.transform.SetParent(go.transform,false);
            var text=label.AddComponent(Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro",true));Set(slots[i],"label",text);
            Call(slots[i],"SetItem",new object[]{null});
        }
        var list=Field(window,"weaponSlots") as IList;foreach(var slot in slots)list.Add(slot);
        var inventory=(IList)T("ShipLoadoutManager").GetProperty("SharedInventory").GetValue(null);var item=inventory[0];
        var viewGo=new GameObject("view",typeof(RectTransform));viewGo.transform.SetParent(windowGo.transform,false);
        var view=viewGo.AddComponent(T("FittingItemView"));Set(view,"window",window);
        var imageType=Type.GetType("UnityEngine.UI.Image, UnityEngine.UI",true);
        Set(view,"background",viewGo.GetComponent(imageType));
        T("FittingItemView").GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
        Call(view,"SetItem",item);
        try
        {
            Assert.True((bool)Call(window,"CanDrop",slots[0],item,null));Assert.False((bool)Call(window,"CanDrop",slots[1],item,null));
            Call(window,"HighlightDestinations",item,null);
            Assert.True((bool)Field(slots[0],"isHighlighted"));Assert.False((bool)Field(slots[1],"isHighlighted"));
            Call(view,"OnBeginDrag",ev);Assert.False(viewGo.GetComponent<CanvasGroup>().blocksRaycasts);
            Call(view,"OnEndDrag",ev);Assert.AreSame(windowGo.transform,viewGo.transform.parent);
            Assert.False((bool)Field(slots[0],"isHighlighted"));
            Call(slots[0],"SetItem",item);
            Assert.False((bool)Call(window,"CanDrop",slots[0],item,null));Assert.False((bool)Call(window,"CanDrop",slots[0],item,slots[0]));
            Assert.AreEqual("Лазер T1",Field(slots[0],"label").GetType().GetProperty("text").GetValue(Field(slots[0],"label")));
            foreach(var slot in slots)
            {
                slot.transform.position=new Vector3(Screen.width-5,Screen.height-5,0);Call(window,"DescribeSlot",slot);
                var tooltip=(RectTransform)windowGo.transform.Find("SlotTooltip");var corners=new Vector3[4];tooltip.GetWorldCorners(corners);
                foreach(var corner in corners){Assert.That(corner.x,Is.InRange(0f,(float)Screen.width));Assert.That(corner.y,Is.InRange(0f,(float)Screen.height));}
            }
            Call(window,"Close");Assert.False((bool)Field(slots[0],"isHighlighted"));
            Assert.AreEqual(1,((IList)T("ShipLoadoutManager").GetProperty("SharedInventory").GetValue(null)).Count);
        }
        finally
        {
            T("ShipLoadoutManager").GetField("ledger",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,oldLedger);
            T("CombatSession").GetProperty("Instance").SetValue(null,oldSession);
            if(game!=null)Set(game,"isCombatStarted",started);
            UnityEngine.Object.DestroyImmediate(definition);
            SceneManager.SetActiveScene(previous);foreach(var root in disabled)if(root!=null)root.SetActive(true);
        }
        yield return SceneManager.UnloadSceneAsync(scene);
    }
}