using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using PolarBasedEngine;

namespace PObjectives
{
    class ProgramInterpretator
    {
        public static void Main5(string[] args)
        {
            Console.WriteLine("Interpretator starts.");
            string path = "../../../Databases/";
            string data_path = @"C:\home\FactographDatabases\PolarDemo\perpho.xml";
            ProgramInterpretator pi = new ProgramInterpretator(path, XElement.Parse(formats_str));

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            // Загрузка
            bool toload = true;
            if (toload)
            {
                sw.Restart();
                pi.LoadXML(XElement.Load(data_path));
                sw.Stop();
                System.Console.WriteLine("LoadXML OK. duration={0}", sw.ElapsedMilliseconds);
            }

            // Проверка поиска по имени
            sw.Restart();
            foreach (var xrecord in pi.SearchByNameIn("Марчук", "person"))
            {
                Console.WriteLine(xrecord.ToString());
            }
            sw.Stop();
            System.Console.WriteLine("SearchByNameIn OK. duration={0}", sw.ElapsedMilliseconds); // 367 мс.
            // Проверка портрета
            sw.Restart();
            XElement element = pi.GetPortraitByIdIn(2870, "person");
            if (element != null) Console.WriteLine(element.ToString());
            sw.Stop();
            System.Console.WriteLine("GetPortraitByIdIn OK. duration={0}", sw.ElapsedMilliseconds); // 14 мс., 4 после смены GetFirstByKey в FlexIndex

            // Проверка портрета
            sw.Restart();
            XElement portr = null;
            portr = pi.GetPortraitById(2870,
                new XElement("record", new XAttribute("type", "person"),
                    new XElement("field", new XAttribute("prop", "name")),
                    new XElement("field", new XAttribute("prop", "from-date")),
                    new XElement("field", new XAttribute("prop", "description")),
                    new XElement("inverse", new XAttribute("prop", "reflected"),
                        new XElement("record", new XAttribute("type", "reflection"),
                            new XElement("field", new XAttribute("prop", "ground")),
                            null)),
                    new XElement("field", new XAttribute("prop", "to-date")),
                    new XElement("field", new XAttribute("prop", "sex")),
                    null));
            if (portr != null) Console.WriteLine(portr.ToString());
            sw.Stop();
            System.Console.WriteLine("GetPortraitById OK. duration={0}", sw.ElapsedMilliseconds); // 4(?)ms. 10118 ticks




        }
        private string path;
        private XElement fschema;
        private Database db;
        public ProgramInterpretator(string path, XElement fschema)
        {
            this.path = path;
            this.fschema = fschema;
            db = new Database(path);
            // Здесь либо есть коллекции, либо их нет
            if (db.NamedCollections().Count() == 0) BuildCollectionStructure();
        }
        private void BuildCollectionStructure()
        {
            foreach (XElement frecord in fschema.Elements())
            {
                NamedType[] nt_arr = frecord.Elements()
                    .Where(el => el.Name == "field" || el.Name == "direct")
                    .Select(el =>
                    {
                        PType tpe = null;
                        if (el.Name == "direct")
                        {
                            tpe = new PType(PTypeEnumeration.integer);
                        }
                        else if (el.Name == "field")
                        {
                            string el_type = el.Attribute("datatype").Value;
                            if (el_type == "string") tpe = new PType(PTypeEnumeration.sstring);
                            else if (el_type == "int") tpe = new PType(PTypeEnumeration.integer);
                        }
                        return new NamedType(el.Attribute("prop").Value, tpe);
                    })
                    .ToArray();
                PType tp = new PTypeRecord(nt_arr);
                string type = frecord.Attribute("type").Value;
                db.CreateCollection(type, tp);
                // Заведем индексы
                foreach (XElement el in frecord.Elements("direct"))
                { // есть type и есть:
                    string column = el.Attribute("prop").Value;
                    string totype = el.Element("record").Attribute("type").Value;
                    string name_combination = type + "(" + column + ")" + totype;
                    db.CreateCollection(name_combination, new PType(PTypeEnumeration.longinteger));
                }
            }
        }

