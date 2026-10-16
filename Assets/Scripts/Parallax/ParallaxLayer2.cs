using UnityEngine;

namespace Parallax
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Renderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)][SerializeField] private float parallaxFactor = 0.5f;
        [SerializeField] private Vector2 autoScrollSpeed = Vector2.zero;
        [SerializeField] private Vector2 quadSize = new Vector2(80f, 50f);

        private const string TEX_PROP = "_BaseMap";
        private Transform cam;
        private Vector3 lastCamPos;
        private Material mat;
        private Vector2 uvOffset;

        /// <summary>Вызывается генератором, чтобы настроить поля из кода.</summary>
        public void Configure(float factor, Vector2 scrollSpeed, Vector2 size)
        {
            parallaxFactor = Mathf.Clamp01(factor);
            autoScrollSpeed = scrollSpeed;
            quadSize = size;
        }

        void Awake()
        {
            if (Camera.main == null)
            {
                Debug.LogError($"[ParallaxLayer] Camera.main == null на '{name}'. Проверь тег MainCamera.", this);
                enabled = false;
                return;
            }

            cam = Camera.main.transform;
            lastCamPos = cam.position;

            mat = GetComponent<Renderer>().material;

            if (!mat.HasProperty(TEX_PROP))
            {
                Debug.LogError($"[ParallaxLayer] У материала '{mat.shader.name}' нет {TEX_PROP}", this);
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

            // Квад жёстко едет за камерой по XY — не разъезжается
            transform.position += new Vector3(delta.x, delta.y, 0f);

            // Параллакс через UV
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
}