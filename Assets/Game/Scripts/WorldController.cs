using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

/// <summary>
/// The top-level manager for the game world.
/// </summary>
public class WorldController : MonoBehaviour
{
    private static readonly Color StartTileColour = new Color(0.2f, 0.6f, 1);
    private static readonly Color EndTileColour = Color.yellow;
    private static readonly Color OpenSetTileColour = new Color(0.5f, 1, 0.3f);
    private static readonly Color ClosedSetTileColour = new Color(0.9f, 0.3f, 0.3f);

    private static readonly Vector2Int NorthDirection = new Vector2Int(0, 1);
    private static readonly Vector2Int SouthDirection = new Vector2Int(0, -1);
    private static readonly Vector2Int EastDirection = new Vector2Int(1, 0);
    private static readonly Vector2Int WestDirection = new Vector2Int(-1, 0);

    public int Width => width;
    public int Height => height;

    private Tile startTile;
    private Tile goalTile;

    [SerializeField]
    private CameraController cameraController;
    [SerializeField]
    private int width = 10;
    [SerializeField]
    private int height = 10;
    [SerializeField]
    private bool stepThrough;
    [SerializeField]
    private bool autoStepThrough;
    [SerializeField]
    private float nextStepCooldown = 1;
    private float nextStepTimer;
    private bool doAutoStepThrough;
    [SerializeField]
    private int autoStepsPerFrame = 1;

    [SerializeField]
    private bool generateRandomObstacles;
    [Range(0, 1)]
    [SerializeField]
    private float obstacleChance = 0.5f;
    [SerializeField]
    private bool generateMaze = true;
    private HashSet<Vector2Int> mazeGenerationVisited;

    private Tile[,] tiles;

    private TileBuildMode tileBuildMode;
    private TileType buildTileType;

    private TilePath tilePath;
    private Tilegraph tilegraph;
    private Pathfinder pathfinder;

    private GameObject tileParent;
    private Dictionary<Tile, GameObject> tileGameObjects;
    private int stepCount;

    private void Start()
    {
        tiles = new Tile[width, height];
        tileGameObjects = new Dictionary<Tile, GameObject>(width * height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = new Tile(new Vector2Int(x, y), TileType.Floor);
                if (generateRandomObstacles && Random.value <= obstacleChance)
                {
                    tile.Type = TileType.Wall;
                }

                if (generateMaze)
                {
                    if (x % 2 != 0 && y % 2 != 0)
                    {
                        tile.Type = TileType.Floor;
                    }
                    else
                    {
                        tile.Type = TileType.Wall;
                    }
                }

                tiles[x, y] = tile;
                SpawnTile(tile);
            }
        }

        GenerateMaze();    
        tilegraph = new Tilegraph(this);

