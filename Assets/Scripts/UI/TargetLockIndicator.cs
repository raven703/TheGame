using UnityEngine;

/// <summary>
/// World-space индикатор захвата.
/// - Если идёт захват (PendingTarget != null) — рамка вокруг него, МИГАЕТ.
/// - Если цель захвачена (Target != null) — рамка ПОСТОЯННАЯ.
/// </summary>
public class TargetLockIndicator : MonoBehaviour
{
    [Header("Dependencies")]
    public ShipWeapon playerWeapon;

    [Header("Visual Settings")]
    public Sprite targetIconSprite;
    public Color lockedColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    public Color lockingColor = new Color(1f, 0.8f, 0.2f, 0.85f); // жёлтая мигающая
    public Vector3 reticleScale = new Vector3(1.2f, 1.2f, 1f);
    public float rotationSpeed = 90f;
    public float blinkSpeed = 6f;

    private GameObject reticleInstance;
    private SpriteRenderer reticleRenderer;

    private void Start()
    {
        CreateReticleObject();
    }

    private void CreateReticleObject()
    {
        reticleInstance = new GameObject("[UI_TargetReticle]");
        reticleRenderer = reticleInstance.AddComponent<SpriteRenderer>();

        if (targetIconSprite != null)
            reticleRenderer.sprite = targetIconSprite;
        else
            reticleRenderer.sprite = CreateFallbackReticleSprite();

        reticleRenderer.sortingOrder = 10;
        reticleInstance.transform.localScale = reticleScale;
        reticleInstance.SetActive(false);
    }

    private void LateUpdate()
    {
        if (playerWeapon == null)
        {
            Hide();
            return;
        }

        // 1) Идёт захват — мигаем вокруг PendingTarget
        if (playerWeapon.PendingTarget != null
            && playerWeapon.PendingTarget.gameObject.activeInHierarchy)
        {
            ShowAt(playerWeapon.PendingTarget.transform.position, lockingColor, true);
            return;
        }

        // 2) Цель захвачена — постоянная рамка
        if (playerWeapon.Target != null
            && playerWeapon.Target.gameObject.activeInHierarchy)
        {
            ShowAt(playerWeapon.Target.transform.position, lockedColor, false);
            return;
        }

        // 3) Ничего
        Hide();
    }

    private void ShowAt(Vector3 position, Color baseColor, bool blink)
    {
        if (!reticleInstance.activeSelf)
            reticleInstance.SetActive(true);

        reticleInstance.transform.position = position;
        reticleInstance.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        Color c = baseColor;
        if (blink)
        {
            // Мигание: альфа от 0.2 до 1.0
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            c.a = Mathf.Lerp(0.2f, 1f, t);
        }

        reticleRenderer.color = c;
    }

    private void Hide()
    {
        if (reticleInstance != null && reticleInstance.activeSelf)
            reticleInstance.SetActive(false);
    }

    private Sprite CreateFallbackReticleSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color transparent = new Color(0, 0, 0, 0);

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool border = x == 0 || x == 31 || y == 0 || y == 31;
                bool corner = (x < 8 || x > 23) && (y < 8 || y > 23);
                tex.SetPixel(x, y, (border && corner) ? Color.white : transparent);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
    }

    private void OnDestroy()
    {
        if (reticleInstance != null)
            Destroy(reticleInstance);
    }
}