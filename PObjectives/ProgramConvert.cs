using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PObjectives
{
    class ProgramConvert
    {
        public static void Main2(string[] args)
        {
            DateTime tt0 = DateTime.Now;
            Console.WriteLine("ConvertProg start");
            string filename = @"C:\home\FactographDatabases\PolarDemo\0001.xml";
            XElement db = XElement.Load(filename);
            Console.WriteLine("Load ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            //Console.WriteLine(db.Elements().Count()); // 176566

            // Посмотрим сколько имеется классов
            Dictionary<string, int> enames = new Dictionary<string, int>();
            foreach (XElement element in db.Elements())
            {
                string ename = element.Name.NamespaceName + element.Name.LocalName;
                int n = -1;
                if (enames.TryGetValue(ename, out n))
                { // Есть в словаре
                    enames[ename] = n + 1;
                }
                else
                { // нет в словаре
                    enames.Add(ename, 1);
                }
            }
            Console.WriteLine(enames.Count); // 23
            Console.WriteLine("Load ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Посмотрим сколько в классах элементов
            foreach (var pair in enames)
            {
                Console.WriteLine("{0} {1}", pair.Key, pair.Value);
            }

            // Сделаем выборку трех классов (три сосны...) person, photo-doc, reflection. Это почти 100 тыс. объектов, т.е. больше половины от всех.
            string formats_str =
@"<formats>
    <record type='http://fogid.net/o/person'>
        <field prop='http://fogid.net/o/name'/>
        <field prop='http://fogid.net/o/from-date'/>
        <field prop='http://fogid.net/o/description'/>
        <field prop='http://fogid.net/o/to-date'/>
        <field prop='http://fogid.net/o/sex'/>
    </record>
    <record type='http://fogid.net/o/photo-doc'>
        <field prop='http://fogid.net/o/name'/>
        <field prop='http://fogid.net/o/from-date'/>
        <field prop='http://fogid.net/o/description'/>
    </record>
    <record type='http://fogid.net/o/reflection'>
        <field prop='http://fogid.net/o/ground'/>
        <direct prop='http://fogid.net/o/reflected'/>
        <direct prop='http://fogid.net/o/in-doc'/>
    </record>
</formats>";
            XElement formats = XElement.Parse(formats_str);

            XElement db1 = new XElement("db");
            // Поехали...
            foreach (XElement element in db.Elements())
            {
                string ename = element.Name.NamespaceName + element.Name.LocalName;
                XElement format = formats.Elements().FirstOrDefault(e => e.Attribute("type").Value == ename);
                if (format == null) continue;
                string id = element.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about").Value;
                int c = CodeId(id);
                XElement el = new XElement(element.Name.LocalName, new XAttribute("id", c));
                bool wrong_element = false;
                foreach (XElement fel in format.Elements())
                {
                    string prop = fel.Attribute("prop").Value;
                    string pro = prop.Substring(prop.LastIndexOf('/') + 1);
                    XElement subel = new XElement(pro);
                    if (fel.Name == "field")
                    {
                        XElement f = element.Elements().FirstOrDefault(e => e.Name.NamespaceName + e.Name.LocalName == prop);
                        if (f != null) subel.Add(f.Value);
                    }
                    else if (fel.Name == "direct")
                    {
                        XElement d = element.Elements().FirstOrDefault(e => e.Name.NamespaceName + e.Name.LocalName == prop);
                        if (d != null)
                        {
                            XAttribute r = d.Attribute("{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource");
                            if (r != null)
                            {
                                subel.Add(new XAttribute("ref", CodeId(r.Value)));
                            }
                            else { wrong_element = true; continue; }
                        }
                        else { wrong_element = true; continue; }
                    }
                    el.Add(subel);
                }
                if (!wrong_element) db1.Add(el);
            }
            db1.Save(@"C:\home\FactographDatabases\PolarDemo\perpho.xml");
            foreach (XElement ee in db1.Elements("reflection").Take(100))
            {
                Console.WriteLine(ee.ToString());
            }
 
        }

        private static  Dictionary<string, int> ids = new Dictionary<string, int>();
        private static int cod = 0;
        private static int CodeId(string id)
        {
            int c = -1;
            if (!ids.TryGetValue(id, out c))
            {
                c = cod;
                cod++;
                ids.Add(id, c);
            }
            return c;
        }
    }
}
