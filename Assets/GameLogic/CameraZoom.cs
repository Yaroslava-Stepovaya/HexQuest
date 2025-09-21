using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public Camera cam;
    public float zoomSpeed = 2f;
    public float minSize = 2f;
    public float maxSize = 20f;

    void Update()
    {
        if (cam == null) cam = Camera.main;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minSize, maxSize);
        }
    }
}
