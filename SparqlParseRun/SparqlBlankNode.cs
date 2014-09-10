using System;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    public class SparqlBlankNode : SparqlNode
    {
        public SparqlBlankNode(string s)
        {
            throw new NotImplementedException();
        }

        internal override void CreateNode(IStore store)
        {
            base.CreateNode(store);
        }
    }
}