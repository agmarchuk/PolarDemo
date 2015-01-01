using System;
using System.Collections.Generic;
using TripleStoreForDNR;

namespace SparqlParseRun
{
    public interface ISparqlWhereItem
    {
        Func<IEnumerable<Action>> SelectVariableValuesOrFilter { get; }
        void CreateNodes(PolarTripleStore store);
    }
}