using System;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class SparqlUriNode :SparqlNode
    {
        public Uri Uri;
        internal override void CreateNode(PolarTripleStore store)
        {
            Value = store.GetUriNode(Uri);
        }
    }
}