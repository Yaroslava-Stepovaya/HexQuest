using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SectorData
{
    public int id;
    public Color displayColor;                 // color for each sector
    public List<Vector3Int> cells = new();     // list that contains cells' coordinates
    public Vector3 centerWorld;                // calculated centroid 
    public List<EdgeData> edges = new();       // sector's edges with information about neighbours, weights and keys needed
}

[Serializable]
public class EdgeData
{
    public int toSectorId;
    public float weight = 1f;                  // sector transition weight (initially equals 1)
    public bool locked = false;                // check if sector is locked
    public string requiredKeyId;               // key ID
}