using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    class EntitiesMemoryHashTable
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
            diapasons=new Diapason[ArraySize];
            Diapason diapason = new Diapason() { start = 0, numb = 1 };
            int hashCurrent = GetHash((int)entites.EWTable.Root.Element(0).Field(0).Get());
            foreach (var hashNew in entites.EWTable.Root.Elements()
                .Skip(1)
                .Select(e=>e.Field(0).Get())
                .Cast<int>()
                .Select(GetHash))
            {
                if (hashNew == hashCurrent) diapason.numb++;
                else {
                    diapasons[hashCurrent] = diapason;
                    diapason = new Diapason { start = diapason.start + diapason.numb, numb=1 };
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

        public PaEntry GetEntity(int id_code)
        {
            if (entites.EWTable.IsEmpty) return PaEntry.Empty;
            if (id_code == Int32.MaxValue) return PaEntry.Empty;
            var diapason=diapasons[GetHash(id_code)];
            return entites.EWTable.Root.BinarySearchFirst(diapason.start, diapason.numb, entry =>((int)(entry.Field(0).Get())).CompareTo(id_code));
        }
    }
}
