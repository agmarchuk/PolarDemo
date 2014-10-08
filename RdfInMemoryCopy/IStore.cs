using System;
using System.Collections.Generic;

namespace RdfInMemoryCopy
{
    public interface IStore
    {                
        bool Contains(Triple  triple);
        IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subjectNode, INode predicateNode);
        IEnumerable<Triple> GetTriplesWithSubjectObject(INode subjectNode, INode objectNode);
        IEnumerable<Triple> GetTriplesWithSubject(INode subjectNode);
        IEnumerable<Triple> GetTriplesWithPredicateObject(INode predicateNode, INode objectNode);
        IEnumerable<Triple> GetTriplesWithPredicate(INode predicateNode);
        IEnumerable<Triple> GetTriplesWithObject(INode objectNode);
        IEnumerable<Triple> GetTriples();

       // IEnumerable<Triple> Find(INode subj, INode pred, INode obj);

        ILiteralNode CreateLiteralNode(Uri type, object literal, string lang);
     

       INode CreateBlankNode(string p);

       

       INode CreateUriNode(string p);

        INamespaceMapper NamespaceMaper { get; }
        IUriNode GetUriNode(Uri uri);
        ILiteralNode GetLiteralNode(Uri type, dynamic content, string lang);
        IGraph CreateGraph();
    }
}