using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public class Program
    {
        public static void Main(string[] args)
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

                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature13",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature4",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature8",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature11",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature3",
                "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19",
                //"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19",
                //"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19",
            };

            DateTime tt0 = DateTime.Now;
            Console.WriteLine("Start");
            string path = "../../../Databases/";
            TripleStore ts = new TripleStore(path);
            bool toload = false;
            if (toload)
            {
                //ts.LoadXML(path + "0001.xml");
                //Console.WriteLine("LoadXML ok.");
                PolarDB.PaEntry.bufferBytes = 20000000;
                ts.LoadTurtle(@"D:\home\FactographDatabases\dataset\dataset1m.ttl");
                Console.WriteLine("LoadTurtle ok.");
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
            }

            ts.CreateScale();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            ts.ShowScale();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            bool pseudosparql = true;
            if (pseudosparql)
            {
                //var qu = ts.GetItem("http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19");
                //Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //if (qu != null) Console.WriteLine(qu.ToString());

                string bsbm = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/";
                string bsbm_inst = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/";
                // query 1
                object[] row = new object[3];
                int _produc = 0, _value1 = 1, _label = 2;
                var quer = Enumerable.Repeat<RPack>(new RPack(row, ts), 1)
                    .Spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature19")
                    .spo(_produc, bsbm + "productFeature", bsbm_inst + "ProductFeature8")
                    .spo(_produc, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", bsbm_inst + "ProductType1")
                    .spD(_produc, bsbm + "productPropertyNumeric1", _value1)
                    ;
                //foreach (var vv in quer)
                //{
                //    var a = vv.Get(0);
                //    var b = vv.Get(1);
                //    Console.WriteLine("?product={0} ?value1={1}", a, b); //vv.Get(0), vv.Get(1));
                //}
                Console.WriteLine(quer.Count());
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
