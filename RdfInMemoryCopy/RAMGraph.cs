using System;
using System.Collections.Generic;
using System.Linq;

namespace RdfInMemoryCopy
{
    public class RAMGraph : IGraph
    {
        public bool IsEmpty { get; private set; }
        public Uri Uri { get; private set; }
        public INamespaceMapper NamespaceMap { get; set; }
        private List<Triple> triplets = new List<Triple>();
        public IUriNode CreateUriNode(string uriOrQname)
        {  
            return new RamUriNode(){Graph=this, Uri = new Uri(uriOrQname)};
        }

        public ILiteralNode CreateLiteralNode(string value)
        {
            throw new NotImplementedException();
        }

        public ILiteralNode CreateLiteralNode(string value, Uri datatype)
        {
            return new RamLiteralNode() {DataType = datatype, Graph = this, Value = value};
        }

        public ILiteralNode CreateLiteralNode(string value, string lang)
        {
            return new RamLiteralNode() { DataType = XmlSchema.XMLSchemaLangString, Graph = this, Value = value, Language = lang};
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Assert(Triple t)
        {
          triplets.Add(t);
            return true;
        }

        public void Build()
        {
          
        }

        public IEnumerable<Triple> GetTriplesWithObject(Uri u)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithObject(INode n)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(INode n)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(Uri u)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubject(INode n)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubject(Uri u)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriples()
        {
            throw new NotImplementedException();
        }

        public bool Contains(IUriNode subject, IUriNode predicate, INode @object)
        {
            throw new NotImplementedException();
        }

        public IUriNode GetUriNode(Uri uri)
        {
            throw new NotImplementedException();
        }

        public ILiteralNode GetLiteralNode(dynamic value, string lang, Uri type)
        {
            throw new NotImplementedException();
        }

        public IUriNode CreateUriNode(Uri uri)
        {
            return new RamUriNode() { Graph = this, Uri = uri };            
        }

        public override string ToString()
        {
            return NamespaceMapCoding.NodesToString(triplets.Select(triple => new INode []{triple.Subject, triple.Predicate, triple.Object}), NamespaceMap);
       
        }
    }

    public class RamLiteralNode : ILiteralNode
    {
        public NodeType NodeType { get{return NodeType.Literal;} }
        public IGraph Graph { get; internal set; }
        public Uri DataType { get; internal set; }
        public string Language { get; set; }
        public string Value { get; set; }
    }

    public class RamUriNode : IUriNode
    {
        public NodeType NodeType { get{ return NodeType.Uri;} }
        public IGraph Graph { get; set; }
        public Uri Uri { get; set; }
    }
}