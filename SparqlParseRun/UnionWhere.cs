using System;
using System.Collections.Generic;
using System.Linq;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class UnionWhere : SparqlWhere
    {
        private List<SparqlWhere> alternatives;
        private SparqlResultSet sparqlResultSet;

        public UnionWhere(SparqlResultSet resultSet)
        {
          sparqlResultSet = resultSet;
         startVariable = resultSet.Variables.Count;  
        }            

        internal void Add(SparqlWhere sparqlWhere, int endIndex)
        {
            sparqlWhere.endIndexVariable = endIndex;
            if (alternatives == null)
                alternatives = new List<SparqlWhere> {sparqlWhere};
            else
                alternatives.Add(sparqlWhere);
        }

        public override Func<IEnumerable<Action>> SelectVariableValuesOrFilter
        {
            get
            {
                return () => alternatives.SelectMany(@where =>
                {
                    sparqlResultSet.ResetDiapason(startVariable, @where.endIndexVariable);
                    return @where.SelectVariableValuesOrFilter();
                });
            }
        }
        public override void CreateNodes(PolarTripleStore store)
        {
         
            foreach (var alternative in alternatives)
            {
                alternative.CreateNodes(store);
                sparqlResultSet.ResetDiapason(startVariable, alternative.endIndexVariable);
            }             
        }        
    }
}