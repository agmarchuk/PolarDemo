using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    public class SparqlResultSet   :IEnumerable<SparqlResult>   , IEnumerable<string>
    {
        public List<SparqlResult> Results =new List<SparqlResult>();
        public bool AnyResult;
        public IGraph GraphResult;
        internal ResultType ResultType;

        internal IStore Store { get; set; }
        
        internal Dictionary<string, VariableNode> Variables = new Dictionary<string, VariableNode>();

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {                                                     
           return this.Select<SparqlResult, string>(res => res.ToString()).GetEnumerator();
        }

        IEnumerator<SparqlResult> IEnumerable<SparqlResult>.GetEnumerator()
        {
            return Results.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public SparqlResultSet()
        {
         
        }

        public SparqlResultSet(ISet<string> set)
        {
            throw new NotImplementedException();
        }

        public SparqlResultSet(IEnumerable<string> values, IStore store)
        {
            Store = store;
        }

        public SparqlResultSet(SparqlResultSet from, List<string> subVariables)
        {
            Store = from.Store;
            Variables = subVariables.Where(s => from.Variables.ContainsKey(s)).ToDictionary(s => s, s => from.Variables[s]);
            Results = new List<SparqlResult>(from.Results.Select(result => new SparqlResult(subVariables.Where(subVar => from.Variables.ContainsKey(subVar)).Select(subVar => result[from.Variables[subVar].index]), subVariables, Store)));
            int ii = 0;
            foreach (var variableNode in Variables)
                variableNode.Value.index = ii++;
        }
        public void ResetDiapason(int parametersStartIndex, int endIndex)
        {
            for (int i = parametersStartIndex; i < endIndex; i++)
            {
                VariableNode variableNode = Variables.Values.ElementAt(i);
                variableNode.Value = null;
                variableNode.isNew = true;
            }
        }

        public SparqlResult VariablesValues
        {
            get
            {   
                return new SparqlResult(Variables.Values, Variables.Keys, Store);
            }
        }

        public void DistinctReduse(bool isDistinct, bool isReduce)
        {

            if (isDistinct)
            {
                Results = Results.Distinct().ToList();
            }
            if(isReduce) throw new NotImplementedException();
        }

        public override string ToString()
        {
            switch (ResultType)
            {
                case ResultType.Describe:
                case ResultType.Construct:
                    return GraphResult.ToString();
                case ResultType.Select:
                 return NamespaceMapCoding.NodesToString(Results, Store.NamespaceMaper);
                case ResultType.Ask:
                   return AnyResult.ToString();
                   
                default:
                    throw new ArgumentOutOfRangeException();
            }
          
        }
    }

    internal enum ResultType
    {
        Describe, Select, Construct, Ask
    }

    
}