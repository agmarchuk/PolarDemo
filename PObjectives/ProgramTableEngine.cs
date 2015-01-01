using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace PObjectives
{
    public class ProgramTableEngine
    {
        public static void Main4(string[] args)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            string pth = "../../../Databases/";
            Console.WriteLine("TableEngine start");
            ProgramTableEngine engine = new ProgramTableEngine(pth);
            
            // sw.Restart();
            //engine.LoadXML(@"C:\home\FactographDatabases\PolarDemo\perpho.xml");
            //sw.Stop(); 
            //Console.WriteLine("Load ok. duration={0}", sw.ElapsedMilliseconds);


            sw.Restart();
            var qu = engine.SearchByNameIn("Марчук", "person");
            Console.WriteLine(qu.Count());
            sw.Stop();
            Console.WriteLine("Search ok. duration={0}", sw.ElapsedMilliseconds);
            sw.Restart();
            foreach (XElement found in qu)
            {
                Console.WriteLine(found.ToString());
            }
            sw.Stop();
            Console.WriteLine("Search ok. duration={0}", sw.ElapsedMilliseconds); 
            
            // ag: 2870 gi:1309
            sw.Restart();
            XElement item; // = engine.GetPortraitByIdIn(1309, "person");
            item = engine.GetPortraitByIdIn(2870, "person");
            sw.Stop();
            Console.WriteLine(item.ToString());
            Console.WriteLine("Дерево размера: {0}", item.Elements().Count()); //item.DescendantsAndSelf().Count());
            Console.WriteLine("Item ok. duration={0}", sw.ElapsedMilliseconds);
            // Размер данных
            Console.WriteLine("Размер данных: persons {0}, photos {1}, reflections {2}",
                engine.index_persons_id.GetAll().Count(),
                engine.index_photo_docs_id.GetAll().Count(),
                engine.cell_reflections.Root.Count());

        }
        private string path;
        private PType tp_persons, tp_photo_docs, tp_reflections;
        private PaCell cell_persons, cell_photo_docs, cell_reflections;
        private IndexView<int> index_persons_id, index_photo_docs_id;
        private IndexView<int> index_reflection_reflected, index_reflection_in_doc;
        private IndexView<string> index_persons_name, index_photo_docs_name;

        private ProgramTableEngine(string path)
        {
            this.path = path;
            ConstructTypes();
            cell_persons = new PaCell(tp_persons, path + "persons.pac", false);
            if (cell_persons.IsEmpty) cell_persons.Fill(new object[0]); 
            cell_photo_docs = new PaCell(tp_photo_docs, path + "photo-docs.pac", false);
            if (cell_photo_docs.IsEmpty) cell_photo_docs.Fill(new object[0]);
            cell_reflections = new PaCell(tp_reflections, path + "reflections.pac", false);
            if (cell_reflections.IsEmpty) cell_reflections.Fill(new object[0]);
            if (true)
            {
                OpenIndexes();
            }
        }
        private void OpenIndexes()
        {
            index_persons_id = new IndexView<int>(path + "index_persons_id", cell_persons.Root,
                ent => (int)ent.Field(0).Get());
            index_photo_docs_id = new IndexView<int>(path + "index_photo_docs_id", cell_photo_docs.Root,
                ent => (int)ent.Field(0).Get());
            index_persons_name = new IndexView<string>(path + "index_persons_name", cell_persons.Root,
                ent => (string)ent.Field(1).Get());
            index_photo_docs_name = new IndexView<string>(path + "index_photo_docs_name", cell_photo_docs.Root,
                ent => (string)ent.Field(1).Get());

            index_reflection_reflected = new IndexView<int>(path + "index_reflection_reflected", cell_reflections.Root,
                ent => (int)ent.Field(2).Get());
            index_reflection_in_doc = new IndexView<int>(path + "index_reflection_in_doc", cell_reflections.Root,
                ent => (int)ent.Field(3).Get());
        }
        private void LoadIndexes()
        {
            index_persons_id.Load(null);
            index_persons_name.Load(null);
            index_photo_docs_id.Load(null);
            index_photo_docs_name.Load(null);
            index_reflection_reflected.Load(null);
            index_reflection_in_doc.Load(null);
        }
        private void CloseIndexes()
        {
            index_persons_id.Close();
            index_persons_name.Close();
            index_photo_docs_id.Close();
            index_photo_docs_name.Close();
            index_reflection_reflected.Close();
            index_reflection_in_doc.Close();
        }

        private void LoadXML(string xmldb_name)
        {
            cell_persons.Clear(); cell_persons.Fill(new object[0]);
            cell_photo_docs.Clear(); cell_photo_docs.Fill(new object[0]);
            cell_reflections.Clear(); cell_reflections.Fill(new object[0]);

            XElement db = XElement.Load(xmldb_name);
            foreach (XElement element in db.Elements())
            {
                int id = Int32.Parse(element.Attribute("id").Value);
                if (element.Name == "person")
                {
                    var name_el = element.Element("name");
                    string name = name_el == null ? "" : name_el.Value;
                    var sex_el = element.Element("sex");
                    string sex = sex_el == null ? "" : sex_el.Value;
                    var fd_el = element.Element("from-date");
                    string fd = fd_el == null ? "" : fd_el.Value;
                    var td_el = element.Element("to-date");
                    string td = td_el == null ? "" : td_el.Value;
                    var des_el = element.Element("description");
                    string des = des_el == null ? "" : des_el.Value;
                    cell_persons.Root.AppendElement(new object[] { id, name, sex, fd, td, des });
                }
                else if (element.Name == "photo-doc")
                {
                    var name_el = element.Element("name");
                    string name = name_el == null ? "" : name_el.Value;
                    var fd_el = element.Element("from-date");
                    string fd = fd_el == null ? "" : fd_el.Value;
                    var des_el = element.Element("description");
                    string des = des_el == null ? "" : des_el.Value;
                    cell_photo_docs.Root.AppendElement(new object[] { id, name, fd, des });
                }
                else if (element.Name == "reflection")
                {
                    var ground_el = element.Element("ground");
                    string ground = ground_el == null ? "" : ground_el.Value;
                    if (element.Element("reflected") == null || element.Element("in-doc") == null) continue; 
                    int reflected = Int32.Parse(element.Element("reflected").Attribute("ref").Value);
                    int in_doc = Int32.Parse(element.Element("in-doc").Attribute("ref").Value);
                    cell_reflections.Root.AppendElement(new object[] { id, ground, reflected, in_doc });
                }
            }
            cell_persons.Flush();
            cell_photo_docs.Flush();
            cell_reflections.Flush();

            LoadIndexes();
        }

        public IEnumerable<XElement> SearchByNameIn(string searchstring, string type)
        {
            if (cell_persons.Root.Count() == 0) return Enumerable.Empty<XElement>();
            string ss = searchstring.ToLower();
            if (type == "person")
            {
                var qu = index_persons_name.GetAll(ent =>
                {
                    string s = ((string)ent.Field(1).Get()).ToLower();
                    if (s.StartsWith(ss)) return 0;
                    return s.CompareTo(ss);
                });
                return qu.Select(ent =>
                {
                    object[] rec = (object[])ent.Get();
                    int id = (int)rec[0];
                    string name = (string)rec[1];
                    return new XElement("record", new XAttribute("id", id), new XAttribute("type", "person"),
                        new XElement("field", new XAttribute("prop", "name"), name));
                });
            }
            else return Enumerable.Empty<XElement>();
        }
        public XElement GetPortraitByIdIn(int id, string type)
        {
            PaEntry entry = PaEntry.Empty;
            if (type == "person")
            {
                entry = index_persons_id.GetFirstByKey0(id);
            }
            else if (type == "photo-doc")
            {
                entry = index_photo_docs_id.GetFirstByKey0(id);
            }
            if (entry.IsEmpty) return null;
            object[] val = (object[])entry.Get();
            var res = new XElement("record", new XAttribute("id", id), new XAttribute("type", type),
                new XElement("field", new XAttribute("prop", "name"), (string)val[1]),
                null);
            if (type == "person")
            {
                foreach (PaEntry ent in index_reflection_reflected.GetAllByKey(id))
                {
                    object[] reflect_obj = (object[])ent.Get();
                    int cod = (int)reflect_obj[3];
                    XElement port = GetPortraitByIdIn(cod, "photo-doc");
                    res.Add(new XElement("inverse", new XAttribute("prop", "reflected"),
                        new XElement("record", new XAttribute("id", reflect_obj[0]),
                            new XElement("field", new XAttribute("prop", "ground"), reflect_obj[1]),
                            new XElement("direct", new XAttribute("prop", "in-doc"), port),
                            null)));
                }
            }
            return res;
        }

        private void ConstructTypes()
        {
            tp_persons = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("sex", new PType(PTypeEnumeration.sstring)),
                new NamedType("from-date", new PType(PTypeEnumeration.sstring)),
                new NamedType("to-date", new PType(PTypeEnumeration.sstring)),
                new NamedType("description", new PType(PTypeEnumeration.sstring))));
            tp_photo_docs = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("from-date", new PType(PTypeEnumeration.sstring)),
                new NamedType("description", new PType(PTypeEnumeration.sstring))));
            tp_reflections = new PTypeSequence(new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.integer)),
                new NamedType("ground", new PType(PTypeEnumeration.sstring)),
                new NamedType("reflected", new PType(PTypeEnumeration.integer)),
                new NamedType("in-doc", new PType(PTypeEnumeration.integer))));
        }
    }
}
