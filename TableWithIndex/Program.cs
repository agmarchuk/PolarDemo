using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace TableWithIndex
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";
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
            //XElement db = XElement.Load(@"D:\home\dev2012\tm3.xml");
            XElement db = XElement.Load(@"..\..\..\Databases\0001.xml");

            var query = db.Elements()
                .Where(el => el.Attribute(ONames.rdfabout) != null && el.Element(ONames.tag_name) != null);

            DateTime tt0 = DateTime.Now;

            string variant = "semiindex";
            if (variant == "sql")
            {
                var test = new SQLTest(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\home\FactographDatabases\test20131025.mdf;Integrated Security=True;Connect Timeout=30");
                test.PrepareToLoad();
                Console.WriteLine("PrepareToLoad ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Load(query);
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Index1();
                Console.WriteLine("Index1 ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                
                
                test.SelectById("w20070417_5_8436");
                Console.WriteLine("Первый Select ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) test.SelectById(id);
                Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test.SelectById("pavl_100531115859_6952");
                //Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Count();
            }
            else if (variant == "polartest")
            {
                var test2 = new PolarTest(@"..\..\..\Databases\");
                //test2.Load(query);
                //Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test2.CreateIndex();
                //Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                test2.SelectById("w20070417_5_8436");
                Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) test2.SelectById(id);
                Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            else if (variant == "semiindex")
            {
                Console.WriteLine("SemiIndex Strat. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                SITest sit = new SITest(path);
                sit.Load(query);
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                var rec = sit.GetById("w20070417_5_8436");
                Console.WriteLine("GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //Console.WriteLine(rec.Type.Interpret(rec.Value));
                foreach (string id in ids) sit.GetById(id);
                Console.WriteLine("10 GetById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                sit.Search("марчук");
                Console.WriteLine("Search ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
        }
    }
}
