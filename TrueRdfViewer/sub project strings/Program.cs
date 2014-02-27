using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TrueRdfViewer
{
    public class Program
    {
        public static void Main0(string[] args)
        {

            string[] ids = new string[]
            {
                //"svet_100616111408_10844",
                //"pavl_100531115859_2020",
                //"piu_200809051791",
                //"pavl_100531115859_6952",
                //"svet_100616111408_10864",
                //"w20090506_svetlana_5727",
                //"piu_200809051742",
                //"p0013313",
                //"p0011098",
                //"svet_100616111408_14354"

                //"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature13",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature4",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature8",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature11",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature3",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19",
                //"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19",
            };

            Console.WriteLine("Start");
            string path = "../../../Databases/";
            TripleStore ts = new TripleStore(path);

            DateTime tt0 = DateTime.Now;

            foreach (string id in ids)
            {
                 XElement el = ts.GetItem(id);
                 Console.WriteLine(el.Elements().Count());
                 Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            return;

            bool toload = false;
            if (toload)
            {
                //ts.LoadXML(path + "0001.xml");
                //Console.WriteLine("LoadXML ok.");
                PolarDB.PaEntry.bufferBytes = 20000000;
                ts.LoadTurtle(@"D:\home\FactographDatabases\dataset\dataset10M.ttl");
                Console.WriteLine("LoadTurtle ok.");
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
            }

            //ts.CreateScale();
            //Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //ts.ShowScale();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            bool runpseudosoalqltests = true;
            if (runpseudosoalqltests)
            {
                //var query0 = BerlinTests.Query0(ts);
                var query1 = BerlinTests.Query1(ts);
                //var query1 = BerlinTests.Query1_1(ts);
                var query2 = BerlinTests.Query2(ts);
                //var query3 = BerlinTests.Query3(ts);
                var query3 = BerlinTests.Query3_1(ts);
                var query6 = BerlinTests.Query6(ts);
                tt0 = DateTime.Now;
                //Console.WriteLine(query0.Count());
                //Console.WriteLine("query0 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(query1.Count());
                Console.WriteLine("1 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(query2.Count());
                Console.WriteLine("2 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(query3.Count());
                Console.WriteLine("3 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(query6.Count());
                Console.WriteLine("6 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
            }
            bool pseudosparql = false;
            if (pseudosparql)
            {
                var query = BerlinTests.Query6(ts);
                foreach (var pack in query)
                {
                    var row = pack.row;
                    foreach (var val in row)
                    {
                        Console.Write("{0} ", val);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine(query.Count());
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
            }
            bool run6 = false;
            if (run6)
            {
                tt0 = DateTime.Now;
                foreach (string id in ids)
                {
                    var query =
                        ts.GetSubjectByObjPred(id, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature")
                        .Where(_product => ts.ChkOSubjPredObj(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature8"))
                        .Where(_product => ts.ChkOSubjPredObj(_product, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductType1"))
                        .SelectMany(_product => ts.GetDataBySubjPred(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productPropertyNumeric1"))
                        ;
                    int cnt = query.Count();
                    Console.WriteLine(cnt);
                    //if (id == "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19")
                    //foreach (var dd in query)
                    //{
                    //    Console.WriteLine("dd={0}", dd);
                    //}
                }
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            // Контрольный расчет
            {
                string id = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19";
                var query =
                    ts.GetSubjectByObjPred(id, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature")
                    .Where(_product => ts.ChkOSubjPredObj(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature8"))
                    .Where(_product => ts.ChkOSubjPredObj(_product, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductType1"))
                    //.SelectMany(_product => ts.GetDataBySubjPred(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productPropertyNumeric1"))
                    ;
                int cnt = query.Count();
                Console.WriteLine(cnt);
                foreach (var vv in query)
                {
                    Console.WriteLine("r=" + vv);
                }
            }
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

        }
    }
}
