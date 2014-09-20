using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;


namespace TripleStoreForDNR
{
    internal class PolarTripleStore : IStorageProvider
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
    }
}
