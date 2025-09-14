using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SectorData
{
    public int id;
    public Color displayColor;                 // чисто визуал (для миникарты/подсветки)
    public List<Vector3Int> cells = new();     // координаты тайлмапа
    public Vector3 centerWorld;                // рассчитанный центроид
    public List<EdgeData> edges = new();       // явные рёбра (соседи + веса/замки)
}

[Serializable]
public class EdgeData
{
    public int toSectorId;
    public float weight = 1f;                  // стоимость перехода (по умолчанию 1)
    public bool locked = false;                // ворота закрыты?
    public string requiredKeyId;               // ид ключа (можно оставить пустым на старте)
}