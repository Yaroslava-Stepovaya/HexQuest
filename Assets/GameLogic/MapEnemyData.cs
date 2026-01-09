using UnityEngine;

[System.Serializable]
public class MapEnemyData
{
    public int SectorId;     // в каком секторе лежит
    public EnemyType EnemyType;  // тип ключа

    public MapEnemyData(int sectorId, EnemyType enemyType)
    {
        SectorId = sectorId;
        EnemyType = enemyType;
    }
}
