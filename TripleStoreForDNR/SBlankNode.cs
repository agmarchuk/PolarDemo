using System;

namespace TripleStoreForDNR
{
    public class SBlankNode : INode
    {
        private SGraph g;

        public IGraph Graph
        {
            get { return g; }
        }


        public NodeType NodeType
        {
            get { return NodeType.Blank; }
        }

        private long ocode;
        

        public long Code
        {
            get { return ocode; }
        }

        public SBlankNode(string name, SGraph graph)
        {

            this.Name = name;
            this.g = graph as SGraph;
            ocode = graph.namespaceMaper.coding.GetCode(name);


        }

        public SBlankNode(long code, SGraph graph)
        {
            ocode = code;
            this.g = graph;
        }
        public string Name { get; set; }

   
    }
}
