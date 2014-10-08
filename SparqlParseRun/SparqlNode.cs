using System;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class SparqlNode
    {
        public INode Value;
        internal virtual void CreateNode(PolarTripleStore store)
        {
            throw new NotImplementedException();
        }
    }
}