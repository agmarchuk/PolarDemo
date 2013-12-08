
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
            bool toload = false;
            if (toload)
            {
                // var freebase = "F:\\freebase-rdf-2013-02-10-00-00.nt2";
                string db = path + "0001.xml";
                graph.Load(10000000, db);
            }
            //string result= graph.GetItem("ns:m.0102c1j");
            //Console.WriteLine(result);

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
