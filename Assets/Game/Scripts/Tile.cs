using UnityEngine;

/// <summary>
/// An individual element of the tilegraph.
/// </summary>
public class Tile
{    
    /// <summary>
    /// The position of this <see cref="Tile"/> in grid space.
    /// </summary>
    public Vector2Int Position { get; }

    /// <summary>
    /// The type of this tile.
    /// </summary>
    public TileType Type { get; set; }

    /// <summary>
    /// The cost of walking on this tile.
    /// </summary>
    public float MovementCost => CalculateMovementCost();

    /// <summary>
    /// Initialize this <see cref="Tile"/> with a position and type.
    /// </summary>
    /// <param name="position">The position of the <see cref="Tile"/> in the grid.</param>
    /// <param name="type">The type of the <see cref="Tile"/>.</param>
    public Tile(Vector2Int position, TileType type)
    {
        Position = position;
        Type = type;
    }

    /// <summary>
    /// Calculate the movement cost of a tile based on it's type.
    /// </summary>
    private float CalculateMovementCost()
    {
        switch (Type)
        {
            case TileType.Floor:
                return 1;
            case TileType.Wall:
                return 0;
        }

        return 0;
    }
}