using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "HexQuest/Map Asset", fileName = "NewMapAsset")]
public class MapAsset : ScriptableObject
{
    public List<SectorData> sectors = new();
}
