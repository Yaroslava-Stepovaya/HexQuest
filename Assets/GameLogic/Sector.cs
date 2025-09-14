using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class Sector
{
    public int Id { get; }
    public IReadOnlyList<Vector3Int> Cells => _cells;
    public Vector3 CenterWorld { get; private set; }

    // сосед → ребро (вес/замок)
    private readonly Dictionary<int, SectorEdge> _edges = new();
    private readonly List<Vector3Int> _cells;

    public IEnumerable<int> Neighbors => _edges.Keys;

    public Sector(int id, List<Vector3Int> cells, Vector3 centerWorld)
    {
        Id = id;
        _cells = cells;
        CenterWorld = centerWorld;
    }

    public void AddOrUpdateEdge(int toSectorId, float weight = 1f, bool locked = false, string requiredKeyId = null)
    {
        _edges[toSectorId] = new SectorEdge(Id, toSectorId, weight, locked, requiredKeyId);
    }

    public bool TryGetEdge(int toSectorId, out SectorEdge edge)
    {
        return _edges.TryGetValue(toSectorId, out edge);
    }

    public bool ContainsCell(Vector3Int cell) => _cells.Contains(cell);

    // если позже решите пересчитывать центр (например, после редактирования карты)
    public void RecomputeCenter(Tilemap map)
    {
        if (_cells.Count == 0) return;
        Vector3 sum = Vector3.zero;
        foreach (var c in _cells) sum += map.GetCellCenterWorld(c);
        CenterWorld = sum / _cells.Count;
    }
}

public class SectorEdge
{
    public int From { get; }
    public int To { get; }
    public float Weight { get; private set; }
    public bool Locked { get; private set; }
    public string RequiredKeyId { get; private set; }

    public SectorEdge(int from, int to, float weight, bool locked, string requiredKeyId)
    {
        From = from; To = to; Weight = weight; Locked = locked; RequiredKeyId = requiredKeyId;
    }

    public void SetLock(bool locked, string requiredKeyId = null) { Locked = locked; RequiredKeyId = requiredKeyId; }
    public void SetWeight(float weight) { Weight = weight; }
}
