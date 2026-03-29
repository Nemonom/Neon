using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameDatas", menuName = "Scriptable Objects/GameDatas")]
public class GameDatas : ScriptableObject
{
    [Header("Word Bank (´Ü¾îÀå)")]
    public List<string> wordList;
}


[CreateAssetMenu(fileName = "EnemyDatas", menuName = "Scriptable Objects/EnemyDatas")]
public class EnemyDatas : ScriptableObject
{
    [Header("Visual Settings")]
    public Color themeColor = Color.cyan;
    public int polygonSides = 6; // 3=»ï°¢, 4=¸¶¸§¸ð, 6=À°°¢
    public float size = 25f;

    [Header("Combat Stats")]
    public float baseSpeed = 0.5f;
}
