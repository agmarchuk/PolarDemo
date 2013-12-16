using System;
using PolarDB;

namespace SequenceIndex
{  
    class HashIndex<Tkey> where Tkey:IComparable
    {
        private PType offsetsCellType = new PTypeSequence(OffsetPType);
        private PType offsetsOnOffsetsCellType = new PTypeSequence(OffsetPType);
        /// <summary>
        /// максимально достпное число строк с одинаковым хеш.
        /// </summary>
        public int CollisionMax = 3;

        private PaEntry entryOffsetCell, entryTableCell;
        private static PType OffsetPType = new PType(PTypeEnumeration.longinteger);
        private PaCell table;
        private PaCell offsetsOnOffsetsCell, offsetsCell;
        private Func<Tkey, Int32> hashProducer;
        private Func<PaEntry, Tkey> keyProducer;

        public HashIndex(string dirPath, PaCell table, Func<PaEntry, Tkey> keyProducer) :
            this(dirPath, table, keyProducer, o=>o.GetHashCode())
        {}

        public HashIndex(string dirPath, PaCell table, Func<PaEntry, Tkey> keyProducer, Func<Tkey, Int32> hashProducer)
        {
            this.table = table;
            offsetsOnOffsetsCell = new PaCell(offsetsCellType, dirPath + "offsetsOnOffsetsCell.pac", false);
            offsetsCell = new PaCell(offsetsOnOffsetsCellType, dirPath + "offsetsCell.pac", false);
            if (offsetsCell.IsEmpty)
                offsetsCell.Fill(new object[0]);
            this.keyProducer = keyProducer;
            this.hashProducer = hashProducer;
            entryOffsetCell = new PaEntry(OffsetPType, 0, offsetsCell);
            entryTableCell=new PaEntry(((PTypeSequence)table.Type).ElementType, 0, table);
        }



        public void Close() { offsetsCell.Close(); offsetsOnOffsetsCell.Close(); }


        public void Load() // В стандартном случае, задается null
        {

            offsetsCell.Clear();
            if (!offsetsOnOffsetsCell.IsEmpty && offsetsOnOffsetsCell.Root.Count() != (long)Int32.MaxValue + Int32.MaxValue)
                offsetsOnOffsetsCell.Clear();
            if (offsetsOnOffsetsCell.IsEmpty)
            {
                offsetsOnOffsetsCell.Fill(new object[0]);
                for (int i = Int32.MinValue; i < Int32.MaxValue; i++)
                    offsetsOnOffsetsCell.Root.AppendElement(long.MinValue);
                offsetsOnOffsetsCell.Flush();
            }
            else
                for (long i = 0; i < (long) Int32.MaxValue + Int32.MaxValue; i++)
                    offsetsOnOffsetsCell.Root.Element(i).Set(long.MinValue);
            offsetsCell.Fill(new object[0]);
            ArrayIntMax<bool> hashExists=new ArrayIntMax<bool>();
            foreach (var rec in table.Root.Elements()) //.Where(ent => (bool)ent.Field(0).Get() == false) загрузка всех элементов за исключением уничтоженных
            {
                Tkey key = keyProducer(rec);
                Int32 hash = hashProducer(key);
                var offsetOnOffsetEntry = offsetsOnOffsetsCell.Root.Element((long) hash + (long) Int32.MaxValue);
                if (hashExists[hash])
                {
                    entryOffsetCell.offset = (long)offsetOnOffsetEntry.Get() + 8; //пропускаем первый.
                    int i = 1;
                    while ((long) entryOffsetCell.Get() != long.MinValue)
                    {
                        if (++i == CollisionMax)
                            throw new Exception(
                                "Достигнуо максимально допустимое количество ключей с одинаковым хэш-значением");
                        entryOffsetCell.offset += 8;
                    }
                    entryOffsetCell.Set(rec.offset);
                }
                else
                {
                    hashExists[hash] = true;
                    offsetOnOffsetEntry.Set(offsetsCell.Root.AppendElement(rec.offset));
                    for (int i = 1; i < CollisionMax; i++)
                        offsetsCell.Root.AppendElement(long.MinValue);
                }
            }
            offsetsCell.Flush();
        }
        public PaEntry GetFirst(Tkey key)
        {
            Int32 hash = hashProducer(key);
            if (table.Root.Count() == 0) return PaEntry.Empty;
            var offsetOnOffset = (long) offsetsOnOffsetsCell.Root.Element((long) hash + (long) Int32.MaxValue).Get();
            if (offsetOnOffset == long.MinValue) return PaEntry.Empty;
            entryOffsetCell.offset = offsetOnOffset;
            int i = 0;
            long offset =0;

            while ((offset= (long)entryOffsetCell.Get()) != long.MinValue)
            {
                if (++i == CollisionMax) return PaEntry.Empty;
                entryTableCell.offset = offset;
                if (keyProducer(entryTableCell).CompareTo(key)==0) return entryTableCell;
                entryOffsetCell.offset += 8;
            }
            return PaEntry.Empty;
        }
        /*
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
        }*/
    }
}
