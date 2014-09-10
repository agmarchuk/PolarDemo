using System;
using System.Collections.Generic;
using System.Linq;

namespace RdfInMemoryCopy
{
    public class SStore:IStore
    {
        private List<IGraph> graphs;

        private IGraph defualutGraph;

        public SStore(params IGraph[] graphs)
        {
            this.graphs = graphs.ToList();
            this.graphs.Add(defualutGraph=new RAMGraph());
        }

        public bool Contains(Triple  triple)
        {
            return graphs.Any(g=>g.Contains((IUriNode) triple.Subject, (IUriNode) triple.Predicate, triple.Object));
        }
        

        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subjectNode, INode predicateNode)
        {
            return graphs.SelectMany(graph =>graph.GetTriplesWithSubjectPredicate(subjectNode, predicateNode));
        }

        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subjectNode, INode objectNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubject(INode subjectNode)
        {
            return graphs.SelectMany(graph => graph.GetTriplesWithSubject(subjectNode));
        }

        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode predicateNode, INode objectNode)
        {
            return graphs.SelectMany(graph => graph.GetTriplesWithPredicateObject(predicateNode, objectNode));
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(INode predicateNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithObject(INode objectNode)
        {
            return graphs.SelectMany(graph => graph.GetTriplesWithObject(objectNode));
        }

        public IEnumerable<Triple> GetTriples()
        {
            throw new NotImplementedException();
        }

        public ILiteralNode CreateLiteralNode(Uri type, object literal, string lang)
        {
            throw new NotImplementedException();
        }



        public INode CreateBlankNode(string p)
        {
            throw new NotImplementedException();
        }


        public INode CreateUriNode(string p)
        {
            return defualutGraph.CreateUriNode(p);
        }

        public INamespaceMapper NamespaceMaper { get { return defualutGraph.NamespaceMap; }  }
  
        public IUriNode GetUriNode(Uri uri)
        {
            return graphs.Select(graph => graph.GetUriNode(uri)).FirstOrDefault(ln => ln != null);
        }

        public ILiteralNode GetLiteralNode(Uri type, dynamic content, string lang)
        {
            return graphs.Select(graph =>  graph.GetLiteralNode(content, lang, type)).FirstOrDefault(ln=>ln!=null);
        }

        public IGraph CreateGraph()
        {
            return new RAMGraph(){NamespaceMap=NamespaceMaper};
        }
    }
}