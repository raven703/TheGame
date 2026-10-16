using UnityEngine;
using UnityEngine.Rendering;

namespace Parallax
{
    /// <summary>
    /// Создаёт все слои параллакса в Awake:
    /// либо из готового PNG (texture), либо из StarfieldData (генерация в рантайме).
    /// </summary>
    public class ParallaxBackgroundGenerator : MonoBehaviour
    {
        [System.Serializable]
        public class LayerConfig
        {
            public string layerName = "Layer";

            [Header("Источник текстуры (заполнить одно из двух)")]
            [Tooltip("Готовый PNG. Если задан — используется он.")]
            public Texture2D texture;

            [Tooltip("Данные для процедурной генерации. Используется, если texture == null.")]
            public StarfieldData starData;

            [Header("Параметры слоя")]
            [Range(0f, 1f)] public float parallaxFactor = 0.5f;
            public float zPosition = 10f;
            public Vector2 quadSize = new Vector2(80f, 50f);
            public Vector2 autoScrollSpeed = Vector2.zero;
            public Color tint = Color.white;

            [Tooltip("Opaque для фона (галактика), Transparent для звёзд.")]
            public bool transparent = true;
        }

        [Header("Слои (порядок: от дальнего к ближнему)")]
        [SerializeField] private LayerConfig[] layers;

        [Header("Шейдер")]
        [Tooltip("Оставь пустым — найдётся автоматически.")]
        [SerializeField] private Shader unlitShader;

        void Awake()
        {
            if (layers == null || layers.Length == 0)
            {
                Debug.LogWarning("[ParallaxBG] Нет слоёв.");
                return;
            }

            if (unlitShader == null)
                unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

            if (unlitShader == null)
            {
                Debug.LogError("[ParallaxBG] Не найден шейдер URP/Unlit.");
                return;
            }

            Debug.Log($"[ParallaxBG] Старт генерации. Слоёв: {layers.Length}, шейдер: {unlitShader.name}");

            // Очищаем старых детей
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            foreach (var cfg in layers)
                CreateLayer(cfg);
        }

        void CreateLayer(LayerConfig cfg)
        {
            Texture2D tex = cfg.texture;
            bool ownsTexture = false;

            if (tex == null && cfg.starData != null)
            {
                tex = StarfieldTextureBuilder.Build(cfg.starData);
                ownsTexture = true;
            }

            if (tex == null)
            {
                Debug.LogWarning($"[ParallaxBG] Слой '{cfg.layerName}': нет текстуры. " +
                                 $"texture={(cfg.texture == null ? "null" : cfg.texture.name)}, " +
                                 $"starData={(cfg.starData == null ? "null" : cfg.starData.name)}");
                return;
            }

            Debug.Log($"[ParallaxBG] Слой '{cfg.layerName}': tex='{tex.name}' ({tex.width}x{tex.height}), " +
                      $"transparent={cfg.transparent}, quad={cfg.quadSize}, parallax={cfg.parallaxFactor}, z={cfg.zPosition}");

            // Quad
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Layer_" + cfg.layerName;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, cfg.zPosition);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(cfg.quadSize.x, cfg.quadSize.y, 1f);

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Материал
            var mat = new Material(unlitShader)
            {
                name = $"Runtime_{cfg.layerName}",
                hideFlags = ownsTexture ? HideFlags.DontSave : HideFlags.None
            };
            mat.SetTexture("_BaseMap", tex);
            // Гарантируем, что tint всегда непрозрачный (alpha=1),
            // иначе Transparent-материал станет полностью прозрачным
            Color tintColor = cfg.tint;
            tintColor.a = 1f;
            mat.SetColor("_BaseColor", tintColor);
            
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetTextureOffset("_BaseMap", Vector2.zero);

            SetupSurface(mat, cfg.transparent);

            var rend = go.GetComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.lightProbeUsage = LightProbeUsage.Off;
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Проверяем, что текстура реально в материале
            var assigned = mat.GetTexture("_BaseMap");
            Debug.Log($"[ParallaxBG] Слой '{cfg.layerName}': материал создан, " +
                      $"BaseMap={(assigned != null ? assigned.name : "NULL")}, " +
                      $"SurfaceType={(mat.GetFloat("_Surface") == 0f ? "Opaque" : "Transparent")}");

            // ParallaxLayer
            var pl = go.AddComponent<ParallaxLayer>();
            pl.Configure(cfg.parallaxFactor, cfg.autoScrollSpeed, cfg.quadSize);
        }

        private static void SetupSurface(Material mat, bool transparent)
        {
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.One);
                mat.SetFloat("_DstBlend", (float)BlendMode.Zero);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
                mat.SetFloat("_ZWrite", 1f);
                mat.EnableKeyword("_SURFACE_TYPE_OPAQUE");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
        }
    }
}