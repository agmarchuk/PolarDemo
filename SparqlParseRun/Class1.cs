using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    class Class1
    {
        static void Main(string[] args)
        
        {



            SparqlQuery sparqlQuery = new SparqlQueryParser().Parse(File.ReadAllText(
                @"C:\Users\Admin\Source\Repos\PolarDemo\SparqlParser\sparql data\queries\with constants\9.rq"));

            var turtleFile = @"C:\deployed\1M.ttl";

            SGraph sGraph = new SGraph(@"..\..\..\Databases\", new Uri("bsbm1m"));
           new TurtleParser().LoadTriplets(sGraph, turtleFile);

         

            var sStore = new SStore(sGraph);
            File.WriteAllText(@"C:\Users\Admin\Source\Repos\PolarDemo\SparqlParser\sparql data\queries\with constants\res.txt", sparqlQuery.Run(sStore).ToString());

        }
    }


}
