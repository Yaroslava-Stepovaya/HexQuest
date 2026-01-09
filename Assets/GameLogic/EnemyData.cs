using UnityEngine;

[System.Serializable]
public class EnemyData
{
    public int sectorId;     // в каком секторе лежит ключ
    public EnemyType type;
}

public enum EnemyType
{
    Basic,
    Strong
}