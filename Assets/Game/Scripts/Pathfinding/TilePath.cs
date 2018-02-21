using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TilePath : IEnumerable<Tile>
{
    public int Length => path?.Length ?? 0;

    public Tile StartTile => path?[0];
    public Tile DestinationTile => path?[path.Length - 1];
    public Tile NextTile => path == null || currentTileIndex < 0 ? null : path[currentTileIndex--];

    private int currentTileIndex;
    private readonly Tile[] path;

    public TilePath(IEnumerable<Tile> tiles)
    {
        path = tiles?.ToArray();
        currentTileIndex = Length - 1;
    }

    public IEnumerable<Tile> Reverse() => path?.Reverse();

    public IEnumerator<Tile> GetEnumerator() => Reverse().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
