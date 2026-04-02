using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DifficultyWordBucket
{
    public string difficultyKey;
    public List<string> words = new();
}

[CreateAssetMenu(fileName = "GameDatas", menuName = "Scriptable Objects/GameDatas")]
public class GameDatas : ScriptableObject
{
    [SerializeField] private List<DifficultyWordBucket> entries = new();

    private Dictionary<string, List<string>> lookup;

    public IReadOnlyList<DifficultyWordBucket> Entries => entries;

    public void SetEntries(List<DifficultyWordBucket> newEntries)
    {
        entries = newEntries ?? new List<DifficultyWordBucket>();
        lookup = null;
    }

    public bool TryGetWords(string difficultyKey, out List<string> words)
    {
        EnsureLookup();
        return lookup.TryGetValue(NormalizeKey(difficultyKey), out words);
    }

    public bool TryGetRandomWord(string difficultyKey, out string word)
    {
        word = string.Empty;
        if (!TryGetWords(difficultyKey, out List<string> words) || words.Count == 0) {
            return false;
        }

        word = words[UnityEngine.Random.Range(0, words.Count)];
        return true;
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

        lookup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (DifficultyWordBucket entry in entries) {
            if (entry == null || string.IsNullOrWhiteSpace(entry.difficultyKey)) {
                continue;
            }

            string normalizedKey = NormalizeKey(entry.difficultyKey);
            if (!lookup.TryGetValue(normalizedKey, out List<string> words)) {
                words = new List<string>();
                lookup.Add(normalizedKey, words);
            }

            if (entry.words == null) {
                continue;
            }

            foreach (string candidate in entry.words) {
                if (!string.IsNullOrWhiteSpace(candidate)) {
                    words.Add(candidate.Trim().ToUpperInvariant());
                }
            }
        }
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }
}
