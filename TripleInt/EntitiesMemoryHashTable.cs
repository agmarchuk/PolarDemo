using System;
using System.Linq;
using PolarDB;

namespace TripleIntClasses
{
    public class EntitiesMemoryHashTable
    {
        private EntitiesWideTable entites;

        public EntitiesMemoryHashTable(EntitiesWideTable entites)
        {
            this.entites = entites;
        
        }

        
        public void Test()
        {
            var groupBy = entites.EWTable.Root.Elements()
                .GroupBy(entry => GetHash((int) entry.Field(0).Get()));
            Console.WriteLine("hash-values count = " + groupBy.Count());
            Console.WriteLine("max elements count in hash-value = " + groupBy.Max(entries => entries.Count()));
        }
        Diapason[] diapasons;
        public void Load()
        {
            if (entites.EWTable.IsEmpty) return;
            if (entites.EWTable.Root.Count()==0) return;

            diapasons = new Diapason[ArraySize];
            Diapason diapason = new Diapason() { start = 0, numb = 1 };
            int hashCurrent = GetHash((int)entites.EWTable.Root.Element(0).Field(0).Get());
            foreach (var hashNew in entites.EWTable.Root.ElementValues()
                .Skip(1)
                .Select(v => ((object[])v)[0])
                .Cast<int>()
                .Select(GetHash))
            {
                if (hashNew == hashCurrent) diapason.numb++;
                else
                {
                    diapasons[hashCurrent] = diapason;
                    diapason = new Diapason { start = diapason.start + diapason.numb, numb = 1 };
                    hashCurrent = hashNew;
                }
            }
            diapasons[hashCurrent] = diapason;
        }

        public static int ArraySizeLog = 20, ArraySize = (int)Math.Pow(2, ArraySizeLog), BitsShift=32-ArraySizeLog, ResultIndexShift=ArraySize >> 1;
        
        public int GetHash(int source)
        {
            return (source >> BitsShift) + ResultIndexShift;
        }

        public static long max = 0;
        public static long total = 0;
        public static long count = 0;
        public static long maxRange = 0;
        public static long totalRange = 0;
        public PaEntry GetEntity(int id_code)
        {
            if (entites.EWTable.IsEmpty) return PaEntry.Empty;
            if (id_code == Int32.MaxValue) return PaEntry.Empty;
            var st = DateTime.Now;
            var diapason=diapasons[GetHash(id_code)];
            PaEntry binarySearchFirst = entites.EWTable.Root.BinarySearchFirst(diapason.start, diapason.numb, entry =>((int)(entry.Field(0).Get())).CompareTo(id_code));
            long spent = (DateTime.Now - st).Ticks/10000;
            total += spent;
            count ++;
            if (spent > max) max = spent;
            if (diapason.numb > maxRange) maxRange = diapason.numb;
            totalRange += diapason.numb;
            return binarySearchFirst;
        }
    }
}
