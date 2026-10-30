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
    [SerializeField] private float minimumSize=3f,maximumSize=30f,scrollSensitivity=.12f;
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
    }

    private void LateUpdate()
    {
        if (target == null) return;
        UpdateZoom(Time.unscaledDeltaTime);

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
            smoothSpeed * Time.deltaTime);
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