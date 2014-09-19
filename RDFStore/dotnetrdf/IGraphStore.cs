using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RdfInMemory;

namespace RDFStore.dotnetrdf
{

    internal interface IGraphStore
    {
        IEnumerable<Uri> GraphUris { get; }

        IEnumerable<IGraph> Graphs { get; }

        IGraph this[Uri u] { get; }

        bool HasGraph(Uri u);

        bool Add(IGraph g);

        bool Add(IGraph g, Uri graphUri);

        bool AddTriple(Uri graphUri, Triple t);

        bool AddQuad(Quad q);

        bool Copy(Uri srcUri, Uri destUri);

        bool Move(Uri srcUri, Uri destUri);

        bool Remove(Uri u);

        //Get all Triples in the Store
        IEnumerable<Triple> Triples { get; }

        //Find matching triples
        IEnumerable<Triple> Find(INode subj, INode pred, INode obj);

        //Get all Quads in the store
        IEnumerable<Quad> Quads { get; }

        //Find all matching quads
        IEnumerable<Quad> Find(INode graph, INode subj, INode pred, INode obj);

        //Is a Triple found anywhere in the store
        bool ContainsTriple(Triple t);

        //Is the Triple contained in the given Graphs
        bool ContainsTriple(IEnumerable<Uri> graphUris, Triple t);

        //Does a Quad exist in the store
        bool ContainsQuad(Quad q);
    }

    internal class Quad
    {
        //TODO
    }
}
