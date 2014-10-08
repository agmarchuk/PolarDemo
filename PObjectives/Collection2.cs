using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;

namespace PObjectives
{
    public class Collection2
    {
        private string collectionname;
        public string Name { get { return collectionname; } }
        private XElement frecord;
        public XElement FRecord { get { return frecord; } }
        private PType eType;
        public PType CollectionElementType { get { return eType; } }
        private Database2 inDatabase;
        internal Database2 InDatabase { get { return inDatabase; } }
        private PType eeType; // extended element type
        private PaCell cell;
        private FlexIndex2<int> key_index;
        private List<IIndex> indexes = new List<IIndex>();
        //private int keyNew = 0;
        //private int counter = -1;
        internal Collection2(string cname, XElement schema, Database2 inDatabase)
        {
            collectionname = cname;
            this.inDatabase = inDatabase;
            this.frecord = schema.Elements("record").First(re => re.Attribute("type").Value == cname);
            string ftype = cname;
            // Это будет коллекция записей, сначала выявим тип записи для коллекции
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
            this.eType = new PTypeRecord(nt_arr);
            
            this.eeType = new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("key", new PType(PTypeEnumeration.integer)),
                new NamedType("element", eType));
            string path = inDatabase.Path;
            cell = new PaCell(new PTypeSequence(eeType), path + cname + ".pac", false);
            if (cell.IsEmpty) cell.Fill(new object[0]);
            key_index = new FlexIndex2<int>(path + cname + "_id_i", cell.Root, en => (int)en.Field(1).Get(), null);
            // Другие индексы
            foreach (var pair in frecord.Elements().Select((el, ind) => new {el=el, ind=ind}))
            {
                if (pair.el.Name != "direct") continue;
                string column = pair.el.Attribute("prop").Value;
                string totype = pair.el.Element("record").Attribute("type").Value;
                string name_combination = ftype + "(" + column + ")" + totype;
                FlexIndex2<int> index = new FlexIndex2<int>(path + name_combination + ".pac", cell.Root,
                    (PaEntry ent) => (int)ent.Field(2).Field(pair.ind).Get(), null);
                // Запишем в местный список
                indexes.Add(index);
                // Запишем в общий список
                inDatabase.external_indexes.Add(new IndexContext() { type = ftype, prop = column, totype = totype, index = index });

            }
        }
        public void Clear()
        {
            cell.Clear();
            cell.Fill(new object[0]);
            key_index.Load();
            foreach (var index in indexes) index.Load();
        }
        // Быстрое добавление элемента при загрузке. В конце серии, требуется Flush(). 
        public void AppendElement(int key, object pvalue)
        {
            var off = cell.Root.AppendElement(new object[] { false, key, pvalue });
        }
        public void Flush()
        {
            cell.Flush();
            // Кроме сброса размера в ячейку, произведем вычисление индекса
            key_index.Load();
            foreach (var index in indexes) index.Load();
        }
        // "Медленное" добавление элемента, Flush() не требуется
        public PaEntry AddElement(int key, object pvalue)
        {
            var off = cell.Root.AppendElement(new object[] { false, key, pvalue });
            cell.Flush();
            PaEntry entry = new PaEntry(eeType, off, cell);
            key_index.AddEntry(entry);
            foreach (var index in indexes) index.AddEntry(entry);
            return entry;
        }
        //// Создание нового элемента - (пока) отменено в силу нерешенности вопроса о генерации новых ключей
        //public Element CreateElement(object pvalue)
        //{
        //    var off = cell.Root.AppendElement(new object[] { false, keyNew, pvalue });
        //    cell.Flush();
        //    PaEntry entry = new PaEntry(eeType, off, cell);
        //    key_index.AppendEntry(entry);
        //    keyNew++;
        //    return new Element() { entry = entry, inCollection = this };
        //}
        public void UpdateElement(Element element, object pvalue)
        {
            bool deleted = (bool)element.entry.Field(0).Get();
            if (deleted) throw new Exception("deleted element can't be updated");
            int key = (int)element.entry.Field(1).Get();
            element.entry.Field(0).Set(true);
            PaEntry entry = AddElement(key, pvalue);
            element.entry = entry;
        }
        public IEnumerable<Element2> Elements()
        {
            return cell.Root.Elements()
                .Where(en => !(bool)en.Field(0).Get())
                .Select(en => new Element2() { entry = en, inCollection = this });
        }
        public IEnumerable<object> ElementValues()
        {
            return cell.Root.Elements()
                .Select<PaEntry, object[]>(en => (object[])en.Get())
                .Where(ev => !(bool)ev[0])
                .Select(ev => ev[2]);
        }
        public Element2 Element(int key)
        {
            PaEntry entry = GetEntryByKey(key);
            return new Element2() { entry = entry, inCollection = this };
        }
        public TElement Element<TElement>(int key) where TElement : Element2, new()
        {
            Element2 el = this.Element(key);
            var res = new TElement() { inCollection = el.inCollection, entry = el.entry };
            return res;
        }
        private Dictionary<int, long> key_entry_dic = new Dictionary<int, long>();
        internal PaEntry GetEntryByKey(int key)
        {
            if (cell.Root.Count() == 0) return PaEntry.Empty;
            long off;
            if (key_entry_dic.TryGetValue(key, out off))
            {
                PaEntry en = cell.Root.Element(0);
                en.offset = off;
                return en;
            }
            PaEntry ent = key_index.GetFirstByKey(key);
            key_entry_dic.Add(key, ent.offset);
            return ent;
        }
    }
}
