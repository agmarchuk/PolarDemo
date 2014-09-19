using System;

namespace TripleStoreForDNR
{
    public interface ILiteralNode : INode
    {
        Uri DataType { get; }
        string Language { get; }
        string Value { get; }
        

    }
}