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