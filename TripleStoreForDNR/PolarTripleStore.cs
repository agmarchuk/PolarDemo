using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;

namespace TripleStoreForDNR
{
    class PolarTripleStore : ITripleStore
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool Add(IGraph g)
        {
            Console.WriteLine("added graph"); 
            GraphAdded(this, new TripleStoreEventArgs(this, g));
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
        public BaseGraphCollection Graphs { get; private set; }
        public IEnumerable<Triple> Triples { get; private set; }

        public IGraph this[Uri graphUri]
        {
            get { throw new NotImplementedException(); }
        }

        public event TripleStoreEventHandler GraphAdded;
        public event TripleStoreEventHandler GraphRemoved;
        public event TripleStoreEventHandler GraphChanged;
        public event TripleStoreEventHandler GraphCleared;
        public event TripleStoreEventHandler GraphMerged;
    }
}
