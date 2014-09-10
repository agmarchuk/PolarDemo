using System;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    public class SparqlNode
    {
        public INode Value;
        internal virtual void CreateNode(IStore store)
        {
            throw new NotImplementedException();
        }
    }
}