// Assets/Scripts/Runtime/MapManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;



//map management during runtime
public class MapManager : MonoBehaviour
{
    [Header("Data")]
    public MapAsset mapAsset;                // scriptableObject with map data
    public Tilemap sourceTilemap;            // tilemap with map 

    [SerializeField] private UnitView heroViewPrefab; // префаб UnitView (с двумя SpriteRenderer)
    [SerializeField] private KeyDatabase _keyDatabase;
    [SerializeField] public KeyView keyViewPrefab;
    [SerializeField] public UnitView enemyViewPrefab;


    [Header("State")]
    //public List<Unit> units = new();                 // логические юниты
    public List<MapKeyData> KeysOnMap = new List<MapKeyData>();
    public List<Unit> EnemiesOnMap = new List<Unit>();
    private readonly Dictionary<int, UnitView> _unitViewsById = new();
    public IReadOnlyDictionary<int, UnitView> UnitViewsById => _unitViewsById;
    //public List<UnitView> unitViews = new();         // их визуалы


    [Header("Sector Slots (anti-overlap)")]
    [SerializeField] private int slotsPerSector = 5;
    [SerializeField, Range(0.3f, 1f)] private float corePercent = 0.7f;
    [SerializeField] private int slotPickStep = 2;
    [SerializeField] private float minSlotDistance = 1f;

    // sectorId -> slot world positions
    private readonly Dictionary<int, List<Vector3>> _sectorSlotsCache = new();

    // runtime structures
    private List<Sector> _sectors = new();
    private Dictionary<Vector3Int, int> _cellToSector = new();

    public KeyDatabase KeyDatabase => _keyDatabase;


    public IReadOnlyList<Sector> Sectors => _sectors;

    public SectorGraphPathfinder Pathfinder { get; private set; }

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
                s.AddOrUpdateEdge(e.toSectorId, e.weight, e.locked, e.requiredKeyType);

