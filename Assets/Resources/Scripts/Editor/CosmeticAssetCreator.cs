using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using MakeupMechanic.Data;

namespace MakeupMechanic.Editor
{
    public static class CosmeticAssetCreator
    {
        private const string OutputPath = "Assets/Resources/Cosmetics";
        private const string LevelConfigPath = "Assets/Resources/LevelConfig.asset";
        private const string BasePath = "Assets/Resources/UI/MakeUp";

        private struct CosmeticMapping
        {
            public CosmeticType type;
            public string itemFolder;   // спрайт в палетке
            public string resultFolder; // спрайт результата на персонаже
            public string prefix;       // префикс для имени SO
        }

        [MenuItem("Tools/Create Cosmetic Assets")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputPath))
            {
                var parts = OutputPath.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            var mappings = new CosmeticMapping[]
            {
                new()
                {
                    type = CosmeticType.Eyeshadow,
                    itemFolder = BasePath + "/Цвета мейка",
                    resultFolder = BasePath + "/Макияж/Тени",
                    prefix = "eyeshadow"
                },
                new()
                {
                    type = CosmeticType.Blush,
                    itemFolder = BasePath + "/Цвета румян",
                    resultFolder = BasePath + "/Макияж/Румяна",
                    prefix = "blush"
                },
                new()
                {
                    type = CosmeticType.Lipstick,
                    itemFolder = BasePath + "/Помады",
                    resultFolder = BasePath + "/Макияж/Помады",
                    prefix = "lipstick"
                }
            };

            var allItems = new List<CosmeticItemSO>();

            foreach (var mapping in mappings)
            {
                var itemSprites = LoadSortedSprites(mapping.itemFolder);
                var resultSprites = LoadSortedSprites(mapping.resultFolder);

                var count = Mathf.Min(itemSprites.Length, resultSprites.Length);
                if (itemSprites.Length != resultSprites.Length)
                {
                    Debug.LogWarning(
                        $"[{mapping.prefix}] item count ({itemSprites.Length}) != result count ({resultSprites.Length}), using min ({count})");
                }

                for (int i = 0; i < count; i++)
                {
                    var soName = $"{mapping.prefix}_{(i + 1):D2}";
                    var soPath = $"{OutputPath}/{soName}.asset";

                    var existing = AssetDatabase.LoadAssetAtPath<CosmeticItemSO>(soPath);
                    if (existing != null)
                    {
                        existing.type = mapping.type;
                        existing.itemSprite = itemSprites[i];
                        existing.resultSprite = resultSprites[i];
                        EditorUtility.SetDirty(existing);
                        allItems.Add(existing);
                        Debug.Log($"Updated: {soPath}");
                    }
                    else
                    {
                        var so = ScriptableObject.CreateInstance<CosmeticItemSO>();
                        so.type = mapping.type;
                        so.itemSprite = itemSprites[i];
                        so.resultSprite = resultSprites[i];

                        AssetDatabase.CreateAsset(so, soPath);
                        allItems.Add(so);
                        Debug.Log($"Created: {soPath}");
                    }
                }
            }

            // Create LevelConfig
            var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(LevelConfigPath);
            if (levelConfig == null)
            {
                levelConfig = ScriptableObject.CreateInstance<LevelConfigSO>();
                AssetDatabase.CreateAsset(levelConfig, LevelConfigPath);
            }

            levelConfig.availableItems = allItems.ToArray();
            EditorUtility.SetDirty(levelConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Done! Created {allItems.Count} CosmeticItemSO assets and LevelConfig.");
        }

        private static Sprite[] LoadSortedSprites(string folderPath)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            var sprites = new List<Sprite>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                // Только прямые дети папки, не рекурсивно
                if (Path.GetDirectoryName(path).Replace("\\", "/") != folderPath)
                    continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    sprites.Add(sprite);
            }

            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.ToArray();
        }
    }
}
