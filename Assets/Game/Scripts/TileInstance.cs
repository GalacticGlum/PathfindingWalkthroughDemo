using TMPro;
using UnityEngine;

public class TileInstance : MonoBehaviour
{
    public Node<Tile> node;
    public Node<Tile> Node
    {
        get { return node; }
        set
        {
            node = value;
            UpdateDisplay();
        }
    }

    [SerializeField]
    private GameObject displayParent;
    [SerializeField]
    private TextMeshPro gCostText;
    [SerializeField]
    private TextMeshPro hCostText;
    [SerializeField]
    private TextMeshPro fCostText;

    private CameraController cameraController;

    public void Initialize(Node<Tile> node, CameraController cameraController)
    {
        this.cameraController = cameraController;
        Node = node;

        cameraController.Zoomed += (sender, args) => UpdateDisplay();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayParent.SetActive(Node != null && cameraController.Camera.orthographicSize <= 15);
        if (Node == null) return;

        gCostText.text = Mathf.RoundToInt(Node.GCost * 10).ToString();
        hCostText.text = Mathf.RoundToInt(Node.HCost * 10).ToString();
        fCostText.text = Mathf.RoundToInt(Node.FCost * 10).ToString();
    }
}
