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
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            string[] ids = new[]
            {
                "svet_100616111408_10844",
                "pavl_100531115859_2020",
                "piu_200809051791",
                "pavl_100531115859_6952",
                "svet_100616111408_10864",
                "w20090506_svetlana_5727",
                "piu_200809051742",
                "p0013313",
                "p0011098",
                "svet_100616111408_14354"
            };


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

            //var query = graph.SearchByName("марчук");
            //foreach (XElement rec in query) 
            //{
            //    Console.WriteLine(rec.ToString());
            //}
            watch.Start();
            var item = graph.GetItemByIdBasic("w20070417_5_8436", true);
            watch.Stop();
            Console.WriteLine(item.ToString());
            Console.WriteLine(watch.ElapsedTicks);

            watch.Restart();
            foreach (string id in ids)
            {
                graph.GetItemByIdBasic(id, true);
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedTicks);

        }
    }
}
