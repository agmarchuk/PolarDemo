using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PObjectives
{
    class ProgramSimpleEngine
    {
        private static XElement db = null;
        public static void Main2(string[] args)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Console.WriteLine("SimpleEngine start");
            sw.Start();
            db = XElement.Load(@"C:\home\FactographDatabases\PolarDemo\perpho.xml");
            sw.Stop();
            Console.WriteLine("Load ok. duration={0}", sw.ElapsedMilliseconds); // 448 мс.
            ProgramSimpleEngine engine = new ProgramSimpleEngine();

            sw.Restart();
            var query = engine.SearchByName("Марчук");
            Console.WriteLine(query.Count());
            sw.Stop();
            Console.WriteLine("Search/Count ok. duration={0}", sw.ElapsedMilliseconds); // 80 мс.
            sw.Restart();
            foreach (XElement item in query)
            {
                Console.WriteLine(item.ToString());
            }
            sw.Stop();
            Console.WriteLine("Results ok. duration={0}", sw.ElapsedMilliseconds); // 41 мс.

            // ag: 2870 gi: 1309
            sw.Restart();
            XElement person = engine.GetItemByIdIn("2870", "person");
            person = engine.GetItemByIdIn("1309", "person");
            sw.Stop();
            if (person != null)
            {
                Console.WriteLine(person.ToString());
                Console.WriteLine("item ok. duration={0}", sw.ElapsedMilliseconds); // 2 мс.
            }

            // Строим генератор
            int n_probes = 1000;
            int one_of = 20;
            string dbname = "test_1000_20";
            string dbmsname = "xml_engine";
            string tracer_name = @"C:\home\FactographDatabases\PolarDemo\tracer.xml";
            //GenerateTests(tracer_name, n_probes, one_of, dbname, dbmsname);
            //return;

            // "Крутим" тесты
            XElement tracer = XElement.Load(tracer_name);
            string trace_name = @"C:\home\FactographDatabases\PolarDemo\trace.txt";
            System.IO.StreamWriter writer = new System.IO.StreamWriter(trace_name, true);
            DateTime tt00 = DateTime.Now;
            foreach (XElement command in tracer.Elements())
            {
                sw.Restart();
                XElement comm = new XElement(command);
                if (command.Name == "search")
                {
                    int cnt = engine.SearchByName(command.Attribute("ss").Value).Count();
                }
                else if (command.Name == "portrait")
                {
                    int cnt = engine.GetItemByIdIn(command.Attribute("id").Value, command.Attribute("type").Value).Elements().Count();
                }
                sw.Stop();
                comm.Add(new XAttribute("duration", sw.ElapsedMilliseconds)); 
                writer.WriteLine(comm.ToString());
            }
            Console.WriteLine("tests ok. duration={0}", (DateTime.Now - tt00).Ticks / 10000L); tt00 = DateTime.Now; //  мс.
        }

        private static void GenerateTests(string tracer_name, int n_probes, int one_of, string dbname, string dbmsname)
        {
            XElement tests = new XElement("tests");
            Random rnd = new Random();
            int n_elements = db.Elements().Count();
            for (int i = 0; i < n_probes; i++)
            {
                int ind = rnd.Next(n_elements - 1);
                XElement element = db.Elements().Take(ind).Last();
                if (element.Name == "person" || element.Name == "photo-doc")
                {
                    if (rnd.Next(one_of - 1) == 0)
                    {
                        string searchsample = element.Element("name").Value;
                        if (searchsample.Length > 6) searchsample = searchsample.Substring(0, 6);
                        tests.Add(new XElement("search", new XAttribute("dbname", dbname), new XAttribute("dbmsname", dbmsname),
                            new XAttribute("ss", searchsample)));
                    }
                    else
                    {
                        tests.Add(new XElement("portrait", new XAttribute("dbname", dbname), new XAttribute("dbmsname", dbmsname),
                            new XAttribute("id", element.Attribute("id").Value), new XAttribute("type", element.Name.LocalName)));
                    }
                }
                tests.Save(tracer_name);
            }
        }
        public IEnumerable<XElement> SearchByName(string searchstring)
        {
            string ss = searchstring.ToLower();
            return db.Elements()
                .Select(el => el.Element("name"))
                .Where(name => name != null && name.Value.ToLower().StartsWith(ss))
                .Select(name => name.Parent);
        }
        public XElement GetItemByIdIn(string id, string type)
        {
            return db.Elements(type).FirstOrDefault(item => item.Attribute("id").Value == id);
        }
    }
}
