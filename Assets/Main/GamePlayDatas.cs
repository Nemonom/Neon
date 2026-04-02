using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameplayDifficultyEntry
{
    public string difficultyKey;
    public GameplayDifficultySettings settings = new();
}

[Serializable]
public class GameplayDifficultySettings
{
    [Min(0.1f)] public float spawnInterval = 1.25f;
    [Min(1)] public int maxEnemyCount = 8;
    [Min(0.1f)] public float enemySpeedMultiplier = 1f;
    [Min(1)] public int contactDamage = 12;
    [Min(1)] public int scorePerLetter = 10;
    [Min(0f)] public float comboBonusStep = 0.1f;
    [Min(1)] public int startHp = 100;
}

[CreateAssetMenu(fileName = "GamePlayDatas", menuName = "Scriptable Objects/GamePlayDatas")]
public class GamePlayDatas : ScriptableObject
{
    [SerializeField] private List<GameplayDifficultyEntry> entries = new();

    private Dictionary<string, GameplayDifficultySettings> lookup;

    public IReadOnlyList<GameplayDifficultyEntry> Entries => entries;

    public void SetEntries(List<GameplayDifficultyEntry> newEntries)
    {
        entries = newEntries ?? new List<GameplayDifficultyEntry>();
        lookup = null;
    }

    public bool TryGetSettings(string difficultyKey, out GameplayDifficultySettings settings)
    {
        EnsureLookup();
        return lookup.TryGetValue(NormalizeKey(difficultyKey), out settings);
    }

    private void OnEnable()
    {
        lookup = null;
    }

    private void EnsureLookup()
    {
        if (lookup != null) {
            return;
        }

        lookup = new Dictionary<string, GameplayDifficultySettings>(StringComparer.OrdinalIgnoreCase);
        foreach (GameplayDifficultyEntry entry in entries) {
            if (entry == null || string.IsNullOrWhiteSpace(entry.difficultyKey) || entry.settings == null) {
                continue;
            }

            lookup[NormalizeKey(entry.difficultyKey)] = entry.settings;
        }
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }
}
