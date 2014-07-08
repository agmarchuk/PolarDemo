using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public interface INode
    {
        public NodeType NodeType { public get; }
        public IGraph Graph { public get; }

    }
    public interface IUriNode : INode { public Uri Uri { public get; } }
    public interface ILiteralNode : INode
    {
        public Uri DataType { public get; }
        public string Language { public get; }
        public string Value { public get; }
    }
    public enum NodeType
    {
        // Blank,
        Uri, Literal
        //, GraphLiteral, Variable
    }
    public class Triple
    {
        Triple(INode subj, INode pred, INode obj);
        public IGraph Graph { public get; }
        public INode Subject { public get; }
        public INode Predicate { public get; }
        public INode Object { public get; }
    }
    public interface IGraph
    {
        public bool IsEmpty { public get; }
        public INamespaceMapper NamespaceMap { public get; }
        public IEnumerable<INode> Nodes { public get; }
        // Создатели
        IUriNode CreateUriNode();
        ILiteralNode CreateLiteralNode(string value);
        ILiteralNode CreateLiteralNode(string value, Uri datatype);
        ILiteralNode CreateLiteralNode(string value, string lang);
    }
    public interface INamespaceMapper
    {
        IEnumerable<string> Prefixes { public get; }
        void Clear();
        void AddNamespace(string prefix, Uri uri);
        void RemoveNamespace(string prefix);
        Uri GetNamespaceUri(string prefix);
        string GetPrefix(Uri uri);
        bool HasNamespace(string prefix);
        /// <summary>
        /// A Function which attempts to reduce a Uri to a QName. 
        /// This function will return a Boolean indicated whether it succeeded in reducing the Uri to a QName. 
        /// If it did then the out parameter qname will contain the reduction, otherwise it will be the empty string.
        /// </summary>
        /// <param name="uri">The Uri to attempt to reduce</param>
        /// <param name="qname">The value to output the QName to if possible</param>
        /// <returns></returns>
        bool ReduceToQName(string uri, out string qname);
    }
}
