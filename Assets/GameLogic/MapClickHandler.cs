using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class MapClickHandler : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private GameManager _gameManager;

    private void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;

        if (_mapManager == null)
            Debug.LogWarning("MapClickHandler: MapManager not set or empty.");

        if (_gameManager == null)
            Debug.LogWarning("MapClickHandler: GameManager not set or empty.");

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI()) return;

            Vector3 screenPos = Input.mousePosition;
            screenPos.z = Mathf.Abs(_camera.transform.position.z - _mapManager.sourceTilemap.transform.position.z);
            Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);
            Vector3Int cell = _mapManager.sourceTilemap.WorldToCell(worldPos);

            if (_mapManager.TryGetSectorByCell(cell, out Sector sector))
            {
                Debug.Log($"Clicked sector {sector.Id}");
                // действия по клику
                _gameManager.HandleSectorClick(sector.Id);
            }
        }
    }

    private static bool IsPointerOverUI(int? fingerId = null)
    {
        if (EventSystem.current == null) return false;
        return fingerId.HasValue
            ? EventSystem.current.IsPointerOverGameObject(fingerId.Value)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
