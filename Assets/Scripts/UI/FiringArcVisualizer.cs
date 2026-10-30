using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>One scene-local toggle. Replaces the legacy arc; no runtime geometry is serialized.</summary>
[RequireComponent(typeof(ShipWeapon))]
public class FiringArcVisualizer : MonoBehaviour
{
    public bool Visible { get; private set; }
    public int RingCount { get; private set; }
    public int SectorCount { get; private set; }
    private ShipWeapon weapon;
    private Material material;
    private GameObject root;
    private Button button;
    private readonly List<GameObject> owned=new List<GameObject>();
    private readonly List<Mesh> meshes=new List<Mesh>();
    private readonly List<Transform> sectors=new List<Transform>();
    private readonly List<int> sectorSlots=new List<int>();
    private readonly List<LineRenderer> rays=new List<LineRenderer>();
    private readonly List<int> raySlots=new List<int>();
    private readonly List<Transform> labels=new List<Transform>();
    private readonly List<float> labelRanges=new List<float>();
    private int fingerprint=int.MinValue;
    private readonly Color cyan=new Color(.48f,.9f,1f,.52f);
    private void Awake()
    {
        weapon=GetComponent<ShipWeapon>();
        var legacy=GetComponent<LineRenderer>();if(legacy!=null)legacy.enabled=false;
        if(!CompareTag("Player")){enabled=false;return;}
        material=new Material(Shader.Find("Sprites/Default")){name="Tactical range transparent (runtime)",hideFlags=HideFlags.None};
        root=new GameObject("Tactical weapon geometry (runtime)"){hideFlags=HideFlags.None};
        root.SetActive(false);
        var layout=FindFirstObjectByType<CombatInterfaceLayout>();
        if(layout!=null)
        {
            button=CombatUIStyle.Button("WeaponRangeToggle",layout.transform,"",Toggle);
            CombatUIStyle.Rect(button.transform,Vector2.one,Vector2.one,new Vector2(-79,-604),new Vector2(-21,-558));
            var icon=CombatUIStyle.Panel("SectorIcon",button.transform,Color.white);icon.raycastTarget=false;
            icon.sprite=Resources.Load<Sprite>("TacticalIcons/distance");icon.preserveAspect=true;
            CombatUIStyle.Box(icon.transform,18,7,22,22);
            var label=button.GetComponentInChildren<TMPro.TextMeshProUGUI>();label.text="V";label.fontSize=10;
            CombatUIStyle.Box(label.transform,0,29,58,15);
            var hint=button.gameObject.AddComponent<TacticalTooltip>();hint.panel=FindFirstObjectByType<TacticalCommandPanel>();
            hint.description="Секторы [V]. Дальность каждого ствола от его крепления; кольца — ориентиры. Укрытия блокируют огонь.";
            CombatUIStyle.ToggleVisual(button,false);
        }
    }
    public void Toggle()=>SetVisible(!Visible);
    public void SetVisible(bool value)
    {
        Visible=value;
        if(root!=null)root.SetActive(value);
        CombatUIStyle.ToggleVisual(button,value);
    }
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if(Keyboard.current!=null && Keyboard.current.vKey.wasPressedThisFrame)Toggle();
#else
        if(Input.GetKeyDown(KeyCode.V))Toggle();
#endif
    }
    private int Signature()
    {
        unchecked
        {
            int hash=weapon.range.GetHashCode();
            for(int i=0;i<weapon.EquippedSlotCount;i++)
            {
                var item=weapon.EquippedWeapon(i);hash=hash*31+(item==null?0:item.id.GetHashCode());
                if(item==null)continue;
                hash=hash*31+weapon.MountRange(i).GetHashCode();
                hash=hash*31+weapon.EffectiveSector(i,item).GetHashCode();
                hash=hash*31+(weapon.MountId(i)??i.ToString()).GetHashCode();
                hash=hash*31+item.mountType.GetHashCode();
                Vector3 local=transform.InverseTransformPoint(weapon.MountOrigin(i));
                hash=hash*31+Mathf.RoundToInt(local.x*10000);hash=hash*31+Mathf.RoundToInt(local.y*10000);
                Vector2 axis=transform.InverseTransformDirection(weapon.MountAxis(i));
                hash=hash*31+Mathf.RoundToInt(axis.x*10000);hash=hash*31+Mathf.RoundToInt(axis.y*10000);
            }
            return hash;
        }
    }
    private void LateUpdate()
    {
        var legacy=GetComponent<LineRenderer>();if(legacy!=null)legacy.enabled=false;
        if(root==null)return;
        var health=weapon.OwnerHealth;
        bool alive=health==null || health.Data==null || health.Data.currentHullHP>0;
        root.SetActive(Visible&&alive);
        if(!Visible||!alive)return;
        int signature=Signature();if(signature!=fingerprint){Rebuild();fingerprint=signature;}
        for(int i=0;i<sectors.Count;i++)
        {
            int slot=sectorSlots[i];var axis=weapon.MountAxis(slot);
            sectors[i].position=weapon.MountOrigin(slot);
            sectors[i].rotation=Quaternion.Euler(0,0,Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg);
        }
        for(int i=0;i<rays.Count;i++)
        {
            int slot=raySlots[i];var origin=weapon.MountOrigin(slot);
            var item=weapon.EquippedWeapon(slot);
            var axis=item.mountType==WeaponMountType.Turret?weapon.BarrelDirection(slot):weapon.MountAxis(slot);
            rays[i].SetPosition(0,origin);rays[i].SetPosition(1,origin+(Vector3)axis*.8f);
        }
        foreach(var go in owned)if(go!=null && go.name=="Range ring")go.transform.position=transform.position;
        for(int i=0;i<labels.Count;i++){labels[i].position=transform.position+Vector3.right*labelRanges[i];labels[i].rotation=Quaternion.identity;}
    }
    private GameObject Node(string name,Transform parent)
    {
        var go=new GameObject(name){hideFlags=HideFlags.None};go.transform.SetParent(parent,false);owned.Add(go);return go;
    }
    private LineRenderer Line(GameObject go,Color color)
    {
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;
        line.startWidth=line.endWidth=.035f;line.startColor=line.endColor=color;line.sortingOrder=2;return line;
    }
    private void Clear()
    {
        foreach(var go in owned)if(go!=null){go.SetActive(false);Destroy(go);}owned.Clear();
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);meshes.Clear();
        sectors.Clear();sectorSlots.Clear();rays.Clear();raySlots.Clear();labels.Clear();labelRanges.Clear();RingCount=SectorCount=0;
    }
    public void Rebuild()
    {
        Clear();var rings=new HashSet<float>();var shapes=new HashSet<string>();
        for(int i=0;i<weapon.EquippedSlotCount;i++)
        {
            var item=weapon.EquippedWeapon(i);if(item==null)continue;
            float range=weapon.MountRange(i),sector=weapon.EffectiveSector(i,item);
            if(rings.Add(range))
            {
                var ring=Node("Range ring",root.transform);var line=Line(ring,cyan);line.loop=true;line.positionCount=128;
                for(int k=0;k<128;k++){float a=k*Mathf.PI*2/128;line.SetPosition(k,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*range);}RingCount++;
                var label=Node("Range label",root.transform).AddComponent<TMPro.TextMeshPro>();label.text=range.ToString("0.#")+" юн.";
                label.fontSize=4f;label.color=cyan;label.alignment=TMPro.TextAlignmentOptions.Left;label.rectTransform.sizeDelta=new Vector2(2,.5f);
                labels.Add(label.transform);labelRanges.Add(range);
            }
            Vector3 local=transform.InverseTransformPoint(weapon.MountOrigin(i));
            Vector2 axis=transform.InverseTransformDirection(weapon.MountAxis(i));
            string key=local.ToString("F4")+axis.ToString("F4")+sector.ToString("F4")+range.ToString("F4");
            if(shapes.Add(key))
            {
                var shape=Node("Available sector",root.transform);sectors.Add(shape.transform);sectorSlots.Add(i);SectorCount++;
                int steps=Mathf.Max(2,Mathf.CeilToInt(sector/3));
                var vertices=new Vector3[steps+2];var triangles=new int[steps*3];
                for(int k=0;k<=steps;k++)
                {
                    float a=(-sector*.5f+sector*k/steps)*Mathf.Deg2Rad;vertices[k+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*range;
                    if(k<steps){triangles[k*3]=0;triangles[k*3+1]=k+1;triangles[k*3+2]=k+2;}
                }
                var mesh=new Mesh{name="Tactical sector fan",hideFlags=HideFlags.None};mesh.vertices=vertices;mesh.triangles=triangles;
                var colors=new Color[vertices.Length];for(int k=0;k<colors.Length;k++)colors[k]=new Color(.35f,.65f,.9f,.008f);mesh.colors=colors;mesh.RecalculateBounds();meshes.Add(mesh);
                shape.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=shape.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=2;
                var outline=Line(shape,cyan);bool full=sector>=359.999f;outline.loop=!full;outline.positionCount=full?steps+1:steps+2;
                if(!full)outline.SetPosition(0,Vector3.zero);
                for(int k=0;k<=steps;k++)outline.SetPosition(k+(full?0:1),vertices[k+1]);
            }
            var ray=Line(Node("Actual weapon axis",root.transform),item.mountType==WeaponMountType.Turret?new Color(.6f,.93f,1,.8f):new Color(.4f,.45f,.7f,.6f));
            ray.useWorldSpace=true;ray.positionCount=2;rays.Add(ray);raySlots.Add(i);
        }
    }
    private void OnDisable(){if(root!=null)root.SetActive(false);}
    private void OnDestroy(){Clear();if(root!=null)Destroy(root);if(material!=null)Destroy(material);if(button!=null)Destroy(button.gameObject);}
}

public class WeaponRangeHint : MonoBehaviour,UnityEngine.EventSystems.IPointerEnterHandler,UnityEngine.EventSystems.IPointerExitHandler
{
    public FiringArcVisualizer owner;
    private GameObject popup;
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
    {
        if(popup==null)
        {
            var panel=CombatUIStyle.Panel("RangeHint",transform,new Color(.05f,.07f,.12f,.97f));panel.raycastTarget=false;popup=panel.gameObject;
            CombatUIStyle.Rect(panel.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(-170,8),new Vector2(170,68));
            var text=CombatUIStyle.Text("Text",panel.transform,"Потенциальная зона огня.\nАстероиды блокируют выстрелы.\nКольцо — ориентир от центра корабля.",13);
            CombatUIStyle.Rect(text.transform,Vector2.zero,Vector2.one,new Vector2(8,5),new Vector2(-8,-5));
        }
        popup.SetActive(true);
    }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e){if(popup!=null)popup.SetActive(false);}
    private void OnDisable(){if(popup!=null)popup.SetActive(false);}
}
