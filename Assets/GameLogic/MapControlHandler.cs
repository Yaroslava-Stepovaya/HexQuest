using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class MapControlHandler : MonoBehaviour
{
    public enum Mode { Gameplay, EditEdges }

    public enum EditSubMode
    {
        ToggleEdge,   // как сейчас: добавл€ть/удал€ть ребро
        PaintLock     // новый: назначать requiredKeyType и locked
    }

    [Header("Refs")]
    [SerializeField] private Camera _camera;
    private MapManager _mapManager;
    [SerializeField] private GameManager _gameManager;

    [Header("Mode")]
    [SerializeField] private Mode _mode = Mode.Gameplay;
    [SerializeField] private KeyCode _toggleEditKey = KeyCode.E;

    [Header("Edit Edges Keys")]
    [SerializeField] private KeyCode _saveMapKey = KeyCode.S;
    //[SerializeField] private KeyCode _delEdgeKey = KeyCode.X;

    [Header("Edit SubMode")]
    [SerializeField] private EditSubMode _editSubMode = EditSubMode.ToggleEdge;

    // выбранный "ключ-тип" дл€ PaintLock
    [SerializeField] private KeyType _selectedKeyType = KeyType.None;

    // хоткей переключени€ подрежима (пример)
    [SerializeField] private KeyCode _toggleLockPaintKey = KeyCode.K;

    private int _selected = -1;   // сектор A (дл€ EditEdges)

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
        if (_gameManager == null || _gameManager.MapManager == null)
        {
            Debug.LogWarning("MapClickHandler: GameManager not set.");
        }
        _mapManager = _gameManager.MapManager;
    }

    public void SetMode(Mode mode)
    {
        _mode = mode;
        if (_mode == Mode.Gameplay) _selected = -1;
        Debug.Log("Mode has switched to " + mode.ToString());
    }

    private void Update()

    {
        //if (Input.GetKeyDown(KeyCode.K))
        //{
        //    if (!TryGetSectorUnderCursor(out var sector)) return;

        //    _gameManager.MapManager.CreateKey(sector.Id, KeyType.Blue);
        //}

        if (Input.GetKeyDown(_toggleEditKey))
            SetMode(_mode == Mode.Gameplay ? Mode.EditEdges : Mode.Gameplay);

        if (Input.GetKeyDown(_saveMapKey))
            MapRuntimeSaver.SaveRuntimeToAsset(_mapManager.mapAsset, _gameManager);

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            if (!TryGetSectorUnderCursor(out var sector)) return;

            if (_mode == Mode.Gameplay)
            {
                // обычна€ логика
                _gameManager?.HandleSectorClick(sector.Id);
            }
            else // EditEdges
            {
                HandleEditClick(sector.Id);
            }
        }

        if (_mode == Mode.EditEdges)
        {
            //// гор€чие клавиши дл€ A/B
            //if (_selected >= 0 && TryGetSectorUnderCursor(out var hover) && hover.Id != _selected)
            //{
            //    if (Input.GetKeyDown(_addEdgeKey))
            //        SafeSetEdge(_selected, hover.Id, true);

            //    if (Input.GetKeyDown(_delEdgeKey))
            //        SafeSetEdge(_selected, hover.Id, false);
            //}

            if (Input.GetKeyDown(_toggleLockPaintKey))
            {
                _editSubMode = (_editSubMode == EditSubMode.ToggleEdge)
                    ? EditSubMode.PaintLock
                    : EditSubMode.ToggleEdge;

                _selectedKeyType = KeyType.None;
            }

            if (_editSubMode == EditSubMode.PaintLock)
                HandleKeyPaletteHotkeys();

            if (Input.GetKeyDown(KeyCode.Escape))
                _selected = -1;
        }
    }

    private void HandleKeyPaletteHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0)) _selectedKeyType = KeyType.None;
        if (Input.GetKeyDown(KeyCode.Alpha1)) _selectedKeyType = KeyType.Red;
        if (Input.GetKeyDown(KeyCode.Alpha2)) _selectedKeyType = KeyType.Blue;
        if (Input.GetKeyDown(KeyCode.Alpha3)) _selectedKeyType = KeyType.Yellow;
    }

    private void HandleEditClick(int clickedId)
    {
        if (_selected < 0)
        {
            _selected = clickedId; // выбрали A
            return;
        }

        if (_selected == clickedId)
        {
            _selected = -1; // сн€ли выбор
            return;
        }

        if (_editSubMode == EditSubMode.ToggleEdge) 
        { 
        // Ћ ћ по B Ч быстрый toggle ребра AЦB
            bool exists = _mapManager.AreNeighbors(_selected, clickedId);
            SafeSetEdge(_selected, clickedId, exists);
        }
        else if (_editSubMode == EditSubMode.PaintLock)
        {
            if (!_mapManager.AreNeighbors(_selected, clickedId))
                return;
            ChangeEdgeLock(_selected, clickedId, _selectedKeyType);
        }
            // удобно Ђвести кистьюї: новый A = только что кликнутый B
            _selected = clickedId;
    }

    private void SafeSetEdge(int a, int b, bool exists)
    {
        if (a < 0 || b < 0 || a == b) return;
        _mapManager.SafeSetEdge(a, b, exists);
        //_mapManager.RefreshEdgeVisual(a, b); // если есть визуальные линии Ч обновить
    }


    private void ChangeEdgeLock(int a, int b, KeyType keyType)
    {
        if (a < 0 || b < 0 || a == b) return;
        _mapManager.ChangeEdgeLock(a, b, keyType);
    //    //_mapManager.RefreshEdgeVisual(a, b); // если есть визуальные линии Ч обновить
    }


    private bool TryGetSectorUnderCursor(out Sector sector)
    {
        sector = null;
        var tilemap = _mapManager.sourceTilemap;
        if (!tilemap) return false;

        Vector3 sp = Input.mousePosition;
        sp.z = Mathf.Abs(_camera.transform.position.z - tilemap.transform.position.z);
        Vector3 world = _camera.ScreenToWorldPoint(sp);
        Vector3Int cell = tilemap.WorldToCell(world);

        return _mapManager.TryGetSectorByCell(cell, out sector);
    }

    private static bool IsPointerOverUI(int? fingerId = null)
    {
        if (EventSystem.current == null) return false;
        return fingerId.HasValue
            ? EventSystem.current.IsPointerOverGameObject(fingerId.Value)
            : EventSystem.current.IsPointerOverGameObject();
    }

    // (опционально) проста€ подсказка режима
    private void OnGUI()
    {
        if (_mode != Mode.EditEdges) return;
        GUILayout.BeginArea(new Rect(10, 10, 360, 200), GUI.skin.box);
        GUILayout.Label("<b>Edge Edit Mode</b> (E to toggle)", Rich());
        GUILayout.Label("<b>ToggleEdge/PaintLock Mode</b> (K to toggle)", Rich());
        GUILayout.Label($"Selected A: {(_selected >= 0 ? _selected.ToString() : "Ч")}");

        GUILayout.Label($"SubMode:" + _editSubMode.ToString());
        if (_editSubMode == EditSubMode.PaintLock)
        {
            GUILayout.Label($"Lock keys:" + "0 - none, 1 - red, 2 - blue, 3 - yellow");
            GUILayout.Label($"Selected Lock:" + _selectedKeyType.ToString());
        }

        GUILayout.Label("LMB: pick/toggle, Esc: clear");
        GUILayout.EndArea();
    }
    private static GUIStyle Rich() { var s = new GUIStyle(GUI.skin.label) { richText = true }; return s; }
}


