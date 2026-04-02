using UnityEngine;
using UnityEngine.UI;

public struct GameHudState
{
    public string Difficulty;
    public int Score;
    public int Hp;
    public int MaxHp;
    public int Combo;
    public bool HasLockTarget;
    public string LockWord;
    public bool IsPaused;
    public bool IsGameOver;
}

public class GameHudUI : MonoBehaviour
{
    private Canvas canvas;
    private Text titleText;
    private Text difficultyText;
    private Text scoreText;
    private Text comboText;
    private Text targetText;
    private Text pauseText;
    private Text gameOverText;
    private Image hpFill;

    private void Awake()
    {
        EnsureCanvas();
    }

    public void SetState(GameHudState state)
    {
        EnsureCanvas();

        titleText.text = "NEON / SHATTERED GLASS";
        difficultyText.text = $"DIFFICULTY : {state.Difficulty}";
        scoreText.text = $"SCORE : {state.Score:000000}";
        comboText.text = $"COMBO : x{state.Combo}";
        targetText.text = state.HasLockTarget ? $"LOCK : {state.LockWord}" : "LOCK : NONE";
        pauseText.text = state.IsPaused ? "PAUSED" : string.Empty;
        gameOverText.text = state.IsGameOver ? "SYSTEM BREACHED" : string.Empty;

        float hpRatio = state.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)state.Hp / state.MaxHp);
        hpFill.fillAmount = hpRatio;
        hpFill.color = Color.Lerp(new Color(1f, 0.15f, 0.35f), new Color(0f, 0.95f, 1f), hpRatio);
    }

    private void EnsureCanvas()
    {
        if (canvas != null) {
            return;
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null) {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        GetOrAddComponent<GraphicRaycaster>(gameObject);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        titleText = CreateText("Title", font, new Vector2(20f, -20f), 32, TextAnchor.UpperLeft, new Color(0f, 0.95f, 1f));
        difficultyText = CreateText("Difficulty", font, new Vector2(20f, -70f), 22, TextAnchor.UpperLeft, Color.white);
        scoreText = CreateText("Score", font, new Vector2(-20f, -20f), 34, TextAnchor.UpperRight, Color.white);
        comboText = CreateText("Combo", font, new Vector2(20f, -110f), 22, TextAnchor.UpperLeft, new Color(0.2f, 1f, 0.3f));
        targetText = CreateText("Target", font, new Vector2(20f, -145f), 22, TextAnchor.UpperLeft, new Color(1f, 0.92f, 0.2f));
        pauseText = CreateText("Pause", font, Vector2.zero, 42, TextAnchor.MiddleCenter, Color.white);
        gameOverText = CreateText("GameOver", font, new Vector2(0f, -80f), 52, TextAnchor.MiddleCenter, new Color(1f, 0.2f, 0.35f));

        pauseText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        pauseText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        pauseText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        pauseText.rectTransform.anchoredPosition = Vector2.zero;
        pauseText.rectTransform.sizeDelta = new Vector2(800f, 120f);

        gameOverText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        gameOverText.rectTransform.anchoredPosition = new Vector2(0f, -70f);
        gameOverText.rectTransform.sizeDelta = new Vector2(1200f, 220f);

        CreateHpBar();
    }

    private void CreateHpBar()
    {
        GameObject root = new("HpBar");
        root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = new Vector2(20f, 20f);
        rootRect.sizeDelta = new Vector2(420f, 24f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);

        GameObject fill = new("Fill");
        fill.transform.SetParent(root.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        hpFill = fill.AddComponent<Image>();
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;
        hpFill.color = new Color(0f, 0.95f, 1f);
    }

    private Text CreateText(string name, Font font, Vector2 anchoredPosition, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject go = new(name);
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = anchor == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(900f, 80f);

        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = color;
        text.text = string.Empty;
        return text;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null) {
            component = target.AddComponent<T>();
        }

        return component;
    }
}
