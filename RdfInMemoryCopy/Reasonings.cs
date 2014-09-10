using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    //
    // см. https://bitbucket.org/dotnetrdf/dotnetrdf/wiki/User%20Guide
    //
    public interface INode
    {
        NodeType NodeType { get; }
        IGraph Graph { get; }

    }
    public interface IUriNode : INode { Uri Uri { get; } }
    public interface ILiteralNode : INode
    {
        Uri DataType { get; }
        string Language { get; }
        string Value { get; }
    }
    public enum NodeType
    {
        // Blank,
        Uri, Literal
        //, GraphLiteral, Variable
    }
    public class Triple
    {
        public extern Triple(INode subj, INode pred, INode obj);
        public extern IGraph Graph { get; }
        public extern INode Subject { get; }
        public extern INode Predicate { get; }
        public extern INode Object { get; }
    }
    public interface IGraph
    {
        bool IsEmpty { get; }
        INamespaceMapper NamespaceMap { get; }
        IEnumerable<INode> Nodes { get; }
        // Создатели
        IUriNode CreateUriNode();
        ILiteralNode CreateLiteralNode(string value);
        ILiteralNode CreateLiteralNode(string value, Uri datatype);
        ILiteralNode CreateLiteralNode(string value, string lang);
    }
    public interface INamespaceMapper
    {
        IEnumerable<string> Prefixes { get; }
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
    // Парсеры
    public class TurtleParser
    {
        public extern void Load(IGraph g, string filename);
    }
    public class TripleStore
    {
        //SparqlQueryParser sparqlparser = new SparqlQueryParser();
        //SparqlQuery query = sparqlparser.ParseFromString("CONSTRUCT { ?s ?p ?o } WHERE { { GRAPH ?g { ?s ?p ?o } } UNION { ?s ?p ?o } }");
        //results = store.ExecuteQuery(query);
        //if (results is IGraph)
    }
    public class SparqlQueryParser
    {
    }
    public class SparqlQuery
    {
    }
    public class SparqlResultSet
    {
    }
}
