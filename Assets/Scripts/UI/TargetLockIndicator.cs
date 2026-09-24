using UnityEngine;

/// <summary>
/// World-space target locking marker. Tracks PlayerWeapon target and renders a reticle around it.
/// </summary>
public class TargetLockIndicator : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the player's weapon script.")]
    public ShipWeapon playerWeapon;

    [Header("Visual Settings")]
    public Sprite targetIconSprite;
    public Color reticleColor = new Color(1f, 0.2f, 0.2f, 0.85f);
    public Vector3 reticleScale = new Vector3(1.2f, 1.2f, 1f);
    public float rotationSpeed = 90f;

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
        {
            reticleRenderer.sprite = targetIconSprite;
        }
        else
        {
            // Автоматически генерируем простой квадратный спрайт-прицел, если никакой не назначен
            reticleRenderer.sprite = CreateFallbackReticleSprite();
        }

        reticleRenderer.color = reticleColor;
        reticleRenderer.sortingOrder = 10;
        reticleInstance.transform.localScale = reticleScale;
        reticleInstance.SetActive(false);
    }

    private void LateUpdate()
    {
        if (playerWeapon == null || playerWeapon.Target == null || !playerWeapon.Target.gameObject.activeInHierarchy)
        {
            if (reticleInstance != null && reticleInstance.activeSelf)
                reticleInstance.SetActive(false);
            return;
        }

        if (!reticleInstance.activeSelf)
            reticleInstance.SetActive(true);

        // Следование за захваченной целью и вращение
        reticleInstance.transform.position = playerWeapon.Target.transform.position;
        reticleInstance.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
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