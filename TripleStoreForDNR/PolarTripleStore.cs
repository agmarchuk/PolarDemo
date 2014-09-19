using System;
using System.Collections.Generic;
using VDS.RDF;


namespace TripleStoreForDNR
{
    internal class PolarTripleStore
    {
        private IGraph defaultGraph;

        public PolarTripleStore(IGraph defaultGraph)
        {
            this.defaultGraph = defaultGraph;
        }

        public void Dispose()
    {
        throw new NotImplementedException();
    }

    public bool Add(VDS.RDF.IGraph g)
    {
        Console.WriteLine("added graph");
        defaultGraph.Clear();

        int ntriples = 0;
        foreach (var triple in g.Triples)
        {
            if (ntriples % 100000 == 0) Console.Write("r{0} ", ntriples++ / 100000);
            defaultGraph.Assert(new Triple(defaultGraph.CreateUriNode("<"+triple.Subject.ToString()+">"),
                defaultGraph.CreateUriNode("<" + triple.Predicate.ToString() + ">"),

                triple.Object.NodeType == VDS.RDF.NodeType.Literal
                    ? (INode) defaultGraph.CreateLiteralNode(triple.Object.ToString())
                    : defaultGraph.CreateUriNode("<" + triple.Object.ToString() + ">")));
        }
        defaultGraph.Build();
        //  GraphAdded(this, new TripleStoreEventArgs(this, g));
        return false;
    }

    public bool Add(IGraph g, bool mergeIfExists)
    {
        throw new NotImplementedException();
    }

    public bool AddFromUri(Uri graphUri)
    {
        throw new NotImplementedException();
    }

    public bool AddFromUri(Uri graphUri, bool mergeIfExists)
    {
        throw new NotImplementedException();
    }

    public bool Remove(Uri graphUri)
    {
        throw new NotImplementedException();
    }

    public bool HasGraph(Uri graphUri)
    {
        throw new NotImplementedException();
    }

    public bool IsEmpty { get; private set; }
    public IGraph[] Graphs { get; private set; }
    public IEnumerable<Triple> Triples { get; private set; }

    public IGraph this[Uri graphUri]
    {
        get { throw new NotImplementedException(); }
    }

    //public event TripleStoreEventHandler GraphAdded;
    //public event TripleStoreEventHandler GraphRemoved;
    //public event TripleStoreEventHandler GraphChanged;
    //public event TripleStoreEventHandler GraphCleared;
    //public event TripleStoreEventHandler GraphMerged;
}
}
