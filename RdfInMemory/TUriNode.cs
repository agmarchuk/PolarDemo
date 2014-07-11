using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public class TUriNode : IUriNode
    {
        private IGraph g;
        private int code;
        public IGraph Graph { get { return g; } }
        public NodeType NodeType { get { return RdfInMemory.NodeType.Uri; } }
        internal TUriNode(string uri, IGraph g)
        {
            this.g = g;
            this.code = uri.GetHashCode(); // Отладочное решение
        }
        internal TUriNode(int code, IGraph g)
        {
            this.g = g;
            this.code = code;
        }
        public Uri Uri { get { return new Uri("http://test/" + code); } }
        internal int Code { get { return code; } }
    }
}
