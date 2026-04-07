using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public class Manager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private GameDatas gameDatas;
    [SerializeField] private GamePlayDatas gamePlayDatas;
    [SerializeField] private List<EnemyDatas> enemyTypes = new();

    [Header("Scene")]
    [SerializeField] private EnemyObject enemyPrefab;
    [SerializeField] private Transform enemyRoot;
    [SerializeField] private Transform coreAnchor;
    [SerializeField] private GameHudUI hud;
    [SerializeField] private ScreenFlashController screenFlash;

    [Header("Flow")]
    [SerializeField] private string startingDifficulty = "Normal";
    [SerializeField] private int initialSpawnCount = 3;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float coreHitRadius = 0.8f;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private bool autoStart = true;

    private readonly List<EnemyObject> liveEnemies = new();
    private EnemyObject currentTarget;
    private GameplayDifficultySettings currentSettings;
    private string currentDifficulty;
    private float spawnTimer;
    private int score;
    private int hp;
    private int combo;
    private bool isPaused;
    private bool isGameOver;

    public Vector3 CorePosition => coreAnchor != null ? coreAnchor.position : transform.position;
    public float CoreHitRadius => coreHitRadius;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        EnsureSceneReferences();
    }

    private void Start()
    {
        if (autoStart) {
            StartGame(startingDifficulty);
        }
    }

    private void Update()
    {
        if (isGameOver && IsRebootPressed()) {
            RebootGame();
            return;
        }

        if (IsPausePressed()) {
            TogglePause();
        }

        if (isPaused || isGameOver || currentSettings == null) {
            return;
        }

        HandleTypingInput();
        HandleSpawning(Time.deltaTime);
        RefreshHud();
    }

    public void StartGame(string difficultyKey)
    {
        currentDifficulty = string.IsNullOrWhiteSpace(difficultyKey) ? startingDifficulty : difficultyKey.Trim();
        currentSettings = ResolveGameplaySettings(currentDifficulty);
        score = 0;
        combo = 0;
        hp = currentSettings.startHp;
        isPaused = false;
        isGameOver = false;
        spawnTimer = 0f;
        ClearEnemies();

        for (int i = 0; i < initialSpawnCount; i++) {
            SpawnEnemy();
        }

        RefreshHud();
    }

    public void TogglePause()
    {
        if (isGameOver) {
            return;
        }

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        RefreshHud();
    }

    public void NotifyEnemyReachedCore(EnemyObject enemy)
    {
        if (enemy == null || isGameOver) {
            return;
        }

        liveEnemies.Remove(enemy);
        if (currentTarget == enemy) {
            currentTarget = null;
        }

        hp -= currentSettings.contactDamage;
        combo = 0;
        screenFlash?.Flash(new Color(1f, 0.1f, 0.35f, 1f), 0.85f, 0.2f);
        Destroy(enemy.gameObject);

        if (hp <= 0) {
            hp = 0;
            isGameOver = true;
        }

        RefreshHud();
    }

    public void RebootGame()
    {
        Time.timeScale = 1f;
        StartGame(currentDifficulty);
    }

    private void EnsureSceneReferences()
    {
        if (enemyRoot == null) {
            GameObject root = new("EnemyRoot");
            root.transform.SetParent(transform, false);
            enemyRoot = root.transform;
        }

        if (coreAnchor == null) {
            GameObject core = new("CoreAnchor");
            core.transform.SetParent(transform, false);
            coreAnchor = core.transform;
        }

        if (hud == null) {
            hud = FindFirstObjectByType<GameHudUI>();
            if (hud == null) {
                hud = new GameObject("GameHudUI").AddComponent<GameHudUI>();
            }
        }

        if (screenFlash == null) {
            screenFlash = FindFirstObjectByType<ScreenFlashController>();
            if (screenFlash == null) {
                screenFlash = new GameObject("ScreenFlash").AddComponent<ScreenFlashController>();
            }
        }
    }

    private void HandleTypingInput()
    {
        List<char> typedCharacters = new();
        bool collectedLegacyInput = CollectLegacyInput(typedCharacters);
#if ENABLE_INPUT_SYSTEM
        if (!collectedLegacyInput) {
            CollectInputSystemInput(typedCharacters);
        }
#endif

        if (typedCharacters.Count == 0) {
            return;
        }

        for (int i = 0; i < typedCharacters.Count; i++) {
            HandleTypedCharacter(typedCharacters[i]);
        }
    }

    private bool CollectLegacyInput(List<char> buffer)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        string input = Input.inputString;
        if (string.IsNullOrEmpty(input)) {
            return false;
        }

        for (int i = 0; i < input.Length; i++) {
            char rawChar = input[i];
            if (char.IsLetter(rawChar)) {
                buffer.Add(char.ToUpperInvariant(rawChar));
            }
        }

        return buffer.Count > 0;
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void CollectInputSystemInput(List<char> buffer)
    {
        if (Keyboard.current == null) {
            return;
        }

        TryAppendLetter(Keyboard.current.aKey, 'A', buffer);
        TryAppendLetter(Keyboard.current.bKey, 'B', buffer);
        TryAppendLetter(Keyboard.current.cKey, 'C', buffer);
        TryAppendLetter(Keyboard.current.dKey, 'D', buffer);
        TryAppendLetter(Keyboard.current.eKey, 'E', buffer);
        TryAppendLetter(Keyboard.current.fKey, 'F', buffer);
        TryAppendLetter(Keyboard.current.gKey, 'G', buffer);
        TryAppendLetter(Keyboard.current.hKey, 'H', buffer);
        TryAppendLetter(Keyboard.current.iKey, 'I', buffer);
        TryAppendLetter(Keyboard.current.jKey, 'J', buffer);
        TryAppendLetter(Keyboard.current.kKey, 'K', buffer);
        TryAppendLetter(Keyboard.current.lKey, 'L', buffer);
        TryAppendLetter(Keyboard.current.mKey, 'M', buffer);
        TryAppendLetter(Keyboard.current.nKey, 'N', buffer);
        TryAppendLetter(Keyboard.current.oKey, 'O', buffer);
        TryAppendLetter(Keyboard.current.pKey, 'P', buffer);
        TryAppendLetter(Keyboard.current.qKey, 'Q', buffer);
        TryAppendLetter(Keyboard.current.rKey, 'R', buffer);
        TryAppendLetter(Keyboard.current.sKey, 'S', buffer);
        TryAppendLetter(Keyboard.current.tKey, 'T', buffer);
        TryAppendLetter(Keyboard.current.uKey, 'U', buffer);
        TryAppendLetter(Keyboard.current.vKey, 'V', buffer);
        TryAppendLetter(Keyboard.current.wKey, 'W', buffer);
        TryAppendLetter(Keyboard.current.xKey, 'X', buffer);
        TryAppendLetter(Keyboard.current.yKey, 'Y', buffer);
        TryAppendLetter(Keyboard.current.zKey, 'Z', buffer);
    }

    private static void TryAppendLetter(KeyControl key, char value, List<char> buffer)
    {
        if (key != null && key.wasPressedThisFrame) {
            buffer.Add(value);
        }
    }