        public void LoadXML(XElement xdb)
        {
            // Будем чистить и заполнять по-таблично
            foreach (var pair in db.NamedCollections()) 
            {
                string collection_name = pair.Key;
                Collection collection = pair.Value;
                collection.Clear();
                XElement frecord = fschema.Elements("record").First(f => f.Attribute("type").Value == collection_name);
                // Цикл по загружаемым элементам
                foreach (XElement element in xdb.Elements(collection_name))
                {
                    string id = element.Attribute("id").Value;
                    var pvalue = frecord.Elements()
                        .Where(el => el.Name == "field" || el.Name == "direct")
                        .Select(el =>
                        {
                            XElement sub = element.Element(el.Attribute("prop").Value);
                            object res = null;
                            if (el.Name == "direct")
                            {
                                res = Int32.Parse(sub.Attribute("ref").Value);
                            } 
                            else
                            {
                                //TODO: Надо разобрать по типам
                                res = sub.Value;
                            }
                            return res;
                        }).ToArray();
                    collection.AppendElement(Int32.Parse(id), pvalue);
                }
                collection.Flush(); // Здесь будет и загрузка индекса
            }
        }

        public IEnumerable<XElement> SearchByNameIn(string searchstring, string type)
        {
            Collection collection = db.Collection(type);
            string ss = searchstring.ToLower();
            return collection.Elements()
                .Where(el => ((string)((object[])el.Get())[0]).ToLower().StartsWith(ss))
                .Select(el => new XElement("record", new XAttribute("id", el.Id),
                    new XElement("name", (string)((object[])el.Get())[0])));
        }
        public XElement GetPortraitByIdIn(int key, string type)
        {
            Collection collection = db.Collection(type);
            var pelement = collection.Element(key);
            if (pelement == null) return null;
            return new XElement("record", new XElement(type, new XAttribute("id", key), new XAttribute("type", type),
                new XElement("name", (string)((object[])pelement.Get())[0])));
        }
        private static int FirstProp(XElement[] sch, string prop)
        {
            return sch.Select((fd, ind) => new { fd=fd, ind=ind })
                .First(pair => pair.fd.Attribute("prop").Value == prop)
                .ind;
        }
        public XElement GetPortraitById(int key, XElement format)
        {
            string type = format.Attribute("type").Value;
            Collection collection = db.Collection(type);
            var element = collection.Element(key);
            if (element == null) return null;
            object[] pvalues = (object[])element.Get();
            XElement[] fels = format.Elements().Where(el => el.Name == "field" || el.Name == "direct").ToArray();
            XElement[] schem = fschema.Elements("record").First(re => re.Attribute("type").Value == type)
                .Elements().Where(el => el.Name == "field" || el.Name == "direct").ToArray();
            // Элементы pvalues по количеству и по сути соответствуют определениям schem
            if (pvalues.Length != schem.Length) throw new Exception("Assert Error 9843");
            var query = fels.Select(fd =>
            {
                string prop = fd.Attribute("prop").Value;
                int ind = FirstProp(schem, prop);
                XElement sch_el = schem[ind];
                XElement result = null;
                if (sch_el.Name == "field") result = new XElement("field", new XAttribute("prop", prop), pvalues[ind]);
                return result;
            });

            return new XElement("record", new XAttribute("id", key), new XAttribute("type", type),
                query,
                null);
        }


        private static string formats_str =
@"<formats>
  <record type='person'>
    <field prop='name' datatype='string' />
    <field prop='from-date' datatype='string' />
    <field prop='description' datatype='string' />
    <field prop='to-date' datatype='string' />
    <field prop='sex' datatype='string' />
  </record>
  <record type='photo-doc'>
    <field prop='name' datatype='string' />
    <field prop='from-date' datatype='string' />
    <field prop='description' datatype='string' />
  </record>
  <record type='reflection'>
    <field prop='ground' datatype='string' />
    <direct prop='reflected'><record type='person' /> </direct>
    <direct prop='in-doc'><record type='photo-doc' /> </direct>
  </record>
</formats>
";
    }
}