        StaticBatchingUtility.Combine(tileParent);
    }

    private void GenerateMaze(int x = 0, int y = 0)
    {
        if (!generateMaze) return;

        if (mazeGenerationVisited == null)
        {
            mazeGenerationVisited = new HashSet<Vector2Int>();
        }

        Vector2Int[] directions =
        {
            NorthDirection,
            EastDirection,
            SouthDirection,
            WestDirection
        };

        directions.Shuffle();

        mazeGenerationVisited.Add(new Vector2Int(x, y));
        foreach (Vector2Int direction in directions)
        {
            Vector2Int position = new Vector2Int(x, y) + direction * new Vector2Int(2, 2);
            bool inRange = position.y >= 0 && position.y < height && position.x >= 0 && position.x < width;
            if (inRange && !mazeGenerationVisited.Contains(position))
            {
                Tile neighbour = GetTileAt(x + direction.x, y + direction.y);
                if (neighbour != null)
                {
                    neighbour.Type = TileType.Floor;
                    UpdateTileVisuals(neighbour);
                }

                Tile oppositeNeighbour = GetTileAt(x + direction.x * -1, y + direction.y * -1);
                if (oppositeNeighbour != null)
                {
                    oppositeNeighbour.Type = TileType.Floor;
                    UpdateTileVisuals(oppositeNeighbour);
                }

                GenerateMaze(position.x, position.y);
            }
        }
    }
    
    private void Update()
    {
        UpdateTiles();

        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            switch (tileBuildMode)
            {
                case TileBuildMode.Start:
                    Tile newStartTile = GetTileUnderMouse();
                    if (newStartTile == null || newStartTile.Type != TileType.Floor) break;

                    if (startTile != null)
                    {
                        UpdateTileVisuals(startTile);
                    }

                    startTile = newStartTile;
                    tileGameObjects[startTile].GetComponent<SpriteRenderer>().color = StartTileColour;
                    FindPath();

                    break;
                case TileBuildMode.End:
                    Tile newGoalTile = GetTileUnderMouse();
                    if (newGoalTile == null || newGoalTile.Type != TileType.Floor) break;

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

        nextStepTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space) && autoStepThrough)
        {
            doAutoStepThrough = !doAutoStepThrough;
        }

        if (stepThrough && nextStepTimer <= 0 && (doAutoStepThrough || Input.GetKey(KeyCode.Space)))
        {
            NextStep(doAutoStepThrough ? autoStepsPerFrame : 0);
        }
    }

    private void UpdateTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = GetTileAt(x, y);
                GameObject tileGameObject = tileGameObjects[tile];
                tileGameObject.SetActive(cameraController.FrustrumOrthographicBounds.Contains(new Vector2(x, y)));
            }
        }
    }

    private void NextStep(int count = 0)
    {
        stepCount++;
        nextStepTimer = nextStepCooldown;

        if (goalTile == null || startTile == null)
        {
            doAutoStepThrough = false;
            return;
        }

        if (pathfinder == null)
        {
            pathfinder = new Pathfinder(tilegraph.Nodes[startTile], tilegraph.Nodes[goalTile]);
            tilePath = null;
            stepCount = 0;
        }

        pathfinder.ExecutePathfindingStep();
        if (pathfinder.HasFoundPath)
        {
            tilePath = pathfinder.Path;
        }

        foreach (Node<Tile> node in tilegraph.Nodes.Values)
        {
            GameObject tileGameObject = tileGameObjects[node.Data];
            TileInstance tileInstance = tileGameObject.GetComponent<TileInstance>();
            SpriteRenderer spriteRenderer = tileGameObject.GetComponent<SpriteRenderer>();

            if (tilePath != null)
            {
                if (tilePath.Contains(node.Data))
                {
                    tileInstance.Node = null;
                    if (node.Data == startTile || node.Data == goalTile) continue;

                    spriteRenderer.color = Color.blue;
                }
                else
                {
                    ResetTileVisuals(node, tileInstance, spriteRenderer);
                }
            }
            else
            {
                if (pathfinder.IsInOpenSet(node))
                {
                    SetTileVisuals(node, tileInstance, spriteRenderer, OpenSetTileColour);
                }
                else if (pathfinder.IsInClosedSet(node))
                {
                    SetTileVisuals(node, tileInstance, spriteRenderer, ClosedSetTileColour);
                }
                else
                {
                    ResetTileVisuals(node, tileInstance, spriteRenderer);
                }
            }

        }

        if (!pathfinder.HasFoundPath && !pathfinder.SearchIsComplete)
        {
            if (doAutoStepThrough && count > 0)
            {
                NextStep(--count);
            }

            return;
        }

        pathfinder = null;
        doAutoStepThrough = false;

        Debug.Log($"Found path in {stepCount} steps");
    }

    private void SetTileVisuals(Node<Tile> node, TileInstance tileInstance, SpriteRenderer spriteRenderer, Color colour)
    {
        if (node.Data != startTile && node.Data != goalTile)
        {
            spriteRenderer.color = colour;
        }

        tileInstance.Node = node;
    }

    private void ResetTileVisuals(Node<Tile> node, TileInstance tileInstance, SpriteRenderer spriteRenderer)
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

        tileInstance.Node = null;
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
        UpdateTileVisuals(tile);

        TileInstance displayContainer = tileGameObject.GetComponent<TileInstance>();
        displayContainer.Initialize(null, cameraController);
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x >= Width || x < 0 || y >= Height || y < 0) return null;
        return tiles[x, y];
    }

    public Tile GetTileUnderMouse() => GetTileAtWorldCoordinate(cameraController.Camera.ScreenToWorldPoint(Input.mousePosition));

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

        return Resources.Load<Sprite>($"Tiles/Floor{suffix}");
    }
}