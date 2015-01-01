using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarBasedEngine;
using PolarDB;

namespace PObjectives
{
    public class Collection
    {
        private string collectionname;
        public string Name { get { return collectionname; } }
        private PType eType;
        public PType CollectionElementType { get { return eType; } }
        private Database inDatabase;
        public Database InDatabase { get { return inDatabase; } }
        private PType eeType; // extended element type
        private PaCell cell;
        private FlexIndex<int> key_index;
        private int keyNew = 0;
        private int counter = -1;
        public Collection(string cname, PType eType, Database inDatabase)
        {
            this.eType = eType;
            collectionname = cname;
            this.inDatabase = inDatabase;
            this.eeType = new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("key", new PType(PTypeEnumeration.integer)),
                new NamedType("element", eType));
            string path = inDatabase.Path;
            cell = new PaCell(new PTypeSequence(eeType), path + cname + ".pac", false);
            if (cell.IsEmpty) cell.Fill(new object[0]); 
            key_index = new FlexIndex<int>(path + cname + "_id_i", cell.Root, en => (int)en.Field(1).Get());
            keyNew = (int)cell.Root.Count(); // Похоже, это есть "разогрев". Только это неправильный способ получения нового ключа
        }
        public void Clear()
        {
            cell.Clear();
            cell.Fill(new object[0]);
            key_index.Load(null);
        }
        public void AppendElement(int key, object pvalue)
        {
            var off = cell.Root.AppendElement(new object[] { false, key, pvalue });
            //key_index.AppendEntry(new PaEntry(eeType, off, cell));
        }
        public void Flush()
        {
            cell.Flush();
            // Кроме сброса размера в ячейку, произведем вычисление индекса
            key_index.Load(null);
        }
        public Element CreateElement(object pvalue)
        {
            var off = cell.Root.AppendElement(new object[] {false, keyNew, pvalue});
            cell.Flush();
            PaEntry entry = new PaEntry(eeType, off, cell);
            key_index.AppendEntry(entry);
            keyNew++;
            return new Element() { entry = entry, inCollection = this };
        }
        public void UpdateElement(Element element, object pvalue)
        {
            bool deleted = (bool)element.entry.Field(0).Get();
            if (deleted) throw new Exception("deleted element can't be updated");
            int key = (int)element.entry.Field(1).Get();
            element.entry.Field(0).Set(true);
            var off = cell.Root.AppendElement(new object[] { false, key, pvalue });
            cell.Flush();
            PaEntry entry = new PaEntry(eeType, off, cell);
            key_index.AppendEntry(entry);
            element.entry = entry;
        }
        public IEnumerable<Element> Elements()
        {
            return cell.Root.Elements()
                .Where(en => !(bool)en.Field(0).Get())
                .Select(en => new Element() { entry = en, inCollection = this });
        }
        public IEnumerable<object> ElementValues()
        {
            return cell.Root.Elements()
                .Select<PaEntry, object[]>(en => (object[])en.Get())
                .Where(ev => !(bool)ev[0])
                .Select(ev => ev[2]);
        }
        public Element Element(int key) 
        {
            PaEntry entry = key_index.GetFirstByKey(key);
            return new Element() { entry = entry, inCollection = this };
        }
        public TElement Element<TElement>(int key) where TElement : Element, new()
        {
            Element el = this.Element(key);
            var res = new TElement() { inCollection = el.inCollection, entry = el.entry };
            return res;
        }
    }
}
