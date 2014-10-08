using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;


namespace TripleStoreForDNR
{
    public class PolarTripleStore : IStorageProvider, IQueryableStorage, IStore 
    {
        private IGraph defaultGraph;
        Dictionary<Uri,IGraph> namedGraphs=new Dictionary<Uri, IGraph>();

        public PolarTripleStore(IGraph defaultGraph)
        {
            this.defaultGraph = defaultGraph;
        }          

        public bool IsReady { get; private set; }
        public bool IsReadOnly { get; private set; }
        public IOBehaviour IOBehaviour { get; private set; }
        public bool UpdateSupported { get; private set; }
        public bool DeleteSupported { get; private set; }
        public bool ListGraphsSupported { get; private set; }
        public void LoadGraph(VDS.RDF.IGraph g, Uri graphUri)
        {
            throw new NotImplementedException();
        }

        public void LoadGraph(VDS.RDF.IGraph g, string graphUri)
        {               IGraph getGraph;
            if ( string.IsNullOrWhiteSpace(graphUri))
                getGraph = defaultGraph;
            else
            {
                if (!namedGraphs.TryGetValue(g.BaseUri, out getGraph))
                    return;
            }
            Console.WriteLine("getted graph");
           

            int ntriples = 0;
            foreach (var triple in getGraph.GetTriples())
            {
                if (ntriples % 100000 == 0) Console.Write("w{0} ", ntriples++ / 100000);
                g.Assert(new VDS.RDF.Triple(g.CreateUriNode(((IUriNode)triple.Subject).Uri),
                    g.CreateUriNode(((IUriNode)triple.Predicate).Uri),
                    triple.Object.NodeType ==NodeType.Literal
                        ? (VDS.RDF.INode) g.CreateLiteralNode(triple.Object.ToString())
                        : g.CreateUriNode(((IUriNode)triple.Object).Uri)));
            }                        
          
        }

        public void LoadGraph(IRdfHandler handler, Uri graphUri)
        {
            throw new NotImplementedException();
        }

        public void LoadGraph(IRdfHandler handler, string graphUri)
        {
            throw new NotImplementedException();
        }

        public void SaveGraph(VDS.RDF.IGraph g)
        {
            IGraph newGraph;
            if (g.BaseUri == null)
                newGraph = defaultGraph;
            else
            {
                if (!  namedGraphs.TryGetValue(g.BaseUri, out newGraph))
                    namedGraphs.Add(g.BaseUri, newGraph = new SGraph("TODO", g.BaseUri));
            }
            Console.WriteLine("added graph");
            newGraph.Clear();

            int ntriples = 0;
            foreach (var triple in g.Triples)
            {
                if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples++ / 100000);
                newGraph.Assert(new Triple(newGraph.CreateUriNode("<" + triple.Subject.ToString() + ">"),
                    newGraph.CreateUriNode("<" + triple.Predicate.ToString() + ">"),
                    triple.Object.NodeType == VDS.RDF.NodeType.Literal
                        ? (INode)newGraph.CreateLiteralNode(triple.Object.ToString())
                        : newGraph.CreateUriNode("<" + triple.Object.ToString() + ">")));
            }
            newGraph.Build();
        }

        public void UpdateGraph(Uri graphUri, IEnumerable<VDS.RDF.Triple> additions, IEnumerable<VDS.RDF.Triple> removals)
        {
            throw new NotImplementedException();
        }

        public void UpdateGraph(string graphUri, IEnumerable<VDS.RDF.Triple> additions, IEnumerable<VDS.RDF.Triple> removals)
        {
            throw new NotImplementedException();
        }

        public void DeleteGraph(Uri graphUri)
        {
            throw new NotImplementedException();
        }

        public void DeleteGraph(string graphUri)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Uri> ListGraphs()
        {
            throw new NotImplementedException();
        }

        public IStorageServer ParentServer { get; private set; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object Query(string sparqlQuery)
        {
            throw new NotImplementedException();
        }

        public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery)
        {
            throw new NotImplementedException();
        }
        public bool Contains(Triple triple)
        {
            return defaultGraph.Contains((IUriNode)triple.Subject, (IUriNode)triple.Predicate, triple.Object);
        }


        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subjectNode, INode predicateNode)
        {
            return defaultGraph.GetTriplesWithSubjectPredicate(subjectNode, predicateNode);
        }

        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subjectNode, INode objectNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubject(INode subjectNode)
        {
            return defaultGraph.GetTriplesWithSubject(subjectNode);
        }

        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode predicateNode, INode objectNode)
        {
            return defaultGraph.GetTriplesWithPredicateObject(predicateNode, objectNode);
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(INode predicateNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithObject(INode objectNode)
        {
            return defaultGraph.GetTriplesWithObject(objectNode);
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
            return defaultGraph.CreateUriNode(p);
        }

        public INamespaceMapper NamespaceMaper { get { return defaultGraph.NamespaceMap; } }

        public IUriNode GetUriNode(Uri uri)
        {
            return defaultGraph.GetUriNode(uri); //).FirstOrDefault(ln => ln != null);
        }

        public ILiteralNode GetLiteralNode(Uri type, dynamic content, string lang)
        {
            var literalNode = defaultGraph.GetLiteralNode(content, lang, type);
            return literalNode;
            //TODO if(literalNode==null)
        }

        public IGraph CreateGraph()
        {
            return new SGraph("TODO", null){};
        }
    }
}
