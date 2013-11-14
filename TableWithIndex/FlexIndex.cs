using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableWithIndex
{
    public class FlexIndex
    {
        private PaEntry table;
        private PaCell index_cell;
        public FlexIndex(string indexName, PaEntry table)
        {
            this.table = table;
            index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + ".pac", false);
        }
        public void Load<Tkey>(Func<PaEntry, Tkey> keyProducer)
        {
            index_cell.Clear();
            index_cell.Fill(new object[0]);
            foreach (var rec in table.Elements().Where(ent => (bool)ent.Field(0).Get() == false))
            {
                long offset = rec.offset;
                index_cell.Root.AppendElement(offset);
            }
            index_cell.Flush();
            if (index_cell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            // Попробую сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
            var ptr = table.Element(0);
            index_cell.Root.SortByKey<Tkey>((object v) =>
            {
                ptr.offset = (long)v;
                return keyProducer(ptr);
            });
        }
        public PaEntry GetFirst(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return new PaEntry(null, Int64.MinValue, null);
            PaEntry entry = table.Element(0);
            PaEntry entry_in_index = index_cell.Root.BinarySearchFirst(ent =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            });
            if (entry_in_index.offset == Int64.MinValue) return entry_in_index; // не найден
            entry.offset = (long)entry_in_index.Get();
            return entry;
        }
        // Использование GetFirst:
        //var qu = iset_index.GetFirst(ent =>
        //{
        //    int v = (int)ent.Get();
        //    return v.CompareTo(sample);
        //});

        public PaEntry GetFirst(object sample)
        {
            PaEntry found = GetFirst(ent =>
            {
                IComparable v = (IComparable)ent.Get();
                return v.CompareTo(sample);
            });
            //if (found.offset == Int64.MinValue) return found; // Транслируем сообщение о ненахождении
            return found;
        }
        public IEnumerable<PaEntry> GetAll(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() > 0)
            {
                PaEntry entry = table.Element(0);
                Diapason dia = index_cell.Root.BinarySearchDiapason((PaEntry ent) =>
                {
                    long off = (long)ent.Get();
                    entry.offset = off;
                    return elementDepth(entry);
                });
                var query = index_cell.Root.Elements(dia.start, dia.numb)
                    .Select(ent =>
                    {
                        entry.offset = (long)ent.Get();
                        return entry;
                    });
                return query;
            }
            else return Enumerable.Empty<PaEntry>();
        }
        public IEnumerable<PaEntry> GetAll()
        {
            if (table.Count() > 0)
            {
                PaEntry entry = table.Element(0);
                var query = index_cell.Root.Elements()
                    .Select(ent =>
                    {
                        entry.offset = (long)ent.Get();
                        return entry;
                    });
                return query;
            }
            else return Enumerable.Empty<PaEntry>();
        }
        //public IEnumerable<PaEntry> GetAll(object sample)
        //{
        //    return GetAll(ent =>
        //    {
        //        IComparable v = (IComparable)ent.Get();
        //        return v.CompareTo(sample);
        //    });
        //}
    }
}
