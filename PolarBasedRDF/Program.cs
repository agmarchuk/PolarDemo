
using System;
using System.Xml.Linq;
using PolarBasedEngine;

namespace PolarBasedRDF
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start");
            PolarBasedRdfGraph graph = new PolarBasedRdfGraph(path);
            bool toload = false;
            if (toload)
            {
                XElement db = XElement.Load(path + "0001.xml");
                graph.StartFillDb();
                graph.Load(db.Elements());
                graph.FinishFillDb();
            }

            var query = graph.SearchByName("марчук");
            foreach (XElement rec in query)
            {
                Console.WriteLine(rec.ToString());
            }
        }
    }
}
