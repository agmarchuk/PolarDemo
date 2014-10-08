using System;
using System.Collections.Generic;
using System.Linq;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public class SparqlFilter : ISparqlWhereItem
    {
        public Func<SparqlResult, dynamic> Filter;
        public Func<IEnumerable<Action>> SelectVariableValuesOrFilter { get; set; }
        public SparqlResultSet resultSet;

        public SparqlFilter(SparqlResultSet resultSet)
        {
            this.resultSet = resultSet;
        }
        public void CreateNodes(PolarTripleStore store)
        {
            SelectVariableValuesOrFilter = ()=> Filter(resultSet.VariablesValues) ? Enumerable.Repeat<Action>(() => { },1) : Enumerable.Empty<Action>();
        }
    }
}