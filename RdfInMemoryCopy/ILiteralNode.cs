using System;

namespace RdfInMemoryCopy
{
    public interface ILiteralNode : INode
    {
        Uri DataType { get; }
        string Language { get; }
        string Value { get; }
        

    }
}