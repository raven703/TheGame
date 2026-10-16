TL;DR: параллакс-фон в Unity 2D (URP) — пошагово
Подготовка текстур
Создай 4 PNG:

starfield_far.png — 2048×2048, непрозрачный, галактика/туманность.

starfield_mid.png — 1024×1024, прозрачный фон, мелкие звёзды (1–3 px).

starfield_near.png — 1024×1024, прозрачный, средние звёзды (3–5 px).

starfield_close.png — 1024×1024, прозрачный, крупные редкие звёзды (6–12 px).

Фон у Mid/Near/Close — реально прозрачный (шахматка в редакторе, не чёрный). Сохранять как PNG-24 с alpha-каналом.

Импорт PNG в Unity
Выдели каждый PNG → Inspector:

Texture Type: Default

Alpha Source:

Far → None

Mid/Near/Close → Input Texture Alpha

Alpha Is Transparency:

Far → ❌

Mid/Near/Close → ✅

Wrap Mode: Repeat

Filter Mode: Bilinear

Compression: None

Max Size:

Far → 2048

Mid/Near/Close → 1024

Generate Mip Maps: ❌

Создание материалов
4 материала: Starfield_Far.mat, Starfield_Mid.mat, Starfield_Near.mat, Starfield_Close.mat.

Shader: Universal Render Pipeline/Unlit

Для Far:

Surface Type: Opaque

Base Map: starfield_far.png

Tiling: (1, 1), Offset: (0, 0)

Для Mid / Near / Close:

Surface Type: Transparent

Blending Mode: Alpha

Base Map: соответствующий PNG

Tiling: (1, 1), Offset: (0, 0)

Создание объектов в сцене
Hierarchy → Create Empty → назови [Background].

Внутрь [Background] создай 4 объекта через 3D Object → Quad (не через 2D Object → Sprite!).

Для каждого Quad:

Transform:

Объект	Position	Rotation	Scale
Starfield_Far	(0, 0, 30)	(0, 0, 0)	(80, 50, 1)
Starfield_Mid	(0, 0, 20)	(0, 0, 0)	(80, 50, 1)
Starfield_Near	(0, 0, 10)	(0, 0, 0)	(80, 50, 1)
Starfield_Close	(0, 0, 5)	(0, 0, 0)	(80, 50, 1)
Mesh Filter: Mesh = Quad (уже стоит по умолчанию).
Mesh Renderer → Materials → Element 0: соответствующий материал.
Mesh Collider: удалить (правый клик → Remove Component).

Добавление скрипта ParallaxLayer
На каждый Quad → Add Component → Parallax Layer:

Объект	Parallax Factor	Auto Scroll Speed	Quad Size
Starfield_Far	0.97	(0, 0)	(80, 50)
Starfield_Mid	0.80	(0, 0)	(80, 50)
Starfield_Near	0.55	(0, 0)	(80, 50)
Starfield_Close	0.25	(0, 0)	(80, 50)
Quad Size обязан совпадать с Transform.Scale.

Скрипт ParallaxLayer.cs
csharp
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Renderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] [SerializeField] private float parallaxFactor = 0.5f;
    [SerializeField] private Vector2 autoScrollSpeed = Vector2.zero;
    [SerializeField] private Vector2 quadSize = new Vector2(80f, 50f);

    private const string TEX_PROP = "_BaseMap";
    private Transform cam;
    private Vector3 lastCamPos;
    private Material mat;
    private Vector2 uvOffset;

    void Awake()
    {
        if (Camera.main == null) { Debug.LogError("Camera.main == null"); enabled = false; return; }
        cam = Camera.main.transform;
        lastCamPos = cam.position;
        mat = GetComponent<Renderer>().material;
        if (!mat.HasProperty(TEX_PROP)) { Debug.LogError("Нет _BaseMap"); enabled = false; }
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

        // Квад едет за камерой 1:1 — не разъезжается
        transform.position += new Vector3(delta.x, delta.y, 0f);

        // Параллакс — только через UV (без минуса, чтобы направление совпадало с движением камеры)
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
Настройка камеры
Main Camera → Tag = MainCamera ✅

Projection: Orthographic, Size: 10

Тег обязателен — иначе Camera.main вернёт null и скрипт выключится.

Проверка
Scene view: 4 плоских квада, наложены по Z, галактика на дальнем, звёзды поверх.

Play: корабль летит → все слои двигаются вместе с камерой, но UV скроллятся с разной скоростью → визуально звёзды расслаиваются.

Console пуст (нет Camera.main == null и нет _BaseMap).

Типичные ошибки
Симптом	Причина
Фон чёрный, звёзд нет	PNG без альфы или Alpha Is Transparency выключен, или Surface Type = Opaque у звёздного материала
Один слой перекрывает остальные	У него непрозрачная текстура — убери фон в PNG
Ошибка Camera.main == null	У камеры нет тега MainCamera
Ошибка нет _BaseMap	Использован не URP/Unlit шейдер (например, Standard или Sprites/Default)
Шов между слоями	Один из квадов меньше экрана — увеличь Scale или уменьши parallaxFactor
Параллакса не видно	Все parallaxFactor одинаковые или текстуры идентичные
Ориентир по значениям
parallaxFactor: чем ближе слой, тем меньше значение (0.97 далеко → 0.25 близко).

Разница между соседними слоями — минимум 0.15, иначе эффект слабо заметен.

Scale квада — минимум в 2× больше экрана (экран при Ortho Size 10 ≈ 35×20 юнитов).