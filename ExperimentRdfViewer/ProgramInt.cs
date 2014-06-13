using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    public class ProgramInt
    {
        static int counter = 0;
        public static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
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
"http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/Product",
            };
            DateTime tt0 = DateTime.Now;

            Console.WriteLine("Start");
            string path = "../../../Databases/";
            //TripleStore<EntityS> ts = new TripleStore<EntityS>(path, new PolarDB.PType(PolarDB.PTypeEnumeration.sstring));
            TripleStoreInt ts = new TripleStoreInt(path);
            Console.WriteLine("InitTripleStore duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
            bool toload = true;
            if (toload)
            {
                //ts.LoadXML(path + "0001.xml");
                //Console.WriteLine("LoadXML ok.");
                PolarDB.PaEntry.bufferBytes = 1000000000; //2*1000*1000*1000;
                //ts.LoadTurtle(@"D:\deployed\dataset_100M.ttl");
                ts.LoadTurtle(@"D:\home\FactographDatabases\dataset\dataset10m.ttl");
                Console.WriteLine("LoadTurtle ok.");
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
            }

            ts.WarmUp();
            Console.WriteLine("WarmUp duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            bool run148q5 = true;
            if (run148q5)
            {
                int cnt = 500;//BerlinTestsInt.sarr.Count();
                long dur;
                DateTime tt00 = DateTime.Now;
                bool secondtest = true;
                if (secondtest)
                {
                    foreach (var sprod in BerlinTestsInt.sarr)
                    {
                        var query = BerlinTestsInt.Query2param(ts, sprod);
                        Console.WriteLine("22222 {0} d={1}", query.Count(), (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                    }
                    dur = (DateTime.Now - tt00).Ticks / 10000L;
                    Console.WriteLine("Total time for {0} queries: {1}. Everage: {2}. QpS: {3}",
                        cnt, dur, (double)dur / (double)cnt, cnt * 1000 / dur);
                    tt00 = DateTime.Now;
                }
                bool fifthtest = false;
                if (fifthtest)
                {
                    foreach (var sprod in Allproducts.Products.Take(2000))
                    {
                        var query = BerlinTestsInt.Query5parameter(ts, sprod);
                        query.Count();
                    }

                    tt00 = DateTime.Now;
                        foreach (var sprod in Allproducts.Products.Skip(2000).Take(cnt))

                    {
                        var query = BerlinTestsInt.Query5parameter(ts, sprod);
                        //var query = BerlinTestsInt.Query2param(ts, sprod);
                    //    Console.WriteLine("55555 {0} d={1}", query.Count(), (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                        query.Count();
                    }
                    dur = (DateTime.Now - tt00).Ticks / 10000L;
                    Console.WriteLine("Total time for {0} queries: {1}. Everage: {2}. QpS: {3}",
                        cnt, dur, (double)dur / (double)cnt, cnt * 1000 / dur);
                    tt00 = DateTime.Now;
                }
                //tt00 = DateTime.Now;
                //foreach (var sprod in BerlinTestsInt.sarr)
                //{
                //    var query = BerlinTestsInt.Query5parameter(ts, sprod);
                //    //var query = BerlinTestsInt.Query2param(ts, sprod);
                //    Console.WriteLine("22222 {0} d={1}", query.Count(), (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //}
                //dur = (DateTime.Now - tt00).Ticks / 10000L;
                //Console.WriteLine("Total time for {0} queries: {1}. Everage: {2}. QpS: {3}",
                //    cnt, dur, (double)dur / (double)cnt, cnt * 1000 / dur);
            }
               
            bool runpseudosoalqltests = false;
            if (runpseudosoalqltests)
            {
                var berlin1 = BerlinTestsInt.Berlin1(ts);
                tt0 = DateTime.Now;
                //var query0 = BerlinTests.Query0(ts);
                var query1 = BerlinTestsInt.Query1(ts);
                var query2 = BerlinTestsInt.Query2(ts);
                var query1_1 = BerlinTestsInt.Query1_1(ts);
                var berlin3 = BerlinTestsInt.Berlin3(ts);
                var query3 = BerlinTestsInt.Query3(ts);
                var query5 = BerlinTestsInt.Query5(ts);
                var berlin6 = BerlinTestsInt.Berlin6(ts);
                var query6 = BerlinTestsInt.Query6(ts);
                tt0 = DateTime.Now;

                //Console.WriteLine(query3.Count());
                //Console.WriteLine("query0 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //return;

                //Console.WriteLine(query1_1.Count());
                //Console.WriteLine("1_1 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

               ts.TestsOfMethods(ids, ts);

                Console.WriteLine(query1.Count());
                Console.WriteLine("1 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //Console.WriteLine(berlin1.Count());
                //Console.WriteLine("Berlin1 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                Console.WriteLine(query2.Count());
                Console.WriteLine("2 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //Console.WriteLine(berlin3.Count());
                //Console.WriteLine("berlin3 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                Console.WriteLine(query3.Count());
                Console.WriteLine("3 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(query5.Count());
                Console.WriteLine("5 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //Console.WriteLine(berlin6.Count());
                //Console.WriteLine("berlin6 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                Console.WriteLine(query6.Count());
                Console.WriteLine("6 duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                //foreach (var rw in query1)
                //{
                //    Console.WriteLine("{0} {1}", rw.row[1], rw.row[2]);
                //}
                //Console.WriteLine();
                //foreach (var ovr in berlin1)
                //{
                //    Console.WriteLine("{0} {1}", ovr.row[7], ovr.row[8]);
                //}
                return;
            }
            bool pseudosparql = false;
            if (pseudosparql)
            {
                var query = BerlinTestsInt.Query3_1(ts);
                int count = 0;
                foreach (var pack in query)
                {
                    count++;
                    var row = pack.row;
                    foreach (var val in row)
                    {
                        Console.Write("{0} ", val);
                    }
                    Console.WriteLine();
                }
                Console.Write("{0} ", count);
                Console.WriteLine();
                Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                return;
            }
            //bool run6 = false;
            //if (run6)
            //{
            //    tt0 = DateTime.Now;
            //    foreach (string id in ids)
            //    {
            //        var query =
            //            ts.GetSubjectByObjPred(id, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature")
            //            .Where(_product => ts.ChkOSubjPredObj(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature8"))
            //            .Where(_product => ts.ChkOSubjPredObj(_product, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductType1"))
            //            .SelectMany(_product => ts.GetDataBySubjPred(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productPropertyNumeric1"))
            //            ;
            //        int cnt = query.Count();
            //        Console.WriteLine(cnt);
            //        //if (id == "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19")
            //        //foreach (var dd in query)
            //        //{
            //        //    Console.WriteLine("dd={0}", dd);
            //        //}
            //    }
            //    Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //}
            // Контрольный расчет
            //{
            //    string id = "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature19";
            //    var query =
            //        ts.GetSubjectByObjPred(id, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature")
            //        .Where(_product => ts.ChkOSubjPredObj(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productFeature", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductFeature8"))
            //        .Where(_product => ts.ChkOSubjPredObj(_product, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/instances/ProductType1"))
            //        //.SelectMany(_product => ts.GetDataBySubjPred(_product, "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/productPropertyNumeric1"))
            //        ;
            //    int cnt = query.Count();
            //    Console.WriteLine(cnt);
            //    foreach (var vv in query)
            //    {
            //        Console.WriteLine("r=" + vv);
            //    }
            //}
            //Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

        }

      

    
    }
}
