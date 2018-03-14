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
    public int Width => width;
    public int Height => height;

    private Tile startTile;
    private Tile goalTile;

    [SerializeField]
    private int width = 10;
    [SerializeField]
    private int height = 10;

    private Tile[,] tiles;

    private TileBuildMode tileBuildMode;
    private TileType buildTileType;

    private TilePath tilePath;
    private Tilegraph tilegraph;

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
                    tileGameObjects[startTile].GetComponent<SpriteRenderer>().color = Color.green;
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
                    tileGameObjects[goalTile].GetComponent<SpriteRenderer>().color = Color.yellow;
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
    }

    private void FindPath()
    {
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

        GameObject tileGameObject = new GameObject($"Tile ({tile.Position.x}, {tile.Position.y})");
        tileGameObjects.Add(tile, tileGameObject);
        tileGameObject.transform.position = new Vector3(tile.Position.x, tile.Position.y, 1);
        tileGameObject.transform.SetParent(tileParent.transform);

        SpriteRenderer spriteRenderer = tileGameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForTile(tile);
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