using System;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class SparqlBlankNode : SparqlNode
    {
        public SparqlBlankNode(string s)
        {
            throw new NotImplementedException();
        }

        internal override void CreateNode(PolarTripleStore store)
        {
            base.CreateNode(store);
        }
    }
}