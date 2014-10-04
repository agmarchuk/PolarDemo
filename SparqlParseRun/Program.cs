using System;
using System.IO;
using TripleStoreForDNR;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace SparqlParseRun
{
    class Program
    {
        static void Main(string[] args)
        
        {



            SparqlQuery sparqlQuery = new SparqlQueryParser().Parse(File.ReadAllText(
                @"C:\Users\Admin\Source\Repos\PolarDemo\SparqlParser\sparql data\queries\with constants\9.rq"));

            var turtleFile = @"C:\deployed\1M.ttl";
                       
          //  SGraph sGraph = new SGraph(@"..\..\..\Databases\", new Uri("bsbm1m"));
            Graph graph = new Graph();

            new TurtleParser().Load(graph,turtleFile);
         


           //var sStore = new PolarTripleStore(sGraph);
           //   sStore.SaveGraph(graph);
           // File.WriteAllText(@"C:\Users\Admin\Source\Repos\PolarDemo\SparqlParser\sparql data\queries\with constants\res.txt", sparqlQuery.Run(sStore).ToString());

        }
    }


}
