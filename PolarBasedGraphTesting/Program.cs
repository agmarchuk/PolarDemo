using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using PolarDB;
using PolarBasedEngine;

namespace PolarBasedGraphTesting
{
    public class Program
    {
        public static void Main(string[] args)
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
