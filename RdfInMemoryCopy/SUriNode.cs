using System;

namespace RdfInMemoryCopy
{
    public class SUriNode : IUriNode   ,IComparable<SUriNode>  ,IComparable
    {
        public NodeType NodeType
        {
            get { return RdfInMemoryCopy.NodeType.Uri; }
        }
        private SGraph graph;
        private readonly int code;
        private Uri uriOrQname;       
        public IGraph Graph
        {
            get { return graph; }
        }


        public SUriNode(int code, SGraph sGraph)
        {
            // TODO: Complete member initialization
            this.code = code;
            this.graph = sGraph;
        }

      

        public Uri Uri
        {
            get
            {
                return uriOrQname ?? (code == int.MaxValue
                    ? null
                    : (uriOrQname = new Uri(graph.namespaceMaper.coding.GetName(code))));
            }
        }

        public int Code { get { return code; } }
        public int CompareTo(SUriNode other)
        {
            return StringComparer.InvariantCulture.Compare(this.Uri.ToString(), other.Uri.ToString());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SUriNode)) return false;
            return (obj as SUriNode).Code == code;
        }

        public override int GetHashCode()
        {
            return code.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            return code.CompareTo(((SUriNode) obj).Code);
        }

        public override string ToString()
        {
            return "<" + Uri.AbsoluteUri + ">";
        }
    }
}
