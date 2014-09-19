namespace RdfInMemoryCopy
{
    public interface INode
    {
        NodeType NodeType { get; }
        IGraph Graph { get; }
    
    }
}