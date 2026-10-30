using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Selection and movement orders are independent. UI raycasts are resolved before world input.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    private const string EnemyTag = "Enemy";

    [Tooltip("Movement component of the player ship.")]
    public ShipMovement playerMovement;

    [Tooltip("Weapon component of the player ship.")]
    public ShipWeapon playerWeapon;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    // LateUpdate — чтобы EventSystem уже обновил состояние.
    // Если обрабатывать клик в Update (или через InputAction callback) — IsPointerOverGameObject()
    // вернёт состояние прошлого кадра, и клик по UI «провалится» в игровое поле.
    private bool rightGesture, following;
    private float pressedAt;
    private ShipMovement gestureShip;
    private void OnApplicationFocus(bool focus) { if(!focus) EndGesture(); }
    private void OnDisable() => EndGesture();
    public void EndGesture()
    {
        if(gestureShip!=null)gestureShip.Orders.EndFollowCursor();
        rightGesture=false;following=false;gestureShip=null;
    }
    public void ProcessRightPointer(bool pressed, bool held, bool released, bool shift, bool blocked, Vector2 world, float now)
    {
        if(rightGesture && (gestureShip!=playerMovement || gestureShip==null ||
            (following && gestureShip.Orders.Mode!=ShipOrderMode.FollowCursor) ||
            (!following && gestureShip.Orders.Mode!=ShipOrderMode.MoveToPoint))) EndGesture();
        if(pressed)
        {
            EndGesture();
            if(!blocked && playerMovement!=null)
            {
                IssueClick(false,world,null,shift,false);
                if(!shift && !TimeController.IsPaused){rightGesture=true;gestureShip=playerMovement;pressedAt=now;}
            }
        }
        if(!rightGesture)return;
        if(held && !blocked)
        {
            if(!following && now-pressedAt>=.2f){gestureShip.Orders.BeginFollowCursor(world);following=true;}
            if(following)gestureShip.Orders.UpdateFollowCursor(world);
        }
        if(released || !held)EndGesture();
    }
    private void LateUpdate()
    {
        var mouse=Mouse.current;
        var health=playerMovement!=null?playerMovement.GetComponent<ShipHealth>():null;
        if(mouse==null || !Application.isFocused || (Time.timeScale<=0f && !TimeController.IsPaused) || CombatSession.IsPreparing ||
            FittingWindow.OpenWindow!=null || !ShipOrderController.Available(health)) {EndGesture();return;}
        if(Keyboard.current!=null && Keyboard.current.xKey.wasPressedThisFrame && !TimeController.TextInputFocused)
        {EndGesture();playerMovement.Stop();return;}
        if(mainCamera==null)mainCamera=Camera.main;
        if(mainCamera==null){EndGesture();return;}
        Vector2 screen=mouse.position.ReadValue();
        bool blocked=!mainCamera.pixelRect.Contains(screen) || IsScreenPointOverUI(screen);
        var ray=mainCamera.ScreenPointToRay(screen);
        float distance;
        bool valid=new Plane(Vector3.forward,Vector3.zero).Raycast(ray,out distance);
        Vector2 world=valid?(Vector2)ray.GetPoint(distance):Vector2.zero;
        blocked|=!valid;
        bool shift=Keyboard.current!=null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        ProcessRightPointer(mouse.rightButton.wasPressedThisFrame,mouse.rightButton.isPressed,
            mouse.rightButton.wasReleasedThisFrame,shift,blocked,world,Time.unscaledTime);
        if(mouse.leftButton.wasPressedThisFrame && !blocked)
            IssueClick(true,world,PickEnemy(mainCamera,screen,12f),false,false);
    }
    public static ShipHealth PickEnemy(Camera camera, Vector2 screen, float tolerance)
    {
        ShipHealth best=null;bool bestInside=false;float bestDistance=float.PositiveInfinity;
        foreach(var enemy in EnemyRegistry.GetAliveEnemies())
        {
            if(!ShipOrderController.Available(enemy) || !enemy.CompareTag(EnemyTag))continue;
            bool hit=false,inside=false;
            foreach(var sprite in enemy.GetComponentsInChildren<SpriteRenderer>())
            {
                if(!sprite.enabled || sprite.sprite==null || sprite.color.a<=0f ||
                    sprite.GetComponentInParent<ShipHealth>()!=enemy || sprite.GetComponentInParent<Canvas>()!=null ||
                    sprite.GetComponentInParent<ParticleSystem>()!=null)continue;
                Bounds bounds=sprite.bounds;Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=-min;
                bool front=true;
                for(int i=0;i<8;i++)
                {
                    Vector3 p=camera.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    if(p.z<=0f){front=false;break;}min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
                if(!front)continue;
                inside|=Rect.MinMaxRect(min.x,min.y,max.x,max.y).Contains(screen);
                hit|=Vector2.Distance(screen,new Vector2(Mathf.Clamp(screen.x,min.x,max.x),Mathf.Clamp(screen.y,min.y,max.y)))<=tolerance;
            }
            if(!hit)continue;
            float d=((Vector2)camera.WorldToScreenPoint(enemy.transform.position)-screen).sqrMagnitude;
            // Exact ties retain registry insertion order, independent of enumeration of child sprites.
            if(best==null || (inside&&!bestInside) || (inside==bestInside && d<bestDistance))
            {best=enemy;bestInside=inside;bestDistance=d;}
        }
        return best;
    }

    public static bool IsPointerOverUI()
    {
        if (FittingWindow.OpenWindow != null) return true;
        if (Mouse.current == null) return false;
        Vector2 screen = Mouse.current.position.ReadValue();
        return IsScreenPointOverUI(screen);
    }

    public static bool IsScreenPointOverUI(Vector2 screen)
    {
        if(FittingWindow.OpenWindow!=null)return true;
        foreach(var panel in Object.FindObjectsByType<TacticalCommandPanel>(FindObjectsSortMode.None))
            if(panel.PointerInsideTooltip(screen))return true;
        if (EventSystem.current == null) return false;
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, hits);
        return hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster);
    }

    public void IssueClick(bool left, Vector2 position, ShipHealth enemy, bool append, bool overUI)
    {
        if (overUI || FittingWindow.OpenWindow != null || playerMovement == null || playerWeapon == null) return;
        if (left) { if (enemy != null) playerWeapon.SetTarget(enemy); return; }
        playerMovement.Orders.MoveTo(position, append);
    }
}