using System;
using System.Collections.Generic;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    public interface ISparqlWhereItem
    {
        Func<IEnumerable<Action>> SelectVariableValuesOrFilter { get; }
        void CreateNodes(IStore store);
    }
}