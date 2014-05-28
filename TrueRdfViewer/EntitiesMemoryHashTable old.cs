using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TrueRdfViewer
{
    class EntitiesMemoryHashTable
    {                        
        private readonly EntitiesWideTable entites; 
        public EntitiesMemoryHashTable(EntitiesWideTable entites)
        {
            this.entites = entites;
        }

        public static int ArraySizeLog = 20, ArraySizeL = (int) Math.Pow(2, ArraySizeLog);
        public Diapason[] Diapasons=new Diapason[ArraySizeL];
        public void Test()
        {
            var groupBy = entites.EWTable.Root.Elements()
                .GroupBy(entry => GetHash((int) entry.Field(0).Get()));
            Console.WriteLine("hash-values count = " + groupBy.Count());
            Console.WriteLine("max elements count in hash-value = " + groupBy.Max(entries => entries.Count()));
        }

        public void Load()
        {
            Diapason current=new Diapason(){start = 0, numb = 1};
            int hashCurrent = GetHash((int) entites.EWTable.Root.Element(0).Field(0).Get());
            foreach (int hashNew in 
                entites.EWTable.Root.Elements()
                    .Skip(1)
                    .Select(entry => entry.Field(0).Get())
                    .Cast<int>()
                    .Select(GetHash))
                if (hashNew == hashCurrent) current.numb++;
                else
                {
                    Diapasons[hashCurrent] = current;
                    current = new Diapason() {start = current.start + current.numb, numb = 1};
                    hashCurrent = hashNew;
                }
            Diapasons[hashCurrent] = current;
        }

        private readonly int helpConstArraySize = ArraySizeL >> 1;
        private readonly int helperConstShift=32-ArraySizeLog;
        public int GetHash(int source)
        {
            return (source >> helperConstShift) + helpConstArraySize;
        }

        public PaEntry GetEntity(int id_code)
        {
            if (id_code == Int32.MaxValue) return PaEntry.Empty;
            var diapason = Diapasons[GetHash(id_code)];
            if(diapason.numb==0) return PaEntry.Empty;
           return entites.EWTable.Root.BinarySearchFirst(diapason.start, diapason.numb,
                entry => ((int)entry.Field(0).Get()).CompareTo(id_code));
        }

        }
    }
   
