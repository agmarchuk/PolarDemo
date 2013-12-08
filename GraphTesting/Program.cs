using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace GraphTesting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"..\..\..\Databases\";

            Graph gr;
            gr = new Graph(path);
            
            // Следующая строчка закомментаривается для проверки работы базы данных с уже имеющейся загрузкой
            gr.Load(new string[] { path + "0001.xml" });
            
            //XElement formats = XElement.Load(path + "ApplicationProfile.xml").Element("formats");

            DateTime tt0 = DateTime.Now;

            gr.TestSearch("Белинский");
            Console.WriteLine("Search done. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //return;

            XElement format = new XElement("record",
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/participant"),
                    new XElement("record", new XAttribute("type", "http://fogid.net/o/participation"),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/from-date")),
                        new XElement("field", new XAttribute("prop", "http://fogid.net/o/to-date")),
                        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-org"),
                            new XElement("record",
                                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))),
                null);
            string id = "w20070417_5_8436";
            //var item = engine.GetItemByIdBasic(id, true);
            var item1 = gr.GetItemById(id, format);
            if (item1 != null) Console.WriteLine(item1.ToString());


            string[] probes = new string[] {
                    "piu_200809051791",
                    "svet_100616111408_10844",
                    "pavl_100531115859_2020",
                    "pavl_100531115859_6952",
                    "svet_100616111408_10864",
                    "w20090506_svetlana_5727",
                    "piu_200809051742",
                };
            foreach (string idd in probes)
            {
                var item = gr.GetItemById(idd, format);
            }

            Console.WriteLine((probes.Length + 1) + " tests done. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
