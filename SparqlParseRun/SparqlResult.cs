using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TripleStoreForDNR;


namespace SparqlParseRun
{
    public class SparqlResult : IEnumerable<INode>,      IEnumerable<string>  , IEquatable<SparqlResult>
    {
        //public bool result;
        private readonly INode[] row;
        private readonly string[] variablesNames;
       
        internal SparqlResult(SparqlResult sparqlResult)
        {
            row = sparqlResult.row;
            Store = sparqlResult.Store;
            variablesNames = sparqlResult.variablesNames;
        }

        public SparqlResult(ICollection<VariableNode> variableNodes, ICollection<string> keyCollection, IStore store)
        {
            row = variableNodes.Select(varNode=>varNode.Value).ToArray();
            variablesNames = keyCollection.ToArray();
            Store = store;
        }

        public SparqlResult(IEnumerable<INode> rowNodes, IEnumerable<string> keyCollection, IStore store)
        {
            row = rowNodes.ToArray();
            variablesNames = keyCollection.ToArray();
            Store = store;
        }


        public INode this[int index]
        {
            get { return row[index]; }
            set
            {
                row[index]= value;
            }
        }

       
     

        IEnumerator<INode> IEnumerable<INode>.GetEnumerator()
        {
            return (row.Cast<INode>()).GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return row.GetEnumerator();
        }
        public IEnumerable<string> Variables { get { return variablesNames; }}
        public int Count { get { return row.Length; } }
        public IStore Store { get; set; }                                       

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return row.Select(node => node.ToString()).GetEnumerator();
        }


        public bool Equals(SparqlResult other)
        {
           // return ((IStructuralComparable) row).CompareTo(other.row, Comparer<INode>.Default)==0;
           return StructuralComparisons.StructuralComparer.Compare(row, other.row)==0;
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(row);
        }
    }
}