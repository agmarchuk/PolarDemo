
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
            Console.WriteLine("Start");
            RDFTripletsByPolarEngine graph = new RDFTripletsByPolarEngine(new DirectoryInfo(path));
            foreach (var count in new[] { 3 * 1000 * 1000 })//3 * 1000 * 1000, 
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                using (StreamWriter log = new StreamWriter("../../log.txt", true))
                    log.WriteLine("start " + count);
                bool toload = true;
                if (toload)
                {
                    var db = "F:\\freebase-rdf-2013-02-10-00-00.nt2";
                    //string db = path + "0001.xml";
                    watch.Restart();
                    graph.Load(count, db);
                    watch.Stop();
                    using (StreamWriter log = new StreamWriter("../../log.txt", true))
                        log.WriteLine("load db " + watch.Elapsed.Ticks);
                    
                    watch.Restart();
                    graph.LoadIndexes();
                    watch.Stop();
                    using (StreamWriter log = new StreamWriter("../../log.txt", true))
                        log.WriteLine("bufferBYtes= max" + "load indexes " + watch.Elapsed.Ticks.ToString());
                    foreach (var bufferSize in new[] {1000, 400*1000, 1000*1000, 400*1000*1000})
                    {
                        PaEntry.bufferBytes = bufferSize;
                        watch.Restart();
                        graph.LoadIndexes();
                        watch.Stop();
                        using (StreamWriter log = new StreamWriter("../../log.txt", true))
                            log.WriteLine("bufferBYtes=" + bufferSize + "load indexes " + watch.Elapsed.Ticks.ToString());
                    }
                }
                continue;


                string[] ids = new[]
                {
                    "ns:m.0102c1j",
                    "ns:m.03cr17b",
                    "ns:m.03d89qc",
                    "ns:m.03wtp5x",
                    "ns:m.03f10_0",
                    "ns:m.03c0_rb",
                    "ns:m.03c0_r6",
                    "ns:m.03y79mn",
                    "ns:m.051klq",
                    "ns:m.0dfzyd8",
                    "ns:m.051b_wb",
                    "ns:m.0hht9xj",
                    "ns:m.03cr176",
                    "ns:m.03d8bgv",
                    "ns:m.0bmcz6s",
                    "ns:m.03y79nh",
                    "ns:m.0gfs6fw",
                    "ns:m.0hht9xn",
                    "ns:m.051kly",
                    "ns:m.0bjbqzq",
                    "ns:m.03d8bgz",
                    "ns:m.03cr17b",
                    "ns:m.03wt5px",
                    "ns:m.0bmcz6s",
                    "ns:m.06f9t4",
                    "ns:m.0bjh9f"
                    //    "svet_100616111408_10844",
                    //    "pavl_100531115859_2020",
                    //    "piu_200809051791",
                    //    "pavl_100531115859_6952",
                    //    "svet_100616111408_10864",
                    //    "w20090506_svetlana_5727",
                    //    "piu_200809051742",
                    //    "p0013313",
                    //    "p0011098",
                    //    "svet_100616111408_14354"

                };
                //var query = graph.SearchByName("марчук");
                //foreach (XElement rec in query) 
                //{
                //    Console.WriteLine(rec.ToString());
                //}
                watch.Restart();
                var item = graph.GetItemByIdBasic(ids.First(), true);
                watch.Stop();
                //Console.WriteLine(item.ToString());
                //Console.WriteLine(watch.ElapsedTicks);
                using (StreamWriter log = new StreamWriter("../../log.txt", true))
                    log.WriteLine("item.ToString()" + watch.Elapsed.Ticks);

                watch.Restart();
                foreach (string id in ids)
                {
                    graph.GetItemByIdBasic(id, true);
                }
                watch.Stop();
                using (StreamWriter log = new StreamWriter("../../log.txt", true))
                    log.WriteLine("many ids get time = " + watch.Elapsed.Ticks);
            }
        }
    }
}