using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RdfInMemoryCopy
{
    class SUriNode : IUriNode
    {
        public NodeType NodeType
        {
            get { return RdfInMemoryCopy.NodeType.Uri; }
        }
        private SGraph graph;
        private int code;
        private string uriOrQname;       
        public IGraph Graph
        {
            get { return graph; }
        }
        public SUriNode(string uriOrQName, SGraph sGraph)
        {
            code = uriOrQName.GetHashCode();
            this.graph = sGraph;
        }

        public SUriNode(int code, SGraph sGraph)
        {
            // TODO: Complete member initialization
            this.code = code;
            this.graph = sGraph;
        }

        public Uri Uri
        {
            get { throw new NotImplementedException(); }
        }
        internal int Code { get { return code; } }
    }
}
