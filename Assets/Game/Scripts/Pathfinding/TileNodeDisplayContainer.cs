using UnityEngine;
using UnityEngine.UI;

public class TileNodeDisplayContainer : MonoBehaviour
{
    [SerializeField]
    private Canvas textCanvas;
    [SerializeField]
    private Text gCostText;
    [SerializeField]
    private Text hCostText;
    [SerializeField]
    private Text fCostText;

    public void SetDisplayData(Node<Tile> node)
    {
        textCanvas.enabled = node != null;
        if (node == null) return;

        gCostText.text = Mathf.RoundToInt(node.GCost * 10).ToString();
        hCostText.text = Mathf.RoundToInt(node.HCost * 10).ToString();
        fCostText.text = Mathf.RoundToInt(node.FCost * 10).ToString();
    }
}
