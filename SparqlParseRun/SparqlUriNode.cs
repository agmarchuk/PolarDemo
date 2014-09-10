using System;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    public class SparqlUriNode :SparqlNode
    {
        public Uri Uri;
        internal override void CreateNode(IStore store)
        {
            Value = store.GetUriNode(Uri);
        }
    }
}