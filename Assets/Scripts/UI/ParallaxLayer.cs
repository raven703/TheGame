using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Renderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Коэффициент параллакса: 0 = далеко (быстрый UV-скролл), 1 = близко (нет скролла)")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Header("Автоматический дрейф (опционально)")]
    [SerializeField] private Vector2 autoScrollSpeed = Vector2.zero;

    [Header("Размер квада в юнитах (должен совпадать с Transform.Scale)")]
    [SerializeField] private Vector2 quadSize = new Vector2(80f, 50f);

    private const string TEX_PROP = "_BaseMap";

    private Transform cam;
    private Vector3 lastCamPos;
    private Material mat;
    private Vector2 uvOffset;

    void Awake()
    {
        if (Camera.main == null)
        {
            Debug.LogError($"[ParallaxLayer] Camera.main == null на '{name}'", this);
            enabled = false;
            return;
        }

        cam = Camera.main.transform;
        lastCamPos = cam.position;

        var rend = GetComponent<Renderer>();
        mat = rend.material;

        if (!mat.HasProperty(TEX_PROP))
        {
            Debug.LogError($"[ParallaxLayer] У материала '{mat.shader.name}' нет свойства {TEX_PROP}. " +
                           $"Убедись, что используется Unlit (Universal Render Pipeline).", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam != null) lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 delta = cam.position - lastCamPos;
        lastCamPos = cam.position;

        // Квад всегда едет за камерой на 100% — не отстаёт, не разъезжается
        transform.position += new Vector3(delta.x, delta.y, 0f);

        // UV-скролл: без минуса, как было у тебя изначально
        Vector2 uvDelta = new Vector2(
             delta.x * (1f - parallaxFactor) / quadSize.x,
             delta.y * (1f - parallaxFactor) / quadSize.y);

        uvDelta += autoScrollSpeed * Time.deltaTime;

        uvOffset += uvDelta;
        uvOffset.x = Mathf.Repeat(uvOffset.x, 1f);
        uvOffset.y = Mathf.Repeat(uvOffset.y, 1f);

        mat.SetTextureOffset(TEX_PROP, uvOffset);
    }



}