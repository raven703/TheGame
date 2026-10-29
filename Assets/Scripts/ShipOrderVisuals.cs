using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime-only world graphics. No colliders or event handlers.</summary>
public class ShipOrderVisuals : MonoBehaviour
{
    private ShipOrderController orders;
    private GameObject root;
    private LineRenderer route;
    private LineRenderer tactical;
    private Material material;
    private readonly List<GameObject> markers = new List<GameObject>();
    private void Awake() { orders = GetComponent<ShipOrderController>(); }
    private LineRenderer Line(string name, Color color)
    {
        var go = new GameObject(name);go.transform.SetParent(root.transform,false);
        var line = go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
        line.startWidth=line.endWidth=.045f;line.startColor=line.endColor=color;line.sortingOrder=30;
        return line;
    }
    private void LateUpdate()
    {
        if (!CompareTag("Player") || orders == null) return;
        if (root == null)
        {
            root = new GameObject("Runtime Ship Orders");root.hideFlags=HideFlags.DontSave;
            material = new Material(Shader.Find("Sprites/Default"));
            route=Line("Remaining Route",new Color(.3f,.9f,1f,.8f));
            tactical=Line("Tactical Target - Not Weapon Selection",new Color(1f,.65f,.1f));
        }
        bool routeVisible=orders.Mode==ShipOrderMode.MoveToPoint || orders.Mode==ShipOrderMode.FollowCursor || !string.IsNullOrEmpty(orders.Error);
        int count=routeVisible?orders.Waypoints.Count:0;
        route.positionCount=count>0?count+1:0;
        if(count>0)route.SetPosition(0,transform.position);
        while(markers.Count<count)
        {
            var line=Line("Waypoint",Color.cyan);line.loop=true;line.positionCount=24;
            var textGo=new GameObject("Number");textGo.transform.SetParent(line.transform,false);
            var text=textGo.AddComponent<TextMesh>();text.fontSize=40;text.characterSize=.12f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.cyan;
            markers.Add(line.gameObject);
        }
        for(int i=0;i<markers.Count;i++)
        {
            markers[i].SetActive(i<count);if(i>=count)continue;
            Vector2 p=orders.Waypoints[i];route.SetPosition(i+1,p);
            var line=markers[i].GetComponent<LineRenderer>();float radius=i==count-1?.3f:.18f;
            for(int k=0;k<24;k++){float a=k*Mathf.PI*2/24;line.SetPosition(k,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);}
            var label=markers[i].GetComponentInChildren<TextMesh>();label.text=(i+1).ToString();label.transform.position=p+Vector2.up*.55f;
        }
        bool targetVisible=ShipOrderController.Available(orders.OrderTarget);
        tactical.positionCount=targetVisible?5:0;
        if(targetVisible)
        {
            Vector2 p=orders.OrderTarget.transform.position;float size=.8f;
            tactical.SetPosition(0,p+Vector2.up*size);tactical.SetPosition(1,p+Vector2.right*size);
            tactical.SetPosition(2,p+Vector2.down*size);tactical.SetPosition(3,p+Vector2.left*size);tactical.SetPosition(4,p+Vector2.up*size);
        }
    }
    private void OnDisable(){if(root!=null)root.SetActive(false);}
    private void OnEnable(){if(root!=null)root.SetActive(true);}
    private void OnDestroy(){if(root!=null)Destroy(root);if(material!=null)Destroy(material);}
}