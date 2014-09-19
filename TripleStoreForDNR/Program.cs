using System;
using VDS.RDF;
using VDS.RDF.Writing;

namespace TripleStoreForDNR
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Start TripleStore");

          //VDS.RDF.  IGraph g = new Graph();

          //  var uri = UriFactory.Create("http://example.org/g/12345");
          //  VDS.RDF.IUriNode dotNetRDF = g.CreateUriNode(UriFactory.Create("http://www.dotnetrdf.org"));
          //  VDS.RDF.IUriNode says = g.CreateUriNode(UriFactory.Create("http://example.org/says"));
          //  VDS.RDF.ILiteralNode helloWorld = g.CreateLiteralNode("Hello World");
          //  VDS.RDF.ILiteralNode bonjourMonde = g.CreateLiteralNode("Bonjour tout le Monde", "fr");

          //  g.Assert(new VDS.RDF.Triple(dotNetRDF, says, helloWorld));
          //  g.Assert(new VDS.RDF.Triple(dotNetRDF, says, bonjourMonde));

          //  foreach (VDS.RDF.Triple t in g.Triples)
          //  {
          //      Console.WriteLine(t.ToString());
          //  }

          //  NTriplesWriter ntwriter = new NTriplesWriter();
          //  ntwriter.Save(g, "HelloWorld.nt");

          //  RdfXmlWriter rdfxmlwriter = new RdfXmlWriter();
          //  rdfxmlwriter.Save(g, "HelloWorld.rdf");

           //TripleStore store = new TripleStore();
            //Graph gg = new Graph();
            //TurtleParser ttlparser = new TurtleParser();
            //ttlparser.Load(gg, @"D:\home\FactographDatabases\dataset.ttl");
            //store.Add(gg);
            //store.SaveToFile(@"D:\home\FactographDatabases\dataset_db.bin");
            PolarTripleStore store=new PolarTripleStore();
            SGraph graph = new SGraph(@"..\..\..\Databases\", new Uri("https://bsbm1"));
            string turtleFile = @"C:\deployed\1M.ttl";
            new TurtleParser().LoadTriplets(graph, turtleFile);

            store.Add(graph);
        }                                                     
    }
}
