using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The top-level manager for the game world.
/// </summary>
public class WorldController : MonoBehaviour
{
    private static readonly Color StartTileColour = new Color(0.2f, 0.6f, 1);
    private static readonly Color EndTileColour = Color.yellow;
    private static readonly Color OpenSetTileColour = new Color(0.5f, 1, 0.3f);
    private static readonly Color ClosedSetTileColour = new Color(0.9f, 0.3f, 0.3f);

    public int Width => width;
    public int Height => height;

    private Tile startTile;
    private Tile goalTile;

    [SerializeField]
    private int width = 10;
    [SerializeField]
    private int height = 10;
    [SerializeField]
    private bool stepThrough;

    private Tile[,] tiles;

    private TileBuildMode tileBuildMode;
    private TileType buildTileType;

    private TilePath tilePath;
    private Tilegraph tilegraph;
    private Pathfinder pathfinder;

    private GameObject tileParent;
    private Dictionary<Tile, GameObject> tileGameObjects;

    private void Start()
    {
        tiles = new Tile[width, height];
        tileGameObjects = new Dictionary<Tile, GameObject>(width * height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = new Tile(new Vector2Int(x, y), TileType.Floor);

                tiles[x, y] = tile;
                SpawnTile(tile);
            }
        }

        Camera.main.orthographicSize = width / 2f + 0.5f;
        Camera.main.transform.position = new Vector3(width / 2f - 0.5f, height / 2f - 0.5f);
        tilegraph = new Tilegraph(this);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            switch (tileBuildMode)
            {
                case TileBuildMode.Start:
                    Tile newStartTile = GetTileUnderMouse();
                    if (newStartTile == null) break;

                    if (startTile != null)
                    {
                        if (tilePath != null && !tilePath.Contains(startTile))
                        {
                            UpdateTileVisuals(startTile);
                        }
                        else
                        {
                            UpdateTileVisuals(startTile);
                        }
                    }

                    startTile = newStartTile;
                    tileGameObjects[startTile].GetComponent<SpriteRenderer>().color = StartTileColour;
                    FindPath();

                    break;
                case TileBuildMode.End:
                    Tile newGoalTile = GetTileUnderMouse();
                    if (newGoalTile == null) break;

                    if (goalTile != null)
                    {
                        UpdateTileVisuals(goalTile);
                    }

                    goalTile = newGoalTile;
                    tileGameObjects[goalTile].GetComponent<SpriteRenderer>().color = EndTileColour;
                    FindPath();

                    break;
                case TileBuildMode.Type:
                    Tile tile = GetTileUnderMouse();
                    if (tile == null || tile == startTile || tile == goalTile) break;

                    tile.Type = buildTileType;

                    UpdateTileVisuals(tile);
                    tilegraph.Regenerate(tile);
                    FindPath();

                    break;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && stepThrough)
        {
            if (goalTile == null || startTile == null) return;
            if (pathfinder == null)
            {
                pathfinder = new Pathfinder(tilegraph.Nodes[startTile], tilegraph.Nodes[goalTile]);
                tilePath = null;
            }

            pathfinder.ExecutePathfindingStep();
            if (pathfinder.HasFoundPath)
            {
                tilePath = pathfinder.Path;
            }

            foreach (Node<Tile> node in tilegraph.Nodes.Values)
            {
                GameObject tileGameObject = tileGameObjects[node.Data];
                TileNodeDisplayContainer nodeDisplayContainer = tileGameObject.GetComponent<TileNodeDisplayContainer>();

                SpriteRenderer spriteRenderer = tileGameObject.GetComponent<SpriteRenderer>();

                if (tilePath != null)
                {
                    if (tilePath.Contains(node.Data))
                    {
                        nodeDisplayContainer.SetDisplayData(null);
                        if (node.Data == startTile || node.Data == goalTile) continue;

                        spriteRenderer.color = Color.blue;
                    }
                    else
                    {
                        ResetTileVisuals(node, nodeDisplayContainer, spriteRenderer);
                    }
                }
                else
                {
                    if (pathfinder.IsInOpenSet(node))
                    {
                        SetTileVisuals(node, nodeDisplayContainer, spriteRenderer, OpenSetTileColour);
                    }
                    else if (pathfinder.IsInClosedSet(node))
                    {
                        SetTileVisuals(node, nodeDisplayContainer, spriteRenderer, ClosedSetTileColour);
                    }
                    else
                    {
                        ResetTileVisuals(node, nodeDisplayContainer, spriteRenderer);
                    }
                }

            }

            if (pathfinder.HasFoundPath || pathfinder.SearchIsComplete)
            {
                pathfinder = null;
            }
        }
    }

    private void SetTileVisuals(Node<Tile> node, TileNodeDisplayContainer nodeDisplayContainer, SpriteRenderer spriteRenderer, Color colour)
    {
        if (node.Data != startTile && node.Data != goalTile)
        {
            spriteRenderer.color = colour;
        }

        nodeDisplayContainer.SetDisplayData(node);
    }

    private void ResetTileVisuals(Node<Tile> node, TileNodeDisplayContainer nodeDisplayContainer, SpriteRenderer spriteRenderer)
    {
        if (node.Data == startTile)
        {
            spriteRenderer.color = StartTileColour;
        }
        else if (node.Data == goalTile)
        {
            spriteRenderer.color = EndTileColour;
        }
        else
        {
            UpdateTileVisuals(node.Data);
        }

        nodeDisplayContainer.SetDisplayData(null);
    }

    private void FindPath()
    {
        if (stepThrough) return;
        if (goalTile == null || startTile == null)
        {
            if (tilePath == null) return;
            foreach (Tile tile in tilePath)
            {
                if (tile == startTile || tile == goalTile) continue;
                tileGameObjects[tile].GetComponent<SpriteRenderer>().color = Color.white;
            }

            return;
        }

        if (tilePath != null)
        {
            foreach (Tile tile in tilePath)
            {
                if (tile == startTile || tile == goalTile) continue;
                UpdateTileVisuals(tile);
            }

            tilePath = null;
        }

        tilePath = tilegraph.FindPath(startTile, goalTile);
        if (tilePath == null)
        {
            Debug.LogError("WorldController::Update: could not find path from start to end tile.");
            return;
        }

        foreach (Tile tile in tilePath)
        {
            if (tile == startTile || tile == goalTile) continue;
            tileGameObjects[tile].GetComponent<SpriteRenderer>().color = Color.blue;
        }
    }

    private void UpdateTileVisuals(Tile tile)
    {
        SpriteRenderer tileSpriteRenderer = tileGameObjects[tile].GetComponent<SpriteRenderer>();
        switch (tile.Type)
        {
            case TileType.Floor:
                tileSpriteRenderer.color = Color.white;
                break;
            case TileType.Wall:
                tileSpriteRenderer.color = Color.black;
                break;
        }
    }

    private void SpawnTile(Tile tile)
    {
        if (tileParent == null)
        {
            tileParent = new GameObject("Tiles");
        }

        GameObject tileGameObject = Instantiate(Resources.Load<GameObject>("Prefabs/Tile"));
        tileGameObject.name = $"Tile ({tile.Position.x}, {tile.Position.y})";
        tileGameObject.transform.position = new Vector3(tile.Position.x, tile.Position.y, 1);
        tileGameObject.transform.SetParent(tileParent.transform);

        tileGameObjects.Add(tile, tileGameObject);

        SpriteRenderer spriteRenderer = tileGameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForTile(tile);

        tileGameObject.GetComponent<TileNodeDisplayContainer>().SetDisplayData(null);
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0) return null;
        return tiles[x, y];
    }

    public Tile GetTileUnderMouse() => GetTileAtWorldCoordinate(Camera.main.ScreenToWorldPoint(Input.mousePosition));

    public Tile GetTileAtWorldCoordinate(Vector3 coordinates)
    {
        int x = Mathf.FloorToInt(coordinates.x + 0.5f);
        int y = Mathf.FloorToInt(coordinates.y + 0.5f);

        return GetTileAt(x, y);
    }

    public Tile[] GetNeighbours(Tile tile, bool diagonal = false)
    {
        Tile[] neighbours = diagonal == false ? new Tile[4] : new Tile[8];

        int x = tile.Position.x;
        int y = tile.Position.y;

        Tile tileAt = GetTileAt(x, y + 1);
        neighbours[0] = tileAt;
        tileAt = GetTileAt(x + 1, y);
        neighbours[1] = tileAt;
        tileAt = GetTileAt(x, y - 1);
        neighbours[2] = tileAt;
        tileAt = GetTileAt(x - 1, y);
        neighbours[3] = tileAt;

        if (diagonal != true) return neighbours;

        tileAt = GetTileAt(x + 1, y + 1);
        neighbours[4] = tileAt;
        tileAt = GetTileAt(x + 1, y - 1);
        neighbours[5] = tileAt;
        tileAt = GetTileAt(x - 1, y - 1);
        neighbours[6] = tileAt;
        tileAt = GetTileAt(x - 1, y + 1);
        neighbours[7] = tileAt;

        return neighbours;
    }

    public void SetTileBuildMode(int mode) => tileBuildMode = (TileBuildMode)mode;
    public void SetPlaceTileType(int type) => buildTileType = (TileType)type;

    private Sprite GetSpriteForTile(Tile tile)
    {
        string suffix = "_";
        if (tile.Position.y + 1 >= height)
        {
            suffix += "Top";
        }

        if (tile.Position.x + 1 >= width)
        {
            suffix += "Right";
        }

        return Resources.Load<Sprite>($"Tiles/{Enum.GetName(typeof(TileType), tile.Type) + suffix}");
    }
}