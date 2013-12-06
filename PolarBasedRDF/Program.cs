
using System;
using System.IO;
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
            RDFTripletsByPolarEngine graph = new RDFTripletsByPolarEngine(new DirectoryInfo(path));
            bool toload = true;
            if (toload)
            {
                var freebase = "F:\\freebase-rdf-2013-02-10-00-00.nt2";
               // XElement db = XElement.Load(path + "0001.xml");
             //   graph.StartFillDb();
                graph.Load(10000000,freebase);
               // graph.FinishFillDb();
            }

            //var query = graph.SearchByName("марчук");
            //foreach (XElement rec in query)
            //{
            //    Console.WriteLine(rec.ToString());
            //}
        }
    }
}
