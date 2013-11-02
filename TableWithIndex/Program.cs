using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TableWithIndex
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
            //XElement db = XElement.Load(@"..\..\..\Databases\0001.xml");
            XElement db = XElement.Load(@"D:\home\dev2012\tm.xml");
            DateTime tt0 = DateTime.Now;

            Console.WriteLine("Start");
            bool sql = true;
            if (sql)
            {
                var test = new SQLTest(@"Data Source=(LocalDB)\v11.0;AttachDbFilename=D:\home\FactographDatabases\test20131025.mdf;Integrated Security=True;Connect Timeout=30");
                test.PrepareToLoad();
                Console.WriteLine("PrepareToLoad ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Load(db);
                Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Index1();
                Console.WriteLine("Index1 ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test.Index2();
                //Console.WriteLine("Index2 ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.SelectById("w20070417_5_8436");
                Console.WriteLine("Первый Select ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) test.SelectById(id);
                Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test.SelectById("pavl_100531115859_6952");
                //Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                test.Count();
            }
            else
            {
                var test2 = new PolarTest(@"..\..\..\Databases\");
                //test2.Load(db);
                //Console.WriteLine("Load ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                //test2.CreateIndex();
                //Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                test2.SelectById("w20070417_5_8436");
                Console.WriteLine("Index ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                foreach (string id in ids) test2.SelectById(id);
                Console.WriteLine("SelectById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
        }
    }
}
