using UnityEngine;

namespace Parallax
{
    [CreateAssetMenu(fileName = "StarfieldData", menuName = "Parallax/Starfield Data")]
    public class StarfieldData : ScriptableObject
    {
        [System.Serializable]
        public class Star
        {
            [Range(0f, 1f)] public float x;
            [Range(0f, 1f)] public float y;
            [Min(0.5f)] public float size = 1f;
            [Range(0f, 1f)] public float brightness = 1f;
            public Color color = Color.white;

            public Star() { }
            public Star(float x, float y, float size, float brightness, Color color)
            {
                this.x = x; this.y = y; this.size = size;
                this.brightness = brightness; this.color = color;
            }
        }

        [Header("Параметры текстуры")]
        public int textureSize = 1024;

        [Tooltip("Звёзды, из которых строится текстура")]
        public Star[] stars = new Star[0];

        [Header("Параметры генератора (только для Editor)")]
        public int editorSeed = 12345;
        public int editorCount = 200;
        public float editorMinSize = 1f;
        public float editorMaxSize = 2f;
        public float editorMinBrightness = 0.5f;
        public float editorMaxBrightness = 1f;
        public float editorColorVariance = 0.15f;
    }
}