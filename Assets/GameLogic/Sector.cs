using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

//class contains information about sector (each sector contains a certain amount of tiles) 
//class contains information 
public sealed class Sector
{
    public int Id { get; }  //get sector's ID
    public IReadOnlyList<Vector3Int> Cells => _cells;  //list containing all tiles 
    public Vector3 CenterWorld { get; private set; }   //sector's centroid

    public bool Blocked { get; set; }
   
    // dictionary that contains information about sector's edges and its neighbours
    private readonly Dictionary<int, SectorEdge> _edges = new();
    //list that contains information about sector's cells
    private readonly List<Vector3Int> _cells;

    public IEnumerable<int> Neighbors => _edges.Keys;   //ID of neighbour sectors

    public Sector(int id, List<Vector3Int> cells, Vector3 centerWorld)
    {
        Id = id;                    //sector's ID
        _cells = cells;             //list of tiles that belong to the sector
        CenterWorld = centerWorld;  //sector's centroid
    }
    //adds new edge or updates another sector's edge adn 
    public void AddOrUpdateEdge(int toSectorId, float weight = 1f, bool locked = false, string requiredKeyId = null)
    {
        _edges[toSectorId] = new SectorEdge(Id, toSectorId, weight, locked, requiredKeyId);
    }

    //get information about edge if it exists
    public bool TryGetEdge(int toSectorId, out SectorEdge edge)
    {
        return _edges.TryGetValue(toSectorId, out edge);
    }

    public bool ContainsCell(Vector3Int cell) => _cells.Contains(cell);


    // if centre recalculation is needed later (for example after map redation)
    public void RecomputeCenter(Tilemap map)
    {
        if (_cells.Count == 0) return;
        Vector3 sum = Vector3.zero;
        foreach (var c in _cells) sum += map.GetCellCenterWorld(c);
        CenterWorld = sum / _cells.Count;
    }
}
//get information about sector's edge
public class SectorEdge
{
    //variables needed for sector's centre
    public int From { get; }
    public int To { get; }
    //value of the sector needed for optimal route
    public float Weight { get; private set; }
    //check if you can enter the sector
    public bool Locked { get; private set; }
    //check if key is needed to enter the sector
    public string RequiredKeyId { get; private set; }


    //edge constructor
    public SectorEdge(int from, int to, float weight, bool locked, string requiredKeyId)
    {
        From = from; To = to; Weight = weight; Locked = locked; RequiredKeyId = requiredKeyId;
    }

    //set edge's status and key required
    public void SetLock(bool locked, string requiredKeyId = null) { Locked = locked; RequiredKeyId = requiredKeyId; }
    //edge's weight
    public void SetWeight(float weight) { Weight = weight; }
}
