using System;

namespace RdfInMemoryCopy
{
    public interface IUriNode : INode { Uri Uri { get; } }

    public enum NodeType
    {
        // Blank,
        Uri, Literal
        //, GraphLiteral, Variable
        ,
        Variable
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
