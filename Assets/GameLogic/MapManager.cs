// Assets/Scripts/Runtime/MapManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [Header("Data")]
    public MapAsset mapAsset;                // твой запечённый .asset
    public Tilemap sourceTilemap;            // тот Tilemap, где рисуется карта (можно null, но удобно для кликов)

    // Рантайм-структуры:
    private List<Sector> _sectors = new();
    private Dictionary<Vector3Int, int> _cellToSector = new();

    public IReadOnlyList<Sector> Sectors => _sectors;


#if UNITY_EDITOR
    [ContextMenu("Reload from MapAsset")]
    public void ReloadEditor()
    {
        LoadFromAsset();
    }
#endif

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

        // 1) Создаём рантайм-сектора из сохранённых данных
        foreach (var sd in mapAsset.sectors)
        {
            var s = new Sector(sd.id, new List<Vector3Int>(sd.cells), sd.centerWorld);
            // восстанавливаем рёбра
            foreach (var e in sd.edges)
                s.AddOrUpdateEdge(e.toSectorId, e.weight, e.locked, e.requiredKeyId);

            _sectors.Add(s);
        }

        // 2) Индекс клетка → id сектора
        foreach (var s in _sectors)
            foreach (var c in s.Cells)
                _cellToSector[c] = s.Id;
    }

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

    public bool AreNeighbors(int a, int b)
    {
        if (a < 0 || a >= _sectors.Count || b < 0 || b >= _sectors.Count)
            return false;
        return _sectors[a].TryGetEdge(b, out _);
    }
}
