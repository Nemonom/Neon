using UnityEngine;
using UnityEngine.UI;

public class ScreenFlashController : MonoBehaviour
{
    [SerializeField] private Material effectMaterial;

    private Canvas canvas;
    private Image overlay;
    private float flashTimer;
    private float flashDuration;
    private float flashIntensity;
    private Color flashColor;

    private void Awake()
    {
        EnsureOverlay();
    }

    private void Update()
    {
        if (flashTimer <= 0f) {
            overlay.color = Color.clear;
            return;
        }

        flashTimer -= Time.unscaledDeltaTime;
        float normalized = Mathf.Clamp01(flashTimer / Mathf.Max(0.001f, flashDuration));
        float alpha = normalized * flashIntensity;
        overlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);

        if (overlay.material != null) {
            overlay.material.SetColor("_TintColor", flashColor);
            overlay.material.SetFloat("_FlashStrength", alpha);
        }
    }

    public void Flash(Color color, float intensity, float duration)
    {
        EnsureOverlay();
        flashColor = color;
        flashIntensity = intensity;
        flashDuration = Mathf.Max(0.01f, duration);
        flashTimer = flashDuration;
    }

    private void EnsureOverlay()
    {
        if (overlay != null) {
            return;
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null) {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        GameObject go = new("FlashOverlay");
        go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlay = go.AddComponent<Image>();
        overlay.color = Color.clear;
        if (effectMaterial != null) {
            overlay.material = new Material(effectMaterial);
        }
    }
}
