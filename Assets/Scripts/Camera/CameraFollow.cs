using UnityEngine;

/// <summary>
/// Камера следует за игроком с лёгким смещением в сторону движения.
/// Игрок остаётся почти в центре, но обзор открывается вперёд.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;          // PlayerShip

    [Header("Base Follow")]
    [SerializeField] private float smoothSpeed = 5f;    // плавность следования
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Header("Look Ahead")]
    [Tooltip("Максимальное смещение камеры в сторону движения (в юнитах).")]
    [SerializeField] private float maxLookAhead = 3f;

    [Tooltip("Скорость набора смещения (units/sec). Чем больше — тем резче камера едет вперёд.")]
    [SerializeField] private float lookAheadSpeed = 4f;

    [Tooltip("Скорость возврата к игроку, когда он стоит. Меньше — плавнее.")]
    [SerializeField] private float lookAheadReturnSpeed = 3f;

    [Tooltip("Скорость корабля, при которой достигается maxLookAhead.")]
    [SerializeField] private float referenceSpeed = 6f;

    private Rigidbody2D targetRb;
    private Vector2 currentLookAhead;
    [Header("Speed zoom (relative to equipped, non-boost maximum)")]
    [SerializeField, Range(0f,.9f)] private float zoomStartFraction=.2f;
    [SerializeField] private float cruiseZoomMultiplier=1.35f;
    [SerializeField] private float maximumZoomMultiplier=1.6f;
    [SerializeField] private float speedSmoothing=.15f;
    [SerializeField] private float zoomOutTime=.5f;
    [SerializeField] private float zoomInTime=1f;
    private Camera view;
    [Header("Manual zoom")]
    [SerializeField] private float minimumSize=3f,maximumSize=37.5f,scrollSensitivity=.12f;
    [SerializeField] private Rect panBounds=new Rect(-50,-35,100,70);
    private bool panned,returning;
    public void ReturnToPlayer(){panned=false;returning=true;}
    public void Pan(Vector2 worldDelta)
    {
        if(!TimeController.IsPaused||FittingWindow.OpenWindow!=null)return;
        panned=true;returning=false;transform.position+=new Vector3(worldDelta.x,worldDelta.y,0);ClampPan();
    }
    private void ClampPan()
    {
        if(view==null)return;
        if(BattleArena.Current!=null)
        {
            var area=BattleArena.Current.Area;float extent=area.Radius+Mathf.Max(view.orthographicSize,view.orthographicSize*view.aspect)*.5f;
            panBounds=new Rect(area.Center.X-extent,area.Center.Y-extent,extent*2,extent*2);
        }
        float halfY=view.orthographicSize,halfX=halfY*view.aspect;var p=transform.position;
        p.x=panBounds.width<halfX*2?panBounds.center.x:Mathf.Clamp(p.x,panBounds.xMin+halfX,panBounds.xMax-halfX);
        p.y=panBounds.height<halfY*2?panBounds.center.y:Mathf.Clamp(p.y,panBounds.yMin+halfY,panBounds.yMax-halfY);transform.position=p;
    }
    private float pausedMultiplier=1f;
    private bool wasPaused;
    private ShipMovement movement;
    private float baseSize, lastWrittenSize, smoothedSpeed, speedVelocity, zoomVelocity;
    public float BaseSize => baseSize;
    public static float SpeedZoomMultiplier(float speed,float maximum,float start=.2f,float cruise=1.35f,float cap=1.6f)
    {
        float fraction=Mathf.Max(0f,speed)/Mathf.Max(.01f,maximum);
        cruise=Mathf.Max(1f,cruise);cap=Mathf.Max(cruise,cap);
        return fraction<=1f?Mathf.Lerp(1f,cruise,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(Mathf.Clamp(start,0f,.99f),1f,fraction))):
            Mathf.Lerp(cruise,cap,Mathf.SmoothStep(0f,1f,Mathf.Clamp01(fraction-1f)));
    }

    private void Start()
    {
        if (target != null)
            targetRb = target.GetComponent<Rigidbody2D>();
        movement=target!=null?target.GetComponent<ShipMovement>():null;
        view=GetComponent<Camera>();
        if(view!=null)baseSize=lastWrittenSize=view.orthographicSize;
        var layout=FindFirstObjectByType<CombatInterfaceLayout>();
        if(layout!=null)
        {
            var button=CombatUIStyle.Button("CameraHome",layout.transform,"К игроку",ReturnToPlayer);
            CombatUIStyle.Rect(button.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(90,-42),new Vector2(190,-12));
            homeButton=button;
        }
    }
    private UnityEngine.UI.Button homeButton;
    private void OnDestroy(){if(homeButton!=null)Destroy(homeButton.gameObject);}

    private void LateUpdate()
    {
        if (target == null) return;
        UpdateZoom(Time.unscaledDeltaTime);
        if(homeButton!=null)homeButton.gameObject.SetActive(TimeController.IsPaused);
        if(!TimeController.IsPaused&&panned)ReturnToPlayer();
        if(TimeController.IsPaused&&panned){ClampPan();return;}

        // 1. Вычисляем желаемое смещение по скорости
        Vector2 desiredLookAhead = Vector2.zero;

        if (targetRb != null)
        {
            float speed = targetRb.linearVelocity.magnitude;

            if (speed > 0.1f)
            {
                // Направление движения
                Vector2 dir = targetRb.linearVelocity.normalized;

                // Доля от maxLookAhead: чем быстрее — тем больше (но не больше 1)
                float t = Mathf.Clamp01(speed / referenceSpeed);
                desiredLookAhead = dir * (maxLookAhead * t);
            }
        }

        // 2. Плавно двигаем текущее смещение к желаемому
        //    Если желаемое != 0 — используем lookAheadSpeed, иначе — lookAheadReturnSpeed
        float currentSpeed = (desiredLookAhead.sqrMagnitude > 0.0001f)
            ? lookAheadSpeed
            : lookAheadReturnSpeed;

        currentLookAhead = Vector2.MoveTowards(
            currentLookAhead,
            desiredLookAhead,
            currentSpeed * Time.deltaTime);

        // 3. Целевая позиция камеры
        Vector3 desiredPosition = (Vector2)target.position + currentLookAhead + (Vector2)offset;
        desiredPosition.z = transform.position.z;

        // 4. Сглаживание
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * (returning?Time.unscaledDeltaTime:Time.deltaTime));
        if(returning&&(transform.position-desiredPosition).sqrMagnitude<.001f)returning=false;
    }
    private void UpdateZoom(float deltaTime)
    {
        if(view==null||!view.orthographic||targetRb==null||deltaTime<=0f||FittingWindow.OpenWindow!=null)return;
        // Preserve an external/manual zoom as a user base, not as a competing writer.
        if(!Mathf.Approximately(view.orthographicSize,lastWrittenSize))
            baseSize=Mathf.Max(.01f,view.orthographicSize/(lastWrittenSize/Mathf.Max(.01f,baseSize)));
        bool paused=Time.timeScale<=0;
        if(paused&&!wasPaused)pausedMultiplier=lastWrittenSize/Mathf.Max(.01f,baseSize);
        wasPaused=paused;
        if(UnityEngine.InputSystem.Mouse.current!=null&&!PlayerInputHandler.IsPointerOverUI()&&!TimeController.TextInputFocused)
        {
            float scroll=UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
            if(scroll!=0)baseSize=Mathf.Clamp(baseSize*Mathf.Exp(-Mathf.Sign(scroll)*scrollSensitivity),Mathf.Max(.1f,minimumSize),Mathf.Max(minimumSize,maximumSize)/Mathf.Max(1,maximumZoomMultiplier));
        }
        smoothedSpeed=Mathf.SmoothDamp(smoothedSpeed,targetRb.linearVelocity.magnitude,ref speedVelocity,Mathf.Max(.01f,speedSmoothing),Mathf.Infinity,deltaTime);
        float desired=Mathf.Clamp(baseSize*(paused?pausedMultiplier:SpeedZoomMultiplier(smoothedSpeed,movement!=null?movement.maxSpeed:referenceSpeed,zoomStartFraction,cruiseZoomMultiplier,maximumZoomMultiplier)),minimumSize,maximumSize);
        float smoothing=desired>view.orthographicSize?zoomOutTime:zoomInTime;
        view.orthographicSize=Mathf.SmoothDamp(view.orthographicSize,desired,ref zoomVelocity,Mathf.Max(.01f,smoothing),Mathf.Infinity,deltaTime);
        lastWrittenSize=view.orthographicSize;
    }
}