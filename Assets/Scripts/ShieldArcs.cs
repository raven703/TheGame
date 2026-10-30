using UnityEngine;

/// <summary>Reusable geometry only; no colliders, raycasts, or independent shield state.</summary>
public class ShieldArcs : MonoBehaviour
{
    private readonly LineRenderer[,] arcs=new LineRenderer[2,8];
    private ShipHealth health;
    private Material material;
    private float radius;
    private void Awake()
    {
        health=GetComponent<ShipHealth>();
        radius=.65f;
        foreach(var sprite in GetComponentsInChildren<SpriteRenderer>())
            if(sprite.GetComponentInParent<ShipHealth>()==health)radius=Mathf.Max(radius,sprite.bounds.extents.magnitude+.12f);
        material=new Material(Shader.Find("Sprites/Default"));
        for(int side=0;side<2;side++)for(int band=0;band<8;band++)
        {
            var go=new GameObject("ShieldArc_"+side+"_"+band);go.transform.SetParent(transform,false);
            go.hideFlags=HideFlags.DontSave;
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;
            line.positionCount=33;line.startWidth=line.endWidth=.018f;line.sortingOrder=4;
            arcs[side,band]=line;
        }
    }
    private void LateUpdate()
    {
        if(health==null || health.Data==null)return;
        var d=health.Data.Damage;var s=d.Shields;
        // HullVisual is installed after ShipHealth.Awake and may be replaced by fitting.
        radius=.65f;
        foreach(var sprite in GetComponentsInChildren<SpriteRenderer>())
            if(sprite.enabled && sprite.sprite!=null && sprite.GetComponentInParent<ShipHealth>()==health)
                radius=Mathf.Max(radius,Mathf.Max(sprite.bounds.extents.x,sprite.bounds.extents.y)+.15f);
        var movement=GetComponent<ShipMovement>();Vector2 forward=movement!=null?movement.GetForwardVector():(Vector2)transform.up;
        float angle=Mathf.Atan2(forward.y,forward.x);
        bool player=CompareTag("Player"),selected=false;
        if(!player)
        {
            var p=GameObject.FindGameObjectWithTag("Player");var weapon=p!=null?p.GetComponent<ShipWeapon>():null;
            selected=weapon!=null && (weapon.PendingTarget!=null?weapon.PendingTarget:weapon.Target)==health;
        }
        for(int side=0;side<2;side++)for(int band=0;band<8;band++)
        {
            float share=side==0?s.Fraction:1-s.Fraction;
            float count=share*8,coverage=Mathf.Clamp01(count-band);
            var line=arcs[side,band];
            line.enabled=isActiveAndEnabled&&!d.Destroyed&&!d.Module(Game.Core.CombatModule.Shield).Destroyed&&coverage>0f;
            if(!line.enabled)continue;
            float hp=side==0?s.Front:s.Rear,capacity=side==0?s.FrontCapacity:s.RearCapacity;
            float filled=Mathf.Clamp01((capacity>0f?hp/capacity:0f)*count-band);
            float flash=(side==0?s.FrontFlash:s.RearFlash)/.22f;
            float shimmer=.94f+.06f*Mathf.Sin(Time.time*2f+band*.6f);
            float alpha=coverage*(.05f+.28f*filled)*shimmer*(player||selected?1f:.28f);
            alpha=Mathf.Max(alpha,flash*.65f*coverage);
            line.startColor=line.endColor=new Color(.4f,.9f,1f,alpha);
            float r=radius+band*.055f;
            for(int point=0;point<33;point++)
            {
                float a=angle+(side==0?0:Mathf.PI)-Mathf.PI*.48f+point/32f*Mathf.PI*.96f;
                line.SetPosition(point,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*r);
            }
        }
    }
    private void OnDisable(){foreach(var arc in arcs)if(arc!=null)arc.enabled=false;}
    private void OnDestroy(){if(material!=null)Destroy(material);}
}