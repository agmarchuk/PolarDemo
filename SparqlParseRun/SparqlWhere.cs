using System;
using System.Collections.Generic;
using System.Linq;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class SparqlWhere : ISparqlWhereItem
    {
        internal int startVariable = 0, endIndexVariable=0;
        private SparqlNode graphUri;
        public List<ISparqlWhereItem> Triples = new List<ISparqlWhereItem>();

        public virtual Func<IEnumerable<Action>> SelectVariableValuesOrFilter
        {
            get
            {
                return () =>
                {
                    IEnumerable<Action> results = Triples.Aggregate(Enumerable.Repeat(new Action(() => { }), 1), (current, item) => current.SelectMany(setVarOrFilter =>
                    {
                        setVarOrFilter();
                        return item.SelectVariableValuesOrFilter();
                    }));
                    return results;
                };
            } 
        }

        public virtual void CreateNodes(PolarTripleStore store)
        {
           if(graphUri!=null)
               graphUri.CreateNode(store);
            foreach (var sparqlWhereItem in Triples)
                sparqlWhereItem.CreateNodes(store);
        }
        public virtual void Run(SparqlResultSet resultSet)
        {   
            if (resultSet.ResultType == ResultType.Ask)
            {
                resultSet.AnyResult = SelectVariableValuesOrFilter().Any();
                return;
            }
            foreach (Action setLastVariableOrFilter in SelectVariableValuesOrFilter())
            {
                setLastVariableOrFilter();
                resultSet.Results.Add(resultSet.VariablesValues);
            }

        }

    
    }
}