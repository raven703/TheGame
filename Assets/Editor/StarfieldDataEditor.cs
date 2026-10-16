using UnityEngine;
using UnityEditor;
using Parallax;

[CustomEditor(typeof(StarfieldData))]
public class StarfieldDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var data = (StarfieldData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ручное управление", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Star"))
        {
            var list = new System.Collections.Generic.List<StarfieldData.Star>(data.stars);
            list.Add(new StarfieldData.Star(0.5f, 0.5f, 1f, 1f, Color.white));
            data.stars = list.ToArray();
            EditorUtility.SetDirty(data);
        }

        if (GUILayout.Button("Clear Stars"))
        {
            if (EditorUtility.DisplayDialog("Очистить звёзды?",
                $"Удалить все {data.stars.Length} звёзд?", "Да", "Отмена"))
            {
                data.stars = new StarfieldData.Star[0];
                EditorUtility.SetDirty(data);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Генерация", EditorStyles.boldLabel);

        // ----- Поля с числовым вводом -----
        data.editorCount = EditorGUILayout.IntField("Count", data.editorCount);

        data.editorMinSize = EditorGUILayout.FloatField("Min Size", data.editorMinSize);
        data.editorMaxSize = EditorGUILayout.FloatField("Max Size", data.editorMaxSize);

        data.editorMinBrightness = EditorGUILayout.FloatField("Min Brightness", data.editorMinBrightness);
        data.editorMaxBrightness = EditorGUILayout.FloatField("Max Brightness", data.editorMaxBrightness);

        data.editorColorVariance = EditorGUILayout.Slider("Color Variance", data.editorColorVariance, 0f, 1f);

        // ----- Seed с ручным вводом + кнопка рандома -----
        EditorGUILayout.BeginHorizontal();
        data.editorSeed = EditorGUILayout.IntField("Seed", data.editorSeed);
        if (GUILayout.Button("Randomize", GUILayout.Width(90)))
        {
            data.editorSeed = Random.Range(int.MinValue, int.MaxValue);
            GUI.changed = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Generate Random Stars", GUILayout.Height(24)))
        {
            GenerateStars(data);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Save Asset"))
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }

    private static void GenerateStars(StarfieldData data)
    {
        // Валидация
        int count = Mathf.Max(1, data.editorCount);
        float minSize = Mathf.Max(0.5f, data.editorMinSize);
        float maxSize = Mathf.Max(minSize, data.editorMaxSize);
        float minB = Mathf.Clamp01(data.editorMinBrightness);
        float maxB = Mathf.Clamp01(data.editorMaxBrightness);
        if (maxB < minB) maxB = minB;

        var rng = new System.Random(data.editorSeed);
        var stars = new StarfieldData.Star[count];

        for (int i = 0; i < count; i++)
        {
            float x = (float)rng.NextDouble();
            float y = (float)rng.NextDouble();
            float size = Mathf.Lerp(minSize, maxSize, (float)rng.NextDouble());
            float brightness = Mathf.Lerp(minB, maxB, (float)rng.NextDouble());

            float hueShift = ((float)rng.NextDouble() - 0.5f) * 2f * data.editorColorVariance;
            Color color = Color.Lerp(Color.white,
                hueShift > 0 ? new Color(0.75f, 0.85f, 1f) : new Color(1f, 0.92f, 0.75f),
                Mathf.Abs(hueShift));

            stars[i] = new StarfieldData.Star(x, y, size, brightness, color);
        }

        data.stars = stars;
        Debug.Log($"[StarfieldDataEditor] Сгенерировано {count} звёзд, seed = {data.editorSeed}");
    }
}