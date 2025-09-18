// Assets/Scripts/Runtime/MapManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

//map management during runtime
public class MapManager : MonoBehaviour
{
    [Header("Data")]
    public MapAsset mapAsset;                // scriptableObject with map data
    public Tilemap sourceTilemap;            // tilemap with map 

    // runtime structures
    private List<Sector> _sectors = new();
    private Dictionary<Vector3Int, int> _cellToSector = new();

    
    public IReadOnlyList<Sector> Sectors => _sectors;


#if UNITY_EDITOR
    [ContextMenu("Reload from MapAsset")]     //allows to update the map using context menu
    public void ReloadEditor()
    {
        LoadFromAsset();
    }
#endif

    //load the map when launching the game
    void Awake()
    {
        LoadFromAsset();
    }

    public void LoadFromAsset()
    {
        _sectors.Clear();
        _cellToSector.Clear();

        if (mapAsset == null)
        {
            Debug.LogError("MapAsset is null!");
            return;
        }

        // 1) create runtime-sector object based on mapAsset.sectors 
        foreach (var sd in mapAsset.sectors)
        {
            var s = new Sector(sd.id, new List<Vector3Int>(sd.cells), sd.centerWorld);
            // recreate edges
            foreach (var e in sd.edges)
                s.AddOrUpdateEdge(e.toSectorId, e.weight, e.locked, e.requiredKeyId);

            _sectors.Add(s);
        }

        // 2) tilemap's ID → sector's ID 
        foreach (var s in _sectors)
            foreach (var c in s.Cells)
                _cellToSector[c] = s.Id;
    }
    // trying to get sector by tilemap's ID
    public bool TryGetSectorByCell(Vector3Int cell, out Sector sector)
    {
        if (_cellToSector.TryGetValue(cell, out int id))
        {
            sector = _sectors[id];
            return true;
        }
        sector = null;
        return false;
    }

    // trying to get sector in the world
    public bool TryGetSectorByWorld(Vector3 world, out Sector sector)
    {
        if (sourceTilemap == null)
        {
            sector = null;
            return false;
        }

        var cell = sourceTilemap.WorldToCell(world);
        return TryGetSectorByCell(cell, out sector);
    }

    //check if two sectors are neighbours
    public bool AreNeighbors(int a, int b)
    {
        if (a < 0 || a >= _sectors.Count || b < 0 || b >= _sectors.Count)
            return false;
        return _sectors[a].TryGetEdge(b, out _);
    }
}
