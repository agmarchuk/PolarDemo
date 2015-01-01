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
    class IndexContext
    {
        public string type, prop, totype;
        public IIndex index;
    }
    class Database2
    {
        // Это будет новый вариант класса Database, отличающийся тем, что он строго ориентирован на спецификацию через шаблонные деревья 
        public static void Main(string[] args)
        {
            Console.WriteLine("ProgramDatabase starts.");
            Database2 db = new Database2("../../../Databases/", XElement.Parse(schema_str));
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            bool toload = false;
            if (toload)
            {
                sw.Restart();
                string data_path = @"C:\home\FactographDatabases\PolarDemo\perpho.xml";
                db.LoadXML(XElement.Load(data_path));
                sw.Stop();
                Console.WriteLine("Load ok. Duration={0}", sw.ElapsedMilliseconds); // 4456 мс.
            }

            sw.Restart();
            var query = db.SearchByNameIn("Марчук", "person");
            Console.WriteLine(query.Count());
            sw.Stop();
            Console.WriteLine("SearchByNameIn ok. Duration={0}", sw.ElapsedMilliseconds); // 346 мс.

            sw.Restart();
            var portr = db.GetPortraitByIdIn(2870, "person");
            if (portr != null) Console.WriteLine(portr.ToString());
            sw.Stop();
            Console.WriteLine("GetPortraitByIdIn ok. Duration={0}", sw.ElapsedMilliseconds); // 4 мс.

            // Проверка портрета
            XElement format = new XElement("record", new XAttribute("type", "person"),
                    new XElement("field", new XAttribute("prop", "name")),
                    new XElement("field", new XAttribute("prop", "from-date")),
                    new XElement("field", new XAttribute("prop", "description")),
                    new XElement("inverse", new XAttribute("prop", "reflected"),
                        new XElement("record", new XAttribute("type", "reflection"),
                            new XElement("field", new XAttribute("prop", "ground")),
                            new XElement("direct", new XAttribute("prop", "in-doc"),
                                new XElement("record", new XAttribute("type", "photo-doc"),
                                    new XElement("field", new XAttribute("prop", "name")))),
                            null)),
                    new XElement("field", new XAttribute("prop", "to-date")),
                    new XElement("field", new XAttribute("prop", "sex")),
                    null);
            sw.Restart();
            XElement portrait = null;
            portrait = db.GetPortraitById(2870, format);
            sw.Stop();
            System.Console.WriteLine("GetPortraitById OK. duration={0}", sw.ElapsedMilliseconds); // 35 ms. 

            sw.Restart();
            portrait = db.GetPortraitById(2870, format);
            sw.Stop();
            //if (portrait != null) Console.WriteLine(portrait.ToString());
            System.Console.WriteLine("GetPortraitById OK. duration={0}", sw.ElapsedMilliseconds); // 4-5 ms. 

            XElement tracer = XElement.Load(@"C:\home\FactographDatabases\PolarDemo\tracer.xml");
            sw.Restart(); int cnt = 0, sum = 0;
            foreach (XElement portra in tracer.Elements("portrait").Where(po => po.Attribute("type").Value == "person"))
            {
                int id = Int32.Parse(portra.Attribute("id").Value);
                XElement por = db.GetPortraitById(id, format);
                cnt++;
                sum += por.Elements("inverse").Count();
            }
            sw.Stop();
            Console.WriteLine("Tracer test ok. Duration={0} cnt={1} sum={2}", sw.ElapsedMilliseconds, cnt, sum); // 3611 мс., 134

            sw.Restart();
            foreach (XElement portra in tracer.Elements("portrait").Where(po => po.Attribute("type").Value == "person"))
            {
                int id = Int32.Parse(portra.Attribute("id").Value);
                db.GetPortraitById(id, format);
            }
            sw.Stop();
            Console.WriteLine("Tracer test ok. Duration={0}", sw.ElapsedMilliseconds); // 68 мс.

        }
        // директория для базы данных
        private string path;
        public string Path { get { return path; } }
        private XElement schema;
        // Словарь коллекций
        internal Dictionary<string, Collection2> collections = new Dictionary<string, Collection2>(); 
        internal List<IndexContext> external_indexes = new List<IndexContext>();
        // Конструктор
        public Database2(string path, XElement schema)
        {
            this.path = path;
            this.schema = schema;
            // Формирования поля ячеек
            foreach (XElement frecord in schema.Elements("record"))
            {
                string ftype = frecord.Attribute("type").Value;
                // Заводим коллекцию
                Collection2 collection = new Collection2(ftype, schema, this);
                collections.Add(ftype, collection);
            }
        }
        public void LoadXML(XElement xdb)
        {
            // очистка коллекций
            foreach (var coll in collections) coll.Value.Clear();
            // Собственно загрузка
            foreach (XElement element in xdb.Elements())
            {
                string type = element.Name.LocalName;
                Collection2 collection = null;
                if (!collections.TryGetValue(type, out collection)) continue;
                string id = element.Attribute("id").Value;
                var pvalue = collection.FRecord.Elements()
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
            // Теперь для каждок коллекции надо сделать Flush()
            foreach (XElement frecord in schema.Elements("record"))
            {
                string ftype = frecord.Attribute("type").Value;
                Collection2 collection = collections[ftype];
                collection.Flush();
            }
        }
        // Поиск по имени
        public IEnumerable<XElement> SearchByNameIn(string searchstring, string type)
        {
            Collection2 collection = collections[type];
            string ss = searchstring.ToLower();
            return collection.Elements()
                .Where(el => ((string)((object[])el.Get())[0]).ToLower().StartsWith(ss))
                .Select(el => new XElement("record", new XAttribute("id", el.Id),
                    new XElement("name", (string)((object[])el.Get())[0])));
        }
        public XElement GetPortraitByIdIn(int key, string type)
        {
            Collection2 collection = collections[type];
            var pelement = collection.Element(key);
            if (pelement == null) return null;
            return new XElement("record", new XElement(type, new XAttribute("id", key), new XAttribute("type", type),
                new XElement("name", (string)((object[])pelement.Get())[0])));
        }

        public XElement GetPortraitById(int key, XElement format)
        {
            string type = format.Attribute("type").Value;
            Collection2 collection = collections[type];
            return GetPortraitById(collection.GetEntryByKey(key), format);
        }
        private XElement GetPortraitById(PaEntry ent, XElement format)
        {
            if (ent.IsEmpty) return null;
            string type = format.Attribute("type").Value;
            Collection2 collection = collections[type];
            object[] three = (object[])ent.Get();
            int key = (int)three[1];
            object[] pvalues = (object[])three[2];
            XElement[] fels = format.Elements().Where(el => el.Name == "field" || el.Name == "direct").ToArray();
            XElement[] schem = collection.FRecord //schema.Elements("record").First(re => re.Attribute("type").Value == type)
                .Elements().Where(el => el.Name == "field" || el.Name == "direct").ToArray();
            // Элементы pvalues по количеству и по сути соответствуют определениям schem
            if (pvalues.Length != schem.Length) throw new Exception("Assert Error 9843");

            XElement result = new XElement("record", new XAttribute("id", key), new XAttribute("type", type));
            var fields_directs = fels.Select(fd =>
            {
                string prop = fd.Attribute("prop").Value;
                int ind = FirstProp(schem, prop);
                XElement sch_el = schem[ind];
                XElement res = null;
                if (sch_el.Name == "field") res = new XElement("field", new XAttribute("prop", prop), pvalues[ind]);
                else if (sch_el.Name == "direct")
                {
                    int forward_key = (int)pvalues[ind];
                    res = new XElement("direct", new XAttribute("prop", prop), GetPortraitById(forward_key, fd.Element("record")));
                }
                return res;
            });
            result.Add(fields_directs);
            XElement[] iels = format.Elements("inverse").ToArray();
            foreach (var inv in format.Elements("inverse"))
            {
                string iprop = inv.Attribute("prop").Value;
                XElement rec = inv.Element("record");
                string itype = rec.Attribute("type").Value;
                var inde = external_indexes.FirstOrDefault(context => context.totype == type && context.prop == iprop && context.type == itype);
                if (inde == null) continue;
                foreach (PaEntry en in ((FlexIndex2<int>)inde.index).GetAllByKey(key))
                {
                    //int ccod = (int)en.Field(1).Get();
                    result.Add(new XElement("inverse", new XAttribute("prop", iprop), GetPortraitById(en, rec)));
                }
            }

            return result;
        }
        private static int FirstProp(XElement[] sch, string prop)
        {
            return sch.Select((fd, ind) => new { fd = fd, ind = ind })
                .First(pair => pair.fd.Attribute("prop").Value == prop)
                .ind;
        }


        private static string schema_str =
@"<schema>
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
</schema>
";
    }
}
