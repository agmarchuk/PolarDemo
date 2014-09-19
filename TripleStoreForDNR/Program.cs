using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace TripleStoreForDNR
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Start TripleStore");

            IGraph g = new Graph();

            var uri = UriFactory.Create("http://example.org/g/12345");
            IUriNode dotNetRDF = g.CreateUriNode(UriFactory.Create("http://www.dotnetrdf.org"));
            IUriNode says = g.CreateUriNode(UriFactory.Create("http://example.org/says"));
            ILiteralNode helloWorld = g.CreateLiteralNode("Hello World");
            ILiteralNode bonjourMonde = g.CreateLiteralNode("Bonjour tout le Monde", "fr");

            g.Assert(new Triple(dotNetRDF, says, helloWorld));
            g.Assert(new Triple(dotNetRDF, says, bonjourMonde));

            foreach (Triple t in g.Triples)
            {
                Console.WriteLine(t.ToString());
            }

            NTriplesWriter ntwriter = new NTriplesWriter();
            ntwriter.Save(g, "HelloWorld.nt");

            RdfXmlWriter rdfxmlwriter = new RdfXmlWriter();
            rdfxmlwriter.Save(g, "HelloWorld.rdf");

            //TripleStore store = new TripleStore();
            //Graph gg = new Graph();
            //TurtleParser ttlparser = new TurtleParser();
            //ttlparser.Load(gg, @"D:\home\FactographDatabases\dataset.ttl");
            //store.Add(gg);
            //store.SaveToFile(@"D:\home\FactographDatabases\dataset_db.bin");
        }
    }
}
