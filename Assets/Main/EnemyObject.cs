using System.Text;
using UnityEngine;

public class EnemyObject : BaseObject
{
    private const string CoreBodyName = "Body";
    private const string OutlineName = "Outline";
    private const string LabelName = "Label";
    private const string LockRingName = "LockRing";

    private Manager owner;
    private static Material sharedLineMaterial;
    private EnemyDatas enemyDatas;
    private SpriteRenderer bodyRenderer;
    private LineRenderer outlineRenderer;
    private LineRenderer lockRingRenderer;
    private TextMesh labelMesh;
    private MaterialPropertyBlock propertyBlock;
    private string originalWord;
    private string remainingWord;
    private float moveSpeed;
    private float flashTimer;
    private bool isLocked;
    private bool isResolving;

    public EnemyDatas Data => enemyDatas;
    public string RemainingWord => remainingWord;
    public string OriginalWord => originalWord;
    public bool IsAlive => !isResolving;
    public bool IsLocked => isLocked;

    protected override void Awake()
    {
        base.Awake();
        EnsureVisuals();
    }

    public void Initialize(Manager manager, EnemyDatas data, string word, float speedMultiplier)
    {
        if (manager == null || data == null) {
            return;
        }

        owner = manager;
        enemyDatas = data;
        originalWord = string.IsNullOrWhiteSpace(word) ? "VOID" : word.Trim().ToUpperInvariant();
        remainingWord = originalWord;
        moveSpeed = Mathf.Max(0.1f, data.baseSpeed * speedMultiplier);
        isResolving = false;
        isLocked = false;
        flashTimer = 0f;

        EnsureVisuals();
        ApplyData();
        RefreshLabel();
        InitializeBase();
    }

    protected override void Tick(float deltaTime)
    {
        if (owner == null || enemyDatas == null || isResolving) {
            return;
        }

        Vector3 corePosition = owner.CorePosition;
        Vector3 direction = (corePosition - CachedTransform.position).normalized;
        CachedTransform.position += direction * moveSpeed * deltaTime;
        CachedTransform.Rotate(Vector3.forward, 45f * deltaTime);

        if (flashTimer > 0f) {
            flashTimer -= deltaTime * 4f;
            UpdateMaterialBlock();
        }

        if (Vector3.Distance(CachedTransform.position, corePosition) <= owner.CoreHitRadius) {
            isResolving = true;
            owner.NotifyEnemyReachedCore(this);
        }
    }

    public void SetLocked(bool value)
    {
        isLocked = value;
        if (lockRingRenderer != null) {
            lockRingRenderer.enabled = value;
        }

        if (outlineRenderer != null && enemyDatas != null) {
            outlineRenderer.widthMultiplier = value ? enemyDatas.outlineWidth * 1.75f : enemyDatas.outlineWidth;
        }
    }

    public bool CanAccept(char input)
    {
        return !isResolving
            && !string.IsNullOrEmpty(remainingWord)
            && char.ToUpperInvariant(input) == remainingWord[0];
    }

    public bool ConsumeInput(char input, out bool completedWord)
    {
        completedWord = false;
        if (!CanAccept(input)) {
            return false;
        }

        remainingWord = remainingWord.Substring(1);
        flashTimer = enemyDatas != null ? enemyDatas.hitFlashStrength : 1f;
        UpdateMaterialBlock();
        RefreshLabel();

        completedWord = string.IsNullOrEmpty(remainingWord);
        if (completedWord) {
            isResolving = true;
            SetLocked(false);
        }

        return true;
    }

    public void MarkAsKilled()
    {
        isResolving = true;
        Shutdown();
    }

    public void PlayHitPulse()
    {
        flashTimer = 1f;
        UpdateMaterialBlock();
    }

    public void RefreshLabel()
    {
        if (labelMesh == null || enemyDatas == null) {
            return;
        }

        string typedPart = originalWord.Substring(0, originalWord.Length - remainingWord.Length);
        string remainingPart = remainingWord;
        string typedColor = ColorUtility.ToHtmlStringRGB(Color.white);
        string remainingColor = ColorUtility.ToHtmlStringRGB(enemyDatas.textColor);

        StringBuilder builder = new();
        if (!string.IsNullOrEmpty(typedPart)) {
            builder.Append("<color=#").Append(typedColor).Append('>').Append(typedPart).Append("</color>");
        }

        if (!string.IsNullOrEmpty(remainingPart)) {
            builder.Append("<color=#").Append(remainingColor).Append('>').Append(remainingPart).Append("</color>");
        }

        labelMesh.text = builder.ToString();
    }