#endif

    private void HandleTypedCharacter(char character)
    {
        if (currentTarget == null || !currentTarget.CanAccept(character)) {
            AcquireTarget(character);
        }

        if (currentTarget != null && currentTarget.ConsumeInput(character, out bool completedWord)) {
            combo++;
            StartCoroutine(PlayProjectilePulse(currentTarget, completedWord));

            if (completedWord) {
                EnemyObject killedEnemy = currentTarget;
                currentTarget = null;
                score += Mathf.RoundToInt(killedEnemy.OriginalWord.Length * currentSettings.scorePerLetter * (1f + combo * currentSettings.comboBonusStep) * killedEnemy.Data.scoreMultiplier);
                StartCoroutine(ResolveEnemyDeath(killedEnemy));
            }
        }
        else {
            combo = 0;
            hp = Mathf.Max(0, hp - 2);
            screenFlash?.Flash(new Color(1f, 0.2f, 0.45f, 1f), 0.4f, 0.08f);
            if (hp <= 0) {
                isGameOver = true;
            }
        }

        RefreshHud();
    }

    private void AcquireTarget(char character)
    {
        EnemyObject bestEnemy = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < liveEnemies.Count; i++) {
            EnemyObject candidate = liveEnemies[i];
            if (candidate == null || !candidate.CanAccept(character)) {
                continue;
            }

            float distance = Vector3.Distance(candidate.transform.position, CorePosition);
            if (distance < bestDistance) {
                bestDistance = distance;
                bestEnemy = candidate;
            }
        }

        if (currentTarget != null) {
            currentTarget.SetLocked(false);
        }

        currentTarget = bestEnemy;
        if (currentTarget != null) {
            currentTarget.SetLocked(true);
        }
    }

    private void HandleSpawning(float deltaTime)
    {
        if (liveEnemies.Count >= currentSettings.maxEnemyCount) {
            return;
        }

        spawnTimer += deltaTime;
        if (spawnTimer >= currentSettings.spawnInterval) {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyTypes == null || enemyTypes.Count == 0 || gameDatas == null) {
            return;
        }

        if (!gameDatas.TryGetRandomWord(currentDifficulty, out string word)) {
            return;
        }

        EnemyDatas enemyData = enemyTypes[Random.Range(0, enemyTypes.Count)];
        EnemyObject enemy = CreateEnemyInstance();

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 position = CorePosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;
        enemy.transform.SetPositionAndRotation(position, Quaternion.identity);
        enemy.Initialize(this, enemyData, word, currentSettings.enemySpeedMultiplier);

        liveEnemies.Add(enemy);
    }

    private EnemyObject CreateEnemyInstance()
    {
        if (enemyPrefab != null) {
            return Instantiate(enemyPrefab, enemyRoot);
        }

        GameObject go = new("EnemyObject");
        go.transform.SetParent(enemyRoot, false);
        return go.AddComponent<EnemyObject>();
    }

    private GameplayDifficultySettings ResolveGameplaySettings(string difficultyKey)
    {
        if (gamePlayDatas != null && gamePlayDatas.TryGetSettings(difficultyKey, out GameplayDifficultySettings settings)) {
            return settings;
        }

        return new GameplayDifficultySettings();
    }

    private void RefreshHud()
    {
        if (hud == null) {
            return;
        }

        hud.SetState(new GameHudState
        {
            Difficulty = currentDifficulty,
            Score = score,
            Hp = hp,
            MaxHp = currentSettings != null ? currentSettings.startHp : 100,
            Combo = combo,
            HasLockTarget = currentTarget != null,
            LockWord = currentTarget != null ? currentTarget.OriginalWord : string.Empty,
            IsPaused = isPaused,
            IsGameOver = isGameOver
        });
    }

    private void ClearEnemies()
    {
        for (int i = liveEnemies.Count - 1; i >= 0; i--) {
            if (liveEnemies[i] != null) {
                Destroy(liveEnemies[i].gameObject);
            }
        }

        liveEnemies.Clear();
        currentTarget = null;
    }

    private IEnumerator PlayProjectilePulse(EnemyObject enemy, bool finalHit)
    {
        if (enemy == null) {
            yield break;
        }

        enemy.PlayHitPulse();
        Color flashColor = enemy.Data != null ? enemy.Data.themeColor : Color.white;
        screenFlash?.Flash(flashColor, finalHit ? 0.55f : 0.2f, finalHit ? 0.12f : 0.05f);
        yield return null;
    }

    private IEnumerator ResolveEnemyDeath(EnemyObject enemy)
    {
        if (enemy == null) {
            yield break;
        }

        enemy.MarkAsKilled();
        liveEnemies.Remove(enemy);

        EnemyEventType deathEvent = enemy.Data != null ? enemy.Data.deathEvent : EnemyEventType.None;
        if (deathEvent == EnemyEventType.FlashBurst || deathEvent == EnemyEventType.GlitchBurst) {
            Color flashColor = enemy.Data != null ? enemy.Data.themeColor : Color.white;
            screenFlash?.Flash(flashColor, deathEvent == EnemyEventType.GlitchBurst ? 0.9f : 0.65f, 0.16f);
        }

        yield return new WaitForSeconds(0.08f);
        Destroy(enemy.gameObject);
    }

    private bool IsPausePressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(pauseKey)) {
            return true;
        }
#endif

#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private bool IsRebootPressed()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R)) {
            return true;
        }
#endif

#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