            _sectors.Add(s);
        }

        // 2) tilemap's ID → sector's ID 
        foreach (var s in _sectors)
            foreach (var c in s.Cells)
                _cellToSector[c] = s.Id;

        Pathfinder = new SectorGraphPathfinder(_sectors);

        foreach(var kd in mapAsset.keys)
        {
            CreateKey(kd.sectorId, kd.type);
        }

        int idCounter = 2;
        foreach (var enemy in mapAsset.enemies)
        {
            CreateEnemy(enemy.sectorId, enemy.type, idCounter);
            idCounter++;
        }

            _sectors[0].AddOrUpdateEdge(2, 1, true, KeyType.Blue);
        _sectors[2].AddOrUpdateEdge(0, 1, true, KeyType.Blue);

        _sectorSlotsCache.Clear();
    }

    public Vector3 ReserveSlot(Sector sector, int unitId)
    {
        if (sector == null) return Vector3.zero;

        EnsureSlotsBuilt(sector);
        var slots = _sectorSlotsCache[sector.Id];

        // уже есть слот
        if (sector.TryGetAssignedSlot(unitId, out int existing))
            return slots[Mathf.Clamp(existing, 0, slots.Count - 1)];

        // ищем свободный
        for (int i = 0; i < slots.Count; i++)
        {
            if (!sector.IsSlotTaken(i))
            {
                sector.AssignSlot(unitId, i);
                return slots[i];
            }
        }

        // слоты закончились
        return sector.CenterWorld;
    }

    public void ReleaseSlot(Sector sector, int unitId)
    {
        if (sector == null) return;
        sector.ReleaseSlot(unitId);
    }

    public Vector3 GetAssignedSlotWorldOrCenter(Sector sector, int unitId)
    {
        if (sector == null) return Vector3.zero;

        EnsureSlotsBuilt(sector);

        if (sector.TryGetAssignedSlot(unitId, out int slotIndex))
        {
            var slots = _sectorSlotsCache[sector.Id];
            if (slotIndex >= 0 && slotIndex < slots.Count) return slots[slotIndex];
        }

        return sector.CenterWorld;
    }

    public void CreateKey(int sectorId, KeyType keyType)
    {
        var keyData = new MapKeyData(sectorId, keyType);
        Sector sector = GetSectorByID(sectorId);
        var keyView = Instantiate(keyViewPrefab, sector.CenterWorld, Quaternion.identity);
        keyView.Init(keyData, _keyDatabase.Get(keyType), sector.CenterWorld);

        KeysOnMap.Add(keyData);
    }

    public void CreateEnemy(int sectorID, EnemyType enemyType, int id)
    {
        Sector sector = GetSectorByID(sectorID);
        var EnemyData = new Unit(id, enemyType.ToString(), sector, 10);

        // резервируем слот и спавним туда
        Vector3 spawnPos = ReserveSlot(sector, EnemyData.Id);

        var EnemyView = Instantiate(enemyViewPrefab, sector.CenterWorld, Quaternion.identity);
        EnemyView.Bind(EnemyData, false);
        EnemyView.transform.position = spawnPos;
        EnemiesOnMap.Add(EnemyData);
        _unitViewsById[EnemyData.Id] = EnemyView;

    }

    private void EnsureSlotsBuilt(Sector sector)
    {
        if (sector == null) return;
        if (_sectorSlotsCache.ContainsKey(sector.Id)) return;

        _sectorSlotsCache[sector.Id] = BuildSlotsForSector(sector);
    }

    private List<Vector3> BuildSlotsForSector(Sector sector)
    {
        var result = new List<Vector3>(slotsPerSector);

        if (sourceTilemap == null&  sector.Cells == null& sector.Cells.Count == 0)
    {
            result.Add(sector.CenterWorld);
            return result;
        }

        // (cell, dist, world)
        var buf = new List<(Vector3Int cell, float dist, Vector3 world)>(sector.Cells.Count);
        for (int i = 0; i < sector.Cells.Count; i++)
        {
            var c = sector.Cells[i];
            var w = sourceTilemap.GetCellCenterWorld(c);
            float d = Vector3.Distance(w, sector.CenterWorld);
            buf.Add((c, d, w));
        }

        // ближе к центру = "дороже"
        buf.Sort((a, b) => a.dist.CompareTo(b.dist));

        int total = buf.Count;
        int coreCount = Mathf.CeilToInt(total * corePercent);

        // чтобы было из чего выбирать (ядро не меньше 2*slots, если возможно)
        int minCore = Mathf.Min(total, Mathf.Max(slotsPerSector * 2, slotsPerSector));
        if (coreCount < minCore) coreCount = minCore;
        if (coreCount < 1) coreCount = 1;

        var used = new HashSet<Vector3Int>();

        void AddIfUnique(int idx)
        {
            var item = buf[idx];

            // уже брали эту клетку
            if (!used.Add(item.cell))
                return;

            // 🔹 НОВОЕ: фильтр по минимальной дистанции между слотами
            // (чтобы юниты не налезали друг на друга)
            for (int i = 0; i < result.Count; i++)
            {
                if (Vector3.Distance(result[i], item.world) < minSlotDistance)
                    return;
            }

            result.Add(item.world);
        }

        // slot0 — самый центральный
        AddIfUnique(0);

        int step = Mathf.Max(1, slotPickStep);
        int idxStep = step;

        // основной набор по шагу
        while (result.Count < slotsPerSector && idxStep < coreCount)
        {
            AddIfUnique(idxStep);
            idxStep += step;
        }

        // fallback: добираем следующими из ядра
        if (result.Count < slotsPerSector)
        {
            for (int i = 0; i < coreCount && result.Count < slotsPerSector; i++)
                AddIfUnique(i);
        }

        // финальный fallback
        if (result.Count == 0)
            result.Add(sector.CenterWorld);

        return result;
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
    public Sector GetSectorByID(int id)
    {
        return _sectors.Where(x => x.Id == id).FirstOrDefault();
    }

    public bool IsEdgeLocked(int a, int b)
    {
        var sa = GetSectorByID(a);
        if (sa == null) return true; // если сектора нет — считаем непроходимым
        if (!sa.TryGetEdge(b, out var edge)) return true; // нет связи

        return edge.Locked; // true = замок
    }

    public bool IsSectorBlocked(int id)
    {
        return GetSectorByID(id).Blocked;
    }

    public void SafeSetEdge(int a, int b, bool exists)
    {
        if (exists)
        {
            GetSectorByID(a).RemoveEdge(b);
            GetSectorByID(b).RemoveEdge(a);
        }
        else
        {
            GetSectorByID(a).AddOrUpdateEdge(b);
            GetSectorByID(b).AddOrUpdateEdge(a);
        }

            GameEvents.RebuildEdges?.Invoke();
    }

    public void ChangeEdgeLock(int a, int b, KeyType keyType = KeyType.None)
    {
        bool edgeLock = keyType != KeyType.None;

        GetSectorByID(a).AddOrUpdateEdge(b,1,edgeLock,keyType);
        GetSectorByID(b).AddOrUpdateEdge(a, 1, edgeLock, keyType);

        GameEvents.RebuildEdges?.Invoke();
    }
    
}