    private void EnsureVisuals()
    {
        bodyRenderer = CreateOrGetSpriteRenderer(CoreBodyName);
        outlineRenderer = CreateOrGetLineRenderer(OutlineName, 0);
        lockRingRenderer = CreateOrGetLineRenderer(LockRingName, 1);
        labelMesh = CreateOrGetLabel(LabelName);
        propertyBlock = propertyBlock ?? new MaterialPropertyBlock();
    }

    private void ApplyData()
    {
        if (enemyDatas == null) {
            return;
        }

        bodyRenderer.sprite = enemyDatas.renderSprite;
        bodyRenderer.color = enemyDatas.themeColor;
        if (enemyDatas.overrideMaterial != null) {
            bodyRenderer.sharedMaterial = enemyDatas.overrideMaterial;
        }

        bodyRenderer.transform.localScale = Vector3.one * enemyDatas.renderSize;
        bodyRenderer.enabled = enemyDatas.renderSprite != null;

        ConfigurePolygon(outlineRenderer, enemyDatas.polygonSides, enemyDatas.renderSize * 0.8f, enemyDatas.themeColor, enemyDatas.outlineWidth);
        ConfigurePolygon(lockRingRenderer, Mathf.Max(enemyDatas.polygonSides, 6), enemyDatas.renderSize + 0.45f, Color.white, enemyDatas.outlineWidth * 0.75f);
        lockRingRenderer.enabled = false;

        labelMesh.characterSize = 0.12f;
        labelMesh.fontSize = 56;
        labelMesh.anchor = TextAnchor.MiddleCenter;
        labelMesh.alignment = TextAlignment.Center;
        labelMesh.richText = true;
        labelMesh.color = enemyDatas.textColor;
        labelMesh.transform.localPosition = new Vector3(0f, -enemyDatas.renderSize - 0.45f, 0f);

        UpdateMaterialBlock();
    }

    private void ConfigurePolygon(LineRenderer renderer, int sides, float radius, Color color, float width)
    {
        renderer.loop = true;
        renderer.positionCount = Mathf.Max(3, sides);
        renderer.widthMultiplier = width;
        renderer.useWorldSpace = false;
        renderer.startColor = color;
        renderer.endColor = color;
        renderer.textureMode = LineTextureMode.Stretch;
        renderer.numCornerVertices = 2;
        renderer.numCapVertices = 2;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.alignment = LineAlignment.TransformZ;

        for (int i = 0; i < renderer.positionCount; i++) {
            float angle = Mathf.PI * 2f * i / renderer.positionCount;
            renderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }

    private void UpdateMaterialBlock()
    {
        if (bodyRenderer == null || enemyDatas == null) {
            return;
        }

        bodyRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_TintColor", enemyDatas.themeColor);
        propertyBlock.SetFloat("_GlitchStrength", enemyDatas.glitchStrength + flashTimer * 0.1f);
        propertyBlock.SetFloat("_FlashStrength", flashTimer);
        bodyRenderer.SetPropertyBlock(propertyBlock);
    }

    private SpriteRenderer CreateOrGetSpriteRenderer(string childName)
    {
        Transform child = CachedTransform.Find(childName);
        if (child == null) {
            child = new GameObject(childName).transform;
            child.SetParent(CachedTransform, false);
        }

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 1;
        }

        return renderer;
    }

    private LineRenderer CreateOrGetLineRenderer(string childName, int sortingOrder)
    {
        Transform child = CachedTransform.Find(childName);
        if (child == null) {
            child = new GameObject(childName).transform;
            child.SetParent(CachedTransform, false);
        }

        LineRenderer renderer = child.GetComponent<LineRenderer>();
        if (renderer == null) {
            renderer = child.gameObject.AddComponent<LineRenderer>();
            renderer.sharedMaterial = GetSharedLineMaterial();
            renderer.sortingOrder = sortingOrder + 2;
        }

        return renderer;
    }

    private static Material GetSharedLineMaterial()
    {
        if (sharedLineMaterial == null) {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) {
                sharedLineMaterial = new Material(shader);
            }
        }

        return sharedLineMaterial;
    }

    private TextMesh CreateOrGetLabel(string childName)
    {
        Transform child = CachedTransform.Find(childName);
        if (child == null) {
            child = new GameObject(childName).transform;
            child.SetParent(CachedTransform, false);
        }

        TextMesh mesh = child.GetComponent<TextMesh>();
        if (mesh == null) {
            mesh = child.gameObject.AddComponent<TextMesh>();
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 10;
        }

        return mesh;
    }
}
