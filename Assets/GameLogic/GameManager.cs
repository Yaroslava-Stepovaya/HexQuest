using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MapManager mapManager;   // брось сюда из сцены
    [SerializeField] private UnitView heroViewPrefab; // префаб UnitView (с двумя SpriteRenderer)

    [Header("State")]
    public List<Unit> units = new();                 // логические юниты
    public List<UnitView> unitViews = new();         // их визуалы

    private HeroUnit hero;
    private UnitView heroView;

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

    
}
