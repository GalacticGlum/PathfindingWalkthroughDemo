using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraControllerEventArgs : EventArgs
{
    public CameraController CameraController { get; }
    public CameraControllerEventArgs(CameraController cameraController)
    {
        CameraController = cameraController;
    }
}

public class CameraControllerZoomedEventArgs : CameraControllerEventArgs
{
    public float Amount { get; }
    public float Direction { get; }
    public bool ZoomedIn { get; }
    public bool ZoomedOut { get; }

    public CameraControllerZoomedEventArgs(CameraController cameraController, float amount, float direction) : base(cameraController)
    {
        Amount = amount;
        Direction = direction;

        ZoomedIn = direction > 0;
        ZoomedOut = direction < 0;
    }
}

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private bool canControlCamera = true;
    public bool CanControlCamera
    {
        get { return canControlCamera; }
        set
        {
            if (value == canControlCamera) return;
            canControlCamera = value;

            ControlStateChanged?.Invoke(this, new CameraControllerEventArgs(this));
        }
    }

    /// <summary>
    /// The bounds containing all the objects our camera can see.
    /// </summary>
    public Bounds FrustrumOrthographicBounds { get; private set; }
    public Bounds OrthographicBounds { get; private set; }
    public Camera Camera { get; private set; }

    public event EventHandler<CameraControllerEventArgs> ControlStateChanged;
    public event EventHandler<CameraControllerZoomedEventArgs> Zoomed;

    private const float PanningThreshold = 0.015f;

    [SerializeField]
    private WorldController worldController;
    [SerializeField]
    private float zoomLerp = 3;
    [SerializeField]
    private float zoomSensitivity = 3;

    private bool isPanning;
    private Vector3 panningMouseStart = Vector3.zero;
    private Vector3 lastFramePosition;
    private Vector3 currentFramePosition;

    private float maximumZoom;
    private float zoomTarget;

    private Vector2 centrePosition;
    private Vector2 previousCameraPosition;
    private float previousCameraZoom;

    private void Awake()
    {
        Camera = GetComponent<Camera>();

        maximumZoom = Mathf.Ceil(worldController.Width / 2f + 0.5f);
        Zoom(-maximumZoom / zoomSensitivity);

        Camera.orthographicSize = maximumZoom;

        centrePosition = new Vector3(worldController.Width / 2f - 0.5f, worldController.Height / 2f - 0.5f);
        Camera.transform.position = centrePosition;

        UpdateOrthographicBounds();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CanControlCamera = !CanControlCamera;
            if (!CanControlCamera)
            {
                previousCameraPosition = Camera.transform.position;
                previousCameraZoom = Camera.orthographicSize;

                Camera.transform.position = centrePosition;
                Camera.orthographicSize = maximumZoom;
            }
            else
            {
                Camera.transform.position = previousCameraPosition;
                Camera.orthographicSize = previousCameraZoom;
            }

            UpdateOrthographicBounds();
        }

        if (!CanControlCamera) return;

        UpdateCurrentFramePosition();
        UpdateCameraMovement();
        UpdateCameraZoom();
        StoreFramePosition();
    }

    private void UpdateCurrentFramePosition()
    {
        currentFramePosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        currentFramePosition.z = 0;
    }

    private void StoreFramePosition()
    {
        lastFramePosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    private void UpdateCameraMovement()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            panningMouseStart = Camera.ScreenToWorldPoint(Input.mousePosition);
            panningMouseStart.z = 0;
        }

        if (!isPanning)
        {
            Vector3 currentMousePosition = Camera.ScreenToWorldPoint(Input.mousePosition);
            currentMousePosition.z = 0;

            if (Vector3.Distance(panningMouseStart, currentMousePosition) > PanningThreshold * Camera.orthographicSize)
            {
                isPanning = true;
            }
        }

        if (isPanning && (Input.GetMouseButton(1) || Input.GetMouseButton(2)))
        {
            Vector3 distance = lastFramePosition - currentFramePosition;
            if (distance != Vector3.zero)
            {
                Camera.transform.Translate(distance);
                UpdateOrthographicBounds();
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

        HandleWorldBounds();
    }

    private void UpdateCameraZoom()
    {
        Vector3 oldMousePosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        oldMousePosition.z = 0;

        if (Camera.orthographicSize != zoomTarget)
        {
            float target = zoomTarget;
            if (zoomLerp > 0)
            {
                target = Mathf.Lerp(Camera.orthographicSize, zoomTarget, zoomLerp * Time.deltaTime);
            }

            Camera.orthographicSize = Mathf.Clamp(target, 3f, maximumZoom);
        }

        Vector3 newMousePosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        newMousePosition.z = 0;

        Vector3 distance = oldMousePosition - newMousePosition;
        Camera.transform.Translate(distance);
    }

    private void Zoom(float amount)
    {
        zoomTarget = Camera.orthographicSize - zoomSensitivity * (Camera.orthographicSize * amount);

        Zoomed?.Invoke(this, new CameraControllerZoomedEventArgs(this, amount, Mathf.Sign(amount)));
        UpdateOrthographicBounds();
    }

    private void HandleWorldBounds()
    {
        Vector3 oldPosition = Camera.transform.position;

        oldPosition.x = Mathf.Clamp(oldPosition.x, 0, (float)worldController.Width - 1);
        oldPosition.y = Mathf.Clamp(oldPosition.y, 0, (float)worldController.Height - 1);

        Camera.transform.position = oldPosition;
    }

    private void UpdateOrthographicBounds()
    {
        float aspect = (float) Screen.width / Screen.height;
        float height = Camera.orthographicSize * 2;
        OrthographicBounds = new Bounds(Camera.transform.position, new Vector3(height * aspect, height, 0));
        FrustrumOrthographicBounds = new Bounds(OrthographicBounds.center, OrthographicBounds.size + new Vector3(2, 2));
    }
}
