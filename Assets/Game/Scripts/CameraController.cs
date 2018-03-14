using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private WorldController worldController;

    private bool isPanning;

    private const float panningThreshold = 0.015f;
    private Vector3 panningMouseStart = Vector3.zero;

    private Vector3 lastFramePosition;
    private Vector3 currentFramePosition;

    [SerializeField]
    private float zoomLerp = 3;
    [SerializeField]
    private float zoomSensitivity = 3;

    private float zoomTarget;

    private void Awake()
    {
        Zoom(-zoomSensitivity);
    }

    private void Update()
    {
        UpdateCurrentFramePosition();
        UpdateCameraMovement();
        UpdateCameraZoom();
        StoreFramePosition();
    }

    private void UpdateCurrentFramePosition()
    {
        currentFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currentFramePosition.z = 0;
    }

    private void StoreFramePosition()
    {
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    private void UpdateCameraMovement()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            panningMouseStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            panningMouseStart.z = 0;
        }

        if (!isPanning)
        {
            Vector3 currentMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            currentMousePosition.z = 0;

            if (Vector3.Distance(panningMouseStart, currentMousePosition) > panningThreshold * Camera.main.orthographicSize)
            {
                isPanning = true;
            }
        }

        if (isPanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            Vector3 distance = lastFramePosition - currentFramePosition;
            if (distance != Vector3.zero)
            {
                Camera.main.transform.Translate(distance);
            }
        }

        if (!Input.GetMouseButton(1) && !Input.GetMouseButton(2))
        {
            isPanning = false;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            Zoom(Input.GetAxis("Mouse ScrollWheel"));
        }

        UpdateCameraBounds();
    }

    private void UpdateCameraZoom()
    {
        Vector3 oldMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        oldMousePosition.z = 0;

        if (Camera.main.orthographicSize != zoomTarget)
        {
            float target = zoomTarget;
            if (zoomLerp > 0)
            {
                target = Mathf.Lerp(Camera.main.orthographicSize, zoomTarget, zoomLerp * Time.deltaTime);
            }

            Camera.main.orthographicSize = Mathf.Clamp(target, 3f, 25f);
        }

        Vector3 newMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        newMousePosition.z = 0;

        Vector3 distance = oldMousePosition - newMousePosition;
        Camera.main.transform.Translate(distance);
    }

    private void Zoom(float amount)
    {
        zoomTarget = Camera.main.orthographicSize - zoomSensitivity * (Camera.main.orthographicSize * amount);
    }

    private void UpdateCameraBounds()
    {
        Vector3 oldPosition = Camera.main.transform.position;

        oldPosition.x = Mathf.Clamp(oldPosition.x, 0, (float)worldController.Width - 1);
        oldPosition.y = Mathf.Clamp(oldPosition.y, 0, (float)worldController.Height - 1);

        Camera.main.transform.position = oldPosition;
    }
}
