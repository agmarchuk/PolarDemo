using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using PolarBasedEngine;

namespace PolarBasedTest
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Start");
            string path = @"D:\home\dev2012\PolarDemo\Databases\";
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
            XElement formats = XElement.Load(path + "ApplicationProfile.xml").Element("formats");
            XElement format = formats.Elements("record").First(r => r.Attribute("type").Value == "http://fogid.net/o/person");
            bool toload = false;
            XElement db = null;
            IEnumerable<XElement> query = Enumerable.Empty<XElement>();

            DateTime tt0 = DateTime.Now;

            var graph = new PolarBasedRdfGraph(path);
            Console.WriteLine("graph ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            toload = true;
            if (toload)
            {
                db = XElement.Load(path + "0001.xml");
                query = db.Elements()
                    .Where(el => el.Attribute(ONames.rdfabout) != null);
                graph.StartFillDb();
                // Загрузка элементами или потоками элементов
                graph.Load(query);
                graph.FinishFillDb();
                Console.WriteLine("Load ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            var xFlow = graph.SearchByName("марчук");
            foreach (XElement x in xFlow)
            {
                //Console.WriteLine(x.ToString());
            }
            Console.WriteLine("SearchByName ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            var xres = graph.GetItemByIdBasic("w20070417_5_8436", true); // Это я
            //var xres = graph.GetItemByIdBasic("piu_200809051508", true); // Это Г.И.
            //var xres = graph.GetItemByIdBasic("w20071030_1_20927", true); // Это Андрей
            Console.WriteLine("GetItemByIdBasic ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            foreach (string id in ids) graph.GetItemByIdBasic(id, true);
            Console.WriteLine("10 GetItemByIdBasic ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            xres = graph.GetItemById("w20070417_5_8436", format);
            //xres = engine.GetItemById("piu_200809051508", format);
            //if (xres != null) Console.WriteLine(xres.ToString());
            Console.WriteLine("GetItemById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            foreach (string id in ids) graph.GetItemById(id, format);
            Console.WriteLine("10 GetItemById ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Редактирование
            XElement record1 = new XElement(ONames.tag_person, new XAttribute(ONames.rdfabout, "testzone_0001"),
                new XElement(ONames.tag_name, "Пупкин Василий Васильевич"),
                new XElement(ONames.tag_fromdate, "2013-11-22"));
            graph.InsertXElement(record1);
            Console.WriteLine("InsertXElement ok. Duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            xFlow = graph.SearchByName("пупкин");
            foreach (XElement x in xFlow)
            {
                Console.WriteLine(x.ToString());
            }
            Console.WriteLine("SearchByName ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            xres = graph.GetItemByIdBasic("testzone_0001", true); // Это Пупкин
            if (xres != null) Console.WriteLine(xres.ToString());

            graph.Delete("testzone_0001");
            xFlow = graph.SearchByName("пупкин");
            foreach (XElement x in xFlow)
            {
                Console.WriteLine(x.ToString());
            }
            Console.WriteLine("Delete ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            xres = graph.GetItemByIdBasic("testzone_0001", true); // Это Пупкин
            if (xres != null) Console.WriteLine(xres.ToString());

        }
    }
}
