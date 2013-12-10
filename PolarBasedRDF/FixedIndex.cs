using System;
using System.Collections.Generic;
using System.Linq;

using PolarDB;

namespace PolarBasedRDF
{
    public class FixedIndex<Tkey>
    {
        // Использовать надо следующим образом:
        public class SubjPred :IComparable
        { 
            public string subj, pred;
            public int CompareTo(object sp)
            {
                int cmp = subj.CompareTo(((SubjPred)sp).subj);
                if (cmp != 0) return cmp;
                return pred.CompareTo(((SubjPred)sp).pred);
            }
        }
        public class SubjPredComparer : IComparer<SubjPred>
        {
            public int Compare(SubjPred x, SubjPred y)
            {
                return x.CompareTo(y);
            }
        }
        public static void Test()
        {
            PType objectTriplets = new PTypeSequence(new PTypeRecord(
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)),
                new NamedType("s", new PType(PTypeEnumeration.sstring)),
                new NamedType("p", new PType(PTypeEnumeration.sstring)),
                new NamedType("o", new PType(PTypeEnumeration.sstring))
            ));
            // инициализация таблицы
            var directCell = new PaCell(objectTriplets, "path", false); 
            // Более компактный способ заполнения ячейки
            directCell.Clear();
            directCell.Fill(new object[0]);
            //foreach (var element in elements) // Закомментарил из-за отсутствия перечислителя elements
            {
                directCell.Root.AppendElement(new object[] { false, "subject", "predicate", "object" });
            }
            directCell.Flush();
            // Создание индекса
            FixedIndex<SubjPred> sp_index = new FixedIndex<SubjPred>("..sp", directCell.Root, entry =>
                {
                    return new SubjPred() { subj = (string)entry.Field(1).Get(), pred = (string)entry.Field(1).Get() };
                });
        }

        private PaEntry table;
        private PaCell index_cell;
        private Func<PaEntry, Tkey> keyProducer;
        public FixedIndex(string indexName, PaEntry table, Func<PaEntry, Tkey> keyProducer)
        {
            this.table = table;
            this.keyProducer = keyProducer;
            index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + ".pac", false);
            if (index_cell.IsEmpty) index_cell.Fill(new object[0]);
        }
        public void Close() { index_cell.Close(); }
        public void Load(IComparer<Tkey> comparer) // В стандартном случае, задается null
        {
            index_cell.Clear();
            index_cell.Fill(new object[0]);
            foreach (var rec in table.Elements()) //.Where(ent => (bool)ent.Field(0).Get() == false) загрузка всех элементов за исключением уничтоженных
            {
                long offset = rec.offset;
                index_cell.Root.AppendElement(offset);
            }
            index_cell.Flush();
            if (index_cell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            // Сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
            var ptr = table.Element(0);
            //index_cell.Root.SortByKey<Tkey>((object v) =>
            index_cell.Root.SortByKey<Tkey>((object v) =>
            {
                ptr.offset = (long)v;
                return keyProducer(ptr);
            }, comparer);
        }
        public PaEntry GetFirstByKey(Tkey key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            return GetFirstFromByKey(index_cell, key);
        }
        public PaEntry GetFirst(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            return GetFirstFrom(index_cell, elementDepth);
        }

        private PaEntry GetFirstFromByKey(PaCell i_cell, Tkey key)
        {
            PaEntry entry = table.Element(0);
            PaEntry entry2 = table.Element(0); // сделан, потому что может entry во внешнем и внутренниц циклах проинтерферируют?
            return i_cell.Root.BinarySearchAll(ent =>
            {
                long off1 = (long)ent.Get();
                entry.offset = off1;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            }) // здесь мы имеем множество найденных входов в ячейку i_cell
                .Select(ent =>
                {
                    entry2.offset = (long)ent.Get(); // вход в запись таблицы
                    return entry2;
                }) // множество входов, удовлетворяющих условиям
                //  .Where(t_ent => !(bool)t_ent.Field(0).Get()) // остаются только неуничтоженные
                .DefaultIfEmpty(PaEntry.Empty) // а вдруг не останется ни одного, тогда - пустышка
                .First();
        }
        private PaEntry GetFirstFrom(PaCell i_cell, Func<PaEntry, int> elementDepth)
        {
            PaEntry entry = table.Element(0);
            PaEntry entry2 = table.Element(0); // сделан, потому что может entry во внешнем и внутренниц циклах проинтерферируют?
            var candidate = i_cell.Root.BinarySearchAll(ent =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            }) // здесь мы имеем множество найденных входов в ячейку i_cell
            .Select(ent =>
            {
                entry2.offset = (long)ent.Get(); // вход в запись таблицы
                return entry2;
            }) // множество входов, удовлетворяющих условиям
            .Where(t_ent => !(bool)t_ent.Field(0).Get()) // остаются только неуничтоженные
            .DefaultIfEmpty(PaEntry.Empty) // а вдруг не останется ни одного, тогда - пустышка
            .First(); // обязательно есть хотя бы пустышка
            return candidate;
        }

        public IEnumerable<PaEntry> GetAllByKey(Tkey key)
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            var ents = GetAllFromByKey(index_cell, key);
            return ents;
        }
        // Возвращает множество входов в записи опорной таблицы, удовлетворяющие elementDepth == 0
        public IEnumerable<PaEntry> GetAll(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            return GetAllFrom(index_cell, elementDepth);
        }
        private IEnumerable<PaEntry> GetAllFromByKey(PaCell cell, Tkey key)
        {
            PaEntry entry = table.Element(0);
            Diapason dia = cell.Root.BinarySearchDiapason((PaEntry ent) =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            });
            var query = cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                });
                //.Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
        private IEnumerable<PaEntry> GetAllFrom(PaCell cell, Func<PaEntry, int> elementDepth)
        {
            PaEntry entry = table.Element(0);
            Diapason dia = cell.Root.BinarySearchDiapason((PaEntry ent) =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            });
            var query = cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
        public IEnumerable<PaEntry> GetAll()
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            return GetAllFrom(index_cell);
        }
        private IEnumerable<PaEntry> GetAllFrom(PaCell cell)
        {
            PaEntry entry = table.Element(0);
            var query = cell.Root.Elements()
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
    }
}
