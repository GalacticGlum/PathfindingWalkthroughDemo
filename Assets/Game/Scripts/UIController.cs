using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIController : MonoBehaviour
{
    [SerializeField]
    private CameraController cameraController;
    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        cameraController.ControlStateChanged += (sender, args) => ToggleVisibility(args.CameraController.CanControlCamera);
    }

    public void ToggleVisibility(bool visible)
    {
        canvas.enabled = visible;
    }
}