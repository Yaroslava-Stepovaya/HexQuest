using System;
using UnityEngine;

[Serializable]
public class KeyData
{
    public int sectorId;     // в каком секторе лежит ключ
    public KeyType type;     // "red", "blue", "yellow" и т.п.
}