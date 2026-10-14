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

    private void Start()
    {
        if (target != null)
            targetRb = target.GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

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
}