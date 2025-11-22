using UnityEngine;

[System.Serializable]
public class MapKeyData
{
    public int SectorId;     // в каком секторе лежит
    public KeyType KeyType;  // тип ключа

    public MapKeyData(int sectorId, KeyType keyType)
    {
        SectorId = sectorId;
        KeyType = keyType;
    }
}
