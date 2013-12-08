using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolarDB;

namespace PolarBasedRDF
{
    public class FlexIndex<TKey>: IBRW, IComparable where TKey:IComparable
    {
        private PaEntry table;
        private readonly PaCell indexCell;
        private readonly PaCell indexCellSmall;
        //private long nloaded; // число записей, обработанных методом Load 
        private readonly Func<PaEntry, TKey> keyProducer;
        
        private PaEntry ptr;
        private TKey currentKey;

        public FlexIndex(string indexName, PaEntry table, Func<PaEntry, TKey> keyProducer)
        {
            this.table = table;
            this.keyProducer = keyProducer;
            indexCell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + ".pac", false);
            if (indexCell.IsEmpty) indexCell.Fill(new object[0]);
            indexCellSmall = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + "_s.pac", false);
            if (indexCellSmall.IsEmpty) indexCellSmall.Fill(new object[0]);
          
        }
        public void Close() { indexCell.Close(); indexCellSmall.Close(); }
        //public void Load<Tkey>(Func<PaEntry, Tkey> keyProducer)
        public void Load()
        {
            // Маленький массв будет после загрузки пустым
            indexCellSmall.Clear();
            indexCellSmall.Fill(new object[0]); indexCellSmall.Flush();
            indexCell.Clear();
            indexCell.Fill(new object[0]);
            foreach (var rec in table.Elements().Where(ent => !(bool)ent.Field(0).Get())) // загрузка всех элементов за исключением уничтоженных
            {
                var offset = rec.offset;
                indexCell.Root.AppendElement(offset);
            }
            indexCell.Flush();
            if (indexCell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            // Сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
 
            indexCell.Root.Sort<FlexIndex<TKey>>();
        }
        public void AppendEntry(PaEntry ent)
        {
            var offset = ent.offset;
            indexCellSmall.Root.AppendElement(offset);
            indexCellSmall.Flush();
            indexCell.Root.Sort<FlexIndex<TKey>>();
        }
        public PaEntry GetFirstByKey(TKey key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            var ent = GetFirstFromByKey(indexCellSmall, key); // сначала из маленького массива
            return !ent.IsEmpty ? ent : GetFirstFromByKey(indexCell, key);
        }
        public PaEntry GetFirst(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            var ent = GetFirstFrom(indexCellSmall, elementDepth); // сначала из маленького массива
            return !ent.IsEmpty ? ent : GetFirstFrom(indexCell, elementDepth);
        }
        // Использование GetFirst:
        //var qu = iset_index.GetFirst(ent =>
        //{
        //    int v = (int)ent.Get();
        //    return v.CompareTo(sample);
        //});

        private PaEntry GetFirstFromByKey(PaCell iCell, TKey key)
        {
            var entry = table.Element(0);
            var entry2 = table.Element(0); // сделан, потому что может entry во внешнем и внутренниц циклах проинтерферируют?
            return iCell.Root.BinarySearchAll(ent =>
            {
                var off1 = (long)ent.Get();
                entry.offset = off1;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            }) // здесь мы имеем множество найденных входов в ячейку i_cell
                .Select(ent =>
                {
                    entry2.offset = (long)ent.Get(); // вход в запись таблицы
                    return entry2;
                }) // множество входов, удовлетворяющих условиям
                .Where(tEnt => !(bool)tEnt.Field(0).Get()) // остаются только неуничтоженные
                .DefaultIfEmpty(PaEntry.Empty) // а вдруг не останется ни одного, тогда - пустышка
                .First();
        }
        private PaEntry GetFirstFrom(PaCell iCell, Func<PaEntry, int> elementDepth)
        {
            var entry = table.Element(0);
            var entry2 = table.Element(0); // сделан, потому что может entry во внешнем и внутренниц циклах проинтерферируют?
            return iCell.Root.BinarySearchAll(ent =>
            {
                var off1 = (long)ent.Get();
                entry.offset = off1;
                return elementDepth(entry);
            }) // здесь мы имеем множество найденных входов в ячейку i_cell
                .Select(ent => 
                {
                    entry2.offset = (long)ent.Get(); // вход в запись таблицы
                    return entry2;
                }) // множество входов, удовлетворяющих условиям
                .Where(tEnt => !(bool)tEnt.Field(0).Get()) // остаются только неуничтоженные
                .DefaultIfEmpty(PaEntry.Empty) // а вдруг не останется ни одного, тогда - пустышка
                .First();
        }

        public IEnumerable<PaEntry> GetAllByKey(TKey key)
        {
            return table.Count() == 0
                ? Enumerable.Empty<PaEntry>()
                : GetAllFromByKey(indexCell, key)
                    .Concat(GetAllFromByKey(indexCellSmall, key));
        }

        // Возвращает множество входов в записи опорной таблицы, удовлетворяющие elementDepth == 0
        public IEnumerable<PaEntry> GetAll(Func<PaEntry, int> elementDepth)
        {
            return table.Count() == 0 ? Enumerable.Empty<PaEntry>() : 
                GetAllFrom(indexCell, elementDepth).Concat(GetAllFrom(indexCellSmall, elementDepth));
        }

        private IEnumerable<PaEntry> GetAllFromByKey(PaCell cell, TKey key)
        {
            var entry = table.Element(0);
            var dia = cell.Root.BinarySearchDiapason(ent =>
            {
                var off = (long)ent.Get();
                entry.offset = off;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            });
            var query = cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long) ent.Get();
                    return entry;
                })
                .Where(tEnt => !(bool)tEnt.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
        private IEnumerable<PaEntry> GetAllFrom(PaCell cell, Func<PaEntry, int> elementDepth)
        {
            var entry = table.Element(0);
            var dia = cell.Root.BinarySearchDiapason(ent =>
            {
                var off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            });
            return cell.Root.Elements(dia.start, dia.numb)
                .Select(ent  =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(tEnt => !(bool)tEnt.Field(0).Get());
        }
        public IEnumerable<PaEntry> GetAll()
        {
            return table.Count() == 0 ? Enumerable.Empty<PaEntry>() :
                GetAllFrom(indexCell).Concat(GetAllFrom(indexCellSmall));
        }

        private IEnumerable<PaEntry> GetAllFrom(PaCell cell)
        {
            var entry = table.Element(0);
            var query = cell.Root.Elements()
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(tEnt => !(bool)tEnt.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }

        public IBRW BRead(BinaryReader br)
        {
            ptr.offset = br.ReadInt64(); 
            currentKey = keyProducer(ptr);
            return this;
        }

        public void BWrite(BinaryWriter bw)
        {
            bw.Write(ptr.offset);
        }

        public int CompareTo(object obj)
        {
           var ptr1 = table.Element(0);
           ptr1.offset = (long)obj;
           return keyProducer(ptr1).CompareTo(currentKey);
        }
    }
}
