/// <summary>
/// An element of the <see cref="Tilegraph"/>.
/// </summary>
public class Node<T>
{
    /// <summary>
    /// The user data which this <see cref="Node{T}"/> contains.
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// The edges of this node (adjacencies).
    /// </summary>
    public Edge<T>[] Edges { get; set; }

    /// <summary>
    /// The H cost of this <see cref="Node{T}"/>.
    /// </summary>
    public float GCost { get; set; }

    /// <summary>
    /// The F cost of this <see cref="Node{T}"/>.
    /// </summary>
    public float FCost { get; set; }

    /// <summary>
    /// Initializes this <see cref="Node{T}"/>.
    /// </summary>
    public Node(T data)
    {
        Data = data;
        Edges = null;
    }
}