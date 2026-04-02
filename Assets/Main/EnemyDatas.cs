using UnityEngine;

public enum EnemyEventType
{
    None,
    FlashBurst,
    GlitchBurst,
    ScoreBurst
}

[CreateAssetMenu(fileName = "EnemyDatas", menuName = "Scriptable Objects/EnemyDatas")]
public class EnemyDatas : ScriptableObject
{
    [Header("Identity")]
    public string enemyId = "enemy";
    public string displayName = "Enemy";

    [Header("Rendering")]
    public Sprite renderSprite;
    public Material overrideMaterial;
    public Color themeColor = Color.cyan;
    public Color textColor = Color.white;
    [Min(3)] public int polygonSides = 6;
    [Min(0.25f)] public float renderSize = 1.2f;
    [Range(0.01f, 0.4f)] public float outlineWidth = 0.08f;
    [Range(0f, 2f)] public float glitchStrength = 0.2f;
    [Range(0f, 3f)] public float hitFlashStrength = 1f;

    [Header("Gameplay")]
    [Min(0.1f)] public float baseSpeed = 2f;
    [Min(0f)] public float scoreMultiplier = 1f;
    public EnemyEventType deathEvent = EnemyEventType.FlashBurst;
}
