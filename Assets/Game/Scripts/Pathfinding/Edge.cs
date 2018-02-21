/// <summary>
/// An edge of a <see cref="Node{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Edge<T>
{
    /// <summary>
    /// The cost to move on this <see cref="Edge{T}"/>.
    /// </summary>
    public float Cost { get; }

    /// <summary>
    /// The <see cref="Node{T}"/> which this <see cref="Edge{T}"/> belongs to.
    /// </summary>
    public Node<T> Node { get; }
    
    /// <summary>
    /// Initializes this <see cref="Edge{T}"/>.
    /// </summary>
    public Edge(float cost, Node<T> node)
    {
        Cost = cost;
        Node = node;
    }
}