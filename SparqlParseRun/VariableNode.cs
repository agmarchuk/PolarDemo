using System;
using RdfInMemoryCopy;

namespace SparqlParseRun
{   
    public class VariableNode :SparqlNode
    {
        public bool isNew;
        //public NodeType NodeType { get { return NodeType.Variable; } }
        //public INode Value;
        public int index;
        public IGraph Graph { get { return Value == null ? null : Value.Graph; } }
        public object Clone()
        {
            throw new NotImplementedException();
        }

        public string Name;
        internal override void CreateNode(IStore store)
        {
            
        }
    }
}