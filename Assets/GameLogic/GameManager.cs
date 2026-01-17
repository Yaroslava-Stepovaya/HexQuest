using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

public class GameManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private MapManager mapManager;   // брось сюда из сцены
    [SerializeField] private UnitView heroViewPrefab; // префаб UnitView (с двумя SpriteRenderer)
    [SerializeField] private KeyDatabase keyDatabase;
    [SerializeField] public KeyView keyViewPrefab;
    [SerializeField] private EnemyAgent _enemyAgent;

    [Header("Combat (MVP)")]
    [SerializeField] private int enemyContactDamage = 1;

    [Header("State")]
    public List<Unit> units = new();                 // логические юниты
    
    public List<UnitView> unitViews = new();         // их визуалы

    public MapManager MapManager => mapManager; 
    private HeroUnit hero;
    private UnitView heroView;
    public KeyDatabase KeyDatabase => keyDatabase;
    public HeroUnit Hero => hero;

    private void Start()
    {
        // 1) Проверки
        if (mapManager == null || mapManager.Sectors == null || mapManager.Sectors.Count == 0)
        {
            Debug.LogError("GameManager: MapManager не назначен или нет секторов.");
            return;
        }
        if (heroViewPrefab == null)
        {
            Debug.LogError("GameManager: не назначен prefab UnitView.");
            return;
        }

        // 2) Берём сектор 0

        Sector startSector = mapManager.GetSectorByID(0);

        if (startSector == null)
        {
            Debug.LogError("GameManager: нет сектора с id=0.");
            return;
        }
        //var startSector = mapManager.Sectors[0];

        // 3) Создаём логического героя
        hero = new HeroUnit(
            id: 1,
            name: "Hero",
            startSector: startSector,
            startHP: 10,
            moveSpeed: 4f
        );
        units.Add(hero);

        // 4) Спавним визуал
        heroView = Instantiate(heroViewPrefab, startSector.CenterWorld, Quaternion.identity);
        heroView.name = "HeroView";
        heroView.Bind(hero, snapToSector: true);
        unitViews.Add(heroView);


        GameEvents.ArrivedAtSector += OnUnitArrivedAtSector;
        _enemyAgent.Init();

//        List<Sector> path = new List<Sector>();

//        foreach (var sec in mapManager.Sectors)
//        {
//            if (sec.Id == 0)
//                continue;
//            path.Add(mapManager.GetSectorByID(sec.Id));
//        }
//        view.GetComponent<UnitView>().MoveAlongPath(path);
    }

   
    public void HandleSectorClick(int targetSectorId)
    {
        if (hero == null) return;
        int start = hero.CurrentSector.Id;
        int goal = targetSectorId;

        var pf = mapManager.Pathfinder;

        bool ok = pf.TryFindPath(
          start, goal, out var path,
          canEnterSector: id => !mapManager.IsSectorBlocked(id),
          canTraverseEdge: (a, b) => !mapManager.IsEdgeLocked(a, b)
      );

        if (ok) {

            List<Sector> sectorPath = new List<Sector>();

            foreach (int i in path)
                sectorPath.Add(mapManager.GetSectorByID(i));

            heroView.GetComponent<UnitView>().MoveAlongPath(sectorPath);

        }
    }

    private void OnUnitArrivedAtSector(Unit unit, int sectorID)
    {
        if (unit == null) return;

        // 1) Если прибыл герой — обработка ключей (как раньше)
        if (hero != null && unit.Id == hero.Id)
        {
            Debug.Log("hero has arrived at sector " + sectorID);

            var keys = mapManager.KeysOnMap.Where(x => x.SectorId == sectorID).ToList();
            foreach (var key in keys)
            {
                Debug.Log(key.KeyType + " ");
                UnlockEdgesByKey(key.KeyType);
                GameEvents.SwitchKey?.Invoke(key);
            }

            // MVP-коллизия: герой вошёл в сектор с врагом
            TryResolveHeroEnemyCollision(sectorID, arrivedEnemy: null);
            return;
        }

        // 2) Если прибыл враг — проверяем, не в секторе ли герой
        // (на MVP определяем "враг" через список EnemiesOnMap)
        if (mapManager.EnemiesOnMap != null && mapManager.EnemiesOnMap.Any(e => e != null && e.IsAlive && e.Id == unit.Id))
        {
            TryResolveHeroEnemyCollision(sectorID, arrivedEnemy: unit);
        }
    }



    private void DestroyEnemyView(int enemyId)
    {
        var view = MapManager.GetUnitViewByID(enemyId);

        if (view != null)
            Destroy(view.gameObject);
    }
    private void TryResolveHeroEnemyCollision(int sectorID, Unit arrivedEnemy)
    {
        if (hero == null || !hero.IsAlive) return;

        int heroSectorId = hero.CurrentSector != null ? hero.CurrentSector.Id : -1;
        if (heroSectorId != sectorID) return; // столкновения нет: герой не в этом секторе

        // Определяем врага:
        // - если прибыл враг: используем его
        // - если прибыл герой: ищем любого живого врага в секторе
        Unit enemy = arrivedEnemy;

        if (enemy == null)
        {
            enemy = mapManager.EnemiesOnMap
                .FirstOrDefault(e => e != null && e.IsAlive && e.CurrentSector != null && e.CurrentSector.Id == sectorID);
        }

        if (enemy == null || !enemy.IsAlive) return;

        // --- Эффект: герой теряет HP ---
        // ВАЖНО: у тебя Unit.TakeDamage пока TODO. Тут предполагается что в HeroUnit/Unit ты это реализовал.
        hero.TakeDamage(enemyContactDamage);

        // --- Аннигиляция врага ---
        enemy.Die();            // если реализовано
        enemy.IsAlive = false;  // страховка на MVP

        // освободить слот, если используешь слоты
        if (enemy.CurrentSector != null)
            mapManager.ReleaseSlot(enemy.CurrentSector, enemy.Id);

        // убрать визуал врага
        DestroyEnemyView(enemy.Id);

        Debug.Log($"Collision in sector {sectorID}: hero damaged, enemy {enemy.Id} annihilated");
    }




    private void UnlockEdgesByKey(KeyType keyType)
    {

        foreach (var sector in mapManager.Sectors)
        {

            var edges = sector.Edges
                .Where(e => e.Value.RequiredKeyType == keyType)
                .Select(e => e.Value)
                .ToList();


            foreach (var edge in edges)
                sector.AddOrUpdateEdge(edge.To, 1, false);


        }

        
        GameEvents.RebuildEdges?.Invoke();
    }

}
