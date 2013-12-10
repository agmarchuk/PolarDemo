
using System;
using System.IO;
using System.Linq;
using PolarDB;

namespace PolarBasedRDF
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";

            if (File.Exists(path + "test")) File.Delete(path + "test");
            PaCell test=new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)),  path+"test",false);
            test.Fill(new object[0]);
            test.Close();
            Random r=new Random();
            for (int i = 0; i < 10; i++)
            {
                test.Root.AppendElement((object) r.Next(10));
                Console.WriteLine(test.Root.Element(i).Get());
            }
            test.Root.SortByKey(o => (int)o);
            for (int i = 0; i < 10; i++)
                Console.WriteLine(test.Root.Element(i).Get());
            return;
            Console.WriteLine("Start");
            RDFTripletsByPolarEngine graph = new RDFTripletsByPolarEngine(new DirectoryInfo(path));
            bool toload = true;
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