//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.Tilemaps;

//public class MapClickHandler : MonoBehaviour
//{
//    [SerializeField] private Camera _camera;
//    [SerializeField] private MapManager _mapManager;
//    [SerializeField] private GameManager _gameManager;

//    private void Awake()
//    {
//        if (_camera == null)
//            _camera = Camera.main;

//        if (_mapManager == null)
//            Debug.LogWarning("MapClickHandler: MapManager not set or empty.");
//        if (_gameManager == null)
//            Debug.LogWarning("MapClickHandler: GameManager not set or empty.");

//    }

//    private void Update()
//    {
//        if (Input.GetMouseButtonDown(0))
//        {
//            if (IsPointerOverUI()) return;

//            Vector3 screenPos = Input.mousePosition;
//            screenPos.z = Mathf.Abs(_camera.transform.position.z - _mapManager.sourceTilemap.transform.position.z);
//            Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);
//            Vector3Int cell = _mapManager.sourceTilemap.WorldToCell(worldPos);

//            if (_mapManager.TryGetSectorByCell(cell, out Sector sector))
//            {
//                Debug.Log($"Clicked sector {sector.Id}");
//                // действи€ по клику
//                _gameManager.HandleSectorClick(sector.Id);
//            }
//        }
//    }

//    private static bool IsPointerOverUI(int? fingerId = null)
//    {
//        if (EventSystem.current == null) return false;
//        return fingerId.HasValue
//            ? EventSystem.current.IsPointerOverGameObject(fingerId.Value)
//            : EventSystem.current.IsPointerOverGameObject();
//    }
//}
