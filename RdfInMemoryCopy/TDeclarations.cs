using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public interface INode:ICloneable
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
        private IGraph g;
        private INode subj, pred, obj;
        public Triple(INode subj, INode pred, INode obj) 
        { 
            this.subj = subj; this.pred = pred; this.obj = obj;
            this.g = subj.Graph;
            if (!g.Equals(pred.Graph) || !g.Equals(obj.Graph)) throw new Exception("Err in Triple constructor");
        }
        public IGraph Graph { get { return g; } }
        public INode Subject { get { return subj; } }
        public INode Predicate { get { return pred; } }
        public INode Object { get { return obj; } }
    }
    public interface IGraph
    {
        bool IsEmpty { get; }
        INamespaceMapper NamespaceMap { get; }
        //IEnumerable<INode> Nodes { get; } // Пока вредне не нужен...
        // Создатели
        IUriNode CreateUriNode(string uriOrQname);
        ILiteralNode CreateLiteralNode(string value); // Это когда надо разбираться с текстом до точки
        ILiteralNode CreateLiteralNode(string value, Uri datatype);
        ILiteralNode CreateLiteralNode(string value, string lang);
        // Очистка, добавление триплетов, построение графа
        void Clear();
        bool Assert(Triple t);
        void Build(); // Это действие отсутствует в стандарте dotnetrdf!
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
    //public class TurtleParser
    //{
    //    public extern void Load(IGraph g, string filename);
    //}
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
